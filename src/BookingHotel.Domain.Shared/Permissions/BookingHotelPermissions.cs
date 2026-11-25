namespace BookingHotel.Permissions;

public static class BookingHotelPermissions
{
    public const string GroupName = "BookingHotel";

    public static class Tenants
    {
        public const string Default = GroupName + ".Tenants";
        public const string Create = Default + ".Create";
        public const string Edit = Default + ".Edit";
        public const string Delete = Default + ".Delete";
        public const string Suspend = Default + ".Suspend";
    }

    public static class Hotels
    {
        public const string Default = GroupName + ".Hotels";
        public const string Create = Default + ".Create";
        public const string Edit = Default + ".Edit";
        public const string Delete = Default + ".Delete";
        public const string ManageBranches = Default + ".ManageBranches";
        public const string AssignStaff = Default + ".AssignStaff";
    }

    public static class Rooms
    {
        public const string Default = GroupName + ".Rooms";
        public const string Create = Default + ".Create";
        public const string Edit = Default + ".Edit";
        public const string Delete = Default + ".Delete";
        public const string SetRates = Default + ".SetRates";
        public const string UpdateStatus = Default + ".UpdateStatus";
        public const string ChangeStatus = Default + ".ChangeStatus";
        public const string UpdateHousekeeping = Default + ".UpdateHousekeeping";
        public const string ManageClosures = Default + ".ManageClosures";
        public const string ResolveIncidents = Default + ".ResolveIncidents";
    }

    public static class Bookings
    {
        public const string Default = GroupName + ".Bookings";
        public const string Create = Default + ".Create";
        public const string Confirm = Default + ".Confirm";
        public const string Reject = Default + ".Reject";
        public const string MarkNoShow = Default + ".MarkNoShow";
        public const string AssignRoom = Default + ".AssignRoom";
        public const string CheckIn = Default + ".CheckIn";
        public const string CheckOut = Default + ".CheckOut";
        public const string Cancel = Default + ".Cancel";
        public const string ApproveException = Default + ".ApproveException";
    }

    public static class Guests
    {
        public const string Default = GroupName + ".Guests";
        public const string ManageNotes = Default + ".ManageNotes";
    }

    public static class Reviews
    {
        public const string Default = GroupName + ".Reviews";
        public const string Reply = Default + ".Reply";
        public const string ManageComplaints = Default + ".ManageComplaints";
    }

    public static class Folios
    {
        public const string Default = GroupName + ".Folios";
        public const string View = Default + ".View";
        public const string AddCharge = Default + ".AddCharge";
        public const string Settle = Default + ".Settle";
        public const string Refund = Default + ".Refund";
    }

    public static class Identity
    {
        public const string Default = GroupName + ".Identity";
        public const string UnlockUser = Default + ".UnlockUser";
    }

    public static class RatePlans
    {
        public const string Default = GroupName + ".RatePlans";
        public const string Manage = Default + ".Manage";
    }

    public static class Reports
    {
        public const string Default = GroupName + ".Reports";
        public const string Consolidated = Default + ".Consolidated";
        public const string HotelPerformance = Default + ".HotelPerformance";
    }
}

