//MSTISR001


// Summary: Defines the main Windows Forms interface for managing guests, bookings, enquiries, and reports.

#region Using Directives
using PhumlaKamnandiHotelSystem.Business.Models;
using PhumlaKamnandiHotelSystem.Business.Services;
using PhumlaKamnandiHotelSystem.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
#endregion

namespace PhumlaKamnandiHotelSystem.Presentation
{
    public class MainForm : Form
    {
        #region Fields

        private readonly GuestService guestService;
        private readonly ReservationService reservationService;
        private readonly ReportService reportService;
        private readonly DatabaseInitializer databaseInitializer;

        private readonly BindingList<ReservationRecord> reservationBinding = new BindingList<ReservationRecord>();
        private readonly BindingList<ReservationRecord> enquiryBinding = new BindingList<ReservationRecord>();
        private readonly BindingList<OccupancySnapshot> occupancyBinding = new BindingList<OccupancySnapshot>();
        private readonly BindingList<RevenueSummary> revenueBinding = new BindingList<RevenueSummary>();

        private readonly BindingSource reservationBindingSource = new BindingSource();
        private readonly BindingSource enquiryBindingSource = new BindingSource();
        private readonly BindingSource occupancyBindingSource = new BindingSource();
        private readonly BindingSource revenueBindingSource = new BindingSource();

        private List<GuestProfile> guestCache = new List<GuestProfile>();
        private List<RoomInfo> availableRoomsForCreation = new List<RoomInfo>();

        private ReservationRecord? activeReservation;
        private GuestProfile? activeReservationGuest;

        private TabControl tabMain = null!;
        private bool suppressGuestValueEvents;

        // booking controls
        private ComboBox cmbGuestPicker = null!;
        private TextBox txtGuestFirstName = null!;
        private TextBox txtGuestLastName = null!;
        private TextBox txtGuestPhone = null!;
        private TextBox txtGuestEmail = null!;
        private TextBox txtGuestAddress = null!;
        private TextBox txtGuestPostal = null!;
        private TextBox txtGuestCardNumber = null!;
        private TextBox txtGuestBankId = null!;
        private NumericUpDown nudGuestExpiryMonth = null!;
        private NumericUpDown nudGuestExpiryYear = null!;
        private TextBox txtGuestSecurityCode = null!;
        private Label lblBookingSeasonInfo = null!;
        private DateTimePicker dtpBookingCheckIn = null!;
        private DateTimePicker dtpBookingCheckOut = null!;
        private NumericUpDown nudBookingAdults = null!;
        private NumericUpDown nudBookingChildrenUnder5 = null!;
        private NumericUpDown nudBookingChildrenUnder16 = null!;
        private Label lblBookingAvailability = null!;
        private Label lblBookingAssignedRoom = null!;
        private Label lblBookingTotal = null!;
        private Label lblBookingDeposit = null!;
        private Button btnBookingCreate = null!;
        private Button btnBookingCheckRooms = null!;
        private Button btnBookingClear = null!;

        // management controls
        private DataGridView gridReservations = null!;
        private TextBox txtManageFirstName = null!;
        private TextBox txtManageLastName = null!;
        private TextBox txtManagePhone = null!;
        private TextBox txtManageEmail = null!;
        private TextBox txtManageAddress = null!;
        private TextBox txtManagePostal = null!;
        private TextBox txtManageCardNumber = null!;
        private TextBox txtManageBankId = null!;
        private NumericUpDown nudManageExpiryMonth = null!;
        private NumericUpDown nudManageExpiryYear = null!;
        private TextBox txtManageSecurityCode = null!;
        private DateTimePicker dtpManageCheckIn = null!;
        private DateTimePicker dtpManageCheckOut = null!;
        private Label lblManageSeasonInfo = null!;
        private NumericUpDown nudManageAdults = null!;
        private NumericUpDown nudManageChildrenUnder5 = null!;
        private NumericUpDown nudManageChildrenUnder16 = null!;
        private Label lblManageReference = null!;
        private Label lblManageRoom = null!;
        private Label lblManageTotal = null!;
        private Label lblManageDeposit = null!;
        private Button btnManageUpdate = null!;
        private Button btnManageCancel = null!;
        private Button btnManageRefresh = null!;

        // enquiry controls
        private TextBox txtEnquirySearch = null!;
        private Button btnEnquirySearch = null!;
        private Button btnEnquiryReset = null!;
        private DataGridView gridEnquiry = null!;
        private Label lblEnquirySummary = null!;

        // reports controls
        private DateTimePicker dtpReportFrom = null!;
        private DateTimePicker dtpReportTo = null!;
        private Button btnRunOccupancy = null!;
        private Button btnRunRevenue = null!;
        private DataGridView gridOccupancy = null!;
        private DataGridView gridRevenue = null!;
        private Label lblOccupancySummary = null!;
        private Label lblRevenueSummary = null!;

        #endregion

        #region Constructors

        public MainForm()
        {
            databaseInitializer = new DatabaseInitializer();
            var guestGateway = new GuestDataGateway();
            var reservationGateway = new ReservationDataGateway();
            var roomGateway = new RoomDataGateway();

            guestService = new GuestService(guestGateway);
            reservationService = new ReservationService(reservationGateway, roomGateway, guestService);
            reportService = new ReportService(reservationGateway, roomGateway);

            InitializeComponent();

            reservationBindingSource.DataSource = reservationBinding;
            enquiryBindingSource.DataSource = enquiryBinding;
            occupancyBindingSource.DataSource = occupancyBinding;
            revenueBindingSource.DataSource = revenueBinding;

            gridReservations.DataSource = reservationBindingSource;
            gridEnquiry.DataSource = enquiryBindingSource;
            gridOccupancy.DataSource = occupancyBindingSource;
            gridRevenue.DataSource = revenueBindingSource;

            Load += async (s, e) => await LoadInitialDataAsync();
        }

        #endregion

        #region UI Construction

        private void InitializeComponent()
        {
            SuspendLayout();

            Text = "Rest Well Client Booking Console";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(1100, 760);
            Size = new Size(1280, 820);

            tabMain = new TabControl
            {
                Dock = DockStyle.Fill
            };

            tabMain.TabPages.Add(CreateBookingTab());
            tabMain.TabPages.Add(CreateManageTab());
            tabMain.TabPages.Add(CreateEnquiryTab());
            tabMain.TabPages.Add(CreateReportsTab());

            Controls.Add(tabMain);

            ResumeLayout(false);
        }

        private TabPage CreateBookingTab()
        {
            var page = new TabPage("New Booking");

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                Padding = new Padding(12)
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 80));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 20));

            var guestGroup = new GroupBox { Text = "Guest Details", Dock = DockStyle.Fill };
            guestGroup.Padding = new Padding(12);
            var guestLayout = CreateFormLayout(11);

            cmbGuestPicker = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDown, // <-- allow typing
                AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                AutoCompleteSource = AutoCompleteSource.ListItems
            };
            cmbGuestPicker.SelectedIndexChanged += (s, e) => ApplySelectedGuest();

            txtGuestFirstName = CreateTextBox();
            txtGuestLastName = CreateTextBox();
            txtGuestPhone = CreateTextBox();
            txtGuestEmail = CreateTextBox();
            txtGuestAddress = CreateTextBox(multiline: true);
            txtGuestPostal = CreateTextBox();
            txtGuestCardNumber = CreateTextBox();
            txtGuestBankId = CreateTextBox();
            nudGuestExpiryMonth = new NumericUpDown { Minimum = 1, Maximum = 12, Width = 60, Value = DateTime.Today.Month };
            nudGuestExpiryYear = new NumericUpDown { Minimum = DateTime.Today.Year, Maximum = DateTime.Today.Year + 20, Width = 80, Value = DateTime.Today.Year };
            txtGuestSecurityCode = CreateTextBox();

            AddLabeledControl(guestLayout, "Existing Guest", cmbGuestPicker, 0);
            AddLabeledControl(guestLayout, "First Name", txtGuestFirstName, 1);
            AddLabeledControl(guestLayout, "Last Name", txtGuestLastName, 2);
            AddLabeledControl(guestLayout, "Phone", txtGuestPhone, 3);
            AddLabeledControl(guestLayout, "Email", txtGuestEmail, 4);
            AddLabeledControl(guestLayout, "Address", txtGuestAddress, 5);
            AddLabeledControl(guestLayout, "Postal Code", txtGuestPostal, 6);
            AddLabeledControl(guestLayout, "Card Number", txtGuestCardNumber, 7);
            AddLabeledControl(guestLayout, "Bank", txtGuestBankId, 8);

            var bookingExpiryPanel = CreateFlowPanel();
            bookingExpiryPanel.Controls.Add(new Label { Text = "Month", AutoSize = true, Margin = new Padding(0, 6, 6, 3) });
            bookingExpiryPanel.Controls.Add(nudGuestExpiryMonth);
            bookingExpiryPanel.Controls.Add(new Label { Text = "Year", AutoSize = true, Margin = new Padding(12, 6, 6, 3) });
            bookingExpiryPanel.Controls.Add(nudGuestExpiryYear);
            AddLabeledControl(guestLayout, "Expiry", bookingExpiryPanel, 9);

            AddLabeledControl(guestLayout, "Security Code", txtGuestSecurityCode, 10);

            guestGroup.Controls.Add(guestLayout);

            var stayGroup = new GroupBox { Text = "Stay & Payment", Dock = DockStyle.Fill };
            stayGroup.Padding = new Padding(12);

            var stayLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 6,
                AutoSize = true
            };
            stayLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
            stayLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            stayLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
            stayLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            for (var i = 0; i < 6; i++)
            {
                stayLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            }

            dtpBookingCheckIn = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 140 };
            dtpBookingCheckOut = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 140 };
            dtpBookingCheckIn.ValueChanged += async (s, e) => await RefreshAvailableRoomsForBookingAsync();
            dtpBookingCheckOut.ValueChanged += async (s, e) => await RefreshAvailableRoomsForBookingAsync();

            lblBookingSeasonInfo = new Label
            {
                Text = "Season: -",
                AutoSize = true,
                Font = new Font(Font, FontStyle.Bold)
            };

            nudBookingAdults = new NumericUpDown { Minimum = 1, Maximum = 4, Width = 80, Value = 2 };
            nudBookingChildrenUnder5 = new NumericUpDown { Minimum = 0, Maximum = 4, Width = 80 };
            nudBookingChildrenUnder16 = new NumericUpDown { Minimum = 0, Maximum = 4, Width = 80 };
            nudBookingAdults.ValueChanged += (s, e) => HandleBookingGuestCountChanged(nudBookingAdults);
            nudBookingChildrenUnder5.ValueChanged += (s, e) => HandleBookingGuestCountChanged(nudBookingChildrenUnder5);
            nudBookingChildrenUnder16.ValueChanged += (s, e) => HandleBookingGuestCountChanged(nudBookingChildrenUnder16);

            lblBookingAssignedRoom = new Label { Text = "Room: -", AutoSize = true, Font = new Font(Font, FontStyle.Bold) };
            var checkRow = CreateFlowPanel();
            btnBookingCheckRooms = new Button { Text = "Check Availability", AutoSize = true };
            btnBookingCheckRooms.Click += async (s, e) => await RefreshAvailableRoomsForBookingAsync(true);
            checkRow.Controls.Add(btnBookingCheckRooms);
            lblBookingAvailability = new Label
            {
                AutoSize = true,
                ForeColor = Color.FromArgb(64, 64, 64),
                Margin = new Padding(12, 8, 3, 3)
            };
            checkRow.Controls.Add(lblBookingAvailability);

            AddLabeledControl(stayLayout, "Check-in", dtpBookingCheckIn, 0, column: 0);
            AddLabeledControl(stayLayout, "Check-out", dtpBookingCheckOut, 0, column: 2);
            AddLabeledControl(stayLayout, "Season", lblBookingSeasonInfo, 1, column: 0);
            AddLabeledControl(stayLayout, "Assigned", lblBookingAssignedRoom, 1, column: 2);

            var guestPanel = CreateFlowPanel();
            guestPanel.Controls.Add(new Label { Text = "Adults", AutoSize = true, Margin = new Padding(0, 6, 6, 3) });
            guestPanel.Controls.Add(nudBookingAdults);
            guestPanel.Controls.Add(new Label { Text = "Under 5", AutoSize = true, Margin = new Padding(12, 6, 6, 3) });
            guestPanel.Controls.Add(nudBookingChildrenUnder5);
            guestPanel.Controls.Add(new Label { Text = "5-16", AutoSize = true, Margin = new Padding(12, 6, 6, 3) });
            guestPanel.Controls.Add(nudBookingChildrenUnder16);
            AddLabeledControl(stayLayout, "Guests", guestPanel, 2, column: 0);
            stayLayout.SetColumnSpan(guestPanel, 3);

            stayLayout.Controls.Add(checkRow, 0, 3);
            stayLayout.SetColumnSpan(checkRow, 4);

            var totalsPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2
            };
            totalsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            totalsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            totalsPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            totalsPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            lblBookingTotal = new Label { Text = "Total: R0.00", AutoSize = true, Font = new Font(Font, FontStyle.Bold) };
            lblBookingDeposit = new Label { Text = "Deposit (due by -): R0.00", AutoSize = true };
            totalsPanel.Controls.Add(new Label { Text = "Total", AutoSize = true, Margin = new Padding(0, 6, 3, 3) }, 0, 0);
            totalsPanel.Controls.Add(lblBookingTotal, 1, 0);
            totalsPanel.Controls.Add(new Label { Text = "Deposit", AutoSize = true, Margin = new Padding(0, 6, 3, 3) }, 0, 1);
            totalsPanel.Controls.Add(lblBookingDeposit, 1, 1);

            stayLayout.Controls.Add(totalsPanel, 0, 4);
            stayLayout.SetColumnSpan(totalsPanel, 4);

            var bookingButtons = CreateFlowPanel();
            bookingButtons.FlowDirection = FlowDirection.RightToLeft;
            bookingButtons.Dock = DockStyle.Fill;
            bookingButtons.Padding = new Padding(0, 12, 0, 0);

            btnBookingCreate = new Button { Text = "Create Booking", AutoSize = true };
            btnBookingCreate.Click += async (s, e) => await CreateBookingAsync();
            btnBookingClear = new Button { Text = "Clear", AutoSize = true };
            btnBookingClear.Click += async (s, e) =>
            {
                ClearNewBookingForm();
                await RefreshAvailableRoomsForBookingAsync();
            };

            bookingButtons.Controls.Add(btnBookingCreate);
            bookingButtons.Controls.Add(btnBookingClear);

            stayLayout.Controls.Add(bookingButtons, 0, 5);
            stayLayout.SetColumnSpan(bookingButtons, 4);

            stayGroup.Controls.Add(stayLayout);

            root.Controls.Add(guestGroup, 0, 0);
            root.Controls.Add(stayGroup, 1, 0);

            var infoPanel = new Panel { Dock = DockStyle.Fill };
            var infoLabel = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = false,
                Font = new Font(Font, FontStyle.Italic),
                Text = "Capture guest details and stay information, then confirm to reserve a room."
            };
            infoPanel.Controls.Add(infoLabel);
            root.Controls.Add(infoPanel, 0, 1);
            root.SetColumnSpan(infoPanel, 2);

            page.Controls.Add(root);
            return page;
        }

        private TabPage CreateManageTab()
        {
            var page = new TabPage("Manage Bookings");

            var manageLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(12)
            };
            manageLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 54));
            manageLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 46));
            manageLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var leftPanel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
            leftPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            leftPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            btnManageRefresh = new Button { Text = "Refresh", AutoSize = true, Margin = new Padding(0, 0, 0, 8) };
            btnManageRefresh.Click += async (s, e) => await LoadReservationsAsync();
            leftPanel.Controls.Add(btnManageRefresh, 0, 0);

            gridReservations = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoGenerateColumns = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false
            };
            gridReservations.Columns.Add(CreateTextColumn("Reference", nameof(ReservationRecord.ReservationId), 80));
            gridReservations.Columns.Add(CreateTextColumn("Guest", nameof(ReservationRecord.GuestName), 130));
            gridReservations.Columns.Add(CreateTextColumn("Room", nameof(ReservationRecord.RoomNumber), 40));
            gridReservations.Columns.Add(CreateTextColumn("Check-in", nameof(ReservationRecord.CheckIn), 90, "d"));
            gridReservations.Columns.Add(CreateTextColumn("Check-out", nameof(ReservationRecord.CheckOut), 90, "d"));
            gridReservations.Columns.Add(CreateTextColumn("Status", nameof(ReservationRecord.Status), 90));
            gridReservations.Columns.Add(CreateTextColumn("Total", nameof(ReservationRecord.TotalCost), 90, "C2"));
            gridReservations.SelectionChanged += async (s, e) => await HandleReservationSelectionChangedAsync();

            leftPanel.Controls.Add(gridReservations, 0, 1);

            manageLayout.Controls.Add(leftPanel, 0, 0);

            var rightPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5,
                Padding = new Padding(0, 0, 0, 12)
            };
            for (var i = 0; i < 5; i++)
            {
                rightPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            }

            var rightScrollPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true
            };

            var referencePanel = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
            referencePanel.Controls.Add(new Label { Text = "Reservation #", AutoSize = true, Margin = new Padding(0, 6, 6, 3) });
            lblManageReference = new Label { Text = "-", AutoSize = true, Font = new Font(Font, FontStyle.Bold) };
            referencePanel.Controls.Add(lblManageReference);
            rightPanel.Controls.Add(referencePanel);

            var manageGuestGroup = new GroupBox { Text = "Guest", Dock = DockStyle.Top, Padding = new Padding(12), AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
            var manageGuestLayout = CreateFormLayout(10);
            txtManageFirstName = CreateTextBox();
            txtManageLastName = CreateTextBox();
            txtManagePhone = CreateTextBox();
            txtManageEmail = CreateTextBox();
            txtManageAddress = CreateTextBox(multiline: true);
            txtManagePostal = CreateTextBox();
            txtManageCardNumber = CreateTextBox();
            txtManageBankId = CreateTextBox();
            nudManageExpiryMonth = new NumericUpDown { Minimum = 1, Maximum = 12, Width = 60, Value = DateTime.Today.Month };
            nudManageExpiryYear = new NumericUpDown { Minimum = DateTime.Today.Year, Maximum = DateTime.Today.Year + 20, Width = 80, Value = DateTime.Today.Year };
            txtManageSecurityCode = CreateTextBox();
            AddLabeledControl(manageGuestLayout, "First Name", txtManageFirstName, 0);
            AddLabeledControl(manageGuestLayout, "Last Name", txtManageLastName, 1);
            AddLabeledControl(manageGuestLayout, "Phone", txtManagePhone, 2);
            AddLabeledControl(manageGuestLayout, "Email", txtManageEmail, 3);
            AddLabeledControl(manageGuestLayout, "Address", txtManageAddress, 4);
            AddLabeledControl(manageGuestLayout, "Postal Code", txtManagePostal, 5);
            AddLabeledControl(manageGuestLayout, "Card Number", txtManageCardNumber, 6);
            AddLabeledControl(manageGuestLayout, "Bank", txtManageBankId, 7);

            var manageExpiryPanel = CreateFlowPanel();
            manageExpiryPanel.Controls.Add(new Label { Text = "Month", AutoSize = true, Margin = new Padding(0, 6, 6, 3) });
            manageExpiryPanel.Controls.Add(nudManageExpiryMonth);
            manageExpiryPanel.Controls.Add(new Label { Text = "Year", AutoSize = true, Margin = new Padding(12, 6, 6, 3) });
            manageExpiryPanel.Controls.Add(nudManageExpiryYear);
            AddLabeledControl(manageGuestLayout, "Expiry", manageExpiryPanel, 8);

            AddLabeledControl(manageGuestLayout, "Security Code", txtManageSecurityCode, 9);
            manageGuestGroup.Controls.Add(manageGuestLayout);
            rightPanel.Controls.Add(manageGuestGroup);

            var manageStayGroup = new GroupBox { Text = "Stay", Dock = DockStyle.Top, Padding = new Padding(12), AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
            var manageStayLayout = CreateFormLayout(5);
            dtpManageCheckIn = new DateTimePicker { Format = DateTimePickerFormat.Short };
            dtpManageCheckOut = new DateTimePicker { Format = DateTimePickerFormat.Short };
            dtpManageCheckIn.ValueChanged += async (s, e) => await RefreshManageAvailableRoomsAsync();
            dtpManageCheckOut.ValueChanged += async (s, e) => await RefreshManageAvailableRoomsAsync();
            lblManageSeasonInfo = new Label
            {
                Text = "Season: -",
                AutoSize = true,
                Font = new Font(Font, FontStyle.Bold)
            };
            lblManageRoom = new Label { Text = "Room: -", AutoSize = true, Font = new Font(Font, FontStyle.Bold) };
            nudManageAdults = new NumericUpDown { Minimum = 1, Maximum = 4, Value = 2 };
            nudManageChildrenUnder5 = new NumericUpDown { Minimum = 0, Maximum = 4 };
            nudManageChildrenUnder16 = new NumericUpDown { Minimum = 0, Maximum = 4 };
            nudManageAdults.ValueChanged += (s, e) => HandleManageGuestCountChanged(nudManageAdults);
            nudManageChildrenUnder5.ValueChanged += (s, e) => HandleManageGuestCountChanged(nudManageChildrenUnder5);
            nudManageChildrenUnder16.ValueChanged += (s, e) => HandleManageGuestCountChanged(nudManageChildrenUnder16);
            AddLabeledControl(manageStayLayout, "Check-in", dtpManageCheckIn, 0);
            AddLabeledControl(manageStayLayout, "Check-out", dtpManageCheckOut, 1);
            AddLabeledControl(manageStayLayout, "Season", lblManageSeasonInfo, 2);
            AddLabeledControl(manageStayLayout, "Assigned", lblManageRoom, 3);
            var manageGuestPanel = CreateFlowPanel();
            manageGuestPanel.Controls.Add(new Label { Text = "Adults", AutoSize = true, Margin = new Padding(0, 6, 6, 3) });
            manageGuestPanel.Controls.Add(nudManageAdults);
            manageGuestPanel.Controls.Add(new Label { Text = "Under 5", AutoSize = true, Margin = new Padding(12, 6, 6, 3) });
            manageGuestPanel.Controls.Add(nudManageChildrenUnder5);
            manageGuestPanel.Controls.Add(new Label { Text = "5-16", AutoSize = true, Margin = new Padding(12, 6, 6, 3) });
            manageGuestPanel.Controls.Add(nudManageChildrenUnder16);
            AddLabeledControl(manageStayLayout, "Guests", manageGuestPanel, 4);
            manageStayLayout.SetColumnSpan(manageGuestPanel, 3);
            manageStayGroup.Controls.Add(manageStayLayout);
            rightPanel.Controls.Add(manageStayGroup);

            var manageSummary = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 2,
                RowCount = 2
            };
            manageSummary.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
            manageSummary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            manageSummary.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            manageSummary.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            lblManageTotal = new Label { Text = "Total: R0.00", AutoSize = true, Font = new Font(Font, FontStyle.Bold) };
            lblManageDeposit = new Label { Text = "Deposit (due by -): R0.00", AutoSize = true };
            manageSummary.Controls.Add(new Label { Text = "Total", AutoSize = true, Margin = new Padding(0, 6, 3, 3) }, 0, 0);
            manageSummary.Controls.Add(lblManageTotal, 1, 0);
            manageSummary.Controls.Add(new Label { Text = "Deposit", AutoSize = true, Margin = new Padding(0, 6, 3, 3) }, 0, 1);
            manageSummary.Controls.Add(lblManageDeposit, 1, 1);
            rightPanel.Controls.Add(manageSummary);

            var manageButtons = CreateFlowPanel();
            manageButtons.FlowDirection = FlowDirection.RightToLeft;
            btnManageUpdate = new Button { Text = "Save Changes", AutoSize = true };
            btnManageUpdate.Click += async (s, e) => await UpdateReservationAsync();
            btnManageCancel = new Button { Text = "Cancel Booking", AutoSize = true };
            btnManageCancel.Click += async (s, e) => await CancelReservationAsync();
            manageButtons.Controls.Add(btnManageUpdate);
            manageButtons.Controls.Add(btnManageCancel);
            rightPanel.Controls.Add(manageButtons);

            rightScrollPanel.Controls.Add(rightPanel);
            manageLayout.Controls.Add(rightScrollPanel, 1, 0);

            page.Controls.Add(manageLayout);
            return page;
        }

        private TabPage CreateEnquiryTab()
        {
            var page = new TabPage("Booking Enquiry");

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 3,
                Padding = new Padding(12)
            };
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var searchPanel = CreateFlowPanel();
            searchPanel.Controls.Add(new Label { Text = "Search", AutoSize = true, Margin = new Padding(0, 6, 6, 3) });
            txtEnquirySearch = CreateTextBox();
            txtEnquirySearch.Width = 240;
            txtEnquirySearch.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    ApplyEnquiryFilter();
                }
            };
            btnEnquirySearch = new Button { Text = "Find", AutoSize = true };
            btnEnquirySearch.Click += (s, e) => ApplyEnquiryFilter();
            btnEnquiryReset = new Button { Text = "Reset", AutoSize = true };
            btnEnquiryReset.Click += (s, e) => ResetEnquiryFilter();
            searchPanel.Controls.Add(txtEnquirySearch);
            searchPanel.Controls.Add(btnEnquirySearch);
            searchPanel.Controls.Add(btnEnquiryReset);
            layout.Controls.Add(searchPanel, 0, 0);

            gridEnquiry = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoGenerateColumns = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            gridEnquiry.Columns.Add(CreateTextColumn("Reference", nameof(ReservationRecord.ReservationId), 80));
            gridEnquiry.Columns.Add(CreateTextColumn("Guest", nameof(ReservationRecord.GuestName), 160));
            gridEnquiry.Columns.Add(CreateTextColumn("Room", nameof(ReservationRecord.RoomNumber), 60));
            gridEnquiry.Columns.Add(CreateTextColumn("Check-in", nameof(ReservationRecord.CheckIn), 100, "g"));
            gridEnquiry.Columns.Add(CreateTextColumn("Check-out", nameof(ReservationRecord.CheckOut), 100, "g"));
            gridEnquiry.Columns.Add(CreateTextColumn("Status", nameof(ReservationRecord.Status), 100));
            gridEnquiry.Columns.Add(CreateTextColumn("Deposit", nameof(ReservationRecord.DepositAmount), 90, "C2"));
            layout.Controls.Add(gridEnquiry, 0, 1);

            lblEnquirySummary = new Label
            {
                AutoSize = true,
                Text = "No enquiry results yet.",
                Font = new Font(Font, FontStyle.Italic)
            };
            layout.Controls.Add(lblEnquirySummary, 0, 2);

            page.Controls.Add(layout);
            return page;
        }

        private TabPage CreateReportsTab()
        {
            var page = new TabPage("Reports");

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 3,
                Padding = new Padding(12)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var reportHeader = CreateFlowPanel();
            reportHeader.Controls.Add(new Label { Text = "From", AutoSize = true, Margin = new Padding(0, 6, 6, 3) });
            dtpReportFrom = new DateTimePicker { Format = DateTimePickerFormat.Short };
            reportHeader.Controls.Add(dtpReportFrom);
            reportHeader.Controls.Add(new Label { Text = "To", AutoSize = true, Margin = new Padding(12, 6, 6, 3) });
            dtpReportTo = new DateTimePicker { Format = DateTimePickerFormat.Short };
            reportHeader.Controls.Add(dtpReportTo);
            btnRunOccupancy = new Button { Text = "Occupancy", AutoSize = true, Margin = new Padding(12, 0, 0, 0) };
            btnRunOccupancy.Click += async (s, e) => await RunOccupancyReportAsync();
            btnRunRevenue = new Button { Text = "Revenue", AutoSize = true, Margin = new Padding(6, 0, 0, 0) };
            btnRunRevenue.Click += async (s, e) => await RunRevenueReportAsync();
            reportHeader.Controls.Add(btnRunOccupancy);
            reportHeader.Controls.Add(btnRunRevenue);
            layout.Controls.Add(reportHeader, 0, 0);
            layout.SetColumnSpan(reportHeader, 2);

            gridOccupancy = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoGenerateColumns = false
            };
            gridOccupancy.Columns.Add(CreateTextColumn("Date", nameof(OccupancySnapshot.Date), 120, "d"));
            gridOccupancy.Columns.Add(CreateTextColumn("Rooms Occupied", nameof(OccupancySnapshot.OccupiedRooms), 120));
            gridOccupancy.Columns.Add(CreateTextColumn("Rooms Available", nameof(OccupancySnapshot.AvailableRooms), 120));
            gridOccupancy.Columns.Add(CreateTextColumn("Occupancy %", nameof(OccupancySnapshot.OccupancyPercentage), 120, "P1"));

            gridRevenue = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoGenerateColumns = false
            };
            gridRevenue.Columns.Add(CreateTextColumn("Date", nameof(RevenueSummary.Date), 120, "d"));
            gridRevenue.Columns.Add(CreateTextColumn("Reservations", nameof(RevenueSummary.Reservations), 120));
            gridRevenue.Columns.Add(CreateTextColumn("Revenue", nameof(RevenueSummary.TotalRevenue), 120, "C2"));

            layout.Controls.Add(gridOccupancy, 0, 1);
            layout.Controls.Add(gridRevenue, 1, 1);

            lblOccupancySummary = new Label { Text = "Run the occupancy report to view hotel utilisation.", AutoSize = true };
            lblRevenueSummary = new Label { Text = "Run the revenue report to view income trends.", AutoSize = true };

            var footer = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            footer.Controls.Add(lblOccupancySummary, 0, 0);
            footer.Controls.Add(lblRevenueSummary, 1, 0);
            layout.Controls.Add(footer, 0, 2);
            layout.SetColumnSpan(footer, 2);

            page.Controls.Add(layout);
            return page;
        }

        private TableLayoutPanel CreateFormLayout(int rows)
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = rows,
                AutoSize = true
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            for (var i = 0; i < rows; i++)
            {
                layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            }

            return layout;
        }

        private static TextBox CreateTextBox(bool multiline = false)
        {
            var box = new TextBox
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(3)
            };

            if (multiline)
            {
                box.Multiline = true;
                box.Height = 60;
                box.ScrollBars = ScrollBars.Vertical;
            }

            return box;
        }

        private FlowLayoutPanel CreateFlowPanel()
        {
            return new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };
        }

        private static void SetNumericValue(NumericUpDown control, int value, int fallback)
        {
            var target = value > 0 ? value : fallback;
            var decimalTarget = (decimal)target;
            if (decimalTarget < control.Minimum)
            {
                decimalTarget = control.Minimum;
            }
            if (decimalTarget > control.Maximum)
            {
                decimalTarget = control.Maximum;
            }

            control.Value = decimalTarget;
        }

        private void AddLabeledControl(TableLayoutPanel layout, string labelText, Control control, int row, int column = 0)
        {
            var label = new Label
            {
                Text = labelText,
                AutoSize = true,
                Margin = new Padding(0, 6, 6, 3)
            };

            layout.Controls.Add(label, column, row);
            control.Margin = new Padding(3);
            layout.Controls.Add(control, column + 1, row);
        }

        private DataGridViewTextBoxColumn CreateTextColumn(string header, string dataProperty, int width, string? format = null)
        {
            var column = new DataGridViewTextBoxColumn
            {
                HeaderText = header,
                DataPropertyName = dataProperty,
                Width = width,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None
            };

            if (!string.IsNullOrWhiteSpace(format))
            {
                column.DefaultCellStyle.Format = format;
            }

            return column;
        }

        #endregion

        #region Data Loading

        private async Task LoadInitialDataAsync()
        {
            SetBusyState(true);
            try
            {
                await databaseInitializer.EnsurePreparedAsync();
                await LoadReferenceDataAsync();
                ClearNewBookingForm();
                await LoadReservationsAsync();
                await RefreshAvailableRoomsForBookingAsync();
                dtpReportFrom.Value = DateTime.Today;
                dtpReportTo.Value = DateTime.Today.AddDays(7);
                ResetEnquiryFilter();
                UpdateReportSummaries();
            }
            catch (Exception ex)
            {
                ShowError("Failed to load initial data.", ex);
            }
            finally
            {
                SetBusyState(false);
            }
        }

        private async Task LoadReferenceDataAsync()
        {
            try
            {
                guestCache = (await guestService.GetGuestsAsync()).OrderBy(g => g.FullName).ToList();
                PopulateGuestPicker();
            }
            catch (Exception ex)
            {
                ShowError("Unable to load lookups.", ex);
            }
        }

        private void PopulateGuestPicker()
        {
            var items = new List<GuestProfile>
            {
                new GuestProfile { GuestId = 0, FirstName = "-- New Guest --" }
            };
            items.AddRange(guestCache);

            cmbGuestPicker.DataSource = items;
        }

        private async Task LoadReservationsAsync()
        {
            try
            {
                reservationBinding.Clear();
                var reservations = await reservationService.GetReservationsAsync();
                foreach (var record in reservations.OrderByDescending(r => r.CheckIn))
                {
                    reservationBinding.Add(record);
                }
                reservationBindingSource.ResetBindings(false);
                if (gridReservations.Rows.Count > 0)
                {
                    gridReservations.ClearSelection();
                }
                ClearManageForm();
                enquiryBinding.Clear();
                foreach (var record in reservationBinding)
                {
                    enquiryBinding.Add(record);
                }
                enquiryBindingSource.ResetBindings(false);
                lblEnquirySummary.Text = $"Loaded {enquiryBinding.Count} reservations.";
            }
            catch (Exception ex)
            {
                ShowError("Unable to load reservations.", ex);
            }
        }

        #endregion

        #region Booking Workflow

        private async Task RefreshAvailableRoomsForBookingAsync(bool datesMiss = false)
        {
            try
            {
                if (dtpBookingCheckOut.Value.Date <= dtpBookingCheckIn.Value.Date)
                {
                    lblBookingAvailability.Text = "Check-out must be after check-in.";
                    availableRoomsForCreation = new List<RoomInfo>();
                    lblBookingAssignedRoom.Text = "Room: -";

                    if (datesMiss)
                    {
                        MessageBox.Show(
                            "Check-out must be after check-in.",
                            "Validation",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                    }

                    return;
                }

                var checkInDate = dtpBookingCheckIn.Value.Date;
                var checkOutDate = dtpBookingCheckOut.Value.Date;

                var availableTask = reservationService.GetAvailableRoomsAsync(checkInDate, checkOutDate);
                var allRoomsTask = reservationService.GetAllRoomsAsync();
                await Task.WhenAll(availableTask, allRoomsTask);

                availableRoomsForCreation = (await availableTask)
                    .OrderBy(r => r.RoomNumber)
                    .ToList();

                var allRooms = (await allRoomsTask).OrderBy(r => r.RoomNumber).ToList();
                var totalRooms = allRooms.Count;

                if (totalRooms == 0)
                {
                    lblBookingAvailability.Text = "No rooms are configured.";
                    lblBookingAssignedRoom.Text = "Room: -";
                }
                else
                {
                    var availableCount = availableRoomsForCreation.Count;
                    var occupiedCount = Math.Max(0, totalRooms - availableCount);

                    if (availableCount == 0)
                    {
                        lblBookingAvailability.Text = $"0 of {totalRooms} rooms available ({occupiedCount} occupied).";
                        lblBookingAssignedRoom.Text = "Room: -";
                    }
                    else
                    {
                        lblBookingAvailability.Text = $"{availableCount} of {totalRooms} rooms available ({occupiedCount} occupied).";
                        var firstRoom = availableRoomsForCreation.First();
                        lblBookingAssignedRoom.Text = $"Room: {firstRoom.RoomNumber}";
                    }
                }

                UpdateBookingSummary();

                if (datesMiss)
                {
                    MessageBox.Show(lblBookingAvailability.Text, "Availability", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                ShowError("Unable to check availability.", ex);
            }
        }

        private void UpdateBookingSummary()
        {
            if (dtpBookingCheckOut.Value.Date <= dtpBookingCheckIn.Value.Date)
            {
                ResetPricingSummary(lblBookingTotal, lblBookingDeposit, lblBookingSeasonInfo);
                return;
            }

            try
            {
                GetBookingGuestCounts(out var adults, out var under5, out var under16);
                var quote = reservationService.GetPricingQuote(
                    dtpBookingCheckIn.Value.Date,
                    dtpBookingCheckOut.Value.Date,
                    adults,
                    under5,
                    under16);

                ApplyPricingSummary(quote, lblBookingTotal, lblBookingDeposit, lblBookingSeasonInfo);
            }
            catch (Exception ex)
            {
                lblBookingAvailability.Text = ex.Message;
                ResetPricingSummary(lblBookingTotal, lblBookingDeposit, lblBookingSeasonInfo);
            }
        }

        private void HandleBookingGuestCountChanged(NumericUpDown changedControl)
        {
            AdjustGuestCounts(nudBookingAdults, nudBookingChildrenUnder5, nudBookingChildrenUnder16, changedControl);
            UpdateBookingSummary();
        }

        private void AdjustGuestCounts(NumericUpDown adultsControl, NumericUpDown underFiveControl, NumericUpDown underSixteenControl, NumericUpDown changedControl)
        {
            if (suppressGuestValueEvents)
            {
                return;
            }

            try
            {
                suppressGuestValueEvents = true;
                var total = adultsControl.Value + underFiveControl.Value + underSixteenControl.Value;
                if (total > 4)
                {
                    var overflow = total - 4;
                    var newValue = Math.Max(changedControl.Minimum, changedControl.Value - overflow);
                    changedControl.Value = newValue;
                }
            }
            finally
            {
                suppressGuestValueEvents = false;
            }
        }

        private void GetBookingGuestCounts(out int adults, out int underFive, out int underSixteen)
        {
            adults = (int)nudBookingAdults.Value;
            underFive = (int)nudBookingChildrenUnder5.Value;
            underSixteen = (int)nudBookingChildrenUnder16.Value;
        }

        private void ApplySelectedGuest()
        {
            if (cmbGuestPicker.SelectedItem is GuestProfile profile && profile.GuestId > 0)
            {
                txtGuestFirstName.Text = profile.FirstName;
                txtGuestLastName.Text = profile.LastName;
                txtGuestPhone.Text = profile.PhoneNumber;
                txtGuestEmail.Text = profile.Email;
                txtGuestAddress.Text = profile.Address;
                txtGuestPostal.Text = profile.PostalCode;
                txtGuestCardNumber.Text = profile.BankCard?.CardNumber ?? string.Empty;
                txtGuestBankId.Text = profile.BankCard?.BankId ?? string.Empty;
                SetNumericValue(nudGuestExpiryMonth, profile.BankCard?.ExpirationMonth ?? 0, DateTime.Today.Month);
                SetNumericValue(nudGuestExpiryYear, profile.BankCard?.ExpirationYear ?? 0, DateTime.Today.Year);
                txtGuestSecurityCode.Text = profile.BankCard?.SecurityCode ?? string.Empty;
            }
            else
            {
                txtGuestFirstName.Clear();
                txtGuestLastName.Clear();
                txtGuestPhone.Clear();
                txtGuestEmail.Clear();
                txtGuestAddress.Clear();
                txtGuestPostal.Clear();
                txtGuestCardNumber.Clear();
                txtGuestBankId.Clear();
                SetNumericValue(nudGuestExpiryMonth, 0, DateTime.Today.Month);
                SetNumericValue(nudGuestExpiryYear, 0, DateTime.Today.Year);
                txtGuestSecurityCode.Clear();
            }
        }

        private async Task CreateBookingAsync()
        {
            await RefreshAvailableRoomsForBookingAsync();
            if (!ValidateBookingInput())
            {
                return;
            }

            SetBusyState(true);
            try
            {
                var guest = BuildGuestFromInputs();

                GetBookingGuestCounts(out var adults, out var under5, out var under16);
                var reservation = await reservationService.CreateReservationAsync(
                    guest,
                    dtpBookingCheckIn.Value.Date,
                    dtpBookingCheckOut.Value.Date,
                    adults,
                    under5,
                    under16);

                await LoadReferenceDataAsync();
                await LoadReservationsAsync();
                SelectReservationById(reservation.ReservationId);
                ClearNewBookingForm();

                MessageBox.Show(
                    $"Booking created. Reference #{reservation.ReservationId}.\nSeason: {reservation.SeasonDescription}\nDeposit due by {reservation.DepositDueDate:dd MMM yyyy}.",
                    "Success",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                ShowError("Unable to create booking.", ex);
            }
            finally
            {
                SetBusyState(false);
            }
        }

        private bool ValidateBookingInput()
        {
            if (string.IsNullOrWhiteSpace(txtGuestFirstName.Text) || string.IsNullOrWhiteSpace(txtGuestLastName.Text))
            {
                MessageBox.Show("Guest first and last names are required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (dtpBookingCheckOut.Value.Date <= dtpBookingCheckIn.Value.Date)
            {
                MessageBox.Show("Check-out must be after check-in.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (availableRoomsForCreation.Count == 0)
            {
                MessageBox.Show("No rooms are available for the selected dates.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            GetBookingGuestCounts(out var adults, out var under5, out var under16);
            var totalGuests = adults + under5 + under16;
            if (totalGuests == 0)
            {
                MessageBox.Show("At least one guest must be specified.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (totalGuests > 4)
            {
                MessageBox.Show("No more than four guests may share a room.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            return true;
        }

        private GuestProfile BuildGuestFromInputs()
        {
            var guest = cmbGuestPicker.SelectedItem as GuestProfile;
            if (guest == null || guest.GuestId == 0)
            {
                guest = new GuestProfile();
            }
            else
            {
                guest = guest.Clone();
            }

            guest.FirstName = txtGuestFirstName.Text.Trim();
            guest.LastName = txtGuestLastName.Text.Trim();
            guest.PhoneNumber = txtGuestPhone.Text.Trim();
            guest.Email = txtGuestEmail.Text.Trim();
            guest.Address = txtGuestAddress.Text.Trim();
            guest.PostalCode = txtGuestPostal.Text.Trim();
            guest.BankCard.CardNumber = txtGuestCardNumber.Text.Trim();
            guest.BankCard.BankId = txtGuestBankId.Text.Trim().ToUpperInvariant();
            guest.BankCard.ExpirationMonth = (int)nudGuestExpiryMonth.Value;
            guest.BankCard.ExpirationYear = (int)nudGuestExpiryYear.Value;
            guest.BankCard.SecurityCode = txtGuestSecurityCode.Text.Trim();

            return guest;
        }

        private void ClearNewBookingForm()
        {
            cmbGuestPicker.SelectedIndex = 0;
            ApplySelectedGuest();
            suppressGuestValueEvents = true;
            try
            {
                nudBookingAdults.Value = Math.Min(2, nudBookingAdults.Maximum);
                nudBookingChildrenUnder5.Value = 0;
                nudBookingChildrenUnder16.Value = 0;
            }
            finally
            {
                suppressGuestValueEvents = false;
            }
            dtpBookingCheckIn.Value = DateTime.Today;
            dtpBookingCheckOut.Value = DateTime.Today.AddDays(1);
            lblBookingAvailability.Text = string.Empty;
            lblBookingAssignedRoom.Text = "Room: -";
            txtGuestCardNumber.Clear();
            txtGuestBankId.Clear();
            txtGuestSecurityCode.Clear();
            SetNumericValue(nudGuestExpiryMonth, 0, DateTime.Today.Month);
            SetNumericValue(nudGuestExpiryYear, 0, DateTime.Today.Year);
            ResetPricingSummary(lblBookingTotal, lblBookingDeposit, lblBookingSeasonInfo);
            UpdateBookingSummary();
        }

        #endregion

        #region Reservation Management Workflow

        private async Task HandleReservationSelectionChangedAsync()
        {
            if (gridReservations.SelectedRows.Count == 0)
            {
                ClearManageForm();
                return;
            }

            if (gridReservations.SelectedRows[0].DataBoundItem is ReservationRecord reservation)
            {
                await LoadReservationDetailsAsync(reservation);
            }
        }

        private async Task LoadReservationDetailsAsync(ReservationRecord reservation)
        {
            SetBusyState(true);
            try
            {
                activeReservation = reservation;
                lblManageReference.Text = reservation.ReservationId.ToString();

                activeReservationGuest = await guestService.GetGuestAsync(reservation.GuestId)
                    ?? new GuestProfile { GuestId = reservation.GuestId, FirstName = reservation.GuestName };

                ApplyReservationToForm();
                await RefreshManageAvailableRoomsAsync();
                UpdateManageSummary();
            }
            catch (Exception ex)
            {
                ShowError("Unable to load reservation details.", ex);
            }
            finally
            {
                SetBusyState(false);
            }
        }

        private void ApplyReservationToForm()
        {
            if (activeReservation == null)
            {
                ClearManageForm();
                return;
            }

            var guest = activeReservationGuest ?? new GuestProfile();
            txtManageFirstName.Text = guest.FirstName;
            txtManageLastName.Text = guest.LastName;
            txtManagePhone.Text = guest.PhoneNumber;
            txtManageEmail.Text = guest.Email;
            txtManageAddress.Text = guest.Address;
            txtManagePostal.Text = guest.PostalCode;
            txtManageCardNumber.Text = guest.BankCard?.CardNumber ?? string.Empty;
            txtManageBankId.Text = guest.BankCard?.BankId ?? string.Empty;
            txtManageSecurityCode.Text = guest.BankCard?.SecurityCode ?? string.Empty;
            SetNumericValue(nudManageExpiryMonth, guest.BankCard?.ExpirationMonth ?? 0, DateTime.Today.Month);
            SetNumericValue(nudManageExpiryYear, guest.BankCard?.ExpirationYear ?? 0, DateTime.Today.Year);

            dtpManageCheckIn.Value = activeReservation.CheckIn;
            dtpManageCheckOut.Value = activeReservation.CheckOut;

            suppressGuestValueEvents = true;
            try
            {
                nudManageAdults.Value = Math.Max(nudManageAdults.Minimum, Math.Min(nudManageAdults.Maximum, activeReservation.Adults > 0 ? activeReservation.Adults : 1));
                nudManageChildrenUnder5.Value = Math.Max(nudManageChildrenUnder5.Minimum, Math.Min(nudManageChildrenUnder5.Maximum, activeReservation.ChildrenUnderFive));
                nudManageChildrenUnder16.Value = Math.Max(nudManageChildrenUnder16.Minimum, Math.Min(nudManageChildrenUnder16.Maximum, activeReservation.ChildrenFiveToSixteen));
            }
            finally
            {
                suppressGuestValueEvents = false;
            }

            lblManageRoom.Text = $"Room: {activeReservation.RoomNumber}";
        }

        private async Task<bool> RefreshManageAvailableRoomsAsync(bool ensureRoomAssignment = false)
        {
            if (activeReservation == null)
            {
                return false;
            }

            try
            {
                if (ensureRoomAssignment)
                {
                    var rooms = await reservationService.GetAvailableRoomsAsync(
                        dtpManageCheckIn.Value.Date,
                        dtpManageCheckOut.Value.Date,
                        activeReservation.ReservationId);

                    var roomList = rooms.OrderBy(r => r.RoomNumber).ToList();
                    if (!roomList.Any(r => r.RoomNumber == activeReservation.RoomNumber))
                    {
                        var reassigned = roomList.FirstOrDefault()
                            ?? await reservationService.AllocateRoomAsync(
                                dtpManageCheckIn.Value.Date,
                                dtpManageCheckOut.Value.Date,
                                activeReservation.ReservationId);

                        if (reassigned == null)
                        {
                            activeReservation.RoomNumber = 0;
                            lblManageRoom.Text = "Room: -";
                            MessageBox.Show("No rooms are available for the selected dates.", "Availability", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return false;
                        }

                        activeReservation.RoomNumber = reassigned.RoomNumber;
                        activeReservation.RoomType = reassigned.DisplayName;
                    }
                }

                lblManageRoom.Text = $"Room: {activeReservation.RoomNumber}";
                UpdateManageSummary();
                return true;
            }
            catch (Exception ex)
            {
                ShowError("Unable to refresh available rooms for the reservation.", ex);
                return false;
            }
        }

        private void UpdateManageSummary()
        {
            if (activeReservation == null)
            {
                ResetPricingSummary(lblManageTotal, lblManageDeposit, lblManageSeasonInfo);
                return;
            }

            if (dtpManageCheckOut.Value.Date <= dtpManageCheckIn.Value.Date)
            {
                ResetPricingSummary(lblManageTotal, lblManageDeposit, lblManageSeasonInfo);
                return;
            }

            try
            {
                GetManageGuestCounts(out var adults, out var under5, out var under16);
                var quote = reservationService.GetPricingQuote(
                    dtpManageCheckIn.Value.Date,
                    dtpManageCheckOut.Value.Date,
                    adults,
                    under5,
                    under16);

                ApplyPricingSummary(quote, lblManageTotal, lblManageDeposit, lblManageSeasonInfo);
            }
            catch (Exception)
            {
                ResetPricingSummary(lblManageTotal, lblManageDeposit, lblManageSeasonInfo);
            }
        }

        private void ApplyPricingSummary(PricingQuote quote, Label totalLabel, Label depositLabel, Label seasonLabel)
        {
            totalLabel.Text = $"Total: {quote.Total:C2}";

            var today = DateTime.Today;
            var isOverdue = quote.DepositDueDate < today;
            var depositText = $"Deposit (due by {quote.DepositDueDate:dd MMM yyyy}): {quote.Deposit:C2}";
            if (isOverdue)
            {
                depositText += " (Overdue)";
            }

            depositLabel.Text = depositText;
            depositLabel.ForeColor = isOverdue ? Color.Firebrick : SystemColors.ControlText;

            seasonLabel.Text = $"Season: {quote.SeasonDescription}";
            seasonLabel.ForeColor = DetermineSeasonColor(quote);
        }

        private static Color DetermineSeasonColor(PricingQuote quote)
        {
            if (quote.IsMixedSeason)
            {
                return Color.DarkOrange;
            }

            return quote.PrimarySeason switch
            {
                SeasonCategory.Low => Color.SeaGreen,
                SeasonCategory.Mid => Color.SteelBlue,
                SeasonCategory.High => Color.Firebrick,
                _ => SystemColors.ControlText
            };
        }

        private static void ResetPricingSummary(Label totalLabel, Label depositLabel, Label seasonLabel)
        {
            totalLabel.Text = "Total: R0.00";
            depositLabel.Text = "Deposit (due by -): R0.00";
            depositLabel.ForeColor = SystemColors.ControlText;
            seasonLabel.Text = "Season: -";
            seasonLabel.ForeColor = SystemColors.ControlText;
        }

        private void HandleManageGuestCountChanged(NumericUpDown changedControl)
        {
            AdjustGuestCounts(nudManageAdults, nudManageChildrenUnder5, nudManageChildrenUnder16, changedControl);
            UpdateManageSummary();
        }

        private void GetManageGuestCounts(out int adults, out int underFive, out int underSixteen)
        {
            adults = (int)nudManageAdults.Value;
            underFive = (int)nudManageChildrenUnder5.Value;
            underSixteen = (int)nudManageChildrenUnder16.Value;
        }

        private async Task UpdateReservationAsync()
        {
            if (activeReservation == null)
            {
                MessageBox.Show("Select a reservation to update.", "Update", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (dtpManageCheckOut.Value.Date <= dtpManageCheckIn.Value.Date)
            {
                MessageBox.Show("Check-out must be after check-in.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SetBusyState(true);
            try
            {
                if (!await RefreshManageAvailableRoomsAsync(true))
                {
                    return;
                }

                var guest = activeReservationGuest ?? new GuestProfile { GuestId = activeReservation.GuestId };
                guest.FirstName = txtManageFirstName.Text.Trim();
                guest.LastName = txtManageLastName.Text.Trim();
                guest.PhoneNumber = txtManagePhone.Text.Trim();
                guest.Email = txtManageEmail.Text.Trim();
                guest.Address = txtManageAddress.Text.Trim();
                guest.PostalCode = txtManagePostal.Text.Trim();
                guest.BankCard.CardNumber = txtManageCardNumber.Text.Trim();
                guest.BankCard.BankId = txtManageBankId.Text.Trim().ToUpperInvariant();
                guest.BankCard.ExpirationMonth = (int)nudManageExpiryMonth.Value;
                guest.BankCard.ExpirationYear = (int)nudManageExpiryYear.Value;
                guest.BankCard.SecurityCode = txtManageSecurityCode.Text.Trim();

                if (activeReservation.RoomNumber <= 0)
                {
                    MessageBox.Show("No rooms are available for the selected dates.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                GetManageGuestCounts(out var adults, out var under5, out var under16);

                activeReservation.CheckIn = dtpManageCheckIn.Value.Date;
                activeReservation.CheckOut = dtpManageCheckOut.Value.Date;

                var updated = await reservationService.UpdateReservationAsync(
                    activeReservation,
                    guest,
                    adults,
                    under5,
                    under16);

                await LoadReferenceDataAsync();
                await LoadReservationsAsync();
                SelectReservationById(updated.ReservationId);

                MessageBox.Show(
                    $"Reservation updated successfully.\nSeason: {updated.SeasonDescription}\nDeposit due by {updated.DepositDueDate:dd MMM yyyy}.",
                    "Updated",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                ShowError("Unable to update reservation.", ex);
            }
            finally
            {
                SetBusyState(false);
            }
        }

        private async Task CancelReservationAsync()
        {
            if (activeReservation == null)
            {
                MessageBox.Show("Select a reservation to cancel.", "Cancel", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var confirm = MessageBox.Show("Are you sure you want to cancel this reservation?", "Cancel Reservation", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (confirm != DialogResult.Yes)
            {
                return;
            }

            SetBusyState(true);
            try
            {
                await reservationService.CancelReservationAsync(activeReservation.ReservationId);
                await LoadReservationsAsync();
                ClearManageForm();
                MessageBox.Show("Reservation cancelled.", "Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                ShowError("Unable to cancel the reservation.", ex);
            }
            finally
            {
                SetBusyState(false);
            }
        }

        private void ClearManageForm()
        {
            lblManageReference.Text = "-";
            txtManageFirstName.Clear();
            txtManageLastName.Clear();
            txtManagePhone.Clear();
            txtManageEmail.Clear();
            txtManageAddress.Clear();
            txtManagePostal.Clear();
            txtManageCardNumber.Clear();
            txtManageBankId.Clear();
            txtManageSecurityCode.Clear();
            suppressGuestValueEvents = true;
            try
            {
                nudManageAdults.Value = Math.Max(1, nudManageAdults.Minimum);
                nudManageChildrenUnder5.Value = 0;
                nudManageChildrenUnder16.Value = 0;
            }
            finally
            {
                suppressGuestValueEvents = false;
            }
            dtpManageCheckIn.Value = DateTime.Today;
            dtpManageCheckOut.Value = DateTime.Today.AddDays(1);
            SetNumericValue(nudManageExpiryMonth, 0, DateTime.Today.Month);
            SetNumericValue(nudManageExpiryYear, 0, DateTime.Today.Year);
            ResetPricingSummary(lblManageTotal, lblManageDeposit, lblManageSeasonInfo);
            lblManageRoom.Text = "Room: -";
            activeReservation = null;
            activeReservationGuest = null;

        }

        private void SelectReservationById(int reservationId)
        {
            foreach (DataGridViewRow row in gridReservations.Rows)
            {
                if (row.DataBoundItem is ReservationRecord record && record.ReservationId == reservationId)
                {
                    row.Selected = true;
                    gridReservations.FirstDisplayedScrollingRowIndex = row.Index;
                    return;
                }
            }
        }

        #endregion

        #region Enquiry Workflow

        private void ApplyEnquiryFilter()
        {
            var query = txtEnquirySearch.Text.Trim();
            IEnumerable<ReservationRecord> results = reservationBinding;

            if (!string.IsNullOrWhiteSpace(query))
            {
                results = results.Where(r =>
                    r.ReservationId.ToString().IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    r.GuestName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    r.Status.ToString().IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            enquiryBinding.Clear();
            foreach (var record in results)
            {
                enquiryBinding.Add(record);
            }
            enquiryBindingSource.ResetBindings(false);
            lblEnquirySummary.Text = enquiryBinding.Count == 0
                ? "No reservations match the enquiry."
                : $"Found {enquiryBinding.Count} reservation(s).";
        }

        private void ResetEnquiryFilter()
        {
            txtEnquirySearch.Clear();
            enquiryBinding.Clear();
            foreach (var reservation in reservationBinding)
            {
                enquiryBinding.Add(reservation);
            }
            enquiryBindingSource.ResetBindings(false);
            lblEnquirySummary.Text = $"Loaded {enquiryBinding.Count} reservations.";
        }

        #endregion

        #region Reporting Workflow

        private async Task RunOccupancyReportAsync()
        {
            if (!ValidateReportDates())
            {
                return;
            }

            SetBusyState(true);
            try
            {
                occupancyBinding.Clear();
                var report = await reportService.GetOccupancyReportAsync(dtpReportFrom.Value.Date, dtpReportTo.Value.Date);
                foreach (var snapshot in report.OrderBy(r => r.Date))
                {
                    occupancyBinding.Add(snapshot);
                }
                occupancyBindingSource.ResetBindings(false);
                UpdateReportSummaries();
            }
            catch (Exception ex)
            {
                ShowError("Unable to run occupancy report.", ex);
            }
            finally
            {
                SetBusyState(false);
            }
        }

        private async Task RunRevenueReportAsync()
        {
            if (!ValidateReportDates())
            {
                return;
            }

            SetBusyState(true);
            try
            {
                revenueBinding.Clear();
                var report = await reportService.GetRevenueReportAsync(dtpReportFrom.Value.Date, dtpReportTo.Value.Date);
                foreach (var snapshot in report.OrderBy(r => r.Date))
                {
                    revenueBinding.Add(snapshot);
                }
                revenueBindingSource.ResetBindings(false);
                UpdateReportSummaries();
            }
            catch (Exception ex)
            {
                ShowError("Unable to run revenue report.", ex);
            }
            finally
            {
                SetBusyState(false);
            }
        }

        private void UpdateReportSummaries()
        {
            if (occupancyBinding.Count == 0)
            {
                lblOccupancySummary.Text = "Run the occupancy report to view hotel utilisation.";
            }
            else
            {
                var average = occupancyBinding.Average(o => o.OccupancyPercentage);
                lblOccupancySummary.Text = $"Average occupancy: {average:P1} across {occupancyBinding.Count} day(s).";
            }

            if (revenueBinding.Count == 0)
            {
                lblRevenueSummary.Text = "Run the revenue report to view income trends.";
            }
            else
            {
                var total = revenueBinding.Sum(r => r.TotalRevenue);
                lblRevenueSummary.Text = $"Total revenue: {total:C2} across {revenueBinding.Count} day(s).";
            }
        }

        private bool ValidateReportDates()
        {
            if (dtpReportTo.Value.Date < dtpReportFrom.Value.Date)
            {
                MessageBox.Show("The end date must be on or after the start date.", "Report Dates", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            return true;
        }

        #endregion

        #region Utility Methods

        private void SetBusyState(bool busy)
        {
            Cursor = busy ? Cursors.WaitCursor : Cursors.Default;
            tabMain.Enabled = !busy;
        }

        private void ShowError(string message, Exception ex)
        {
            MessageBox.Show($"{message}\n\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        #endregion
    }
}
