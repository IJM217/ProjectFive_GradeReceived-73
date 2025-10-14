//MSTISR001


// Summary: Stores payment card information for guest billing.

namespace PhumlaKamnandiHotelSystem.Business.Models
{
    public class BankCardDetails
    {
        #region Properties

        public string CardNumber { get; set; } = string.Empty;
        public string BankId { get; set; } = string.Empty;
        public int ExpirationMonth { get; set; }
        public int ExpirationYear { get; set; }
        public string SecurityCode { get; set; } = string.Empty;

        #endregion

        #region Cloning

        public BankCardDetails Clone()
        {
            return (BankCardDetails)MemberwiseClone();
        }

        #endregion

        #region Convenience Properties

        public bool HasDetails => !string.IsNullOrWhiteSpace(CardNumber);

        #endregion
    }
}
