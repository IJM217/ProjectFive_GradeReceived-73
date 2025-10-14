//MSTISR001


// Summary: Handles reservation persistence, retrieval, and room occupancy updates.

#region Using Directives
using PhumlaKamnandiHotelSystem.Business.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
#endregion

namespace PhumlaKamnandiHotelSystem.Data
{
    public class ReservationDataGateway
    {
        #region Constants

        private const string ReservationSelectSql = @"SELECT b.BookingID, b.GuestID, b.RoomNumber, b.CheckInDate, b.CheckOutDate, b.TotalCost,
                                                               g.Name, g.Surname,
                                                               b.Adults,
                                                               b.ChildrenUnderFive,
                                                               b.ChildrenFiveToSixteen,
                                                               b.IsInSeason,
                                                               b.IsSingleOccupancy,
                                                               b.DepositPaid
                                                        FROM Bookings b
                                                        LEFT JOIN Guests g ON b.GuestID = g.GuestID";

        #endregion

        #region Public Methods

        public async Task<IList<ReservationRecord>> GetReservationsAsync()
        {
            var sql = ReservationSelectSql + " ORDER BY b.CheckInDate";
            var results = new List<ReservationRecord>();

            using (var connection = DatabaseConnectionFactory.Create())
            using (var command = new SqlCommand(sql, connection))
            {
                await connection.OpenAsync();
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        results.Add(ReadReservation(reader));
                    }
                }
            }

            return results;
        }

        public async Task<ReservationRecord?> GetReservationByIdAsync(int bookingId)
        {
            var sql = ReservationSelectSql + " WHERE b.BookingID = @BookingID";

            using (var connection = DatabaseConnectionFactory.Create())
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@BookingID", bookingId);
                await connection.OpenAsync();
                using (var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow))
                {
                    if (await reader.ReadAsync())
                    {
                        return ReadReservation(reader);
                    }
                }
            }

            return null;
        }

        public async Task<IList<ReservationRecord>> GetReservationsInRangeAsync(DateTime startDate, DateTime endDate)
        {
            var sql = ReservationSelectSql + " WHERE b.CheckInDate < @EndDate AND b.CheckOutDate > @StartDate";
            var results = new List<ReservationRecord>();

            using (var connection = DatabaseConnectionFactory.Create())
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@StartDate", startDate);
                command.Parameters.AddWithValue("@EndDate", endDate);

                await connection.OpenAsync();
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        results.Add(ReadReservation(reader));
                    }
                }
            }

            return results;
        }

        public async Task<IList<ReservationRecord>> GetReservationsForRoomAsync(int roomNumber, DateTime startDate, DateTime endDate, int? excludeReservationId = null)
        {
            var sql = ReservationSelectSql + @" WHERE b.RoomNumber = @RoomNumber
                                              AND b.CheckInDate < @EndDate AND b.CheckOutDate > @StartDate
                                              AND (@ExcludeId IS NULL OR b.BookingID <> @ExcludeId)";

            var results = new List<ReservationRecord>();
            using (var connection = DatabaseConnectionFactory.Create())
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@RoomNumber", roomNumber);
                command.Parameters.AddWithValue("@StartDate", startDate);
                command.Parameters.AddWithValue("@EndDate", endDate);
                command.Parameters.AddWithValue("@ExcludeId", (object)excludeReservationId ?? DBNull.Value);

                await connection.OpenAsync();
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        results.Add(ReadReservation(reader));
                    }
                }
            }

            return results;
        }

        public async Task<int> InsertReservationAsync(ReservationRecord reservation)
        {
            reservation.ReservationId = await GetNextReservationIdAsync();
            const string sql = @"INSERT INTO Bookings (BookingID, GuestID, RoomNumber, CheckInDate, CheckOutDate, TotalCost,
                                                       Adults, ChildrenUnderFive, ChildrenFiveToSixteen, IsInSeason, IsSingleOccupancy, DepositPaid)
                                  VALUES (@BookingID, @GuestID, @RoomNumber, @CheckInDate, @CheckOutDate, @TotalCost,
                                          @Adults, @ChildrenUnderFive, @ChildrenFiveToSixteen, @IsInSeason, @IsSingleOccupancy, @DepositPaid);";

            using (var connection = DatabaseConnectionFactory.Create())
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@BookingID", reservation.ReservationId);
                command.Parameters.AddWithValue("@GuestID", reservation.GuestId);
                command.Parameters.AddWithValue("@RoomNumber", reservation.RoomNumber);
                command.Parameters.AddWithValue("@CheckInDate", reservation.CheckIn);
                command.Parameters.AddWithValue("@CheckOutDate", reservation.CheckOut);
                command.Parameters.AddWithValue("@TotalCost", reservation.TotalCost);
                command.Parameters.AddWithValue("@Adults", reservation.Adults);
                command.Parameters.AddWithValue("@ChildrenUnderFive", reservation.ChildrenUnderFive);
                command.Parameters.AddWithValue("@ChildrenFiveToSixteen", reservation.ChildrenFiveToSixteen);
                command.Parameters.AddWithValue("@IsInSeason", reservation.IsInSeason);
                command.Parameters.AddWithValue("@IsSingleOccupancy", reservation.IsSingleOccupancy);
                command.Parameters.AddWithValue("@DepositPaid", reservation.DepositPaid);

                await connection.OpenAsync();
                await command.ExecuteNonQueryAsync();
                await UpdateRoomOccupancyAsync(connection, reservation.RoomNumber);
            }

            return reservation.ReservationId;
        }

        public async Task UpdateReservationAsync(ReservationRecord reservation)
        {
            const string sql = @"UPDATE Bookings SET GuestID = @GuestID, RoomNumber = @RoomNumber, CheckInDate = @CheckInDate,
                                                    CheckOutDate = @CheckOutDate, TotalCost = @TotalCost,
                                                    Adults = @Adults, ChildrenUnderFive = @ChildrenUnderFive,
                                                    ChildrenFiveToSixteen = @ChildrenFiveToSixteen,
                                                    IsInSeason = @IsInSeason, IsSingleOccupancy = @IsSingleOccupancy,
                                                    DepositPaid = @DepositPaid
                                  WHERE BookingID = @BookingID";

            using (var connection = DatabaseConnectionFactory.Create())
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@GuestID", reservation.GuestId);
                command.Parameters.AddWithValue("@RoomNumber", reservation.RoomNumber);
                command.Parameters.AddWithValue("@CheckInDate", reservation.CheckIn);
                command.Parameters.AddWithValue("@CheckOutDate", reservation.CheckOut);
                command.Parameters.AddWithValue("@TotalCost", reservation.TotalCost);
                command.Parameters.AddWithValue("@Adults", reservation.Adults);
                command.Parameters.AddWithValue("@ChildrenUnderFive", reservation.ChildrenUnderFive);
                command.Parameters.AddWithValue("@ChildrenFiveToSixteen", reservation.ChildrenFiveToSixteen);
                command.Parameters.AddWithValue("@IsInSeason", reservation.IsInSeason);
                command.Parameters.AddWithValue("@IsSingleOccupancy", reservation.IsSingleOccupancy);
                command.Parameters.AddWithValue("@DepositPaid", reservation.DepositPaid);
                command.Parameters.AddWithValue("@BookingID", reservation.ReservationId);

                await connection.OpenAsync();
                var originalRoom = await GetReservationRoomNumberAsync(connection, reservation.ReservationId);
                await command.ExecuteNonQueryAsync();
                await UpdateRoomOccupancyAsync(connection, reservation.RoomNumber);
                if (originalRoom.HasValue && originalRoom.Value != reservation.RoomNumber)
                {
                    await UpdateRoomOccupancyAsync(connection, originalRoom.Value);
                }
            }
        }

        public async Task DeleteReservationAsync(int reservationId)
        {
            const string sql = "DELETE FROM Bookings WHERE BookingID = @BookingID";
            using (var connection = DatabaseConnectionFactory.Create())
            {
                await connection.OpenAsync();
                int? roomNumber = await GetReservationRoomNumberAsync(connection, reservationId);

                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@BookingID", reservationId);
                    await command.ExecuteNonQueryAsync();
                }

                if (roomNumber.HasValue)
                {
                    await UpdateRoomOccupancyAsync(connection, roomNumber.Value);
                }
            }
        }

        #endregion

        #region Private Helpers

        private async Task<int> GetNextReservationIdAsync()
        {
            const string sql = "SELECT ISNULL(MAX(BookingID), 0) + 1 FROM Bookings";
            using (var connection = DatabaseConnectionFactory.Create())
            using (var command = new SqlCommand(sql, connection))
            {
                await connection.OpenAsync();
                var result = await command.ExecuteScalarAsync();
                return Convert.ToInt32(result);
            }
        }

        private static ReservationRecord ReadReservation(SqlDataReader reader)
        {
            return new ReservationRecord
            {
                ReservationId = reader.GetInt32(0),
                GuestId = reader.GetInt32(1),
                RoomNumber = reader.GetInt32(2),
                CheckIn = reader.GetDateTime(3),
                CheckOut = reader.GetDateTime(4),
                TotalCost = reader.GetDecimal(5),
                GuestName = $"{(reader.IsDBNull(6) ? string.Empty : reader.GetString(6))} {(reader.IsDBNull(7) ? string.Empty : reader.GetString(7))}".Trim(),
                Adults = reader.IsDBNull(8) ? 0 : reader.GetInt32(8),
                ChildrenUnderFive = reader.IsDBNull(9) ? 0 : reader.GetInt32(9),
                ChildrenFiveToSixteen = reader.IsDBNull(10) ? 0 : reader.GetInt32(10),
                IsInSeason = reader.IsDBNull(11) || reader.GetBoolean(11),
                IsSingleOccupancy = !reader.IsDBNull(12) && reader.GetBoolean(12),
                DepositPaid = reader.IsDBNull(13) || reader.GetBoolean(13)
            };
        }

        private static async Task<int?> GetReservationRoomNumberAsync(SqlConnection connection, int reservationId)
        {
            const string sql = "SELECT RoomNumber FROM Bookings WHERE BookingID = @BookingID";
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@BookingID", reservationId);
                var result = await command.ExecuteScalarAsync();
                return result == null || result == DBNull.Value ? (int?)null : Convert.ToInt32(result);
            }
        }

        private static async Task UpdateRoomOccupancyAsync(SqlConnection connection, int roomNumber)
        {
            const string nextReservationSql = @"SELECT TOP 1 CheckInDate, CheckOutDate
                                                 FROM Bookings
                                                 WHERE RoomNumber = @RoomNumber AND CheckOutDate >= CAST(GETDATE() AS DATE)
                                                 ORDER BY CheckInDate";

            DateTime? checkIn = null;
            DateTime? checkOut = null;
            bool hasUpcomingBooking = false;
            
            using (var command = new SqlCommand(nextReservationSql, connection))
            {
                command.Parameters.AddWithValue("@RoomNumber", roomNumber);
                using (var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow))
                {
                    if (await reader.ReadAsync())
                    {
                        checkIn = reader.GetDateTime(0);
                        checkOut = reader.GetDateTime(1);
                        hasUpcomingBooking = true;
                    }
                }
            }

            const string updateRoomSql = @"UPDATE Rooms
                                           SET CheckInDate = @CheckInDate,
                                               CheckOutDate = @CheckOutDate,
                                               IsOccupied = @IsOccupied
                                           WHERE RoomNumber = @RoomNumber";

            using (var updateCommand = new SqlCommand(updateRoomSql, connection))
            {
                updateCommand.Parameters.AddWithValue("@RoomNumber", roomNumber);
                updateCommand.Parameters.AddWithValue("@CheckInDate", (object?)checkIn ?? DBNull.Value);
                updateCommand.Parameters.AddWithValue("@CheckOutDate", (object?)checkOut ?? DBNull.Value);
                updateCommand.Parameters.AddWithValue("@IsOccupied", hasUpcomingBooking);
                await updateCommand.ExecuteNonQueryAsync();
            }
        }

        #endregion
    }
}
