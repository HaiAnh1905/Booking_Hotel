# Implementation Plan: RM-007 - Quản Trị Vận Hành Chuỗi & Phân Quyền Tenant Admin (Tenant Operations & Management Portal)

**Branch**: `007-tenant-admin-management` | **Date**: 2026-11-18 | **Spec**: [specs/007-tenant-admin-management/spec.md](spec.md)  
**Input**: Feature specification from `specs/007-tenant-admin-management/spec.md` và 5 quyết định kiến trúc đã làm rõ từ Clarification Loop.

---

## 1. Summary

Xây dựng và hoàn thiện trọn bộ phân hệ vận hành và quản trị chuỗi khách sạn dành cho vai trò **Quản trị viên Tenant (`TenantAdmin`)** trên nền tảng ABP Framework 10.7 (.NET 10 / EF Core / Angular 19+ Standalone) tuân thủ nghiêm ngặt Hiến pháp dự án `Booking Hotel`:
1. **Quản Lý Cơ Sở Khách Sạn (Hotel Property Management)**:
   - Hoàn thiện vòng đời khách sạn: Tạo mới, cập nhật, mã `HotelCode` (duy nhất trong Tenant), kích hoạt, và phân công `HotelManager`.
   - Quy trình Tạm ngừng (`Suspend`) 2 nhánh rõ ràng: *Tạm ngừng định kỳ* ($\ge 30$ ngày kèm cấu hình cutoff nhận đơn) và *Tạm ngừng khẩn cấp* (bắt buộc nhập lý do, phát thông báo tự động đến khách hàng có đơn tương lai và kích hoạt luồng thông báo nhân sự hỗ trợ đổi khách sạn/hoàn cọc).
2. **Quản Lý Nhân Sự & Vai Trò Nội Bộ (Staff & Role Governance)**:
   - Tạo mới tài khoản nhân viên nội bộ (`IdentityUser`) với mật khẩu khởi tạo và cờ `ShouldChangePasswordOnNextLogin = true`.
   - Cơ chế gán vai trò linh hoạt: Hỗ trợ gán `HotelManager`, `Receptionist` hoặc **gán đồng thời cả hai vai trò (Kiêm nhiệm)** kết hợp bộ chuyển đổi không gian (`Portal Switcher Pill`) trên Topbar.
   - Phân bổ chi nhánh (`UserHotelAssignment`): Quản lý có thể phụ trách 1 hoặc nhiều cơ sở; Lễ tân gắn cố định 1 cơ sở chính.
   - Bộ phòng thủ chống leo thang đặc quyền (Anti-Privilege Escalation): Whitelist cứng vai trò, cấm gán `SystemAdmin` hay quyền ngoài Tenant (HTTP 403 Forbidden).
3. **Bảng Điều Khiển Tổng Quan & So Sánh Hiệu Suất (Executive Dashboard)**:
   - Hiển thị 5 thẻ KPI hợp nhất: Tổng cơ sở (Hoạt động / Tạm ngừng), Tổng nhân viên, Tổng số booking, Doanh thu thực thu (Cash / Settlement Basis từ Folio quyết toán trừ hoàn tiền), và Tỷ lệ lấp đầy bình quân.
   - Bảng so sánh hiệu suất giữa các chi nhánh xếp hạng mặc định theo chuẩn quốc tế **RevPAR** ($\frac{\text{Doanh thu phòng}}{\text{Tổng phòng sẵn có}}$) kèm ADR, Occupancy Rate và Doanh thu.
4. **Giám Sát Đặt Phòng Hợp Nhất (Consolidated Booking Surveillance)**:
   - Cung cấp màn hình giám sát độc lập tại `/tenant/bookings` vận hành ở chế độ **CHỈ XEM (Read-Only 100%)**, tra cứu và lọc đa chiều toàn chuỗi, xem chi tiết lịch sử (Audit Trail) và dòng tiền hóa đơn Folio mà không can thiệp quầy.
5. **Hệ Thống Báo Cáo Hợp Nhất (Consolidated Reports & Analytics)**:
   - Cung cấp 5 nhóm báo cáo chuyên sâu: Revenue (Cash Basis), Booking, Occupancy, Cancellation, và Hotel Performance (RevPAR-ranked), hỗ trợ xuất dữ liệu Excel/CSV.
6. **Cấu Hình Hồ Sơ Doanh Nghiệp & Chính Sách Chuỗi (Tenant Organization Profile & Settings)**:
   - Quản lý tên hiển thị chuỗi, logo thương hiệu, địa chỉ trụ sở, thông tin liên hệ.
   - Triển khai mô hình chính sách **Hybrid**: Giờ Check-in/Check-out làm mặc định cho phép chi nhánh ghi đè (Override); Chính sách Hoàn/Hủy phòng khóa cứng và áp dụng đồng nhất 100% toàn chuỗi.

---

## 2. Technical Context

- **Language / Version**: C# .NET 10, TypeScript 5.x / Angular 19+ (Standalone Components).
- **Primary Dependencies**:
  - ABP Framework 10.7 (`Volo.Abp.Identity`, `Volo.Abp.Authorization`, `Volo.Abp.MultiTenancy`, `Volo.Abp.PermissionManagement`).
  - Entity Framework Core 10.x.
  - Angular Router, Signals, Reactive Forms, `@abp/ng.core`.
  - Design Tokens: CSS Variables từ `RM-004` & `RM-006` (Sidebar Đen `#000000`, Điểm nhấn Cyan `#00FFFF`, Huy hiệu Chuỗi Vàng `#F59E0B`, Nền Trắng `#FFFFFF`).
- **Storage**: SQL Server 2022+ / LocalDB (`localhost\SQLEXPRESS`), EF Core Code-First Migrations qua `BookingHotel.DbMigrator`.
- **Testing Framework**:
  - Backend: xUnit, Shouldly, ABP Integrated TestBase (`BookingHotel.Application.Tests`).
  - Frontend: Angular Build Verification (`npm run build`), Jasmine/Karma Component Tests.
- **Target Platform**: ASP.NET Core Web API Host (`localhost:44322`) & Angular Single Page App (`localhost:4200`).
- **Constraints & Performance**:
  - Thời gian phản hồi API Dashboard & Báo cáo chuỗi < 1.5s cho quy mô $\ge 10$ chi nhánh.
  - Thao tác tạo khách sạn hoàn thành < 2 phút; tạo nhân sự kiêm nhiệm < 90 giây.
  - Chống tuyệt đối Hardcoded Text (Điều khoản V Hiến pháp - 100% i18n tại `vi.json` và `en.json`).
  - Zero Credential Pre-filling (Điều khoản IX Hiến pháp).
  - Tên thương hiệu hiển thị chuẩn xác: `Booking Hotel` có dấu cách (Điều khoản VIII Hiến pháp).

---

## 3. Constitution Check

*GATE: Kiểm tra tính tuân thủ 9 Điều khoản của Hiến pháp dự án trong `constitution.md`*

| Điều khoản Hiến pháp | Tiêu chí đánh giá | Trạng thái | Giải pháp kỹ thuật tương ứng |
| :--- | :--- | :---: | :--- |
| **I. Phân lớp DDD & ABP** | Tách biệt nghiêm ngặt Domain, Contracts, Application, HttpApi, EntityFrameworkCore | **PASS** | `Hotel`, `UserHotelAssignment` tại Domain; DTOs/Interfaces tại Contracts; AppServices tại Application; Controllers tại HttpApi; DbContext mapping tại EntityFrameworkCore. |
| **II. Contract-First & Security** | Kiểm tra quyền ABP, Data Annotations, Audit Logging | **PASS** | Kiểm tra quyền `BookingHotelPermissions.Hotels.*`, `Identity.UnlockUser`, `Reports.*`. Whitelist Role cứng, chặn leo thang đặc quyền `SystemAdmin`. DTOs kiểm tra regex `HotelCode`, email, phone. |
| **III. EF Core Migrations** | Quản lý schema bằng Code-First Migrations qua DbMigrator | **PASS** | Bổ sung các trường chính sách và thời gian tạm ngừng vào `Hotel` và cấu hình Tenant qua EF Core Migrations. |
| **IV. Chuẩn hóa Frontend** | Thin presentation, Reactive Forms, Angular Signals, Strict TS | **PASS** | Xây dựng các component tại `/tenant/`: `hotels`, `staff`, `bookings`, `reports`, `settings`. Sử dụng Reactive Forms, Modal `BhModalComponent`, Bảng `BhDataTableComponent`. |
| **V. Đa ngôn ngữ (i18n)** | Zero Hardcoded Text, đồng bộ `vi.json` và `en.json` | **PASS** | 100% nhãn form, tiêu đề bảng, thông báo toastr, trạng thái enum (Hoạt động, Tạm ngừng định kỳ/khẩn cấp) được dịch đầy đủ ở cả 2 ngôn ngữ. |
| **VI. Backend-Authoritative** | Tính toán doanh thu và logic chuyển trạng thái thuộc về Backend | **PASS** | Backend tính toán Doanh thu theo Cash Basis (từ Folio quyết toán trừ hoàn tiền), tính Occupancy và RevPAR. Client chỉ hiển thị số liệu nhận được từ API. |
| **VII. UI/UX Concept Hệ Màu** | Đúng concept phân hệ Quản lý Tenant, WCAG 2.1 AA $\ge 4.5:1$ | **PASS** | Concept *Corporate Executive Dashboard*: Sidebar Đen `#000000`, Điểm nhấn Cyan `#00FFFF`, Huy hiệu Vàng `#F59E0B`, Nền Trắng `#FFFFFF`. |
| **VIII. Brand Naming** | Thương hiệu `Booking Hotel` có dấu cách | **PASS** | Logo và tiêu đề giao diện viết rời có dấu cách `Booking Hotel`. |
| **IX. Bảo Mật Phiên & Unlock** | Zero credential pre-filling, cờ đổi mật khẩu, mở khóa brute-force | **PASS** | Form tạo nhân viên không điền sẵn mật khẩu; gán cờ `ShouldChangePasswordOnNextLogin = true`; Tenant Admin có quyền mở khóa brute-force cho nhân sự nội bộ qua `UnlockUser`. |

---

## 4. Project Structure & Code Mapping

### Tài liệu Đặc tả & Thiết kế (Documentation Artifacts)
```text
BookingHotel/specs/007-tenant-admin-management/
├── spec.md                          # Đặc tả yêu cầu chức năng hoàn chỉnh
├── checklists/
│   └── requirements.md              # Thẩm định chất lượng đặc tả
├── plan.md                          # Kế hoạch kiến trúc kỹ thuật (Tài liệu này)
├── research.md                      # Báo cáo nghiên cứu giải pháp kỹ thuật
├── data-model.md                    # Thiết kế mô hình dữ liệu & sơ đồ luồng
├── contracts/
│   ├── hotel-management.md          # Đặc tả API quản lý cơ sở khách sạn
│   ├── tenant-staff-management.md   # Đặc tả API nhân sự & phân bổ vai trò
│   ├── tenant-surveillance-reporting.md # Đặc tả API giám sát đặt phòng & báo cáo
│   └── tenant-organization.md       # Đặc tả API hồ sơ tổ chức & cấu hình
└── quickstart.md                    # Hướng dẫn xác minh và kịch bản kiểm thử
```

### Mã Nguồn Backend (.NET 10 / ABP Framework)
```text
BookingHotel/src/
├── BookingHotel.Domain.Shared/
│   ├── Hotels/
│   │   ├── HotelStatus.cs                    # Operating = 1, ScheduledSuspended = 2, EmergencySuspended = 3
│   │   └── SuspensionType.cs                 # Scheduled = 1, Emergency = 2
│   ├── Identity/
│   │   └── BookingHotelRoleConsts.cs         # SystemAdmin, TenantAdmin, HotelManager, Receptionist, Customer
│   └── Localization/BookingHotel/
│       ├── vi.json                           # Bản dịch tiếng Việt toàn diện
│       └── en.json                           # Bản dịch tiếng Anh toàn diện
├── BookingHotel.Domain/
│   ├── Hotels/
│   │   ├── Hotel.cs                          # AggregateRoot: FullAuditedAggregateRoot<Guid>, IMultiTenant
│   │   └── HotelSuspensionRecord.cs          # Entity: Lưu lịch sử và lý do tạm ngừng cơ sở
│   └── Identity/
│       └── UserHotelAssignment.cs            # Entity: FullAuditedEntity<Guid>, IMultiTenant
├── BookingHotel.EntityFrameworkCore/
│   └── EntityFrameworkCore/
│       └── BookingHotelDbContext.cs          # Indexing: Hotel(TenantId, Code) Unique, UserHotelAssignment
├── BookingHotel.Application.Contracts/
│   ├── Hotels/
│   │   ├── IHotelAppService.cs               # GetList, Get, Create, Update, Suspend, Activate, AssignManager
│   │   └── Dtos/
│   │       ├── CreateUpdateHotelDto.cs
│   │       ├── HotelDto.cs
│   │       ├── SuspendHotelInputDto.cs
│   │       └── AssignHotelManagerDto.cs
│   ├── Identity/
│   │   ├── IIdentityUserManagementAppService.cs # CreateStaff, UpdateStaff, AssignRoles, TransferBranch
│   │   └── Dtos/
│   │       ├── CreateTenantStaffDto.cs
│   │       ├── UpdateTenantStaffDto.cs
│   │       └── UserWithAssignmentDto.cs
│   ├── TenantDashboard/
│   │   ├── ITenantDashboardAppService.cs     # GetOverviewAsync (KPIs, RevPAR comparison)
│   │   └── Dtos/
│   │       └── TenantDashboardOverviewDto.cs
│   └── TenantReports/
│       ├── ITenantReportAppService.cs        # GetRevenueReport, GetBookingReport, GetOccupancyReport, etc.
│       └── Dtos/
│           ├── RevenueReportDto.cs
│           └── HotelPerformanceReportDto.cs
├── BookingHotel.Application/
│   ├── Hotels/
│   │   └── HotelAppService.cs
│   ├── Identity/
│   │   └── IdentityUserManagementAppService.cs
│   ├── TenantDashboard/
│   │   └── TenantDashboardAppService.cs
│   └── TenantReports/
│       └── TenantReportAppService.cs
└── BookingHotel.HttpApi/
    └── Controllers/
        ├── HotelController.cs
        ├── IdentityUserManagementController.cs
        ├── TenantDashboardController.cs
        └── TenantReportController.cs
```

### Mã Nguồn Frontend (Angular 19+ Standalone)
```text
BookingHotel/angular/src/app/admin/tenant/
├── tenant.routes.ts                         # Cấu hình định tuyến phân hệ Tenant
├── dashboard/
│   ├── tenant-dashboard.component.ts        # Executive Dashboard & KPI cards
│   ├── tenant-dashboard.component.html
│   └── tenant-dashboard.component.scss
├── hotels/
│   ├── hotel-list.component.ts              # Danh sách khách sạn (BhDataTable)
│   ├── hotel-list.component.html
│   ├── hotel-modal.component.ts             # Modal thêm/sửa khách sạn
│   ├── hotel-suspend-modal.component.ts     # Modal tạm ngừng định kỳ vs khẩn cấp
│   └── hotel-manager-modal.component.ts     # Modal phân công Quản lý khách sạn
├── staff/
│   ├── staff-list.component.ts              # Quản lý nhân viên (BhDataTable)
│   ├── staff-list.component.html
│   ├── staff-modal.component.ts             # Modal tạo/sửa nhân viên & chọn vai trò kép
│   └── staff-transfer-modal.component.ts    # Modal điều chuyển chi nhánh
├── bookings/
│   ├── booking-surveillance.component.ts    # Giám sát đặt phòng toàn chuỗi (Read-only)
│   ├── booking-surveillance.component.html
│   └── booking-detail-modal.component.ts    # Modal xem chi tiết booking & audit trail
├── reports/
│   ├── tenant-reports.component.ts          # Báo cáo hợp nhất 5 tab (Revenue, Occupancy, RevPAR...)
│   └── tenant-reports.component.html
└── settings/
    ├── organization-settings.component.ts   # Cấu hình hồ sơ chuỗi & chính sách Hybrid
    └── organization-settings.component.html
```

---

## 5. Complexity Tracking

> Không có vi phạm Hiến pháp nào phát sinh. Toàn bộ thiết kế kế thừa trực tiếp kiến trúc ABP Multi-Tenancy và các module đã xây dựng từ RM-003, RM-004, RM-005, RM-006.
