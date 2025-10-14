//MSTISR001


// Summary: Describes calculated pricing details for a prospective reservation.

#region Using Directives
using System;
#endregion

namespace PhumlaKamnandiHotelSystem.Business.Models
{
    public class PricingQuote
    {
        #region Properties

        public decimal Total { get; set; }
        public decimal Deposit { get; set; }
        public DateTime DepositDueDate { get; set; }
        public bool UsesPerPersonPricing { get; set; }
        public bool IsMixedSeason { get; set; }
        public SeasonCategory PrimarySeason { get; set; }
        public string SeasonDescription { get; set; } = string.Empty;
        public int Nights { get; set; }

        #endregion
    }
}
