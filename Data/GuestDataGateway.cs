//MSTISR001


// Summary: Manages guest persistence including linked bank card information.

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
    public class GuestDataGateway
    {
        #region Constants

        private const string GuestProjection = @"SELECT g.GuestID, g.Name, g.Surname, g.PhoneNumber, g.Email, g.Address, g.PostalCode,
                                                        g.BankCardNumber, b.BankID, b.ExpirationMonth, b.ExpirationYear, b.SecurityCode
                                                 FROM Guests g
                                                 LEFT JOIN BankingDetails b ON g.BankCardNumber = b.BankCardNumber";

        #endregion

        #region Public Methods

        public async Task<IList<GuestProfile>> GetGuestsAsync()
        {
            var results = new List<GuestProfile>();
            using (var connection = DatabaseConnectionFactory.Create())
            using (var command = new SqlCommand(GuestProjection + " ORDER BY g.Name, g.Surname", connection))
            {
                await connection.OpenAsync();
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        results.Add(ReadGuest(reader));
                    }
                }
            }

            return results;
        }

        public async Task<GuestProfile?> GetGuestByIdAsync(int guestId)
        {
            using (var connection = DatabaseConnectionFactory.Create())
            using (var command = new SqlCommand(GuestProjection + " WHERE g.GuestID = @GuestID", connection))
            {
                command.Parameters.AddWithValue("@GuestID", guestId);
                await connection.OpenAsync();
                using (var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow))
                {
                    if (await reader.ReadAsync())
                    {
                        return ReadGuest(reader);
                    }
                }
            }

            return null;
        }

        public async Task<int> InsertGuestAsync(GuestProfile guest)
        {
            using (var connection = DatabaseConnectionFactory.Create())
            {
                await connection.OpenAsync();
                await UpsertBankCardAsync(connection, guest.BankCard);

                using (var command = new SqlCommand(@"INSERT INTO Guests (Name, Surname, PhoneNumber, Email, Address, PostalCode, BankCardNumber)
VALUES (@Name, @Surname, @PhoneNumber, @Email, @Address, @PostalCode, @BankCardNumber);
SELECT CAST(SCOPE_IDENTITY() AS INT);", connection))
                {
                    command.Parameters.AddWithValue("@Name", guest.FirstName);
                    command.Parameters.AddWithValue("@Surname", guest.LastName);
                    command.Parameters.AddWithValue("@PhoneNumber", (object?)guest.PhoneNumber ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Email", (object?)guest.Email ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Address", (object?)guest.Address ?? DBNull.Value);
                    command.Parameters.AddWithValue("@PostalCode", (object?)guest.PostalCode ?? DBNull.Value);
                    command.Parameters.AddWithValue("@BankCardNumber", GetCardNumberOrDbNull(guest));

                    var id = await command.ExecuteScalarAsync();
                    return Convert.ToInt32(id);
                }
            }
        }

        public async Task UpdateGuestAsync(GuestProfile guest)
        {
            using (var connection = DatabaseConnectionFactory.Create())
            {
                await connection.OpenAsync();
                await UpsertBankCardAsync(connection, guest.BankCard);

                using (var command = new SqlCommand(@"UPDATE Guests SET Name = @Name, Surname = @Surname, PhoneNumber = @PhoneNumber,
Email = @Email, Address = @Address, PostalCode = @PostalCode, BankCardNumber = @BankCardNumber WHERE GuestID = @GuestID", connection))
                {
                    command.Parameters.AddWithValue("@Name", guest.FirstName);
                    command.Parameters.AddWithValue("@Surname", guest.LastName);
                    command.Parameters.AddWithValue("@PhoneNumber", (object?)guest.PhoneNumber ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Email", (object?)guest.Email ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Address", (object?)guest.Address ?? DBNull.Value);
                    command.Parameters.AddWithValue("@PostalCode", (object?)guest.PostalCode ?? DBNull.Value);
                    command.Parameters.AddWithValue("@BankCardNumber", GetCardNumberOrDbNull(guest));
                    command.Parameters.AddWithValue("@GuestID", guest.GuestId);

                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        #endregion

        #region Private Helpers

        private static async Task UpsertBankCardAsync(SqlConnection connection, BankCardDetails? card)
        {
            if (card == null || string.IsNullOrWhiteSpace(card.CardNumber))
            {
                return;
            }

            const string sql = @"IF EXISTS (SELECT 1 FROM BankingDetails WHERE BankCardNumber = @CardNumber)
BEGIN
    UPDATE BankingDetails
    SET BankID = @BankID,
        ExpirationMonth = @ExpirationMonth,
        ExpirationYear = @ExpirationYear,
        SecurityCode = @SecurityCode
    WHERE BankCardNumber = @CardNumber;
END
ELSE
BEGIN
    INSERT INTO BankingDetails (BankCardNumber, BankID, ExpirationMonth, ExpirationYear, SecurityCode)
    VALUES (@CardNumber, @BankID, @ExpirationMonth, @ExpirationYear, @SecurityCode);
END;";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@CardNumber", card.CardNumber);
            command.Parameters.AddWithValue("@BankID", string.IsNullOrWhiteSpace(card.BankId) ? string.Empty : card.BankId);
            command.Parameters.AddWithValue("@ExpirationMonth", card.ExpirationMonth);
            command.Parameters.AddWithValue("@ExpirationYear", card.ExpirationYear);
            command.Parameters.AddWithValue("@SecurityCode", string.IsNullOrWhiteSpace(card.SecurityCode) ? string.Empty : card.SecurityCode);

            await command.ExecuteNonQueryAsync();
        }

        private static object GetCardNumberOrDbNull(GuestProfile guest)
        {
            return string.IsNullOrWhiteSpace(guest?.BankCard?.CardNumber)
                ? (object)DBNull.Value
                : guest!.BankCard!.CardNumber;
        }

        private static GuestProfile ReadGuest(SqlDataReader reader)
        {
            return new GuestProfile
            {
                GuestId = reader.GetInt32(0),
                FirstName = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                LastName = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                PhoneNumber = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                Email = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                Address = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                PostalCode = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                BankCard = new BankCardDetails
                {
                    CardNumber = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
                    BankId = reader.IsDBNull(8) ? string.Empty : reader.GetString(8),
                    ExpirationMonth = reader.IsDBNull(9) ? 0 : reader.GetInt32(9),
                    ExpirationYear = reader.IsDBNull(10) ? 0 : reader.GetInt32(10),
                    SecurityCode = reader.IsDBNull(11) ? string.Empty : reader.GetString(11)
                }
            };
        }

        #endregion
    }
}
