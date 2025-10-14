//MSTISR001


// Summary: Represents the in-memory dataset schema for the Phumla Kamnandi hotel system.

#region Using Directives
using System;
using System.Data;
#endregion

namespace PhumlaKamnandiHotelSystem.Data
{
    public class PKDS : DataSet
    {
        #region Tables

        public DataTable Guests { get; }
        public DataTable Bookings { get; }
        public DataTable Rooms { get; }
        public DataTable BankingDetails { get; }
        public DataTable Admins { get; }

        #endregion

        #region Constructors

        public PKDS()
        {
            DataSetName = "PKDS";

            BankingDetails = CreateBankingDetailsTable();
            Guests = CreateGuestsTable();
            Rooms = CreateRoomsTable();
            Bookings = CreateBookingsTable();
            Admins = CreateAdminsTable();

            Tables.AddRange(new[] { BankingDetails, Guests, Rooms, Bookings, Admins });

            Relations.Add("FK_Guests_BankingDetails",
                BankingDetails.Columns["BankCardNumber"],
                Guests.Columns["BankCardNumber"], false);

            Relations.Add("FK_Bookings_Guests",
                Guests.Columns["GuestID"],
                Bookings.Columns["GuestID"], false);

            Relations.Add("FK_Bookings_Rooms",
                Rooms.Columns["RoomNumber"],
                Bookings.Columns["RoomNumber"], false);
        }

        #endregion

        #region Table Creation Helpers

        private static DataTable CreateBankingDetailsTable()
        {
            var table = new DataTable("BankingDetails");
            table.Columns.Add("BankCardNumber", typeof(string));
            table.Columns.Add("BankID", typeof(string));
            table.Columns.Add("ExpirationMonth", typeof(int));
            table.Columns.Add("ExpirationYear", typeof(int));
            table.Columns.Add("SecurityCode", typeof(string));
            table.PrimaryKey = new[] { table.Columns["BankCardNumber"] };
            return table;
        }

        private static DataTable CreateGuestsTable()
        {
            var table = new DataTable("Guests");
            var id = table.Columns.Add("GuestID", typeof(int));
            id.AutoIncrement = true;
            id.AutoIncrementSeed = 1;
            id.AutoIncrementStep = 1;
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("Surname", typeof(string));
            table.Columns.Add("PhoneNumber", typeof(string));
            table.Columns.Add("Email", typeof(string));
            table.Columns.Add("Address", typeof(string));
            table.Columns.Add("PostalCode", typeof(string));
            table.Columns.Add("BankCardNumber", typeof(string));
            table.PrimaryKey = new[] { id };
            return table;
        }

        private static DataTable CreateRoomsTable()
        {
            var table = new DataTable("Rooms");
            var number = table.Columns.Add("RoomNumber", typeof(int));
            table.Columns.Add("CheckInDate", typeof(DateTime));
            table.Columns.Add("CheckOutDate", typeof(DateTime));
            table.Columns.Add("IsOccupied", typeof(bool));
            table.PrimaryKey = new[] { number };
            return table;
        }

        private static DataTable CreateBookingsTable()
        {
            var table = new DataTable("Bookings");
            var bookingId = table.Columns.Add("BookingID", typeof(int));
            table.Columns.Add("GuestID", typeof(int));
            table.Columns.Add("RoomNumber", typeof(int));
            table.Columns.Add("CheckInDate", typeof(DateTime));
            table.Columns.Add("CheckOutDate", typeof(DateTime));
            table.Columns.Add("TotalCost", typeof(decimal));
            table.Columns.Add("Adults", typeof(int));
            table.Columns.Add("ChildrenUnderFive", typeof(int));
            table.Columns.Add("ChildrenFiveToSixteen", typeof(int));
            table.Columns.Add("IsInSeason", typeof(bool));
            table.Columns.Add("IsSingleOccupancy", typeof(bool));
            table.Columns.Add("DepositPaid", typeof(bool));
            table.PrimaryKey = new[] { bookingId };
            return table;
        }

        private static DataTable CreateAdminsTable()
        {
            var table = new DataTable("Admins");
            var adminId = table.Columns.Add("AdminID", typeof(int));
            adminId.AutoIncrement = true;
            adminId.AutoIncrementSeed = 1;
            adminId.AutoIncrementStep = 1;
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("Surname", typeof(string));
            table.Columns.Add("Username", typeof(string));
            table.Columns.Add("Password", typeof(string));
            table.Columns.Add("PhoneNumber", typeof(string));
            table.Columns.Add("Email", typeof(string));
            table.PrimaryKey = new[] { adminId };
            return table;
        }

        #endregion
    }
}
