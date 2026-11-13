# Technical Research Report: RM-003 - Chuẩn Bị Phân Quyền Hệ Thống & Vai Trò Người Dùng

**Feature Branch**: `003-prepare-permissions`  
**Date**: 2026-11-12  
**Status**: Completed  
**Spec Reference**: [specs/003-prepare-permissions/spec.md](spec.md)

---

## 1. Mục Tiêu Nghiên Cứu

Báo cáo nghiên cứu này nhằm giải quyết các quyết định kiến trúc kỹ thuật phục vụ việc thiết kế và chuẩn bị hạ tầng phân quyền đa tầng (Multi-Tenant RBAC) cho 5 vai trò chuẩn của dự án `BookingHotel`:
- Thiết kế cây hằng số phân quyền và phân tách phạm vi Multi-Tenancy (`Host`, `Tenant`, `Both`).
- Cơ chế tự động gieo dữ liệu (Data Seeding) vai trò và cấp quyền mặc định.
- Cơ chế phân quyền cấp dữ liệu (Data-level Authorization / Hotel Scoping) qua thực thể `UserHotelAssignment`.
- Chuẩn hóa đa ngôn ngữ và tích hợp cơ chế bảo vệ API mặc định.

---

## 2. Các Quyết Định Kỹ Thuật Cốt Lõi (Architecture Decisions)

### Quyết định 1: Kiến trúc Cây Phân Quyền Hạt Nhân (Granular Permission Tree) trong ABP Framework 10.7

- **Quyết định**:
  Khai báo cây phân cấp tĩnh trong `BookingHotelPermissions.cs` tại tầng `Application.Contracts` và đăng ký trong `BookingHotelPermissionDefinitionProvider.cs` với thuộc tính `multiTenancySide` tường minh:
  ```csharp
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
      }

      public static class Rooms
      {
          public const string Default = GroupName + ".Rooms";
          public const string Create = Default + ".Create";
          public const string Edit = Default + ".Edit";
          public const string Delete = Default + ".Delete";
          public const string SetRates = Default + ".SetRates";
          public const string UpdateStatus = Default + ".UpdateStatus"; // Sạch/Bẩn/Đang dọn
      }

      public static class Bookings
      {
          public const string Default = GroupName + ".Bookings";
          public const string Create = Default + ".Create"; // Customer & Receptionist
          public const string CheckIn = Default + ".CheckIn";
          public const string CheckOut = Default + ".CheckOut";
          public const string Cancel = Default + ".Cancel";
          public const string ApproveException = Default + ".ApproveException"; // Hotel Manager
      }

      public static class Folios
      {
          public const string Default = GroupName + ".Folios";
          public const string View = Default + ".View";
          public const string AddCharge = Default + ".AddCharge"; // Minibar, dịch vụ
          public const string Settle = Default + ".Settle"; // Quyết toán thanh toán
          public const string Refund = Default + ".Refund";
      }

      public static class Identity
      {
          public const string Default = GroupName + ".Identity";
          public const string UnlockUser = Default + ".UnlockUser";
      }

      public static class Reports
      {
          public const string Default = GroupName + ".Reports";
          public const string Consolidated = Default + ".Consolidated"; // Tenant Admin
          public const string HotelPerformance = Default + ".HotelPerformance"; // Hotel Manager
      }
  }
  ```
  Trong `BookingHotelPermissionDefinitionProvider`, các quyền được gắn cờ `MultiTenancySides`:
  - `MultiTenancySides.Host`: Dành cho nhóm `Tenants` và quyền mở khóa tài khoản Host (`Identity.UnlockUser`).
  - `MultiTenancySides.Tenant`: Dành cho nhóm `Hotels.ManageBranches`, `Rooms`, `Bookings.CheckIn`, `Bookings.CheckOut`, `Folios`, `Reports`.
  - `MultiTenancySides.Both`: Dành cho `Bookings.Create` (Khách hàng đặt phòng trực tuyến hoặc Lễ tân tạo tại quầy) và `Identity.UnlockUser` (áp dụng theo tầng).
- **Lý do lựa chọn**:
  ABP Framework tự động lọc các quyền theo ngữ cảnh của Tenant hiện tại. Khi thiết lập `MultiTenancySides.Host`, quyền đó sẽ không bao giờ xuất hiện trong bảng phân quyền của Tenant, ngăn ngừa triệt để lỗi vô tình cấp quyền quản trị nền tảng cho khách thuê.
- **Giải pháp thay thế đã loại bỏ**:
  Tự xây dựng hệ thống phân quyền tùy biến không dùng ABP Permissions: Bị loại bỏ vì mất toàn bộ các tính năng có sẵn mạnh mẽ của ABP như Distributed In-memory Permission Cache, `[Authorize(BookingHotelPermissions...)]`, và tự động đồng bộ sang frontend Angular qua `ConfigStateService`.

---

### Quyết định 2: Tự Động Khởi Tạo 5 Roles Chuẩn & Ma Trận Cấp Quyền Qua `IDataSeedContributor`

- **Quyết định**:
  Triển khai `BookingHotelDataSeedContributor : IDataSeedContributor, ITransientDependency` đặt tại tầng `BookingHotel.Domain/Data/`:
  - Khi chạy `DbMigrator` ở cấp Host (`context.TenantId == null`):
    - Đảm bảo vai trò `SystemAdmin` tồn tại, gán toàn bộ quyền Host (`BookingHotelPermissions.Tenants.*`, `BookingHotelPermissions.Identity.UnlockUser`).
    - Đảm bảo vai trò `Customer` tồn tại (Global Role), gán quyền đặt phòng cá nhân (`BookingHotelPermissions.Bookings.Create`, `BookingHotelPermissions.Bookings.Cancel`, `BookingHotelPermissions.Folios.View`).
  - Khi chạy `DbMigrator` ở cấp Tenant (`context.TenantId != null`):
    - Khởi tạo 3 vai trò: `TenantAdmin`, `HotelManager`, `Receptionist`.
    - Sử dụng `IPermissionManager.SetForRoleAsync(...)` để gán ma trận quyền mặc định cho từng vai trò tương ứng:
      * `TenantAdmin`: Toàn quyền các phân hệ trong Tenant (Hotels, Rooms, Bookings, Folios, Reports.Consolidated, Identity.UnlockUser cho nhân viên tenant).
      * `HotelManager`: Quyền quản lý Rooms, SetRates, ApproveException, Reports.HotelPerformance.
      * `Receptionist`: Quyền RoomRack (UpdateStatus), Bookings.CheckIn, Bookings.CheckOut, Bookings.Create (Walk-in), Folios.AddCharge, Folios.Settle.
- **Lý do lựa chọn**:
  Giúp hệ thống hoàn toàn tự động hóa (Zero Manual Setup). Bất kỳ khi nào một Tenant mới được tạo ra hoặc khi khởi chạy ứng dụng lần đầu, 100% các vai trò chuẩn và phân quyền mặc định đã sẵn sàng trong CSDL.
- **Giải pháp thay thế đã loại bỏ**:
  Viết câu lệnh SQL nạp trực tiếp vào CSDL: Vi phạm Hiến pháp Điều III, khó duy trì, không đồng bộ được mã định danh bảo mật (`ConcurrencyStamp`) của ASP.NET Core Identity.

---

### Quyết định 3: Phân Quyền Cấp Dữ Liệu Chi Nhánh (Data-Level Authorization Scoping) Qua Thực Thể `UserHotelAssignment`

- **Quyết định**:
  Xây dựng thực thể `UserHotelAssignment` kế thừa `CreationAuditedEntity<Guid>, IMultiTenant` đặt tại `BookingHotel.Domain/Identity/`:
  - **Cấu trúc trường**:
    * `Id: Guid` (Khóa chính)
    * `TenantId: Guid?` (Mã Tenant - IMultiTenant)
    * `UserId: Guid` (Mã tài khoản nhân viên được phân công)
    * `HotelId: Guid` (Mã cơ sở khách sạn)
    * `AssignedDate: DateTime` (Thời điểm phân công)
    * `IsPrimary: bool` (Đánh dấu chi nhánh chính nếu có)
  - **Quy tắc Nghiệp vụ (Business Invariants)**:
    * Với vai trò `Hotel Manager`: Cho phép tồn tại quan hệ `1-n` (một quản lý có thể có nhiều bản ghi phân công chi nhánh khác nhau trong cùng 1 Tenant).
    * Với vai trò `Receptionist`: Ràng buộc chặt chẽ quan hệ `1-1` tại một thời điểm (nhân viên lễ tân chỉ có duy nhất 1 bản ghi chi nhánh hoạt động).
- **Lý do lựa chọn**:
  RBAC của ABP (`[Authorize(Permission)]`) chỉ kiểm tra người dùng có quyền hành động hay không (Functional Authorization), nhưng không thể biết người dùng được phép thao tác trên khách sạn nào (Data Authorization). Thực thể `UserHotelAssignment` cung cấp mắt xích kiểm tra dữ liệu tin cậy cho mọi truy vấn phòng, booking và báo cáo doanh thu theo chi nhánh.
- **Giải pháp thay thế đã loại bỏ**:
  Lưu `HotelId` trực tiếp vào bảng mở rộng `ExtraProperties` của `IdentityUser`: Bị loại vì không thể đánh chỉ mục khóa ngoại quan hệ toàn vẹn, không tối ưu cho truy vấn LINQ JOIN, và không hỗ trợ được kịch bản `1-n` (Multi-Property) của Hotel Manager.

---

### Quyết định 4: Chuẩn Hóa Hằng Số Tên Vai Trò & Đa Ngôn Ngữ Song Ngữ (i18n)

- **Quyết định**:
  - Tạo hằng số định danh vai trò chuẩn tại `BookingHotel.Domain.Shared/Identity/BookingHotelRoleConsts.cs`:
    ```csharp
    namespace BookingHotel.Identity;

    public static class BookingHotelRoleConsts
    {
        public const string SystemAdmin = "SystemAdmin";
        public const string TenantAdmin = "TenantAdmin";
        public const string HotelManager = "HotelManager";
        public const string Receptionist = "Receptionist";
        public const string Customer = "Customer";
    }
    ```
  - Khai báo đầy đủ nhãn song ngữ tại `vi.json` và `en.json`:
    * Nhóm quyền: `"Permission:BookingHotel": "Booking Hotel"`
    * Tên vai trò: `"Role:SystemAdmin"`, `"Role:TenantAdmin"`, `"Role:HotelManager"`, `"Role:Receptionist"`, `"Role:Customer"`.
    * Tên quyền hạt nhân: `"Permission:Rooms.Create"`, `"Permission:Bookings.CheckIn"`, v.v.
- **Lý do lựa chọn**:
  Loại bỏ hoàn toàn các chuỗi magic string trong mã nguồn, bảo đảm tuân thủ Hiến pháp Điều V và sẵn sàng cho việc hiển thị trực quan trên giao diện Angular.

---

## 3. Kết Luận & Đánh Giá Tính Sẵn Sàng

Tất cả các câu hỏi kỹ thuật và nghiệp vụ đều đã có giải pháp kiến trúc rõ ràng, tương thích 100% với nền tảng ABP Framework 10.7 và Hiến pháp dự án. Sẵn sàng chuyển tiếp sang Phase 1 để thiết kế chi tiết `data-model.md` và `contracts/permissions-matrix.md`.

