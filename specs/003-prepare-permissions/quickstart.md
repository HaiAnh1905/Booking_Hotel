# Quickstart & Verification Guide: RM-003 - Chuẩn Bị Phân Quyền Hệ Thống & Vai Trò Người Dùng

**Feature Branch**: `003-prepare-permissions`  
**Date**: 2026-11-12  
**Status**: Ready for Validation  
**Spec Reference**: [specs/003-prepare-permissions/spec.md](spec.md) | [plan.md](plan.md)

---

## 1. Mục Tiêu & Yêu Cầu Tiên Quyết

Tài liệu này hướng dẫn các bước kiểm thử tự động và xác minh tính toàn vẹn của hệ thống phân quyền 5 vai trò chuẩn trước khi chuyển sang giai đoạn xây dựng giao diện Angular và các phân hệ phòng/đặt phòng tiếp theo.

### Yêu cầu môi trường:
- .NET 10 SDK (`dotnet --version` $\ge$ 10.0.x).
- Visual Studio 2022 / VS Code / JetBrains Rider.
- Cơ sở dữ liệu thử nghiệm (In-memory SQLite hoặc SQL Server Test Database).

---

## 2. Các Kịch Bản Xác Minh Độc Lập (Validation Scenarios)

### Kịch bản 1: Kiểm tra Tính Toàn Vẹn Của Cây Phân Quyền & Multi-Tenancy

**Mục đích**: Đảm bảo tất cả các quyền hạt nhân đã được đăng ký chính xác trong `BookingHotelPermissionDefinitionProvider`, nhóm quyền chuẩn `BookingHotel`, và cờ `MultiTenancySides` được phân định đúng (`Host`, `Tenant`, `Both`).

**Thực thi kiểm thử**:
```powershell
dotnet test test/BookingHotel.Application.Tests/BookingHotel.Application.Tests.csproj --filter "FullyQualifiedName~BookingHotelPermissionDefinitionTests"
```

**Kết quả kỳ vọng**:
- `Define_ShouldRegisterAllCorePermissionGroups`: Nhóm quyền `BookingHotel` được nạp đầy đủ vào `IPermissionDefinitionManager`.
- `TenantsPermissions_ShouldBeRestrictedToHost`: Các quyền `BookingHotelPermissions.Tenants.*` chỉ có cờ `MultiTenancySides.Host`.
- `HotelOperationalPermissions_ShouldBeRestrictedToTenant`: Quyền `Rooms.SetRates`, `Bookings.CheckIn`, `Bookings.CheckOut` chỉ có cờ `MultiTenancySides.Tenant`.
- `AllPermissions_ShouldHaveValidLocalizationKeys`: 100% các quyền đều có key song ngữ tương ứng trong `vi.json` và `en.json`.

---

### Kịch bản 2: Kiểm tra Tự Động Gieo Dữ Liệu Vai Trò (Data Seeding)

**Mục đích**: Xác minh `BookingHotelDataSeedContributor` tự động khởi tạo đúng 5 vai trò chuẩn và nạp ma trận quyền mặc định cho cả 2 ngữ cảnh: Host và Tenant.

**Thực thi kiểm thử**:
```powershell
dotnet test test/BookingHotel.Domain.Tests/BookingHotel.Domain.Tests.csproj --filter "FullyQualifiedName~BookingHotelDataSeedContributorTests"
```

**Các ca kiểm thử hạt nhân**:
1. **Host Seeding Verification (`TenantId = null`)**:
   - Vai trò `SystemAdmin` được tạo thành công ở cấp Host.
   - Vai trò `Customer` được tạo thành công ở cấp Host (Global Role, `IsDefault = true`).
   - `SystemAdmin` sở hữu quyền `Tenants.Manage` và `Identity.UnlockUser`.
   - `Customer` sở hữu quyền `Bookings.Create`, `Bookings.Cancel`, `Folios.View`.
2. **Tenant Seeding Verification (`TenantId != null`)**:
   - Ba vai trò `TenantAdmin`, `HotelManager`, `Receptionist` được tạo thành công trong Tenant.
   - `TenantAdmin` sở hữu toàn quyền quản lý khách sạn chi nhánh và mở khóa nhân sự nội bộ.
   - `HotelManager` sở hữu quyền buồng phòng, bảng giá và báo cáo doanh thu cơ sở.
   - `Receptionist` sở hữu quyền Check-in, Check-out, Folio và cập nhật trạng thái buồng phòng, nhưng **không** có quyền sửa giá phòng hay xóa đơn.

---

### Kịch bản 3: Kiểm tra Phân Bổ Chi Nhánh & Lọc Dữ Liệu (`UserHotelAssignment`)

**Mục đích**: Đảm bảo quy tắc phân công chi nhánh hoạt động chính xác (Hotel Manager được gán 1-nhiều, Receptionist chỉ gán cố định 1-1).

**Các bước kiểm thử**:
1. Gán tài khoản `HotelManager1` vào 2 khách sạn `Hotel A` và `Hotel B` thuộc cùng Tenant $\rightarrow$ Thành công.
2. Kiểm tra bộ lọc dữ liệu của `HotelManager1` $\rightarrow$ Chỉ thấy danh sách phòng và booking của `Hotel A` và `Hotel B`, không thấy `Hotel C`.
3. Gán tài khoản `Receptionist1` vào `Hotel A` $\rightarrow$ Thành công.
4. Thử gán tiếp `Receptionist1` vào `Hotel B` khi chưa hủy gán `Hotel A` $\rightarrow$ Hệ thống tự động thay thế chi nhánh công tác hoặc báo lỗi nghiệp vụ theo đúng quy tắc 1-1.

---

## 3. Lệnh Chạy Cập Nhật CSDL Qua `DbMigrator`

Khi chuyển sang triển khai mã nguồn, áp dụng lược đồ bảng `AppUserHotelAssignments` và nạp dữ liệu phân quyền mẫu bằng lệnh:

```powershell
dotnet run --project src/BookingHotel.DbMigrator/BookingHotel.DbMigrator.csproj
```

**Dấu hiệu thành công**:
- Console hiển thị: `"Started database migrations..."`
- Tạo bảng mới `AppUserHotelAssignments` và chỉ mục `IX_AppUserHotelAssignments_Tenant_User_Hotel`.
- Nạp danh mục 5 vai trò chuẩn vào `AbpRoles` và ma trận quyền mặc định vào `AbpPermissionGrants`.
- Console hiển thị: `"Successfully completed host database migrations."`

