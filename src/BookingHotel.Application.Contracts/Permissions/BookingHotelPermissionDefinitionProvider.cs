using BookingHotel.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;
using Volo.Abp.MultiTenancy;

namespace BookingHotel.Permissions;

public class BookingHotelPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var myGroup = context.AddGroup(BookingHotelPermissions.GroupName, L("Permission:BookingHotel"));

        // Phân hệ Quản trị Nền tảng (Chỉ Host)
        var tenantsPermission = myGroup.AddPermission(
            BookingHotelPermissions.Tenants.Default,
            L("Permission:Tenants"),
            multiTenancySide: MultiTenancySides.Host);
        tenantsPermission.AddChild(BookingHotelPermissions.Tenants.Create, L("Permission:Tenants.Create"), multiTenancySide: MultiTenancySides.Host);
        tenantsPermission.AddChild(BookingHotelPermissions.Tenants.Edit, L("Permission:Tenants.Edit"), multiTenancySide: MultiTenancySides.Host);
        tenantsPermission.AddChild(BookingHotelPermissions.Tenants.Delete, L("Permission:Tenants.Delete"), multiTenancySide: MultiTenancySides.Host);
        tenantsPermission.AddChild(BookingHotelPermissions.Tenants.Suspend, L("Permission:Tenants.Suspend"), multiTenancySide: MultiTenancySides.Host);

        // Phân hệ Khách sạn / Chi nhánh (Both: Host có thể xem danh sách, Tenant quản trị chi tiết)
        var hotelsPermission = myGroup.AddPermission(
            BookingHotelPermissions.Hotels.Default,
            L("Permission:Hotels"),
            multiTenancySide: MultiTenancySides.Both);
        hotelsPermission.AddChild(BookingHotelPermissions.Hotels.Create, L("Permission:Hotels.Create"), multiTenancySide: MultiTenancySides.Tenant);
        hotelsPermission.AddChild(BookingHotelPermissions.Hotels.Edit, L("Permission:Hotels.Edit"), multiTenancySide: MultiTenancySides.Tenant);
        hotelsPermission.AddChild(BookingHotelPermissions.Hotels.Delete, L("Permission:Hotels.Delete"), multiTenancySide: MultiTenancySides.Tenant);
        hotelsPermission.AddChild(BookingHotelPermissions.Hotels.ManageBranches, L("Permission:Hotels.ManageBranches"), multiTenancySide: MultiTenancySides.Tenant);
        hotelsPermission.AddChild(BookingHotelPermissions.Hotels.AssignStaff, L("Permission:Hotels.AssignStaff"), multiTenancySide: MultiTenancySides.Tenant);

        // Phân hệ Buồng phòng & Giá bán (Tenant)
        var roomsPermission = myGroup.AddPermission(
            BookingHotelPermissions.Rooms.Default,
            L("Permission:Rooms"),
            multiTenancySide: MultiTenancySides.Tenant);
        roomsPermission.AddChild(BookingHotelPermissions.Rooms.Create, L("Permission:Rooms.Create"), multiTenancySide: MultiTenancySides.Tenant);
        roomsPermission.AddChild(BookingHotelPermissions.Rooms.Edit, L("Permission:Rooms.Edit"), multiTenancySide: MultiTenancySides.Tenant);
        roomsPermission.AddChild(BookingHotelPermissions.Rooms.Delete, L("Permission:Rooms.Delete"), multiTenancySide: MultiTenancySides.Tenant);
        roomsPermission.AddChild(BookingHotelPermissions.Rooms.SetRates, L("Permission:Rooms.SetRates"), multiTenancySide: MultiTenancySides.Tenant);
        roomsPermission.AddChild(BookingHotelPermissions.Rooms.UpdateStatus, L("Permission:Rooms.UpdateStatus"), multiTenancySide: MultiTenancySides.Tenant);
        roomsPermission.AddChild(BookingHotelPermissions.Rooms.ChangeStatus, L("Permission:Rooms.ChangeStatus"), multiTenancySide: MultiTenancySides.Tenant);
        roomsPermission.AddChild(BookingHotelPermissions.Rooms.UpdateHousekeeping, L("Permission:Rooms.UpdateHousekeeping"), multiTenancySide: MultiTenancySides.Tenant);
        roomsPermission.AddChild(BookingHotelPermissions.Rooms.ManageClosures, L("Permission:Rooms.ManageClosures"), multiTenancySide: MultiTenancySides.Tenant);
        roomsPermission.AddChild(BookingHotelPermissions.Rooms.ResolveIncidents, L("Permission:Rooms.ResolveIncidents"), multiTenancySide: MultiTenancySides.Tenant);

        // Phân hệ Đặt phòng & Tiếp đón (Both)
        var bookingsPermission = myGroup.AddPermission(
            BookingHotelPermissions.Bookings.Default,
            L("Permission:Bookings"),
            multiTenancySide: MultiTenancySides.Both);
        bookingsPermission.AddChild(BookingHotelPermissions.Bookings.Create, L("Permission:Bookings.Create"), multiTenancySide: MultiTenancySides.Both);
        bookingsPermission.AddChild(BookingHotelPermissions.Bookings.Confirm, L("Permission:Bookings.Confirm"), multiTenancySide: MultiTenancySides.Tenant);
        bookingsPermission.AddChild(BookingHotelPermissions.Bookings.Reject, L("Permission:Bookings.Reject"), multiTenancySide: MultiTenancySides.Tenant);
        bookingsPermission.AddChild(BookingHotelPermissions.Bookings.MarkNoShow, L("Permission:Bookings.MarkNoShow"), multiTenancySide: MultiTenancySides.Tenant);
        bookingsPermission.AddChild(BookingHotelPermissions.Bookings.AssignRoom, L("Permission:Bookings.AssignRoom"), multiTenancySide: MultiTenancySides.Tenant);
        bookingsPermission.AddChild(BookingHotelPermissions.Bookings.CheckIn, L("Permission:Bookings.CheckIn"), multiTenancySide: MultiTenancySides.Tenant);
        bookingsPermission.AddChild(BookingHotelPermissions.Bookings.CheckOut, L("Permission:Bookings.CheckOut"), multiTenancySide: MultiTenancySides.Tenant);
        bookingsPermission.AddChild(BookingHotelPermissions.Bookings.Cancel, L("Permission:Bookings.Cancel"), multiTenancySide: MultiTenancySides.Both);
        bookingsPermission.AddChild(BookingHotelPermissions.Bookings.ApproveException, L("Permission:Bookings.ApproveException"), multiTenancySide: MultiTenancySides.Tenant);

        // Phân hệ Quản lý Gói Giá Chuỗi (Tenant)
        var ratePlansPermission = myGroup.AddPermission(
            BookingHotelPermissions.RatePlans.Default,
            L("Permission:RatePlans"),
            multiTenancySide: MultiTenancySides.Tenant);
        ratePlansPermission.AddChild(BookingHotelPermissions.RatePlans.Manage, L("Permission:RatePlans.Manage"), multiTenancySide: MultiTenancySides.Tenant);

        // Phân hệ Khách hàng (Tenant)
        var guestsPermission = myGroup.AddPermission(
            BookingHotelPermissions.Guests.Default,
            L("Permission:Guests"),
            multiTenancySide: MultiTenancySides.Tenant);
        guestsPermission.AddChild(BookingHotelPermissions.Guests.ManageNotes, L("Permission:Guests.ManageNotes"), multiTenancySide: MultiTenancySides.Tenant);

        // Phân hệ Đánh giá & Khiếu nại (Tenant)
        var reviewsPermission = myGroup.AddPermission(
            BookingHotelPermissions.Reviews.Default,
            L("Permission:Reviews"),
            multiTenancySide: MultiTenancySides.Tenant);
        reviewsPermission.AddChild(BookingHotelPermissions.Reviews.Reply, L("Permission:Reviews.Reply"), multiTenancySide: MultiTenancySides.Tenant);
        reviewsPermission.AddChild(BookingHotelPermissions.Reviews.ManageComplaints, L("Permission:Reviews.ManageComplaints"), multiTenancySide: MultiTenancySides.Tenant);

        // Phân hệ Hóa đơn & Thu ngân (Both)
        var foliosPermission = myGroup.AddPermission(
            BookingHotelPermissions.Folios.Default,
            L("Permission:Folios"),
            multiTenancySide: MultiTenancySides.Both);
        foliosPermission.AddChild(BookingHotelPermissions.Folios.View, L("Permission:Folios.View"), multiTenancySide: MultiTenancySides.Both);
        foliosPermission.AddChild(BookingHotelPermissions.Folios.AddCharge, L("Permission:Folios.AddCharge"), multiTenancySide: MultiTenancySides.Tenant);
        foliosPermission.AddChild(BookingHotelPermissions.Folios.Settle, L("Permission:Folios.Settle"), multiTenancySide: MultiTenancySides.Tenant);
        foliosPermission.AddChild(BookingHotelPermissions.Folios.Refund, L("Permission:Folios.Refund"), multiTenancySide: MultiTenancySides.Tenant);

        // Phân hệ An ninh & Mở khóa tài khoản (Both)
        var identityPermission = myGroup.AddPermission(
            BookingHotelPermissions.Identity.Default,
            L("Permission:Identity"),
            multiTenancySide: MultiTenancySides.Both);
        identityPermission.AddChild(
            BookingHotelPermissions.Identity.UnlockUser,
            L("Permission:Identity.UnlockUser"),
            multiTenancySide: MultiTenancySides.Both);

        // Phân hệ Báo cáo & Thống kê (Tenant)
        var reportsPermission = myGroup.AddPermission(
            BookingHotelPermissions.Reports.Default,
            L("Permission:Reports"),
            multiTenancySide: MultiTenancySides.Tenant);
        reportsPermission.AddChild(BookingHotelPermissions.Reports.Consolidated, L("Permission:Reports.Consolidated"), multiTenancySide: MultiTenancySides.Tenant);
        reportsPermission.AddChild(BookingHotelPermissions.Reports.HotelPerformance, L("Permission:Reports.HotelPerformance"), multiTenancySide: MultiTenancySides.Tenant);
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<BookingHotelResource>(name);
    }
}
