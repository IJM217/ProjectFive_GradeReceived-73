//MSTISR001


// Summary: Generates occupancy and revenue reports from reservation data.

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
    public class ReportService
    {
        #region Dependencies

        private readonly ReservationDataGateway reservationGateway;
        private readonly RoomDataGateway roomGateway;

        #endregion

        #region Constructors

        public ReportService(ReservationDataGateway reservationGateway, RoomDataGateway roomGateway)
        {
            this.reservationGateway = reservationGateway ?? throw new ArgumentNullException(nameof(reservationGateway));
            this.roomGateway = roomGateway ?? throw new ArgumentNullException(nameof(roomGateway));
        }

        #endregion

        #region Report Generation

        public async Task<IList<OccupancySnapshot>> GetOccupancyReportAsync(DateTime startDate, DateTime endDate)
        {
            if (endDate.Date < startDate.Date)
            {
                throw new ArgumentException("End date must be on or after the start date.");
            }

            var rooms = await roomGateway.GetRoomsAsync();
            var reservations = await reservationGateway.GetReservationsInRangeAsync(startDate, endDate);
            var totalRooms = rooms.Count;

            var results = new List<OccupancySnapshot>();
            for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
            {
                var occupied = reservations.Count(r => r.CheckIn.Date <= date && r.CheckOut.Date > date);
                var occupancyPercentage = totalRooms == 0 ? 0 : Math.Round((decimal)occupied / totalRooms, 4);
                results.Add(new OccupancySnapshot
                {
                    Date = date,
                    OccupiedRooms = occupied,
                    TotalRooms = totalRooms,
                    OccupancyPercentage = occupancyPercentage
                });
            }

            return results;
        }

        public async Task<IList<RevenueSummary>> GetRevenueReportAsync(DateTime startDate, DateTime endDate)
        {
            if (endDate.Date < startDate.Date)
            {
                throw new ArgumentException("End date must be on or after the start date.");
            }

            var reservations = await reservationGateway.GetReservationsInRangeAsync(startDate, endDate);
            var results = new List<RevenueSummary>();

            for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
            {
                var dayReservations = reservations.Where(r => r.CheckIn.Date == date).ToList();
                results.Add(new RevenueSummary
                {
                    Date = date,
                    Reservations = dayReservations.Count,
                    TotalRevenue = Math.Round(dayReservations.Sum(r => r.TotalCost), 2)
                });
            }

            return results.OrderBy(r => r.Date).ToList();
        }

        #endregion
    }
}
