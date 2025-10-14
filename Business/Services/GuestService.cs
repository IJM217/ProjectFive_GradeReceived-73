//MSTISR001


// Summary: Coordinates guest persistence and retrieval through the data gateway.

#region Using Directives
using PhumlaKamnandiHotelSystem.Business.Models;
using PhumlaKamnandiHotelSystem.Data;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
#endregion

namespace PhumlaKamnandiHotelSystem.Business.Services
{
    public class GuestService
    {
        #region Dependencies

        private readonly GuestDataGateway guestGateway;

        #endregion

        #region Constructors

        public GuestService(GuestDataGateway guestGateway)
        {
            this.guestGateway = guestGateway ?? throw new ArgumentNullException(nameof(guestGateway));
        }

        #endregion

        #region Guest Operations

        public Task<IList<GuestProfile>> GetGuestsAsync()
        {
            return guestGateway.GetGuestsAsync();
        }

        public Task<GuestProfile?> GetGuestAsync(int guestId)
        {
            return guestGateway.GetGuestByIdAsync(guestId);
        }

        public async Task<int> SaveGuestAsync(GuestProfile guest)
        {
            if (guest == null)
            {
                throw new ArgumentNullException(nameof(guest));
            }

            if (guest.GuestId == 0)
            {
                var newId = await guestGateway.InsertGuestAsync(guest);
                guest.GuestId = newId;
                return newId;
            }
            else
            {
                await guestGateway.UpdateGuestAsync(guest);
                return guest.GuestId;
            }
        }

        #endregion

    }
}
