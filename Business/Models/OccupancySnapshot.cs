//MSTISR001


// Summary: Represents daily occupancy metrics for reporting dashboards.

#region Using Directives
using System;
#endregion

namespace PhumlaKamnandiHotelSystem.Business.Models
{
    public class OccupancySnapshot
    {
        #region Properties

        public DateTime Date { get; set; }
        public int OccupiedRooms { get; set; }
        public int TotalRooms { get; set; }
        public int AvailableRooms => Math.Max(0, TotalRooms - OccupiedRooms);
        public decimal OccupancyPercentage { get; set; }

        #endregion
    }
}
