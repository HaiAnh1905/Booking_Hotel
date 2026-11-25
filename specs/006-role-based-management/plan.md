# Implementation Plan: RM-006 - Phân Tách Không Gian & Thiết Kế Giao Diện Quản Lý Theo 4 Vai Trò

**Branch**: `006-role-based-management` | **Date**: 2026-11-13 | **Spec**: [specs/006-role-based-management/spec.md](spec.md)  
**Input**: Feature specification from `specs/006-role-based-management/spec.md` và kết quả làm rõ kiến trúc từ Clarification Sessions 1 & 2.

---

## 1. Summary

Triển khai kiến trúc phân tách không gian giao diện quản lý và phân quyền phân hệ theo 4 vai trò quản trị trên nền tảng ABP Framework 10.7 (.NET 10 / EF Core / Angular 19+ Standalone) tuân thủ nghiêm ngặt Hiến pháp dự án `Booking Hotel`:
1. **Phân Tách 4 Route Namespaces Độc Lập & 4 Layout Shells Chuyên Biệt**:
   - `/admin/*` $\rightarrow$ `SystemAdminLayoutComponent` (*Platform Dark Control*: Đen `#000000`, Cyan `#00FFFF`, Badge Host Tím `#7C3AED`).
   - `/tenant/*` $\rightarrow$ `TenantAdminLayoutComponent` (*Corporate Executive Dashboard*: Đen `#000000`, Cyan `#00FFFF`, Badge Chuỗi Vàng `#F59E0B`).
   - `/manager/*` $\rightarrow$ `HotelManagerLayoutComponent` (*Operational Manager Console*: Đen `#000000`, Cyan `#00FFFF`, Topbar Property Selector).
   - `/frontdesk/*` $\rightarrow$ `ReceptionistLayoutComponent` (*Frontdesk Speed Operation*: Xanh lá tươi nhạt `#99FF99`, Xám `#808080`, Chữ Đen `#000000`, độ tương phản WCAG 2.1 AA $\ge 4.5:1$).
2. **Cơ Chế Điều Hướng Thông Minh & Bảo Vệ 3 Tầng (3-Layer Defense)**:
   - `RoleBasedRedirectGuard` tại tuyến đường `/admin` gốc: Tự động nhận diện vai trò trong phiên và điều hướng đến Dashboard/Room-Rack phù hợp nhất.
   - Bộ 4 Route Guards chuyên trách (`systemAdminGuard`, `tenantAdminGuard`, `hotelManagerGuard`, `receptionistGuard`): Ngăn chặn 100% việc truy cập chéo không gian bằng cách ép URL.
   - Backend Authorization: Mọi API endpoint kiểm tra chặt chẽ `[Authorize(BookingHotelPermissions...)]`, Tenant Scoping và Property Scoping.
3. **Quản Lý Ngữ Cảnh Cơ Sở (`PropertyContextService`) Cho Hotel Manager**:
   - Quản lý trạng thái bằng Angular Signal (`currentHotelId`, `assignedHotels`), đồng bộ hai chiều với `LocalStorage`.
   - Giữ nguyên cấu trúc URL sạch `/admin/manager/rooms`, `/admin/manager/rates`, v.v., các trang tự động reactive filtering theo cơ sở đang chọn.
4. **Giao Diện Sơ Đồ Buồng Phòng Lễ Tân (Visual Matrix Grid + Data Table Toggle)**:
   - Chế độ mặc định: Lưới trực quan phân màu trạng thái thời gian thực (`#99FF99` Trống/Sẵn sàng, `#808080` Phòng bẩn, `#FF6B6B` Đang có khách, `#FFD166` Đã đặt trước).
   - Chế độ danh sách: Tích hợp nút Toggle chuyển sang `BhDataTableComponent` có phân trang, bộ lọc nhanh phục vụ tìm kiếm buồng phòng tốc độ cao.
5. **Quy Trình Phê Duyệt Ngoại Lệ Tác Nghiệp (Operational Exception Approval Workflow)**:
   - Thực thể `OperationalApprovalTicket` (`FullAuditedEntity<Guid>, IMultiTenant`): Lễ tân gửi yêu cầu ngoại lệ (chiết khấu, hủy cọc) từ quầy; Hotel Manager xem xét và duyệt/từ chối tại `/admin/manager/approvals`.
6. **Hỗ Trợ Nhân Sự Kiêm Nhiệm Hai Vai Trò (Dual-Role Personnel)**:
   - Ưu tiên vào không gian Manager khi đăng nhập; Topbar hiển thị nút "Portal Switcher Pill" để chuyển đổi tức thời giữa Quản lý và Lễ tân.

---

## 2. Technical Context

- **Language / Version**: C# .NET 10, TypeScript 5.x / Angular 19+ (Standalone Components).
- **Primary Dependencies**:
  - ABP Framework 10.7 (`Volo.Abp.Identity`, `Volo.Abp.Authorization`, `Volo.Abp.MultiTenancy`, `Volo.Abp.PermissionManagement`).
  - Entity Framework Core 10.x.
  - Angular Router, Signals, Reactive Forms, `@abp/ng.core`.
  - Design Tokens: CSS Variables từ `RM-004` (`_variables.scss`, `_tokens.scss`).
- **Storage**: SQL Server 2022+ / LocalDB (`localhost\SQLEXPRESS`), EF Core Code-First Migrations qua `BookingHotel.DbMigrator`.
- **Testing Framework**:
  - Backend: xUnit, Shouldly, ABP Integrated TestBase (`BookingHotel.Application.Tests`).
  - Frontend: Angular Build Verification (`npm run build`), Karma/Jasmine Component Tests.
- **Target Platform**: ASP.NET Core Web API Host (`localhost:44322`) & Angular Single Page App (`localhost:4200`).
- **Constraints & Performance**:
  - Tải trang và chuyển đổi Portal < 200ms nhờ chia nhỏ lazy-loaded chunks.
  - Zero Credential Pre-filling (Constitution IX).
  - Thương hiệu chuẩn xác: `Booking Hotel` có dấu cách (Constitution VIII).
  - Tương phản giao diện chuẩn WCAG 2.1 AA ($\ge 4.5:1$).

---

## 3. Constitution Check

*GATE: Kiểm tra tính tuân thủ 9 Điều khoản của Hiến pháp dự án trong `constitution.md`*

| Điều khoản Hiến pháp | Tiêu chí đánh giá | Trạng thái | Giải pháp kỹ thuật tương ứng |
| :--- | :--- | :---: | :--- |
| **I. Phân lớp DDD & ABP** | Đúng tầng, tách biệt rõ ràng Domain, Contracts, Application, HttpApi, EntityFrameworkCore | **PASS** | `OperationalApprovalTicket` tại `Domain/Approvals`; DTOs/Interfaces tại `Application.Contracts/Approvals`; AppService tại `Application/Approvals`; Controllers tại `HttpApi/Controllers/Approvals`; DbContext mapping tại `EntityFrameworkCore`. |
| **II. Contract-First & Security** | Bắt buộc kiểm tra quyền, validation chặt chẽ DTO | **PASS** | Tất cả endpoint đều kiểm tra quyền `BookingHotelPermissions.Bookings.ApproveException`, `BookingHotelPermissions.Bookings.Default` và Tenant/Hotel Scoping. DTOs có Data Annotations đầy đủ. |
| **III. EF Core Migrations** | Quản lý schema bằng Code-First Migrations qua DbMigrator | **PASS** | Thêm migration `AddOperationalApprovalTicketTable` và áp dụng bằng `dotnet run --project BookingHotel.DbMigrator`. |
| **IV. Chuẩn hóa Frontend** | Thin presentation, Reactive Forms, Signals, Strict TS | **PASS** | 4 Layout Shells độc lập, sử dụng Angular Signals cho `PropertyContextService` và UI state, tách biệt scss riêng cho từng không gian. |
| **V. Đa ngôn ngữ (i18n)** | Không hardcode chuỗi hiển thị giao diện | **PASS** | 100% nhãn menu, tiêu đề portal, trạng thái phòng, nội dung ticket và thông báo lỗi được định nghĩa tại `vi.json` và `en.json`. |
| **VI. Backend-Authoritative** | Toàn quyền kiểm soát và xác thực tại Backend | **PASS** | Route Guards chỉ là lớp bảo vệ điều hướng phía client; toàn bộ quyết định phê duyệt ngoại lệ, quyền xem dữ liệu cơ sở đều kiểm tra authoritative tại Backend. |
| **VII. UI/UX Concept Hệ Màu** | Nhận diện thương hiệu theo phân hệ, WCAG 2.1 AA $\ge 4.5:1$ | **PASS** | Phân hệ Quản trị (Admin System, Admin Tenant, Hotel Manager) áp dụng Đen `#000000`, Cyan `#00FFFF`, Trắng `#FFFFFF`. Phân hệ Lễ tân áp dụng Xanh lá `#99FF99`, Xám `#808080`, Chữ Đen `#000000`. |
| **VIII. Brand Naming** | Chuẩn hóa thương hiệu `Booking Hotel` | **PASS** | Tất cả Topbar, Sidebar header, trang đăng nhập hiển thị chính xác `Booking Hotel` có dấu cách. |
| **IX. Bảo Mật Phiên & Unlock** | Zero credential pre-filling & chống leo thang đặc quyền | **PASS** | Phân định rõ ràng: Tenant Admin chỉ quản lý nhân sự thuộc chuỗi mình; Hotel Manager & Receptionist bị chặn hoàn toàn khỏi màn hình Identity & Phân quyền. |

---

## 4. Project Structure & Code Mapping

### Tài liệu Đặc tả & Thiết kế (Documentation Artifacts)
```text
BookingHotel/specs/006-role-based-management/
├── spec.md                          # Đặc tả yêu cầu chức năng hoàn chỉnh
├── checklists/
│   └── requirements.md              # Thẩm định chất lượng đặc tả
├── plan.md                          # Kế hoạch kiến trúc kỹ thuật (Tài liệu này)
├── research.md                      # Báo cáo nghiên cứu giải pháp kỹ thuật
├── data-model.md                    # Thiết kế mô hình dữ liệu & sơ đồ luồng
├── contracts/
│   ├── portal-routes.md             # Đặc tả chi tiết 4 tuyến đường & Layout bindings
│   └── operational-approvals.md     # Đặc tả API duyệt ngoại lệ tác nghiệp
└── quickstart.md                    # Hướng dẫn xác minh và kịch bản kiểm thử
```

### Mã Nguồn Backend (.NET 10 / ABP Framework)
```text
BookingHotel/src/
├── BookingHotel.Domain.Shared/
│   ├── Approvals/
│   │   ├── ApprovalTicketType.cs            # Enum: DiscountOverride, PenaltyWaiver, EarlyCheckIn, etc.
│   │   └── ApprovalTicketStatus.cs          # Enum: Pending, Approved, Rejected, Cancelled
│   └── Permissions/
│       └── BookingHotelPermissions.cs       # Đã có ApproveException; bổ sung Approvals constants
├── BookingHotel.Domain/
│   └── Approvals/
│       └── OperationalApprovalTicket.cs     # Entity: FullAuditedEntity<Guid>, IMultiTenant
├── BookingHotel.EntityFrameworkCore/
│   ├── EntityFrameworkCore/
│   │   └── BookingHotelDbContext.cs         # DbSet<OperationalApprovalTicket>, indexing & relations
│   └── Migrations/
│       └── 20261113XXXXXX_AddOperationalApprovalTicket.cs
├── BookingHotel.Application.Contracts/
│   └── Approvals/
│       ├── IOperationalApprovalAppService.cs
│       ├── Dtos/
│       │   ├── CreateApprovalTicketDto.cs
│       │   ├── OperationalApprovalTicketDto.cs
│       │   ├── GetApprovalTicketListDto.cs
│       │   └── ReviewApprovalTicketDto.cs
│       └── Permissions/
│           └── BookingHotelPermissionDefinitionProvider.cs
├── BookingHotel.Application/
│   └── Approvals/
│       └── OperationalApprovalAppService.cs # AppService: GetList, Create, Approve, Reject
└── BookingHotel.HttpApi/
    └── Controllers/
        └── OperationalApprovalController.cs # REST Controller: /api/app/operational-approval
```

### Mã Nguồn Frontend (Angular 19+ Standalone)
```text
BookingHotel/angular/src/app/
├── app.routes.ts                            # Cấu hình 4 Root Namespaces: /admin, /tenant, /manager, /frontdesk
├── admin/
│   ├── admin.routes.ts                      # Cấu hình định tuyến tương thích /admin
│   ├── guards/
│   │   ├── role-based-redirect.guard.ts     # Guard điều hướng thông minh sau đăng nhập
│   │   ├── system-admin.guard.ts            # Guard bảo vệ /admin/*
│   │   ├── tenant-admin.guard.ts            # Guard bảo vệ /tenant/*
│   │   ├── hotel-manager.guard.ts           # Guard bảo vệ /manager/*
│   │   └── receptionist.guard.ts            # Guard bảo vệ /frontdesk/*
│   ├── services/
│   │   ├── property-context.service.ts      # Quản lý Signal khách sạn đang chọn & LocalStorage
│   │   └── operational-approval.service.ts  # Service gọi API duyệt ngoại lệ
│   ├── system/
│   │   ├── system-admin-layout/             # Layout Shell riêng cho Host Admin
│   │   ├── system-dashboard/                # Dashboard giám sát hạ tầng
│   │   └── system.routes.ts                 # Tuyến đường /admin/*
│   ├── tenant/
│   │   ├── tenant-admin-layout/             # Layout Shell riêng cho Quản trị chuỗi
│   │   ├── tenant-dashboard/                # Dashboard kinh doanh toàn chuỗi
│   │   └── tenant.routes.ts                 # Tuyến đường /tenant/*
│   ├── manager/
│   │   ├── hotel-manager-layout/            # Layout Shell riêng cho Quản lý cơ sở (Property Selector)
│   │   ├── manager-dashboard/               # Dashboard điều hành cơ sở
│   │   ├── manager-approvals/               # Màn hình duyệt ngoại lệ tác nghiệp
│   │   └── manager.routes.ts                # Tuyến đường /manager/*
│   └── frontdesk/
│       ├── receptionist-layout/             # Layout Shell riêng cho Lễ tân (#99FF99)
│       ├── room-rack/                       # Sơ đồ buồng phòng (Matrix Grid + Table toggle)
│       ├── frontdesk-approvals/             # Modal gửi yêu cầu phê duyệt ngoại lệ
│       └── frontdesk.routes.ts              # Tuyến đường /frontdesk/*
└── shared/
    └── components/
        └── portal-switcher/                 # Component nút chuyển đổi nhanh Portal cho nhân sự kiêm nhiệm
```

---

## 5. Phase-by-Phase Implementation Plan

### Phase 0: Research & Architectural Proofs (Hoàn tất tại `research.md`)
- Khảo sát mô hình Lazy-Loading Route Namespaces độc lập trong Angular 19+.
- Khảo sát Signal-based State Management cho `PropertyContextService` kết hợp đồng bộ hóa LocalStorage.
- Khảo sát giải pháp đảm bảo tương phản WCAG 2.1 AA ($\ge 4.5:1$) cho gam màu Xanh lá tươi nhạt `#99FF99` và Xám `#808080` của Phân hệ Lễ tân.
- Khảo sát kiến trúc Ticket-based Approval Workflow chống gian lận thu ngân.

### Phase 1: Contracts & Data Model Design (Hoàn tất tại `data-model.md` & `contracts/`)
- Thiết kế thực thể `OperationalApprovalTicket` với đầy đủ Audit fields và Tenant scoping.
- Đặc tả API DTOs và REST endpoints trong `operational-approvals.md`.
- Đặc tả bảng ánh xạ 4 tuyến đường, guard, layout binding và menu điều hướng trong `portal-routes.md`.
- Chuẩn bị kịch bản kiểm thử tích hợp trong `quickstart.md`.

### Phase 2: Backend Architecture & Implementation
1. **Domain Layer**:
   - Thêm Enum `ApprovalTicketType` và `ApprovalTicketStatus` tại `BookingHotel.Domain.Shared/Approvals`.
   - Tạo thực thể `OperationalApprovalTicket` kế thừa `FullAuditedEntity<Guid>`, cài đặt `IMultiTenant`.
2. **EntityFrameworkCore Layer**:
   - Cấu hình quan hệ và chỉ mục trong `BookingHotelDbContext.cs` (`HotelId`, `Status`, `RequestedByStaffId`).
   - Tạo và áp dụng EF Core Migration `AddOperationalApprovalTicketTable`.
3. **Application Layer & Permissions**:
   - Khai báo DTOs: `CreateApprovalTicketDto`, `OperationalApprovalTicketDto`, `GetApprovalTicketListDto`, `ReviewApprovalTicketDto`.
   - Triển khai `OperationalApprovalAppService` thực thi logic:
     - Lễ tân gửi ticket: Kiểm tra `HotelId` hợp lệ, gán `Status = Pending`.
     - Hotel Manager duyệt ticket: Kiểm tra quyền `BookingHotelPermissions.Bookings.ApproveException`, xác thực Manager có phụ trách `HotelId` của ticket, cập nhật `Status = Approved`.
     - Hotel Manager từ chối ticket: Bắt buộc nhập lý do `RejectionReason`, cập nhật `Status = Rejected`.
4. **HttpApi Layer**:
   - Tạo `OperationalApprovalController` tiếp xúc endpoints RESTful tại `/api/app/operational-approval`.
5. **Backend Verification**:
   - Viết xUnit unit/integration tests kiểm thử luồng tạo, duyệt và chặn truy cập chéo khách sạn trong `BookingHotel.Application.Tests`.

### Phase 3: Frontend Architecture, Route Guards & Layout Shells
1. **Route Guards**:
   - Triển khai `RoleBasedRedirectGuard` tại tuyến `/admin`: Đọc vai trò người dùng hiện tại từ session, điều hướng thông minh.
   - Triển khai 4 Guards chuyên biệt: `systemAdminGuard`, `tenantAdminGuard`, `hotelManagerGuard`, `receptionistGuard`.
2. **Layout Shells (4 Phân Hệ Độc Lập)**:
   - `SystemAdminLayoutComponent`: Theme *Platform Dark Control* (Sidebar `#000000`, Cyan `#00FFFF`, Badge Host Tím `#7C3AED`).
   - `TenantAdminLayoutComponent`: Theme *Corporate Executive Dashboard* (Sidebar `#000000`, Cyan `#00FFFF`, Badge Chuỗi Vàng `#F59E0B`).
   - `HotelManagerLayoutComponent`: Theme *Operational Manager Console* (Sidebar `#000000`, Cyan `#00FFFF`, Tích hợp `PropertyContextService` Dropdown, Portal Switcher Pill).
   - `ReceptionistLayoutComponent`: Theme *Frontdesk Speed Operation* (Accent `#99FF99`, Borders `#808080`, High-contrast text `#000000`).
3. **Route Configuration (`admin.routes.ts`)**:
   - Tách biệt rõ ràng 4 namespace: `/admin/system`, `/admin/tenant`, `/admin/manager`, `/admin/frontdesk` với lazy loading.

### Phase 4: Property Context Service & Manager Portal
1. **`PropertyContextService`**:
   - Tích hợp Angular Signals: `currentHotelId = signal<string | null>(null)`, `assignedHotels = signal<HotelItemDto[]>([])`.
   - Tự động nạp danh sách cơ sở từ user profile / assignments; lưu lựa chọn vào `localStorage.getItem('bh_active_hotel_id')`.
   - Cung cấp phương thức `setActiveHotel(hotelId: string)`.
2. **Manager Experience**:
   - Tích hợp Dropdown chọn chi nhánh trên Topbar của `HotelManagerLayoutComponent`.
   - Xây dựng màn hình `manager-dashboard` hiển thị thống kê cơ sở.
   - Xây dựng màn hình `manager-approvals` hiển thị danh sách ticket ngoại lệ kèm nút Duyệt / Từ chối (kèm modal lý do).

### Phase 5: Frontdesk Receptionist Experience & Approval Modal
1. **Sơ Đồ Buồng Phòng (`room-rack`)**:
   - Triển khai chế độ xem Visual Matrix Grid hiển thị thẻ buồng phòng phân màu nhận diện trạng thái (`#99FF99` Trống, `#808080` Chờ dọn, `#FF6B6B` Đang ở, `#FFD166` Đã đặt).
   - Nút Toggle Switch chuyển sang bảng phân trang `BhDataTableComponent`.
   - Thao tác nhanh: Check-in, Check-out, Cập nhật trạng thái phòng.
2. **Phiếu Yêu Cầu Duyệt Ngoại Lệ**:
   - Modal gửi yêu cầu ngoại lệ (loại yêu cầu, lý do, tỷ lệ chiết khấu đề xuất).
   - Thông báo trạng thái ticket (Pending, Approved, Rejected).

### Phase 6: System Admin & Tenant Admin Portals
1. **System Admin Portal**:
   - Tích hợp lại các màn hình Identity Host (`/admin/system/users`, `/admin/system/roles`) từ RM-005.
   - Màn hình `system-dashboard` và liên kết phân hệ Tenants.
2. **Tenant Admin Portal**:
   - Tích hợp lại màn hình quản lý nhân sự chuỗi (`/admin/tenant/staff`) từ RM-005.
   - Màn hình `tenant-dashboard` và quản lý danh mục chi nhánh khách sạn.

### Phase 7: Dual-Role Switcher & Verification
1. **Component `BhPortalSwitcherComponent`**:
   - Hiển thị pill button khi tài khoản có cả 2 vai trò `HotelManager` và `Receptionist`.
   - Nhấn nút cho phép chuyển đổi mượt mà giữa `/admin/manager/dashboard` và `/admin/frontdesk/room-rack`.
2. **Bản Địa Hóa Song Ngữ (i18n)**:
   - Cập nhật 100% nhãn menu, tiêu đề, trạng thái ticket vào `vi.json` và `en.json`.
3. **Verification**:
   - Chạy toàn bộ test suites backend: `dotnet test`.
   - Kiểm tra Angular build: `npm run build`.

---

## 6. Verification & Quality Gates

1. **Gate 1 - Build & Migration**:
   - Migration `AddOperationalApprovalTicketTable` thực thi thành công không lỗi.
   - Toàn bộ backend solution biên dịch sạch 0 lỗi (`dotnet build`).
2. **Gate 2 - Unit & Integration Tests**:
   - Chạy toàn bộ xUnit tests, bao gồm test mới cho `OperationalApprovalAppService` và kiểm tra quyền: 100% PASS.
3. **Gate 3 - Frontend Build & Accessibility**:
   - `npm run build` hoàn tất 0 error, 0 warning.
   - Tương phản màu sắc tại Phân hệ Lễ tân và Phân hệ Quản trị đạt chuẩn WCAG 2.1 AA ($\ge 4.5:1$).
4. **Gate 4 - Security & Isolation Validation**:
   - Kiểm thử truy cập chéo giữa 4 vai trò: 100% bị chặn bởi Route Guards và Backend Authorization (HTTP 403).

