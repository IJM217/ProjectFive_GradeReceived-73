//MSTISR001


// Summary: Provides database access for retrieving room information and occupancy states.

#region Using Directives
using System;
using PhumlaKamnandiHotelSystem.Business.Models;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
#endregion

namespace PhumlaKamnandiHotelSystem.Data
{
    public class RoomDataGateway
    {
        #region Data Retrieval

        public async Task<IList<RoomInfo>> GetRoomsAsync()
        {
            const string sql = "SELECT RoomNumber, CheckInDate, CheckOutDate, IsOccupied FROM Rooms ORDER BY RoomNumber";
            var rooms = new List<RoomInfo>();
            using (var connection = DatabaseConnectionFactory.Create())
            using (var command = new SqlCommand(sql, connection))
            {
                await connection.OpenAsync();
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        rooms.Add(new RoomInfo
                        {
                            RoomNumber = reader.GetInt32(0),
                            CheckInDate = reader.IsDBNull(1) ? (DateTime?)null : reader.GetDateTime(1),
                            CheckOutDate = reader.IsDBNull(2) ? (DateTime?)null : reader.GetDateTime(2),
                            IsOccupied = !reader.IsDBNull(3) && reader.GetBoolean(3)
                        });
                    }
                }
            }

            return rooms;
        }

        #endregion
    }
}