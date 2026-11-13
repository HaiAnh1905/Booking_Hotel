# Contracts: RM-003 - Ma Trận Phân Quyền Hạt Nhân & Định Nghĩa Giao Tiếp (Permissions Matrix Contract)

**Feature Branch**: `003-prepare-permissions`  
**Date**: 2026-11-12  
**Status**: Ready for Implementation  
**Spec Reference**: [specs/003-prepare-permissions/spec.md](../spec.md)

---

## 1. Hợp Đồng Hằng Số Phân Quyền (Permission Constants Contract)

Tất cả các chuỗi định danh quyền hạn hạt nhân bắt buộc phải được khai báo dạng hằng số tĩnh (`public const string`) theo chuẩn cấu trúc:
$$\text{GroupName}.\text{Module}.\text{Action}$$

```csharp
namespace BookingHotel.Permissions;

public static class BookingHotelPermissions
{
    public const string GroupName = "BookingHotel";

    // Phân hệ Quản trị Nền tảng (Platform / Tenants)
    public static class Tenants
    {
        public const string Default = GroupName + ".Tenants";
        public const string Create = Default + ".Create";
        public const string Edit = Default + ".Edit";
        public const string Delete = Default + ".Delete";
        public const string Suspend = Default + ".Suspend";
    }

    // Phân hệ Khách sạn / Chi nhánh (Hotels / Branches)
    public static class Hotels
    {
        public const string Default = GroupName + ".Hotels";
        public const string Create = Default + ".Create";
        public const string Edit = Default + ".Edit";
        public const string Delete = Default + ".Delete";
        public const string ManageBranches = Default + ".ManageBranches";
        public const string AssignStaff = Default + ".AssignStaff";
    }

    // Phân hệ Buồng phòng & Giá bán (Rooms & Rates)
    public static class Rooms
    {
        public const string Default = GroupName + ".Rooms";
        public const string Create = Default + ".Create";
        public const string Edit = Default + ".Edit";
        public const string Delete = Default + ".Delete";
        public const string SetRates = Default + ".SetRates";
        public const string UpdateStatus = Default + ".UpdateStatus"; // Đổi trạng thái Sạch/Bẩn/Đang dọn
    }

    // Phân hệ Đặt phòng & Tiếp đón (Bookings & Frontdesk)
    public static class Bookings
    {
        public const string Default = GroupName + ".Bookings";
        public const string Create = Default + ".Create";       // Đặt trực tuyến (Customer) hoặc tại quầy (Receptionist)
        public const string CheckIn = Default + ".CheckIn";     // Lễ tân
        public const string CheckOut = Default + ".CheckOut";   // Lễ tân
        public const string Cancel = Default + ".Cancel";       // Khách hủy hợp lệ hoặc Quản lý
        public const string ApproveException = Default + ".ApproveException"; // Quản lý duyệt ngoại lệ
    }

    // Phân hệ Hóa đơn & Thu ngân (Folios & Billing)
    public static class Folios
    {
        public const string Default = GroupName + ".Folios";
        public const string View = Default + ".View";           // Xem folio
        public const string AddCharge = Default + ".AddCharge"; // Ghi nhận minibar/dịch vụ phát sinh
        public const string Settle = Default + ".Settle";       // Thu tiền quyết toán checkout
        public const string Refund = Default + ".Refund";       // Hoàn tiền ngoại lệ
    }

    // Phân hệ An ninh & Mở khóa tài khoản (Security & Lockout)
    public static class Identity
    {
        public const string Default = GroupName + ".Identity";
        public const string UnlockUser = Default + ".UnlockUser"; // Mở khóa brute-force phân tầng
    }

    // Phân hệ Báo cáo Doanh thu & Vận hành (Reports & Analytics)
    public static class Reports
    {
        public const string Default = GroupName + ".Reports";
        public const string Consolidated = Default + ".Consolidated";     // Báo cáo hợp nhất chuỗi
        public const string HotelPerformance = Default + ".HotelPerformance"; // Báo cáo cơ sở
    }
}
```

---

## 2. Bảng Ma Trận Cấp Quyền Cho 5 Vai Trò Chuẩn (Default Permission Grants Matrix)

Ký hiệu trong bảng:
- `✓`: Được cấp quyền mặc định khi khởi tạo dữ liệu (`isGranted: true`).
- `✗`: Không được cấp quyền (`isGranted: false`).

| Mã Quyền Hạn Hạt Nhân | Multi-Tenancy Scope | System Admin | Tenant Admin | Hotel Manager | Receptionist | Customer | Khóa Ngôn Ngữ (i18n Key) |
| :--- | :---: | :---: | :---: | :---: | :---: | :---: | :--- |
| **Nhóm Tenants (Nền tảng)** | | | | | | | |
| `BookingHotel.Tenants` | `Host` | `✓` | `✗` | `✗` | `✗` | `✗` | `Permission:Tenants` |
| `BookingHotel.Tenants.Create` | `Host` | `✓` | `✗` | `✗` | `✗` | `✗` | `Permission:Tenants.Create` |
| `BookingHotel.Tenants.Edit` | `Host` | `✓` | `✗` | `✗` | `✗` | `✗` | `Permission:Tenants.Edit` |
| `BookingHotel.Tenants.Delete` | `Host` | `✓` | `✗` | `✗` | `✗` | `✗` | `Permission:Tenants.Delete` |
| `BookingHotel.Tenants.Suspend` | `Host` | `✓` | `✗` | `✗` | `✗` | `✗` | `Permission:Tenants.Suspend` |
| **Nhóm Hotels (Cơ sở)** | | | | | | | |
| `BookingHotel.Hotels` | `Tenant` | `✗` | `✓` | `✓` | `✗` | `✗` | `Permission:Hotels` |
| `BookingHotel.Hotels.Create` | `Tenant` | `✗` | `✓` | `✗` | `✗` | `✗` | `Permission:Hotels.Create` |
| `BookingHotel.Hotels.Edit` | `Tenant` | `✗` | `✓` | `✗` | `✗` | `✗` | `Permission:Hotels.Edit` |
| `BookingHotel.Hotels.ManageBranches` | `Tenant` | `✗` | `✓` | `✗` | `✗` | `✗` | `Permission:Hotels.ManageBranches` |
| `BookingHotel.Hotels.AssignStaff` | `Tenant` | `✗` | `✓` | `✗` | `✗` | `✗` | `Permission:Hotels.AssignStaff` |
| **Nhóm Rooms (Buồng phòng)** | | | | | | | |
| `BookingHotel.Rooms` | `Tenant` | `✗` | `✓` | `✓` | `✓` | `✗` | `Permission:Rooms` |
| `BookingHotel.Rooms.Create` | `Tenant` | `✗` | `✓` | `✓` | `✗` | `✗` | `Permission:Rooms.Create` |
| `BookingHotel.Rooms.Edit` | `Tenant` | `✗` | `✓` | `✓` | `✗` | `✗` | `Permission:Rooms.Edit` |
| `BookingHotel.Rooms.Delete` | `Tenant` | `✗` | `✓` | `✓` | `✗` | `✗` | `Permission:Rooms.Delete` |
| `BookingHotel.Rooms.SetRates` | `Tenant` | `✗` | `✓` | `✓` | `✗` | `✗` | `Permission:Rooms.SetRates` |
| `BookingHotel.Rooms.UpdateStatus` | `Tenant` | `✗` | `✓` | `✓` | `✓` | `✗` | `Permission:Rooms.UpdateStatus` |
| **Nhóm Bookings (Đặt phòng)** | | | | | | | |
| `BookingHotel.Bookings` | `Both` | `✗` | `✓` | `✓` | `✓` | `✓` | `Permission:Bookings` |
| `BookingHotel.Bookings.Create` | `Both` | `✗` | `✓` | `✓` | `✓` | `✓` | `Permission:Bookings.Create` |
| `BookingHotel.Bookings.CheckIn` | `Tenant` | `✗` | `✓` | `✓` | `✓` | `✗` | `Permission:Bookings.CheckIn` |
| `BookingHotel.Bookings.CheckOut` | `Tenant` | `✗` | `✓` | `✓` | `✓` | `✗` | `Permission:Bookings.CheckOut` |
| `BookingHotel.Bookings.Cancel` | `Both` | `✗` | `✓` | `✓` | `✓` | `✓` | `Permission:Bookings.Cancel` |
| `BookingHotel.Bookings.ApproveException` | `Tenant` | `✗` | `✓` | `✓` | `✗` | `✗` | `Permission:Bookings.ApproveException` |
| **Nhóm Folios (Hóa đơn)** | | | | | | | |
| `BookingHotel.Folios` | `Both` | `✗` | `✓` | `✓` | `✓` | `✓` | `Permission:Folios` |
| `BookingHotel.Folios.View` | `Both` | `✗` | `✓` | `✓` | `✓` | `✓` | `Permission:Folios.View` |
| `BookingHotel.Folios.AddCharge` | `Tenant` | `✗` | `✓` | `✓` | `✓` | `✗` | `Permission:Folios.AddCharge` |
| `BookingHotel.Folios.Settle` | `Tenant` | `✗` | `✓` | `✓` | `✓` | `✗` | `Permission:Folios.Settle` |
| `BookingHotel.Folios.Refund` | `Tenant` | `✗` | `✓` | `✓` | `✗` | `✗` | `Permission:Folios.Refund` |
| **Nhóm Identity (An ninh)** | | | | | | | |
| `BookingHotel.Identity.UnlockUser` | `Both` | `✓` | `✓` | `✗` | `✗` | `✗` | `Permission:Identity.UnlockUser` |
| **Nhóm Reports (Báo cáo)** | | | | | | | |
| `BookingHotel.Reports` | `Tenant` | `✗` | `✓` | `✓` | `✗` | `✗` | `Permission:Reports` |
| `BookingHotel.Reports.Consolidated` | `Tenant` | `✗` | `✓` | `✗` | `✗` | `✗` | `Permission:Reports.Consolidated` |
| `BookingHotel.Reports.HotelPerformance` | `Tenant` | `✗` | `✓` | `✓` | `✗` | `✗` | `Permission:Reports.HotelPerformance` |

---

## 3. Hợp Đồng Dịch Vụ Data Seeder (`IDataSeedContributor`)

```csharp
namespace BookingHotel.Data;

public interface IBookingHotelDataSeedContributor : IDataSeedContributor, ITransientDependency
{
    // Tự động được gọi bởi ABP IDataSeeder
    Task SeedAsync(DataSeedContext context);
}
```

### Nguyên tắc Thực thi Hạt nhân:
1. **Host Seeding (`context.TenantId == null`)**:
   - Khởi tạo vai trò `SystemAdmin` $\rightarrow$ Gán toàn bộ quyền `Host` (`Tenants.*`, `Identity.UnlockUser`).
   - Khởi tạo vai trò `Customer` $\rightarrow$ Gán quyền đặt phòng cá nhân (`Bookings.Create`, `Bookings.Cancel`, `Folios.View`).
2. **Tenant Seeding (`context.TenantId != null`)**:
   - Khởi tạo vai trò `TenantAdmin` $\rightarrow$ Gán ma trận quyền cấp Doanh nghiệp.
   - Khởi tạo vai trò `HotelManager` $\rightarrow$ Gán ma trận quyền cấp Quản lý vận hành.
   - Khởi tạo vai trò `Receptionist` $\rightarrow$ Gán ma trận quyền quầy lễ tân.

---

## 4. Hợp Đồng Dịch Vụ Phân Bổ Chi Nhánh (`IUserHotelAssignmentAppService`)

```csharp
namespace BookingHotel.Identity;

public interface IUserHotelAssignmentAppService : IApplicationService
{
    // Lấy danh sách chi nhánh được phân công cho nhân sự
    Task<List<Guid>> GetAssignedHotelIdsAsync(Guid userId);

    // Phân công chi nhánh cho Quản lý (1-n) hoặc Lễ tân (1-1)
    Task AssignUserToHotelsAsync(AssignUserHotelDto input);

    // Kiểm tra nhanh quyền truy cập vào một cơ sở khách sạn cụ thể
    Task<bool> HasAccessToHotelAsync(Guid userId, Guid hotelId);
}
```

