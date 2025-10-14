//MSTISR001


// Summary: Stores guest contact information and payment details.

#region Using Directives
using System;
#endregion

namespace PhumlaKamnandiHotelSystem.Business.Models
{
    public class GuestProfile
    {
        #region Properties

        public int GuestId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public BankCardDetails BankCard { get; set; } = new BankCardDetails();

        public string FullName => string.Join(" ", new[] { FirstName, LastName }).Trim();

        #endregion

        #region Cloning

        public GuestProfile Clone()
        {
            var copy = (GuestProfile)MemberwiseClone();
            copy.BankCard = BankCard?.Clone() ?? new BankCardDetails();
            return copy;
        }

        #endregion

        #region Display Helpers

        public override string ToString()
        {
            var name = FullName;
            if (string.IsNullOrWhiteSpace(name))
            {
                name = $"Guest #{GuestId}";
            }

            return GuestId > 0 ? $"{name} (#{GuestId})" : name;
        }

        #endregion
    }
}
