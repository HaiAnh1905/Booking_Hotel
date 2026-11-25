# Implementation Plan: RM-007/RM-009 - Quản Lý Gói Giá Chuỗi Cho Admin Tenant (Tenant Master Rate Plans Management)

**Branch**: `009-tenant-rate-plan-management` | **Date**: 2026-11-20 | **Spec**: [specs/009-tenant-rate-plan-management/spec.md](spec.md)  
**Input**: Feature specification từ `spec.md`, các quyết định làm rõ từ Session Clarification, mô hình dữ liệu (`data-model.md`), hợp đồng API (`contracts/`), và kịch bản kiểm thử (`quickstart.md`).

---

## 1. Summary

Xây dựng và hoàn thiện phân hệ **Quản Lý Gói Giá Chuỗi (Master Rate Plans)** dành cho vai trò **Quản trị viên Chuỗi (`TenantAdmin`)** trên nền tảng ABP Framework 10.7 (.NET 10 / EF Core / Angular 19+ Standalone), đáp ứng đầy đủ yêu cầu nghiệp vụ và tuân thủ 100% Hiến pháp dự án `Booking Hotel`:
1. **Quản Trị Danh Mục Gói Giá Chuỗi (Master Catalog Management)**:
   - Cung cấp giao diện bảng quản trị trực quan tại route `/tenant/rate-plans`.
   - Cho phép Admin Chuỗi: Xem danh sách, Tạo mới, Chỉnh sửa, và Xóa mềm (Soft Delete) các gói giá phòng gốc của toàn chuỗi (`RatePlan`).
   - Cấu hình chuẩn hóa các thuộc tính cốt lõi: Tên gói, Mã gói (`Code` - Unique trong Tenant), Mô tả, Chính sách hoàn/hủy (`IsRefundable`, `CancellationCutoffHours`), Dịch vụ kèm theo (`IncludesBreakfast`), và Tỷ lệ điều chỉnh giá cơ bản mặc định (`DefaultPriceAdjustmentPercent`).
2. **Kế Thừa & Đồng Bộ Tức Thì Xuống Các Chi Nhánh (Instant Hotel Propagation)**:
   - Dữ liệu `RatePlan` gắn với `TenantId`. Khi Admin Chuỗi thêm/sửa gói giá, API của Quản lý chi nhánh (`ManagerRateAppService.GetRatePlansAsync`) lập tức phản ánh gói mới tại trang `/manager/rates`.
   - Tự động tắt mở bán (`HotelRatePlan.IsActive = false`) tại tất cả các cơ sở khi gói giá bị xóa, bảo toàn toàn bộ đơn đặt phòng lịch sử phục vụ kiểm toán tài chính.
3. **Phân Quyền & Điều Hướng (Navigation & Security)**:
   - Bổ sung mục menu **"Quản lý Gói Giá"** (`Menu:TenantRatePlans`) vào thanh điều hướng Sidebar bên trái của phân hệ Admin Tenant (cùng cấp với *Khách sạn, Nhân sự, Đặt phòng, Báo cáo, Cài đặt*).
   - Bảo vệ route bằng `tenantAdminGuard` ở Frontend và `[Authorize(BookingHotelPermissions.Tenants.Manage)]` ở Backend.

---

## 2. Technical Context

- **Ngôn ngữ / Phiên bản**: C# .NET 10, TypeScript 5.x / Angular 19+ (Standalone Components).
- **Phụ thuộc chính**:
  - ABP Framework 10.7 (`Volo.Abp.MultiTenancy`, `Volo.Abp.Domain`, `Volo.Abp.Application`, `Volo.Abp.AspNetCore.Mvc`).
  - Entity Framework Core 10.x.
  - Angular Router, Signals, Reactive Forms, `@abp/ng.core`.
  - Design Tokens: Concept Quản lý Chuỗi (Nền Trắng `#FFFFFF`, Sidebar Đen `#000000`, Điểm nhấn Cyan `#00FFFF` / Primary `#0284c7`).
- **Lưu trữ CSDL**: SQL Server 2022+ / LocalDB, bảng `AppRatePlans` và `AppHotelRatePlans` có sẵn `TenantId` (`IMultiTenant`).
- **Testing & Verification**:
  - Backend: Build check `dotnet build --no-restore -m:1`.
  - Frontend: TypeScript compilation check `npx tsc -p tsconfig.app.json --noEmit`.
- **Mục tiêu hiệu năng**: Thời gian phản hồi API CRUD < 300ms, tải trang < 1.0s.

---

## 3. Constitution Check (Tuân Thủ Hiến Pháp Dự Án)

*GATE: Kiểm tra 11 Điều khoản của Hiến pháp dự án tại `.specify/memory/constitution.md`*

| Điều khoản Hiến pháp | Tiêu chí đánh giá | Trạng thái | Giải pháp kỹ thuật tương ứng |
| :--- | :--- | :---: | :--- |
| **I. Phân lớp DDD & ABP** | Tách biệt Domain, Contracts, Application, HttpApi | **PASS** | Tái sử dụng Entity `RatePlan` tại Domain; Khai báo `ITenantRatePlanAppService` & DTOs tại Contracts; Viết `TenantRatePlanAppService` tại Application; Viết `TenantRatePlanController` tại HttpApi. |
| **II. Contract-First & Security** | Kiểm tra quyền ABP, Data Annotations, Audit Logging | **PASS** | Sử dụng DTOs validation (`[Required]`, `[StringLength]`); kế thừa `FullAuditedAggregateRoot<Guid>`; áp dụng `[Authorize]`. |
| **III. EF Core Migrations** | Quản lý schema bằng Code-First Migrations | **PASS** | Entity `RatePlan` đã tồn tại trong DbContext và đã có migration `20261119081755_AddHotelManagerOperationsEntities`; không phát sinh thay đổi schema CSDL. |
| **IV. Chuẩn hóa Frontend** | Thin presentation, Angular Signals, Strict TS | **PASS** | Xây dựng standalone component `TenantRatePlansComponent` tại `angular/src/app/admin/tenant/rates/`; tương tác qua `TenantRatePlanService`. |
| **V. Đa ngôn ngữ (Zero Fixed Text)** | Cấm tuyệt đối văn bản cứng; đồng bộ `vi.json` và `en.json` | **PASS** | 100% tiêu đề, nhãn form, trạng thái badge, thông báo lỗi khai báo song ngữ đầy đủ tại `vi.json`, `en.json`, và `fallback-localizations.ts`. |
| **VI. Backend-Authoritative** | Thao tác dữ liệu và tính toán thuộc về Backend | **PASS** | Backend thực hiện validation trùng mã `Code`, Soft Delete và cập nhật cascade trạng thái chi nhánh; Frontend không tự quyết định lưu trữ. |
| **VII. UI Concept Hệ Màu** | Nền Trắng, Sidebar Đen, Điểm nhấn Cyan, WCAG AA $\ge 4.5:1$ | **PASS** | Áp dụng concept Quản trị Tenant: Nền `#ffffff`, Card border `#e2e8f0`, Sidebar đồng bộ layout Tenant. |
| **VIII. Brand Naming** | Tên thương hiệu `Booking Hotel` có dấu cách | **PASS** | Tuân thủ tên chuẩn `Booking Hotel` trên toàn giao diện. |
| **IX. Bảo mật Phiên & Login** | Zero credential pre-filling | **PASS** | Không can thiệp vào form đăng nhập; tuân thủ hoàn toàn. |
| **X. Cấm Dữ Liệu Ảo (Zero Mock)** | 100% dữ liệu thực từ CSDL; dùng empty-state khi rỗng | **PASS** | Đọc dữ liệu trực tiếp từ `IRepository<RatePlan, Guid>`; khi rỗng hiển thị empty state chuẩn có icon và mô tả. |
| **XI. Dấu Sao Đỏ Bắt Buộc Nhập** | Dấu sao `*` màu đỏ `#dc2626`, nhãn không in đậm | **PASS** | Form modal áp dụng chuẩn `<label class="form-label">{{ 'Key' | abpLocalization }} <span class="text-danger">*</span></label>`. |

---

## 4. Project Structure & Code Mapping

### 4.1. Tài Liệu Đặc Tả & Thiết Kế (Documentation Artifacts)
```text
BookingHotel/specs/009-tenant-rate-plan-management/
├── spec.md                          # Đặc tả yêu cầu hoàn chỉnh kèm 3 quyết định làm rõ
├── plan.md                          # Kế hoạch kiến trúc kỹ thuật (Tài liệu này)
├── research.md                      # Báo cáo nghiên cứu kiến trúc & đối soát CSDL Phase 0
├── data-model.md                    # Mô hình dữ liệu và quy tắc nghiệp vụ RatePlan Phase 1
├── quickstart.md                    # Hướng dẫn kiểm thử nghiệm thu End-to-End Phase 1
└── contracts/
    └── tenant-rate-plans.md         # Hợp đồng RESTful API chi tiết cho Tenant Rate Plans
```

### 4.2. Mã Nguồn Backend (.NET 10 / ABP Framework)
```text
BookingHotel/src/
├── BookingHotel.Application.Contracts/
│   └── Tenant/
│       └── Rates/
│           ├── ITenantRatePlanAppService.cs    # Interface hợp đồng nghiệp vụ
│           └── TenantRatePlanDtos.cs           # RatePlanDto, CreateUpdateRatePlanDto
├── BookingHotel.Application/
│   └── Tenant/
│       └── Rates/
│           └── TenantRatePlanAppService.cs     # Triển khai nghiệp vụ CRUD, soft-delete, validate unique
└── BookingHotel.HttpApi/
    └── Controllers/
        └── Tenant/
            └── TenantRatePlanController.cs     # API Controller: /api/app/tenant/rate-plans
```

### 4.3. Mã Nguồn Frontend (Angular 19+ Standalone)
```text
BookingHotel/angular/src/app/admin/
├── services/
│   └── tenant/
│       └── tenant-rate-plan.service.ts         # Service gọi API Backend
├── tenant/
│   ├── rates/
│   │   ├── tenant-rate-plans.component.ts      # Component logic & Signals
│   │   ├── tenant-rate-plans.component.html    # Template giao diện bảng & modal form
│   │   └── tenant-rate-plans.component.scss    # SCSS styling
│   ├── tenant-admin-layout/
│   │   └── tenant-admin-layout.component.html  # Bổ sung menu Quản lý Gói Giá vào Sidebar
│   └── tenant.routes.ts                        # Bổ sung route 'rate-plans'
└── shared/localization/
    └── fallback-localizations.ts               # Khai báo bản dịch dự phòng song ngữ
```

---

## 5. Implementation Phases & Milestones

- **Phase 0 (Research & Foundation)**: Đối soát entity `RatePlan`, kiểm tra `IMultiTenant`, xác thực quan hệ với `HotelRatePlan`.
- **Phase 1 (Design & Contracts)**: Xây dựng tài liệu `research.md`, `data-model.md`, `contracts/tenant-rate-plans.md`, `quickstart.md`.
- **Phase 2 (Backend Implementation)**: Tạo `ITenantRatePlanAppService`, DTOs, `TenantRatePlanAppService`, và `TenantRatePlanController`.
- **Phase 3 (Frontend Implementation)**: Tạo `TenantRatePlanService`, `TenantRatePlansComponent`, cập nhật route `tenant.routes.ts`, gắn menu vào `tenant-admin-layout.component.html`.
- **Phase 4 (Localization & Integration)**: Cập nhật `vi.json`, `en.json`, `fallback-localizations.ts`. Xác nhận đồng bộ dữ liệu sang `/manager/rates`.
- **Phase 5 (Verification & Build Check)**: Chạy `dotnet build` và `npx tsc --noEmit` đạt 0 lỗi.
