//MSTISR001


// Summary: Ensures the SQL database schema and seed data are prepared for the hotel system.

#region Using Directives
using System.Data.SqlClient;
using System.Text;
using System.Threading.Tasks;
#endregion

namespace PhumlaKamnandiHotelSystem.Data
{
    public class DatabaseInitializer
    {
        #region Public Methods

        public async Task EnsurePreparedAsync()
        {
            using var connection = DatabaseConnectionFactory.Create();
            await connection.OpenAsync();

            await EnsureBankingDetailsAsync(connection);
            await EnsureGuestsAsync(connection);
            await EnsureRoomsAsync(connection);
            await EnsureBookingsAsync(connection);
            await RemoveLegacyBookingDetailsAsync(connection);
            await SeedRoomsAsync(connection);
        }

        #endregion

        #region Schema Creation

        private static async Task EnsureBankingDetailsAsync(SqlConnection connection)
        {
            var sql = @"IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'BankingDetails')
BEGIN
    CREATE TABLE BankingDetails(
        BankCardNumber NVARCHAR(32) NOT NULL PRIMARY KEY,
        BankID NVARCHAR(64) NOT NULL,
        ExpirationMonth TINYINT NOT NULL,
        ExpirationYear SMALLINT NOT NULL,
        SecurityCode NVARCHAR(8) NOT NULL
    );
END;";

            using var command = new SqlCommand(sql, connection);
            await command.ExecuteNonQueryAsync();
        }

        private static async Task EnsureGuestsAsync(SqlConnection connection)
        {
            var builder = new StringBuilder();
            builder.AppendLine("IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Guests')");
            builder.AppendLine("BEGIN");
            builder.AppendLine("    CREATE TABLE Guests(");
            builder.AppendLine("        GuestID INT IDENTITY(1,1) NOT NULL PRIMARY KEY,");
            builder.AppendLine("        Name NVARCHAR(100) NOT NULL,");
            builder.AppendLine("        Surname NVARCHAR(100) NOT NULL,");
            builder.AppendLine("        PhoneNumber NVARCHAR(40) NULL,");
            builder.AppendLine("        Email NVARCHAR(150) NULL,");
            builder.AppendLine("        Address NVARCHAR(255) NULL,");
            builder.AppendLine("        PostalCode NVARCHAR(20) NULL,");
            builder.AppendLine("        BankCardNumber NVARCHAR(32) NULL");
            builder.AppendLine("    );");
            builder.AppendLine("END;\n");
            builder.AppendLine("IF COL_LENGTH('Guests', 'BankCardNumber') IS NULL");
            builder.AppendLine("BEGIN");
            builder.AppendLine("    ALTER TABLE Guests ADD BankCardNumber NVARCHAR(32) NULL;");
            builder.AppendLine("END;\n");
            builder.AppendLine("IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Guests_BankingDetails')");
            builder.AppendLine("BEGIN");
            builder.AppendLine("    ALTER TABLE Guests WITH NOCHECK");
            builder.AppendLine("    ADD CONSTRAINT FK_Guests_BankingDetails FOREIGN KEY (BankCardNumber)");
            builder.AppendLine("    REFERENCES BankingDetails(BankCardNumber);");
            builder.AppendLine("END;");

            using var command = new SqlCommand(builder.ToString(), connection);
            await command.ExecuteNonQueryAsync();
        }

        private static async Task EnsureRoomsAsync(SqlConnection connection)
        {
            var builder = new StringBuilder();
            builder.AppendLine("IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Rooms')");
            builder.AppendLine("BEGIN");
            builder.AppendLine("    CREATE TABLE Rooms(");
            builder.AppendLine("        RoomNumber INT NOT NULL PRIMARY KEY,");
            builder.AppendLine("        CheckInDate DATETIME NULL,");
            builder.AppendLine("        CheckOutDate DATETIME NULL,");
            builder.AppendLine("        IsOccupied BIT NOT NULL CONSTRAINT DF_Rooms_IsOccupied DEFAULT(0)");
            builder.AppendLine("    );");
            builder.AppendLine("END;\n");
            builder.AppendLine("IF COL_LENGTH('Rooms', 'CheckInDate') IS NULL");
            builder.AppendLine("BEGIN");
            builder.AppendLine("    ALTER TABLE Rooms ADD CheckInDate DATETIME NULL;");
            builder.AppendLine("END;\n");
            builder.AppendLine("IF COL_LENGTH('Rooms', 'CheckOutDate') IS NULL");
            builder.AppendLine("BEGIN");
            builder.AppendLine("    ALTER TABLE Rooms ADD CheckOutDate DATETIME NULL;");
            builder.AppendLine("END;\n");
            builder.AppendLine("IF COL_LENGTH('Rooms', 'IsOccupied') IS NULL");
            builder.AppendLine("BEGIN");
            builder.AppendLine("    ALTER TABLE Rooms ADD IsOccupied BIT NOT NULL CONSTRAINT DF_Rooms_IsOccupied_New DEFAULT(0);");
            builder.AppendLine("END;");

            using var command = new SqlCommand(builder.ToString(), connection);
            await command.ExecuteNonQueryAsync();
        }

        private static async Task EnsureBookingsAsync(SqlConnection connection)
        {
            var builder = new StringBuilder();
            builder.AppendLine("IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Bookings')");
            builder.AppendLine("BEGIN");
            builder.AppendLine("    CREATE TABLE Bookings(");
            builder.AppendLine("        BookingID INT NOT NULL PRIMARY KEY,");
            builder.AppendLine("        GuestID INT NOT NULL,");
            builder.AppendLine("        RoomNumber INT NOT NULL,");
            builder.AppendLine("        CheckInDate DATETIME NOT NULL,");
            builder.AppendLine("        CheckOutDate DATETIME NOT NULL,");
            builder.AppendLine("        TotalCost DECIMAL(18,2) NOT NULL,");
            builder.AppendLine("        Adults INT NOT NULL DEFAULT(1),");
            builder.AppendLine("        ChildrenUnderFive INT NOT NULL DEFAULT(0),");
            builder.AppendLine("        ChildrenFiveToSixteen INT NOT NULL DEFAULT(0),");
            builder.AppendLine("        IsInSeason BIT NOT NULL DEFAULT(1),");
            builder.AppendLine("        IsSingleOccupancy BIT NOT NULL DEFAULT(0),");
            builder.AppendLine("        DepositPaid BIT NOT NULL DEFAULT(1)");
            builder.AppendLine("    );");
            builder.AppendLine("END;\n");
            builder.AppendLine("IF COL_LENGTH('Bookings', 'Adults') IS NULL BEGIN ALTER TABLE Bookings ADD Adults INT NOT NULL DEFAULT(1); END;\n");
            builder.AppendLine("IF COL_LENGTH('Bookings', 'ChildrenUnderFive') IS NULL BEGIN ALTER TABLE Bookings ADD ChildrenUnderFive INT NOT NULL DEFAULT(0); END;\n");
            builder.AppendLine("IF COL_LENGTH('Bookings', 'ChildrenFiveToSixteen') IS NULL BEGIN ALTER TABLE Bookings ADD ChildrenFiveToSixteen INT NOT NULL DEFAULT(0); END;\n");
            builder.AppendLine("IF COL_LENGTH('Bookings', 'IsInSeason') IS NULL BEGIN ALTER TABLE Bookings ADD IsInSeason BIT NOT NULL DEFAULT(1); END;\n");
            builder.AppendLine("IF COL_LENGTH('Bookings', 'IsSingleOccupancy') IS NULL BEGIN ALTER TABLE Bookings ADD IsSingleOccupancy BIT NOT NULL DEFAULT(0); END;\n");
            builder.AppendLine("IF COL_LENGTH('Bookings', 'DepositPaid') IS NULL BEGIN ALTER TABLE Bookings ADD DepositPaid BIT NOT NULL DEFAULT(1); END;\n");
            builder.AppendLine("IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Bookings_Guests')");
            builder.AppendLine("BEGIN");
            builder.AppendLine("    ALTER TABLE Bookings WITH NOCHECK ADD CONSTRAINT FK_Bookings_Guests FOREIGN KEY (GuestID) REFERENCES Guests(GuestID);");
            builder.AppendLine("END;\n");
            builder.AppendLine("IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Bookings_Rooms')");
            builder.AppendLine("BEGIN");
            builder.AppendLine("    ALTER TABLE Bookings WITH NOCHECK ADD CONSTRAINT FK_Bookings_Rooms FOREIGN KEY (RoomNumber) REFERENCES Rooms(RoomNumber);");
            builder.AppendLine("END;");

            using var command = new SqlCommand(builder.ToString(), connection);
            await command.ExecuteNonQueryAsync();
        }

        #endregion

        #region Maintenance Helpers

        private static async Task RemoveLegacyBookingDetailsAsync(SqlConnection connection)
        {
            const string sql = "IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'BookingDetails') DROP TABLE BookingDetails;";

            using var command = new SqlCommand(sql, connection);
            await command.ExecuteNonQueryAsync();
        }

        private static async Task SeedRoomsAsync(SqlConnection connection)
        {
            var builder = new StringBuilder();
            builder.AppendLine("DECLARE @i INT = 1;");
            builder.AppendLine("WHILE @i <= 5");
            builder.AppendLine("BEGIN");
            builder.AppendLine("    IF NOT EXISTS (SELECT 1 FROM Rooms WHERE RoomNumber = @i)");
            builder.AppendLine("    BEGIN");
            builder.AppendLine("        INSERT INTO Rooms (RoomNumber, CheckInDate, CheckOutDate, IsOccupied) VALUES (@i, NULL, NULL, 0);");
            builder.AppendLine("    END;");
            builder.AppendLine("    SET @i += 1;");
            builder.AppendLine("END;");

            using var command = new SqlCommand(builder.ToString(), connection);
            await command.ExecuteNonQueryAsync();
        }

        #endregion
    }
}