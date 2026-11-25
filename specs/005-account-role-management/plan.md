# Implementation Plan: RM-005 - Quản Lý Tài Khoản & Phân Quyền Vai Trò (Identity & Role Management)

**Branch**: `005-account-role-management` | **Date**: 2026-11-13 | **Spec**: [specs/005-account-role-management/spec.md](spec.md)  
**Input**: Feature specification from `specs/005-account-role-management/spec.md`

---

## 1. Summary

Kế thừa và mở rộng phân hệ Quản trị Định danh & Phân quyền (ABP Identity Module) trên nền tảng ABP Framework 10.7 (.NET 10 / EF Core / Angular 19) theo chuẩn Domain-Driven Design (DDD) và Hiến pháp dự án `BookingHotel`:
1. **Phân Tầng Quản Trị Nhân Sự & Multi-Tenancy**:
   - Host System Admin quản lý tài khoản cấp Host (`TenantId = null`) và mở khóa an ninh Host.
   - Tenant Admin quản lý tài khoản nhân sự nội bộ thuộc Tenant (`HotelManager`, `Receptionist`), phân bổ chi nhánh làm việc và mở khóa an ninh nội bộ chuỗi.
   - Tài khoản `Customer` dùng chung toàn cầu (`TenantId = null`), tự phục vụ qua Customer Portal, cách ly hoàn toàn khỏi cấp Tenant.
2. **Cơ Chế Phân Bổ Chi Nhánh Khách Sạn (`UserHotelAssignment`)**:
   - Nâng cấp thực thể `UserHotelAssignment` kế thừa `FullAuditedEntity<Guid>` bổ sung `IsActive` (bool) và `UnassignedAt` (DateTime?) để thực hiện cơ chế lưu vết lịch sử (Audit-tracked assignment) thay vì xóa cứng.
   - Ràng buộc: `HotelManager` được phân công 1 hoặc nhiều cơ sở (`1-n`); `Receptionist` bị khóa cứng với duy nhất 1 cơ sở cố định (`1-1`).
3. **Mở Khóa Tài Khoản An Ninh Brute-Force (`UnlockUser`)**:
   - Tích hợp quyền `BookingHotelPermissions.Identity.UnlockUser`.
   - Khôi phục `IsActive = true`, xóa `LockoutEnd`, đặt lại bộ đếm Brute-force và cache vi phạm, ghi log `SecurityLog`.
4. **Chống Leo Thang Đặc Quyền & Bảo Vệ 5 Vai Trò Chuẩn (Anti-Privilege Escalation)**:
   - Whitelist validation phía Backend: Tenant Admin chỉ được phép gán `HotelManager` hoặc `Receptionist`.
   - Cố định 5 vai trò chuẩn (`IsStatic = true`), cấm xóa hoặc sửa cấu trúc hạt nhân.
   - Thiết lập cờ `ShouldChangePasswordOnNextLogin = true` cho nhân sự mới tạo.
5. **Tùy Biến Giao Diện Angular Chuẩn Concept RM-004**:
   - Xây dựng giao diện quản lý người dùng và vai trò đặt tại `angular/src/app/identity/` theo Concept Quản trị (Sidebar Đen `#000000`, Highlight Cyan `#00FFFF`, Nền Trắng `#FFFFFF`).
   - Tái sử dụng thư viện UI dùng chung (`BhDataTable`, `BhModal`, `BhStatusBadge`, `BhToastrService`).
   - Không điền sẵn mật khẩu (*Zero Credential Pre-filling*), có checklist mật khẩu mạnh, hỗ trợ 100% song ngữ (`vi.json`, `en.json`).

---

## 2. Technical Context

- **Language / Version**: C# .NET 10, TypeScript 5.x / Angular 19+.
- **Primary Dependencies**:
  - ABP Framework 10.7 (`Volo.Abp.Identity`, `Volo.Abp.Authorization`, `Volo.Abp.MultiTenancy`, `Volo.Abp.PermissionManagement`, `Volo.Abp.Caching`).
  - Entity Framework Core 10.x.
  - Angular Standalone Components, Reactive Forms, `@abp/ng.core`.
- **Storage**: CSDL quan hệ (SQL Server / PostgreSQL) quản lý bằng EF Core Migrations & `BookingHotel.DbMigrator`.
- **Testing**: xUnit, Shouldly, ABP Integrated TestBase (`BookingHotel.Application.Tests`, `BookingHotel.Domain.Tests`), Karma / Jasmine (`angular`).
- **Target Platform**: ASP.NET Core Web API Host, Background Data Migrator & Angular SPA Web Application.
- **Performance Goals**:
  - Thời gian phản hồi API tra cứu danh sách người dùng phân trang < 100ms.
  - Thời gian xử lý mở khóa tài khoản (`UnlockUserAsync`) < 500ms.
- **Constraints (Constitution)**:
  - Backend-Authoritative: Mọi ràng buộc phân quyền, gán vai trò, phân bổ chi nhánh bắt buộc kiểm tra tại Backend.
  - Tách biệt Multi-Tenancy tuyệt đối: Người dùng Tenant A không thể thấy hoặc can thiệp nhân sự Tenant B.
  - 100% tài nguyên hiển thị được bản địa hóa song ngữ tại `vi.json` và `en.json`.

---

## 3. Constitution Check

*GATE: Kiểm tra tính tuân thủ 9 Điều khoản của Hiến pháp dự án trong `constitution.md`*

| Điều khoản Hiến pháp | Tiêu chí đánh giá | Trạng thái | Giải pháp kỹ thuật tương ứng |
| :--- | :--- | :---: | :--- |
| **I. Phân lớp DDD & ABP** | Đúng tầng, không lẫn lộn trách nhiệm | **PASS** | `UserHotelAssignment` nằm ở `Domain/Identity`; DTOs và Interfaces nằm ở `Application.Contracts/Identity`; AppService triển khai ở `Application/Identity`; DbContext Mapping ở `EntityFrameworkCore`; Giao diện ở `angular/src/app/identity`. |
| **II. Contract-First & Security** | Bắt buộc kiểm tra quyền, validation chặt chẽ | **PASS** | Tất cả DTOs đầu vào có Data Annotations (`[Required]`, `[StringLength]`). AppService kiểm tra `[Authorize(BookingHotelPermissions.Identity...)]` và `[Authorize(BookingHotelPermissions.Hotels.AssignStaff)]`. |
| **III. EF Core Migrations** | Quản lý schema bằng Code-First Migrations | **PASS** | Cập nhật các trường mới (`IsActive`, `UnassignedAt`) cho `UserHotelAssignment` bằng Migration mới áp dụng qua `DbMigrator`. |
| **IV. Chuẩn hóa Frontend** | Thin presentation, Reactive Forms, Strict TS | **PASS** | Angular thuần túy thu thập dữ liệu và hiển thị; sử dụng Standalone Components, Reactive Forms và gọi API qua strongly-typed services. |
| **V. Đa ngôn ngữ (i18n)** | Không hardcode chuỗi hiển thị | **PASS** | 100% nhãn cột, tiêu đề form, tên vai trò, thông báo lỗi validation được định nghĩa trong `vi.json` và `en.json`. |
| **VI. Backend-Authoritative** | Toàn quyền kiểm soát và tính toán tại Backend | **PASS** | Whitelist gán role, kiểm tra `ShouldChangePasswordOnNextLogin` và điều kiện 1-1 cho Lễ tân được thực thi 100% tại Backend. |
| **VII. UI/UX Concept Hệ Màu** | Nhận diện thương hiệu theo phân hệ | **PASS** | Áp dụng Concept Quản trị RM-004: Sidebar Đen `#000000`, Điểm nhấn Cyan `#00FFFF`, Nền Trắng `#FFFFFF`, tương phản văn bản đạt chuẩn WCAG 2.1 AA ($\ge 4.5:1$). |
| **VIII. Brand Naming** | Chuẩn hóa thương hiệu `Booking Hotel` | **PASS** | Logo và tên hiển thị trong module Identity luôn là `Booking Hotel` có dấu cách. |
| **IX. Bảo Mật Phiên & Unlock** | Zero credential pre-filling & Mở khóa an ninh | **PASS** | Form tạo/sửa người dùng để trống hoàn toàn mật khẩu. Phân định thẩm quyền mở khóa 2 tầng cho System Admin và Tenant Admin. |

---

## 4. Project Structure & Code Mapping

### Tài liệu Đặc tả & Thiết kế (Documentation Artifacts)
```text
BookingHotel/specs/005-account-role-management/
├── spec.md                          # Đặc tả yêu cầu chức năng (Feature Spec)
├── checklists/
│   └── requirements.md              # Thẩm định chất lượng đặc tả (Quality Checklist)
├── plan.md                          # Kế hoạch kiến trúc kỹ thuật (Tài liệu này)
├── research.md                      # Báo cáo nghiên cứu giải pháp kỹ thuật (Phase 0)
├── data-model.md                    # Thiết kế mô hình dữ liệu & vòng đời tài khoản (Phase 1)
├── contracts/
│   └── identity-contracts.md        # Đặc tả REST APIs, DTOs & Validation Rules (Phase 1)
├── quickstart.md                    # Hướng dẫn kiểm thử & kịch bản chạy thử nghiệm (Phase 1)
└── tasks.md                         # Danh mục đầu việc triển khai (Phase 2 - speckit.tasks)
```

### Cấu trúc Mã nguồn Triển khai (Source Code Layout)
```text
BookingHotel/
├── src/
│   ├── BookingHotel.Domain.Shared/
│   │   ├── Identity/
│   │   │   └── BookingHotelRoleConsts.cs                 # Hằng số 5 vai trò chuẩn
│   │   └── Localization/BookingHotel/
│   │       ├── vi.json                                   # Bản địa hóa tiếng Việt
│   │       └── en.json                                   # Bản địa hóa tiếng Anh
│   │
│   ├── BookingHotel.Domain/
│   │   └── Identity/
│   │       └── UserHotelAssignment.cs                    # Mở rộng FullAuditedEntity (IsActive, UnassignedAt)
│   │
│   ├── BookingHotel.Application.Contracts/
│   │   └── Identity/
│   │       ├── IIdentityUserManagementAppService.cs      # Interface mở rộng quản trị tài khoản
│   │       ├── IUserHotelAssignmentAppService.cs         # Interface phân bổ chi nhánh khách sạn
│   │       └── Dtos/
│   │           ├── CreateUserWithAssignmentDto.cs        # DTO tạo người dùng kèm phân bổ
│   │           ├── UpdateUserWithAssignmentDto.cs        # DTO cập nhật người dùng kèm phân bổ
│   │           ├── UserWithAssignmentDto.cs              # DTO chi tiết người dùng
│   │           ├── UnlockUserDto.cs                      # DTO yêu cầu mở khóa tài khoản
│   │           └── AssignUserHotelDto.cs                 # DTO phân công chi nhánh
│   │
│   ├── BookingHotel.Application/
│   │   └── Identity/
│   │       ├── IdentityUserManagementAppService.cs       # Triển khai mở rộng, whitelist role & unlock logic
│   │       └── UserHotelAssignmentAppService.cs          # Nâng cấp quản lý phân công & lưu vết lịch sử
│   │
│   ├── BookingHotel.EntityFrameworkCore/
│   │   ├── EntityFrameworkCore/
│   │   │   └── BookingHotelDbContextModelCreatingExtensions.cs # Mapping Fluent API & Soft Unlink
│   │   └── Migrations/
│   │       └── [Timestamp]_UpdateUserHotelAssignmentAudit.cs   # Code-First Migration
│   │
│   └── BookingHotel.HttpApi/
│       └── Controllers/
│           └── IdentityUserManagementController.cs       # Controller mở rộng API
│
├── angular/
│   └── src/app/
│       └── identity/
│           ├── users/
│           │   ├── user-list.component.ts                # Danh sách nhân sự với BhDataTable
│           │   ├── user-list.component.html
│           │   ├── user-list.component.scss
│           │   ├── user-modal.component.ts               # Form tạo/sửa với BhModal, role & branch picker
│           │   ├── user-modal.component.html
│           │   └── user-modal.component.scss
│           ├── roles/
│           │   ├── role-list.component.ts                # Hiển thị 5 vai trò chuẩn tĩnh
│           │   └── role-list.component.html
│           └── services/
│               └── identity-user-management.service.ts   # Angular service kết nối API Backend
│
└── test/
    ├── BookingHotel.Application.Tests/
    │   └── Identity/
    │       ├── IdentityUserManagementAppServiceTests.cs  # Unit test whitelist role, unlock & multi-tenancy
    │       └── UserHotelAssignmentTests.cs               # Unit test audit tracking & single-property rule
    └── BookingHotel.Domain.Tests/
```

---

## 5. Phased Execution Plan

### Phase 0: Outline & Research
- Nghiên cứu cơ chế mở rộng `IdentityUserManager` và `IdentityUserAppService` trong ABP Framework 10.7.
- Nghiên cứu cơ chế lưu vết lịch sử phân công chi nhánh (`UserHotelAssignment` audit-tracked assignment) thay cho hard delete.
- Nghiên cứu cơ chế tích hợp cờ `ShouldChangePasswordOnNextLogin` và luồng đổi mật khẩu bắt buộc ở lần đăng nhập đầu tiên.
- Nghiên cứu cơ chế mở khóa Brute-force (`UnlockUser`) kết hợp xóa cache `BruteForceManager` từ RM-001.
- Nghiên cứu tích hợp UI Identity tùy biến vào Concept Quản trị RM-004 trên Angular.
- Đóng gói tài liệu `research.md`.

### Phase 1: Design & Contracts
- Thiết kế chi tiết cấu trúc dữ liệu, sơ đồ thực thể và vòng đời trạng thái trong `data-model.md`.
- Xây dựng đặc tả chi tiết các hợp đồng DTO, API Endpoints và quy tắc kiểm tra tính hợp lệ trong `contracts/identity-contracts.md`.
- Soạn thảo tài liệu hướng dẫn kiểm thử và kịch bản thực nghiệm trong `quickstart.md`.

### Phase 2: Implementation Breakdown (Chuyển giao cho `/speckit.tasks`)
- Lập danh mục chi tiết các đầu việc thực thi tuần tự từ Backend Domain $\rightarrow$ Contracts $\rightarrow$ Application $\rightarrow$ EF Core $\rightarrow$ Angular Frontend $\rightarrow$ Automated Tests.
