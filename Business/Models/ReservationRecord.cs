//MSTISR001


// Summary: Represents a reservation with pricing and occupancy context.

#region Using Directives
using System;
#endregion

namespace PhumlaKamnandiHotelSystem.Business.Models
{
    public class ReservationRecord
    {
        #region Properties

        public int ReservationId { get; set; }
        public int GuestId { get; set; }
        public int RoomNumber { get; set; }
        public DateTime CheckIn { get; set; }
        public DateTime CheckOut { get; set; }
        public decimal TotalCost { get; set; }
        public ReservationStatus Status { get; set; }
        public decimal DepositAmount { get; set; }
        public string GuestName { get; set; } = string.Empty;
        public string RoomType { get; set; } = string.Empty;
        public string SeasonDescription { get; set; } = string.Empty;
        public bool IsInSeason { get; set; }
        public int Adults { get; set; }
        public int ChildrenUnderFive { get; set; }
        public int ChildrenFiveToSixteen { get; set; }
        public bool IsSingleOccupancy { get; set; }
        public bool DepositPaid { get; set; }

        #endregion

        #region Derived Calculations

        public int Nights => (int)Math.Max(1, (CheckOut - CheckIn).TotalDays);

        public int TotalGuests => Adults + ChildrenUnderFive + ChildrenFiveToSixteen;

        public DateTime DepositDueDate => CheckIn.AddDays(-14);

        #endregion
    }
}
