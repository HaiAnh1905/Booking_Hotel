# Tasks: RM-007 / RM-009 - Quản Lý Gói Giá Chuỗi Cho Admin Tenant (Tenant Master Rate Plans Management)

**Input**: Design documents from `/specs/009-tenant-rate-plan-management/` (`plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/`, `quickstart.md`)  
**Prerequisites**: `plan.md` (required), `spec.md` (required), `research.md`, `data-model.md`, `contracts/`  
**Organization**: Tasks được cấu trúc tuần tự theo từng Phase và User Story để đảm bảo khả năng triển khai độc lập, kiểm thử từng phần và tuân thủ nghiêm ngặt Hiến pháp dự án.  

## Format: `- [ ] [ID] [P?] [Story?] Description with exact file path`

- **[P]**: Có thể thực hiện song song (khác file, không có sự phụ thuộc vào task chưa hoàn thành).
- **[Story]**: Đánh dấu User Story tương ứng (`[US1]`: Quản lý danh mục & Tạo mới, `[US2]`: Chỉnh sửa & Xóa mềm an toàn, `[US3]`: Đồng bộ sang Hotel Manager).
- Đường dẫn file mã nguồn chính xác trong mọi mô tả task.

---

## Phase 1: Setup & Data Contracts (Interfaces, DTOs & Permissions)

**Mục đích**: Định nghĩa các hợp đồng DTO, AppService interface, khai báo quyền hạt nhân cho chức năng Quản lý Gói Giá Chuỗi.

- [x] T001 [P] Khai báo hằng số quyền `BookingHotelPermissions.RatePlans.Manage` và đăng ký quyền cho Tenant Admin trong `BookingHotel/src/BookingHotel.Domain.Shared/Permissions/BookingHotelPermissions.cs` và `BookingHotel/src/BookingHotel.Application.Contracts/Permissions/BookingHotelPermissionDefinitionProvider.cs`
- [x] T002 [P] Định nghĩa DTOs quản lý gói giá chuỗi: `CreateUpdateRatePlanDto` (với validation `[Required]`, `[StringLength]`, `[Range]`) và `RatePlanDto` trong `BookingHotel/src/BookingHotel.Application.Contracts/Tenant/Rates/TenantRatePlanDtos.cs`
- [x] T003 [P] Khai báo interface `ITenantRatePlanAppService` với các phương thức CRUD (`GetListAsync`, `GetAsync`, `CreateAsync`, `UpdateAsync`, `DeleteAsync`) trong `BookingHotel/src/BookingHotel.Application.Contracts/Tenant/Rates/ITenantRatePlanAppService.cs`

---

## Phase 2: Backend Implementation (AppService & REST Controller)

**Mục đích**: Triển khai logic nghiệp vụ Backend, kiểm tra trùng mã `Code` trong Tenant, thực hiện Soft Delete và cập nhật cascade trạng thái chi nhánh.

- [x] T004 [US1] Triển khai `TenantRatePlanAppService` kế thừa `BookingHotelAppService`:
  - `GetListAsync`: Truy vấn toàn bộ `RatePlan` theo `CurrentTenant.Id` sắp xếp theo ngày tạo.
  - `GetAsync`: Lấy chi tiết gói giá theo Id.
  - `CreateAsync`: Chuẩn hóa `Code.ToUpper().Trim()`, kiểm tra trùng lặp mã trong Tenant (ném ngoại lệ `BookingHotel:DuplicateRatePlanCode`), gán `TenantId = CurrentTenant.Id` và lưu vào CSDL.
  File: `BookingHotel/src/BookingHotel.Application/Tenant/Rates/TenantRatePlanAppService.cs`
- [x] T005 [US2] Triển khai phương thức cập nhật và xóa mềm an toàn trong `TenantRatePlanAppService`:
  - `UpdateAsync`: Kiểm tra tồn tại, kiểm tra trùng mã `Code` (ngoại trừ bản ghi hiện tại), cập nhật `Name`, `Description`, `IsRefundable`, `CancellationCutoffHours`, `DefaultPriceAdjustmentPercent`.
  - `DeleteAsync`: Soft-delete bản ghi `RatePlan`, đồng thời tự động cập nhật toàn bộ bản ghi `HotelRatePlan` liên kết với `RatePlanId` này sang `IsActive = false` để ngắt mở bán tại các chi nhánh.
  File: `BookingHotel/src/BookingHotel.Application/Tenant/Rates/TenantRatePlanAppService.cs`
- [x] T006 [P] Xây dựng REST API Controller `TenantRatePlanController` phơi bày các endpoints `/api/app/tenant/rate-plans` với phân quyền `[Authorize(BookingHotelPermissions.Tenants.Manage)]` trong `BookingHotel/src/BookingHotel.HttpApi/Controllers/Tenant/TenantRatePlanController.cs`

---

## Phase 3: Frontend Implementation (Angular Service, Component & Navigation)

**Mục đích**: Xây dựng màn hình Quản lý Gói Giá Chuỗi bên Admin Tenant, form modal thêm/sửa, và tích hợp menu vào Sidebar.

- [x] T007 [P] Xây dựng Angular Data Service `TenantRatePlanService` gọi các REST API `/api/app/tenant/rate-plans` trong `BookingHotel/angular/src/app/admin/services/tenant/tenant-rate-plan.service.ts`
- [x] T008 [US1] Xây dựng giao diện bảng danh sách gói giá `TenantRatePlansComponent`:
  - Bảng danh mục gói giá với các cột: Tên gói, Mã gói (`Code`), Mô tả, Trạng thái hoàn hủy, Suất ăn sáng, % Điều chỉnh giá, Thao tác (Sửa, Xóa).
  - Trạng thái rỗng (`Empty State`) đạt chuẩn khi danh sách chưa có dữ liệu.
  - Tích hợp Angular Signals và loading indicator.
  File: `BookingHotel/angular/src/app/admin/tenant/rates/tenant-rate-plans.component.ts` (kèm HTML & SCSS)
- [x] T009 [US1] [US2] Xây dựng modal form Thêm mới & Chỉnh sửa gói giá trong `TenantRatePlansComponent`:
  - Reactive form / Two-way binding với các trường: `Name` `*`, `Code` `*` (tự động uppercase), `Description`, `IsRefundable` (kèm `CancellationCutoffHours` `*` khi bật), `IncludesBreakfast`, `DefaultPriceAdjustmentPercent` `*`.
  - Tuân thủ quy chuẩn dấu sao đỏ `*` bắt buộc theo Hiến pháp dự án.
  File: `BookingHotel/angular/src/app/admin/tenant/rates/tenant-rate-plans.component.html`
- [x] T010 [US2] Tích hợp dialog xác nhận xóa an toàn trước khi gọi API xóa gói giá trong `TenantRatePlansComponent`
- [x] T011 [P] Cấu hình route `/tenant/rate-plans` bảo vệ bởi `tenantAdminGuard` trong `BookingHotel/angular/src/app/admin/tenant/tenant.routes.ts`
- [x] T012 Bổ sung mục menu **"Quản lý Gói Giá"** (`Menu:TenantRatePlans`) kèm icon `fa-solid fa-tags` vào thanh điều hướng Sidebar bên trái (cùng cấp với Khách sạn, Nhân sự, Đặt phòng, Báo cáo) trong `BookingHotel/angular/src/app/admin/tenant/tenant-admin-layout/tenant-admin-layout.component.html`

---

## Phase 4: Localization & Multi-Language Sync (i18n)

**Mục đích**: Bảo đảm 100% văn bản giao diện, tiêu đề, nhãn form, thông báo lỗi, và toastr được bản địa hóa song ngữ, tuân thủ nguyên tắc Zero Hardcoded Text.

- [x] T013 [P] Bổ sung đầy đủ các key bản địa hóa Tiếng Việt (tiền tố `Tenant:RatePlan:*` và `Menu:TenantRatePlans`) trong `BookingHotel/src/BookingHotel.Domain.Shared/Localization/BookingHotel/vi.json`
- [x] T014 [P] Bổ sung đầy đủ các key bản địa hóa Tiếng Anh tương ứng trong `BookingHotel/src/BookingHotel.Domain.Shared/Localization/BookingHotel/en.json`
- [x] T015 [P] Bổ sung các key bản địa hóa dự phòng vào `BookingHotel/angular/src/app/shared/localization/fallback-localizations.ts`

---

## Phase 5: Verification & End-to-End Integration

**Mục đích**: Xác thực tính toàn vẹn hệ thống, đảm bảo không có lỗi biên dịch và hoàn thành các kịch bản kiểm thử End-to-End.

- [x] T016 Kiểm tra biên dịch Backend: Chạy `dotnet build --no-restore -m:1` xác nhận 0 lỗi.
- [x] T017 Kiểm tra biên dịch Frontend: Chạy `npx tsc -p tsconfig.app.json --noEmit` xác nhận 0 lỗi.
- [x] T018 [US3] Kiểm thử nghiệm thu End-to-End:
  - Đăng nhập `TenantAdmin`, vào `/tenant/rate-plans`, tạo gói giá mới `FAMILY_PROMO` (+15%, có ăn sáng, hủy 48h).
  - Chuyển sang `HotelManager`, vào `/manager/rates`, xác nhận gói `FAMILY_PROMO` hiển thị ngay lập tức trong danh mục mở bán.
  - Sửa và xóa gói giá bên `TenantAdmin`, xác nhận phản ánh tức thì bên `HotelManager`.
