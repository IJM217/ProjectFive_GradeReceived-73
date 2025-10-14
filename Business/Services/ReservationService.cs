//MSTISR001


// Summary: Provides reservation management, pricing logic, and availability calculations.

#region Using Directives
using PhumlaKamnandiHotelSystem.Business.Models;
using PhumlaKamnandiHotelSystem.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
#endregion

namespace PhumlaKamnandiHotelSystem.Business.Services
{
    public class ReservationService
    {
        #region Dependencies

        private readonly ReservationDataGateway reservationGateway;
        private readonly RoomDataGateway roomGateway;
        private readonly GuestService guestService;

        #endregion

        #region Pricing Constants

        private const decimal StandardAdultRate = 750m;
        private const decimal SingleOccupancyMultiplier = 1.5m;
        private const decimal OlderChildRate = 375m;
        private const decimal LowSeasonRoomRate = 550m;
        private const decimal MidSeasonRoomRate = 750m;
        private const decimal HighSeasonRoomRate = 995m;

        #endregion

        #region Constructors

        public ReservationService(
            ReservationDataGateway reservationGateway,
            RoomDataGateway roomGateway,
            GuestService guestService)
        {
            this.reservationGateway = reservationGateway ?? throw new ArgumentNullException(nameof(reservationGateway));
            this.roomGateway = roomGateway ?? throw new ArgumentNullException(nameof(roomGateway));
            this.guestService = guestService ?? throw new ArgumentNullException(nameof(guestService));
        }

        #endregion

        #region Reservation Queries

        public async Task<IList<ReservationRecord>> GetReservationsAsync()
        {
            var reservations = await reservationGateway.GetReservationsAsync();
            return await EnrichReservations(reservations);
        }

        public async Task<ReservationRecord?> GetReservationAsync(int reservationId)
        {
            var reservation = await reservationGateway.GetReservationByIdAsync(reservationId);
            if (reservation == null)
            {
                return null;
            }

            return (await EnrichReservations(new[] { reservation })).FirstOrDefault();
        }

        public Task<IList<RoomInfo>> GetAllRoomsAsync()
        {
            return roomGateway.GetRoomsAsync();
        }

        public async Task<IList<RoomInfo>> GetAvailableRoomsAsync(DateTime checkIn, DateTime checkOut, int? excludeReservationId = null)
        {
            ValidateDates(checkIn, checkOut);
            var allRooms = await roomGateway.GetRoomsAsync();
            var overlappingReservations = await reservationGateway.GetReservationsInRangeAsync(checkIn, checkOut);

            var occupiedRoomIds = new HashSet<int>(overlappingReservations
                .Where(r => !excludeReservationId.HasValue || r.ReservationId != excludeReservationId.Value)
                .Select(r => r.RoomNumber));

            return allRooms.Where(room => !occupiedRoomIds.Contains(room.RoomNumber)).ToList();
        }

        #endregion

        #region Reservation Commands

        public async Task<ReservationRecord> CreateReservationAsync(
            GuestProfile guest,
            DateTime checkIn,
            DateTime checkOut,
            int adults,
            int childrenUnderFive,
            int childrenFiveToSixteen)
        {
            var quote = GetPricingQuote(checkIn, checkOut, adults, childrenUnderFive, childrenFiveToSixteen);
            var room = await AllocateRoomAsync(checkIn, checkOut);
            if (room == null)
            {
                throw new InvalidOperationException("No rooms are available for the selected dates.");
            }

            await guestService.SaveGuestAsync(guest);

            var reservation = new ReservationRecord
            {
                GuestId = guest.GuestId,
                RoomNumber = room.RoomNumber,
                CheckIn = checkIn,
                CheckOut = checkOut,
                TotalCost = quote.Total,
                GuestName = guest.FullName,
                Adults = adults,
                ChildrenUnderFive = childrenUnderFive,
                ChildrenFiveToSixteen = childrenFiveToSixteen,
                IsInSeason = quote.UsesPerPersonPricing,
                IsSingleOccupancy = DetermineSingleOccupancy(adults, childrenUnderFive, childrenFiveToSixteen),
                SeasonDescription = quote.SeasonDescription,
                DepositAmount = quote.Deposit,
                DepositPaid = true
            };

            reservation.ReservationId = await reservationGateway.InsertReservationAsync(reservation);
            reservation.Status = DetermineStatus(reservation);
            return (await EnrichReservations(new[] { reservation })).First();
        }

        public async Task<ReservationRecord> UpdateReservationAsync(
            ReservationRecord reservation,
            GuestProfile guest,
            int adults,
            int childrenUnderFive,
            int childrenFiveToSixteen)
        {
            if (reservation == null)
            {
                throw new ArgumentNullException(nameof(reservation));
            }

            var quote = GetPricingQuote(reservation.CheckIn, reservation.CheckOut, adults, childrenUnderFive, childrenFiveToSixteen);

            await guestService.SaveGuestAsync(guest);
            reservation.GuestId = guest.GuestId;
            reservation.GuestName = guest.FullName;
            reservation.Adults = adults;
            reservation.ChildrenUnderFive = childrenUnderFive;
            reservation.ChildrenFiveToSixteen = childrenFiveToSixteen;
            reservation.IsInSeason = quote.UsesPerPersonPricing;
            reservation.IsSingleOccupancy = DetermineSingleOccupancy(adults, childrenUnderFive, childrenFiveToSixteen);
            reservation.TotalCost = quote.Total;
            reservation.SeasonDescription = quote.SeasonDescription;
            reservation.DepositAmount = quote.Deposit;
            reservation.DepositPaid = true;

            var availableRooms = await GetAvailableRoomsAsync(reservation.CheckIn, reservation.CheckOut, reservation.ReservationId);
            if (!availableRooms.Any(r => r.RoomNumber == reservation.RoomNumber))
            {
                var reassigned = availableRooms.OrderBy(r => r.RoomNumber).FirstOrDefault();
                if (reassigned == null)
                {
                    throw new InvalidOperationException("No rooms are available for the updated dates.");
                }

                reservation.RoomNumber = reassigned.RoomNumber;
            }

            await reservationGateway.UpdateReservationAsync(reservation);
            reservation.Status = DetermineStatus(reservation);
            return (await EnrichReservations(new[] { reservation })).First();
        }

        public Task CancelReservationAsync(int reservationId)
        {
            return reservationGateway.DeleteReservationAsync(reservationId);
        }

        #endregion

        #region Pricing and Allocation

        public decimal CalculateDeposit(decimal totalCost)
        {
            return Math.Round(totalCost * 0.10m, 2, MidpointRounding.AwayFromZero);
        }

        public PricingQuote GetPricingQuote(
            DateTime checkIn,
            DateTime checkOut,
            int adults,
            int childrenUnderFive,
            int childrenFiveToSixteen)
        {
            ValidateDates(checkIn, checkOut);
            EnsureOccupancyValid(adults, childrenUnderFive, childrenFiveToSixteen);

            var schedule = BuildNightSchedule(checkIn, checkOut);
            if (schedule.Count == 0)
            {
                throw new ArgumentException("At least one night must be included in the stay.");
            }

            var standardNightlyRate = CalculateStandardNightlyRate(adults, childrenUnderFive, childrenFiveToSixteen);

            decimal total = 0m;
            foreach (var night in schedule)
            {
                total += night.Season switch
                {
                    SeasonCategory.Low => LowSeasonRoomRate,
                    SeasonCategory.Mid => MidSeasonRoomRate,
                    SeasonCategory.High => HighSeasonRoomRate,
                    _ => standardNightlyRate
                };
            }

            total = Math.Round(total, 2, MidpointRounding.AwayFromZero);

            var seasonGroups = schedule
                .GroupBy(n => n.Season)
                .OrderBy(g => g.Min(n => n.Date))
                .ToList();

            var primarySeason = seasonGroups
                .OrderByDescending(g => g.Count())
                .ThenBy(g => g.Min(n => n.Date))
                .Select(g => g.Key)
                .First();

            return new PricingQuote
            {
                Total = total,
                Deposit = CalculateDeposit(total),
                DepositDueDate = checkIn.Date.AddDays(-14),
                UsesPerPersonPricing = seasonGroups.All(g => g.Key == SeasonCategory.Standard),
                IsMixedSeason = seasonGroups.Count > 1,
                PrimarySeason = primarySeason,
                SeasonDescription = BuildSeasonDescription(schedule),
                Nights = schedule.Count
            };
        }

        public async Task<RoomInfo?> AllocateRoomAsync(DateTime checkIn, DateTime checkOut, int? excludeReservationId = null)
        {
            var available = await GetAvailableRoomsAsync(checkIn, checkOut, excludeReservationId);
            return available.OrderBy(r => r.RoomNumber).FirstOrDefault();
        }

        #endregion

        #region Calculation Helpers

        private static List<(DateTime Date, SeasonCategory Season)> BuildNightSchedule(DateTime checkIn, DateTime checkOut)
        {
            var nights = new List<(DateTime Date, SeasonCategory Season)>();
            for (var date = checkIn.Date; date < checkOut.Date; date = date.AddDays(1))
            {
                nights.Add((date, DetermineSeason(date)));
            }

            return nights;
        }

        private static SeasonCategory DetermineSeason(DateTime date)
        {
            if (date.Year == 2025 && date.Month == 12)
            {
                if (date.Day <= 7)
                {
                    return SeasonCategory.Low;
                }

                if (date.Day <= 15)
                {
                    return SeasonCategory.Mid;
                }

                return SeasonCategory.High;
            }

            return SeasonCategory.Standard;
        }

        private static decimal CalculateStandardNightlyRate(int adults, int childrenUnderFive, int childrenFiveToSixteen)
        {
            if (DetermineSingleOccupancy(adults, childrenUnderFive, childrenFiveToSixteen))
            {
                return Math.Round(StandardAdultRate * SingleOccupancyMultiplier, 2, MidpointRounding.AwayFromZero);
            }

            var nightlyTotal = (adults * StandardAdultRate) + (childrenFiveToSixteen * OlderChildRate);
            return Math.Round(nightlyTotal, 2, MidpointRounding.AwayFromZero);
        }

        private static string BuildSeasonDescription(IReadOnlyList<(DateTime Date, SeasonCategory Season)> nights)
        {
            if (nights.Count == 0)
            {
                return "No season information";
            }

            var groups = nights
                .GroupBy(n => n.Season)
                .OrderBy(g => g.Min(n => n.Date))
                .ToList();

            if (groups.Count == 1)
            {
                var group = groups[0];
                var start = group.Min(n => n.Date);
                var endExclusive = group.Max(n => n.Date).AddDays(1);
                var range = $"{start:dd MMM yyyy} - {endExclusive:dd MMM yyyy}";
                return $"{GetSeasonDisplayName(group.Key)} ({range})";
            }

            var names = string.Join(", ", groups.Select(g => GetSeasonDisplayName(g.Key)));
            return $"Mixed Season: {names}";
        }

        private static string GetSeasonDisplayName(SeasonCategory season)
        {
            return season switch
            {
                SeasonCategory.Low => "Low Season",
                SeasonCategory.Mid => "Mid Season",
                SeasonCategory.High => "High Season",
                _ => "Standard Rates"
            };
        }

        #endregion

        #region Validation Helpers

        private static void ValidateDates(DateTime checkIn, DateTime checkOut)
        {
            if (checkOut.Date <= checkIn.Date)
            {
                throw new ArgumentException("Check-out date must be after the check-in date.");
            }
        }

        private static void EnsureOccupancyValid(int adults, int childrenUnderFive, int childrenFiveToSixteen)
        {
            if (adults < 0 || childrenUnderFive < 0 || childrenFiveToSixteen < 0)
            {
                throw new ArgumentException("Guest counts cannot be negative.");
            }

            var totalGuests = adults + childrenUnderFive + childrenFiveToSixteen;
            if (totalGuests == 0)
            {
                throw new ArgumentException("At least one guest is required for a reservation.");
            }

            if (totalGuests > 4)
            {
                throw new ArgumentException("No more than four guests may occupy a room.");
            }
        }

        private static bool DetermineSingleOccupancy(int adults, int childrenUnderFive, int childrenFiveToSixteen)
        {
            var totalGuests = adults + childrenUnderFive + childrenFiveToSixteen;
            return adults == 1 && totalGuests == 1;
        }

        private static ReservationStatus DetermineStatus(ReservationRecord reservation)
        {
            return reservation.DepositPaid ? ReservationStatus.Confirmed : ReservationStatus.Unconfirmed;
        }

        #endregion

        #region Internal Utilities

        private async Task<IList<ReservationRecord>> EnrichReservations(IEnumerable<ReservationRecord> reservations)
        {
            var rooms = await roomGateway.GetRoomsAsync();
            var roomLookup = rooms.ToDictionary(r => r.RoomNumber, r => r);
            var result = new List<ReservationRecord>();

            foreach (var reservation in reservations)
            {
                if (roomLookup.TryGetValue(reservation.RoomNumber, out var room))
                {
                    reservation.RoomType = room.IsOccupied ? "Occupied" : "Available";
                }

                var quote = GetPricingQuote(
                    reservation.CheckIn,
                    reservation.CheckOut,
                    reservation.Adults,
                    reservation.ChildrenUnderFive,
                    reservation.ChildrenFiveToSixteen);

                reservation.DepositAmount = quote.Deposit;
                reservation.SeasonDescription = quote.SeasonDescription;
                reservation.IsInSeason = quote.UsesPerPersonPricing;
                reservation.Status = DetermineStatus(reservation);
                result.Add(reservation);
            }

            return result;
        }

        #endregion
    }
}
