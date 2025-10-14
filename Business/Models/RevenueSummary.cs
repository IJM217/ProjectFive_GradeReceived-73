//MSTISR001


// Summary: Captures daily reservation counts and revenue totals for reporting.

#region Using Directives
using System;
#endregion

namespace PhumlaKamnandiHotelSystem.Business.Models
{
    public class RevenueSummary
    {
        #region Properties

        public DateTime Date { get; set; }
        public int Reservations { get; set; }
        public decimal TotalRevenue { get; set; }

        #endregion
    }
}
