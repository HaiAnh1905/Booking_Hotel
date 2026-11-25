# Tasks: RM-006 - Phân Tách Không Gian & Thiết Kế Giao Diện Quản Lý Theo 4 Vai Trò

**Input**: Design documents from `/specs/006-role-based-management/` (`plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/portal-routes.md`, `contracts/operational-approvals.md`, `quickstart.md`)  
**Prerequisites**: `plan.md` (required), `spec.md` (required), `research.md`, `data-model.md`, `contracts/`  
**Organization**: Tasks được cấu trúc theo từng Phase và User Story để đảm bảo tính độc lập triển khai, khả năng kiểm thử từng phần và tuân thủ chặt chẽ Hiến pháp dự án.  
**Cấu trúc URL**: 4 Root Namespaces độc lập cấp cao nhất: `/admin` (System Admin), `/tenant` (Tenant Admin), `/manager` (Hotel Manager), `/frontdesk` (Receptionist).

## Format: `- [x] [ID] [P?] [Story?] Description with file path`

- **[P]**: Có thể thực hiện song song (khác file, không phụ thuộc vào task chưa hoàn thành).
- **[Story]**: Đánh dấu User Story tương ứng (`[US1]`, `[US2]`, `[US3]`, `[US4]`, `[US5]`).
- Đường dẫn file mã nguồn chính xác trong mô tả task.

---

## Phase 1: Setup (Backend Approvals Foundation & Contracts)

**Mục đích**: Thiết lập thực thể phiếu duyệt ngoại lệ `OperationalApprovalTicket`, enum, chỉ mục CSDL, EF Core Migration và hợp đồng DTOs làm nền tảng cho phân hệ tác nghiệp khách sạn.

- [x] T001 [P] Khai báo Enum `ApprovalTicketType` và `ApprovalTicketStatus` trong `BookingHotel/src/BookingHotel.Domain.Shared/Approvals/ApprovalEnums.cs`
- [x] T002 Tạo thực thể `OperationalApprovalTicket` kế thừa `FullAuditedEntity<Guid>` và `IMultiTenant` trong `BookingHotel/src/BookingHotel.Domain/Approvals/OperationalApprovalTicket.cs`
- [x] T003 Cấu hình EF Core Fluent API mapping và Indexing (`TenantId, HotelId, Status`) cho `OperationalApprovalTicket` trong `BookingHotel/src/BookingHotel.EntityFrameworkCore/EntityFrameworkCore/BookingHotelDbContext.cs`
- [x] T004 Tạo và thực thi EF Core Migration `AddOperationalApprovalTicketTable` cho CSDL qua `BookingHotel.DbMigrator` trong `BookingHotel/src/BookingHotel.EntityFrameworkCore/Migrations/`
- [x] T005 [P] Định nghĩa các DTOs phê duyệt ngoại lệ (`CreateApprovalTicketDto`, `OperationalApprovalTicketDto`, `GetApprovalTicketListDto`, `ReviewApprovalTicketDto`) trong `BookingHotel/src/BookingHotel.Application.Contracts/Approvals/Dtos/`
- [x] T006 [P] Khai báo Interface `IOperationalApprovalAppService` kế thừa `IApplicationService` trong `BookingHotel/src/BookingHotel.Application.Contracts/Approvals/IOperationalApprovalAppService.cs`
- [x] T007 [P] Bổ sung 100% chuỗi bản địa hóa song ngữ cho 4 phân hệ quản trị, trạng thái phòng, tiêu đề portal và phiếu ngoại lệ trong `BookingHotel/src/BookingHotel.Domain.Shared/Localization/BookingHotel/vi.json` và `en.json`

---

## Phase 2: Foundational (Backend AppServices & Frontend Routing Infrastructure)

**Mục đích**: Triển khai tầng Application Service duyệt ngoại lệ, REST API Controller, và hạ tầng Guards, Signal Service phía Angular làm nền tảng cho 4 Portal.

**⚠️ CRITICAL**: Các User Story phía sau phụ thuộc trực tiếp vào các services và guards nền tảng này.

- [x] T008 Triển khai `OperationalApprovalAppService` kế thừa `ApplicationService` thực thi `GetListAsync`, `CreateAsync`, `ApproveAsync`, `RejectAsync` trong `BookingHotel/src/BookingHotel.Application/Approvals/OperationalApprovalAppService.cs`
- [x] T009 Xây dựng REST API Controller `OperationalApprovalController` tiếp xúc endpoints `/api/app/operational-approval` trong `BookingHotel/src/BookingHotel.HttpApi/Controllers/OperationalApprovalController.cs`
- [x] T010 [P] Xây dựng Angular service `OperationalApprovalService` gọi các REST API phiếu ngoại lệ trong `BookingHotel/angular/src/app/admin/services/operational-approval.service.ts`
- [x] T011 [P] Xây dựng Angular service `PropertyContextService` sử dụng Signal (`currentHotelId`, `assignedHotels`) đồng bộ `LocalStorage` trong `BookingHotel/angular/src/app/admin/services/property-context.service.ts`
- [x] T012 Triển khai `RoleBasedRedirectGuard` tự động nhận diện vai trò và điều hướng sau đăng nhập về đúng 4 root portals (`/admin`, `/tenant`, `/manager`, `/frontdesk`) trong `BookingHotel/angular/src/app/admin/guards/role-based-redirect.guard.ts`
- [x] T013 [P] Triển khai bộ 4 Route Guards bảo vệ độc lập: `systemAdminGuard`, `tenantAdminGuard`, `hotelManagerGuard`, `receptionistGuard` trong `BookingHotel/angular/src/app/admin/guards/role-guards.ts`

**Checkpoint**: Nền tảng Backend Services và Angular Guards/Services đã hoàn thiện - tiến hành xây dựng 4 Portal giao diện độc lập.

---

## Phase 3: User Story 1 - Không Gian Quản Trị Nền Tảng (System Admin Portal) (Priority: P1) 🎯 MVP

**Mục tiêu**: System Admin sở hữu layout shell độc lập *Platform Dark Control*, bảng điều khiển nền tảng tại URL `/admin/dashboard`, giám sát tenants, quản lý tài khoản Host và nhật ký an ninh.

**Independent Test**: Đăng nhập bằng `admin`, hệ thống điều hướng vào `/admin/dashboard`, hiển thị huy hiệu Host màu tím `#7C3AED`, mở danh sách tài khoản Host `/admin/users` và mở khóa thành công.

### Tests for User Story 1 ⚠️
- [x] T014 [P] [US1] Xây dựng Unit Tests kiểm tra `OperationalApprovalAppService` cho phân quyền và lọc theo cơ sở trong `BookingHotel/test/BookingHotel.Application.Tests/Approvals/OperationalApprovalAppServiceTests.cs`

### Implementation for User Story 1
- [x] T015 [US1] Xây dựng `SystemAdminLayoutComponent` với Sidebar Đen `#000000`, Điểm nhấn Cyan `#00FFFF`, Huy hiệu Host `#7C3AED` và menu Host (`/admin/dashboard`, `/admin/tenants`, `/admin/users`, `/admin/roles`) trong `BookingHotel/angular/src/app/admin/system/system-admin-layout/system-admin-layout.component.ts` (kèm HTML & SCSS)
- [x] T016 [US1] Xây dựng `SystemDashboardComponent` hiển thị thẻ thống kê nền tảng (Tổng Tenants, Tổng Cơ sở, Cảnh báo an ninh) trong `BookingHotel/angular/src/app/admin/system/system-dashboard/system-dashboard.component.ts` (kèm HTML & SCSS)
- [x] T017 [US1] Cấu hình tuyến đường `/admin/*` (`system.routes.ts`) tích hợp `systemAdminGuard`, nạp Dashboard, Tenants, Host Users, Roles và Audit Logs trong `BookingHotel/angular/src/app/admin/system/system.routes.ts`

**Checkpoint**: User Story 1 hoàn tất - System Admin Portal hoạt động độc lập và hiển thị chuẩn Concept Platform Dark Control (MVP).

---

## Phase 4: User Story 2 - Không Gian Quản Trị Chuỗi Khách Sạn (Tenant Admin Portal) (Priority: P1)

**Mục tiêu**: Tenant Admin làm việc tại `/tenant/*` với layout shell *Corporate Executive Dashboard*, quản lý danh mục chi nhánh, phân bổ nhân sự chuỗi và xem báo cáo hợp nhất (hoàn toàn không mang tiền tố `/admin`).

**Independent Test**: Đăng nhập bằng `tenantadmin_hcm`, tự động vào `/tenant/dashboard`, hiển thị huy hiệu Chuỗi `#F59E0B`, truy cập quản trị nhân sự chuỗi `/tenant/staff` độc lập.

### Implementation for User Story 2
- [x] T018 [US2] Xây dựng `TenantAdminLayoutComponent` với Sidebar Đen `#000000`, Điểm nhấn Cyan `#00FFFF`, Huy hiệu Chuỗi Vàng `#F59E0B` (`/tenant/dashboard`, `/tenant/hotels`, `/tenant/staff`, `/tenant/roles`) trong `BookingHotel/angular/src/app/admin/tenant/tenant-admin-layout/tenant-admin-layout.component.ts` (kèm HTML & SCSS)
- [x] T019 [US2] Xây dựng `TenantDashboardComponent` hiển thị các chỉ số kinh doanh toàn chuỗi (Doanh thu hợp nhất, Công suất buồng trung bình, Số lượng chi nhánh) trong `BookingHotel/angular/src/app/admin/tenant/tenant-dashboard/tenant-dashboard.component.ts` (kèm HTML & SCSS)
- [x] T020 [US2] Cấu hình tuyến đường `/tenant/*` (`tenant.routes.ts`) tích hợp `tenantAdminGuard`, nạp Dashboard, Danh mục Khách sạn, Quản trị nhân sự chuỗi, và Cơ cấu vai trò chỉ đọc trong `BookingHotel/angular/src/app/admin/tenant/tenant.routes.ts`

**Checkpoint**: User Story 2 hoàn tất - Tenant Admin Portal tách biệt hoàn toàn tại `/tenant/*`, hiển thị đúng huy hiệu chuỗi và bảo vệ phân quyền tuyệt đối.

---

## Phase 5: User Story 3 - Không Gian Điều Hành Cơ Sở & Duyệt Ngoại Lệ (Hotel Manager Portal) (Priority: P1)

**Mục tiêu**: Hotel Manager làm việc tại `/manager/*` với layout shell *Operational Manager Console*, bộ chọn chi nhánh (`Property Selector`) trên Topbar, xem tình trạng buồng phòng, bảng giá và phê duyệt phiếu ngoại lệ tác nghiệp (hoàn toàn không mang tiền tố `/admin`).

**Independent Test**: Đăng nhập bằng `manager_riverside`, chuyển đổi giữa `Riverside Hotel` và `Central Hotel` trên Topbar, dữ liệu trang phản ứng tức thì; mở màn hình `/manager/approvals` và thực hiện Duyệt/Từ chối phiếu ngoại lệ.

### Implementation for User Story 3
- [x] T021 [US3] Xây dựng `HotelManagerLayoutComponent` tích hợp Topbar Property Selector dropdown kết nối `PropertyContextService` (`/manager/dashboard`, `/manager/rooms`, `/manager/rates`, `/manager/approvals`) trong `BookingHotel/angular/src/app/admin/manager/hotel-manager-layout/hotel-manager-layout.component.ts` (kèm HTML & SCSS)
- [x] T022 [US3] Xây dựng `ManagerDashboardComponent` hiển thị chỉ số điều hành cơ sở (Công suất phòng hôm nay, Đơn check-in/out dự kiến, Phiếu chờ duyệt) trong `BookingHotel/angular/src/app/admin/manager/manager-dashboard/manager-dashboard.component.ts` (kèm HTML & SCSS)
- [x] T023 [US3] Xây dựng màn hình `ManagerApprovalsComponent` hiển thị danh sách phiếu yêu cầu ngoại lệ tác nghiệp kèm nút Chấp thuận / Từ chối (kèm modal nhập lý do) trong `BookingHotel/angular/src/app/admin/manager/manager-approvals/manager-approvals.component.ts` (kèm HTML & SCSS)
- [x] T024 [US3] Cấu hình tuyến đường `/manager/*` (`manager.routes.ts`) tích hợp `hotelManagerGuard`, nạp Dashboard, Quản lý buồng phòng, Bảng giá, Duyệt ngoại lệ và Ca trực trong `BookingHotel/angular/src/app/admin/manager/manager.routes.ts`

**Checkpoint**: User Story 3 hoàn tất - Hotel Manager Portal chuyển đổi cơ sở mượt mà tại `/manager/*` và phê duyệt ngoại lệ tác nghiệp thời gian thực.

---

## Phase 6: User Story 4 - Không Gian Tác Nghiệp Quầy Lễ Tân (Receptionist Portal) (Priority: P1)

**Mục tiêu**: Lễ tân làm việc tại `/frontdesk/*` với layout shell *Frontdesk Speed Operation* (Xanh lá tươi nhạt `#99FF99`, Xám `#808080`, Chữ Đen `#000000`), Sơ đồ buồng phòng trực quan (Matrix Grid mặc định + Table toggle) và tạo phiếu yêu cầu ngoại lệ (hoàn toàn không mang tiền tố `/admin`).

**Independent Test**: Đăng nhập bằng `receptionist_river`, tự động vào `/frontdesk/room-rack`, hiển thị lưới phòng phân màu trạng thái, gạt sang bảng dữ liệu phân trang, mở modal tạo phiếu xin chiết khấu 15% gửi thành công.

### Implementation for User Story 4
- [x] T025 [US4] Xây dựng `ReceptionistLayoutComponent` theo chuẩn Concept Frontdesk Speed Operation (Màu sắc `#99FF99`, `#808080`, Chữ Đen `#000000`, độ tương phản $\ge 4.5:1$, menu `/frontdesk/room-rack`, `/frontdesk/check-in`, `/frontdesk/check-out`, `/frontdesk/folios`) trong `BookingHotel/angular/src/app/admin/frontdesk/receptionist-layout/receptionist-layout.component.ts` (kèm HTML & SCSS)
- [x] T026 [US4] Xây dựng component `RoomRackComponent` hỗ trợ 2 chế độ hiển thị: Visual Matrix Grid phân màu trạng thái (`#99FF99`, `#808080`, `#FF6B6B`, `#FFD166`) và Bảng dữ liệu phân trang (`BhDataTableComponent`) trong `BookingHotel/angular/src/app/admin/frontdesk/room-rack/room-rack.component.ts` (kèm HTML & SCSS)
- [x] T027 [US4] Tích hợp modal `FrontdeskApprovalModalComponent` cho phép Lễ tân gửi yêu cầu phê duyệt ngoại lệ (chiết khấu, miễn phạt hủy phòng) ngay trên quầy trong `BookingHotel/angular/src/app/admin/frontdesk/components/frontdesk-approval-modal.component.ts` (kèm HTML & SCSS)
- [x] T028 [US4] Cấu hình tuyến đường `/frontdesk/*` (`frontdesk.routes.ts`) tích hợp `receptionistGuard`, nạp Room Rack, Check-in nhanh, Check-out nhanh và Hóa đơn đang mở trong `BookingHotel/angular/src/app/admin/frontdesk/frontdesk.routes.ts`

**Checkpoint**: User Story 4 hoàn tất - Lễ tân thao tác tốc độ cao trên Sơ đồ buồng phòng tại `/frontdesk/*` và gửi phiếu yêu cầu ngoại lệ thành công.

---

## Phase 7: User Story 5 - Chống Xâm Phạm Ranh Giới, Nhân Sự Kiêm Nhiệm & Điều Hướng Tổng Hợp (Priority: P1)

**Mục tiêu**: Hoàn thiện nút chuyển đổi nhanh Portal cho nhân sự kiêm nhiệm, cấu hình tổng hợp 4 Root Namespaces độc lập trên `app.routes.ts`, và kiểm soát an ninh 3 tầng chống leo thang đặc quyền.

**Independent Test**: Đăng nhập bằng tài khoản `dual_staff`, tự động vào Manager Portal `/manager/dashboard`, thấy nút `[ Quầy Lễ Tân (Frontdesk) 🡒 ]` trên Topbar, bấm chuyển mượt mà sang `/frontdesk/room-rack`; cố tình gõ URL `/admin/users` bị chặn 403.

### Implementation for User Story 5
- [x] T029 [US5] Xây dựng component `BhPortalSwitcherComponent` hiển thị pill button chuyển đổi nhanh giữa `/manager/dashboard` và `/frontdesk/room-rack` cho nhân sự kiêm nhiệm trong `BookingHotel/angular/src/app/shared/components/portal-switcher/bh-portal-switcher.component.ts` (kèm HTML & SCSS)
- [x] T030 [US5] Tích hợp `BhPortalSwitcherComponent` vào Topbar của `HotelManagerLayoutComponent` và `ReceptionistLayoutComponent`
- [x] T031 [US5] Cập nhật file định tuyến tổng hợp `app.routes.ts` liên kết 4 namespaces độc lập (`/admin`, `/tenant`, `/manager`, `/frontdesk`) với lazy-loading độc lập và `RoleBasedRedirectGuard` tại tuyến gốc trong `BookingHotel/angular/src/app/app.routes.ts`
- [x] T032 [US5] Triển khai xử lý lỗi truy cập trái phép: Toastr cảnh báo vi phạm quyền và fallback chuyển hướng an toàn về Portal hợp lệ trong các Route Guards

---

## Phase 8: Polish, Verification & Quality Gates

**Mục đích**: Hoàn tất dữ liệu khởi tạo, kiểm thử toàn diện, xác thực độ tương phản giao diện và đóng gói hoàn thiện.

- [x] T033 Bổ sung dữ liệu mẫu cho phiếu ngoại lệ tác nghiệp trong `BookingHotelDataSeedContributor.cs`
- [x] T034 [P] Kiểm tra thẩm định tương phản màu sắc đạt chuẩn WCAG 2.1 AA ($\ge 4.5:1$) cho toàn bộ các màn hình Lễ tân và Quản trị
- [x] T035 Chạy kiểm thử tự động toàn bộ giải pháp Backend (`dotnet test`): Đảm bảo 100% tests PASS không lỗi
- [x] T036 Chạy kiểm thử biên dịch Frontend Angular (`npm run build`): Đảm bảo 0 errors, 0 warnings

---

## Checkpoint Tinh Gọn & Thứ Tự Triển Khai
1. **Phase 1 & 2**: Nền tảng Backend + Guards/Services (`T001` - `T013`) -> **HOÀN TẤT**.
2. **Phase 3**: System Admin Portal MVP (`T014` - `T017`) -> **HOÀN TẤT** (`/admin/*`).
3. **Phase 4**: Tenant Admin Portal (`T018` - `T020`) -> **HOÀN TẤT** (`/tenant/*`).
4. **Phase 5**: Hotel Manager Portal & Approvals (`T021` - `T024`) -> **HOÀN TẤT** (`/manager/*`).
5. **Phase 6**: Receptionist Portal & Matrix Grid (`T025` - `T028`) -> **HOÀN TẤT** (`/frontdesk/*`).
6. **Phase 7**: Dual-Role Switcher & Smart Routing (`T029` - `T032`) -> **HOÀN TẤT** (`app.routes.ts`).
7. **Phase 8**: Verification, Testing & Build (`T033` - `T036`) -> **HOÀN TẤT** (57/57 tests PASS, build 0 errors, 0 warnings).
