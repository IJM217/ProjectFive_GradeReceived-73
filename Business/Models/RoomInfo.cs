//MSTISR001


// Summary: Represents room availability details for display and reservation logic.

#region Using Directives
using System;
#endregion

namespace PhumlaKamnandiHotelSystem.Business.Models
{
    public class RoomInfo
    {
        #region Properties

        public int RoomNumber { get; set; }
        public DateTime? CheckInDate { get; set; }
        public DateTime? CheckOutDate { get; set; }
        public bool IsOccupied { get; set; }

        public string DisplayName
        {
            get
            {
                var status = IsOccupied ? "Occupied" : "Available";
                if (CheckInDate.HasValue && CheckOutDate.HasValue)
                {
                    status += $" ({CheckInDate:dd MMM} - {CheckOutDate:dd MMM})";
                }

                return $"Room {RoomNumber} - {status}";
            }
        }

        public int RoomId => RoomNumber;

        #endregion

        #region Display Helpers

        public override string ToString()
        {
            return DisplayName;
        }

        #endregion
    }
}
