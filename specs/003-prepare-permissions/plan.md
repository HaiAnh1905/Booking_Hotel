# Implementation Plan: RM-003 - Chuẩn Bị Phân Quyền Hệ Thống & Vai Trò Người Dùng (Role-Based Permissions Preparation)

**Branch**: `003-prepare-permissions` | **Date**: 2026-11-12 | **Spec**: [specs/003-prepare-permissions/spec.md](spec.md)  
**Input**: Feature specification from `specs/003-prepare-permissions/spec.md`

---

## 1. Summary

Xây dựng và chuẩn bị nền móng kiến trúc phân quyền hạt nhân (Granular RBAC) và hệ thống vai trò chuẩn hóa (Standard Roles) cho toàn bộ hệ sinh thái `BookingHotel` trên nền tảng ABP Framework 10.7 (.NET 10 / EF Core / Angular) theo mô hình SaaS Đa khách thuê (Multi-Tenancy):
1. **5 Vai trò Chuẩn hóa (Fixed Standard Roles)**:
   - `Customer` (Khách hàng cá nhân toàn cầu - Global Customer với `TenantId = null`).
   - `System Admin` (Quản trị viên Hệ thống - Host scope).
   - `Tenant Admin` (Quản trị viên Doanh nghiệp - Tenant scope).
   - `Hotel Manager` (Quản lý Khách sạn - Phân bổ 1 hoặc nhiều cơ sở `1-n` trong cùng Tenant).
   - `Receptionist` (Nhân viên Lễ tân - Gán cố định `1-1` với một cơ sở khách sạn tại một thời điểm).
2. **Cây Hằng số Phân quyền & Khai báo Multi-Tenancy**:
   - Cung cấp danh mục quyền hạt nhân cho các phân hệ cốt lõi: Tenants, Hotels, Rooms, Bookings, Folios/Invoices, Customers, AuditLogs, và UnlockUser.
   - Định nghĩa rõ ràng thuộc tính `MultiTenancySides` (`Host`, `Tenant`, `Both`) trong `BookingHotelPermissionDefinitionProvider`.
3. **Cơ chế Tự Động Gieo Dữ liệu (Data Seeding)**:
   - Triển khai `BookingHotelDataSeedContributor` thực thi tự động qua `IDataSeeder` khi chạy `DbMigrator`.
   - Khởi tạo sẵn 5 Roles chuẩn và nạp ma trận quyền mặc định (Default Permission Grants) tương ứng.
4. **Phân bổ Chi nhánh Kiểm soát Dữ liệu (Data Scoping)**:
   - Thiết kế thực thể `UserHotelAssignment` để ánh xạ tài khoản nhân viên (`Hotel Manager` và `Receptionist`) với cơ sở khách sạn trực thuộc.
5. **Chuẩn hóa Đa ngôn ngữ (i18n)**:
   - Đồng bộ 100% nhãn tên vai trò, nhóm quyền và quyền hạt nhân trong `vi.json` và `en.json`.

---

## 2. Technical Context

- **Language / Version**: C# .NET 10, TypeScript 5.x / Angular 19+.
- **Primary Dependencies**:
  - ABP Framework 10.7 (`Volo.Abp.Authorization`, `Volo.Abp.Identity`, `Volo.Abp.MultiTenancy`, `Volo.Abp.Data`, `Volo.Abp.PermissionManagement`).
  - Entity Framework Core 10.x.
- **Storage**: Cơ sở dữ liệu quan hệ (SQL Server / PostgreSQL) quản lý bằng EF Core Migrations & `BookingHotel.DbMigrator`.
- **Testing**: xUnit, Shouldly, ABP Integrated TestBase (`BookingHotel.Application.Tests`, `BookingHotel.Domain.Tests`).
- **Target Platform**: ASP.NET Core Web API Host, Background Data Migrator & Angular SPA Web Application.
- **Performance Goals**:
  - Thời gian đánh giá phân quyền (`[Authorize]` / `IsGrantedAsync`) < 5ms nhờ cơ chế phân quyền bộ nhớ đệm (In-memory Permission Cache của ABP).
  - Khởi tạo seed danh mục vai trò & ma trận cấp quyền khi chạy `DbMigrator` < 2 giây.
- **Constraints (Constitution)**:
  - Mọi thao tác kiểm tra quyền và cấp quyền diễn ra 100% tại Backend (*Backend-Authoritative*).
  - Cách ly ranh giới Multi-Tenancy tuyệt đối (không cho phép rò rỉ dữ liệu giữa các Tenant đối tác).
  - Không hardcode chuỗi nhãn phân quyền; 100% bản địa hóa song ngữ tại `vi.json` và `en.json`.

---

## 3. Constitution Check

*GATE: Kiểm tra tính tuân thủ 9 Nguyên tắc tối cao trong `constitution.md`*

| Điều khoản Hiến pháp | Tiêu chí đánh giá | Trạng thái | Giải pháp kỹ thuật tương ứng |
| :--- | :--- | :---: | :--- |
| **I. Phân lớp DDD & ABP** | Đúng tầng, không lẫn lộn trách nhiệm | **PASS** | Cây hằng số Permissions đặt tại `BookingHotel.Application.Contracts/Permissions`; logic gán quyền & seed data đặt tại `BookingHotel.Domain/Data`; thực thể `UserHotelAssignment` đặt tại `BookingHotel.Domain/Identity`; ánh xạ DbContext tại `BookingHotel.EntityFrameworkCore`. |
| **II. Contract-First & Security** | Bắt buộc kiểm tra quyền, bảo mật mặc định | **PASS** | Mọi API tương lai đều được bảo vệ bởi thuộc tính `[Authorize(BookingHotelPermissions...)]`. Ranh giới `MultiTenancySides` ngăn chặn việc cấp quyền host cho tenant và ngược lại. |
| **III. EF Core Migrations** | Không can thiệp DB thủ công | **PASS** | Thực thể `UserHotelAssignment` được đăng ký qua `BookingHotelDbContextModelCreatingExtensions` và áp dụng bằng Code-First Migrations qua `DbMigrator`. |
| **IV. Chuẩn hóa Frontend** | Phân tách giao diện, Thin Presentation | **PASS** | Frontend Angular chỉ tiêu thụ danh sách quyền được cấp qua `PermissionDirective` (`*abpPermission`) và `ConfigStateService` để ẩn/hiện nút; quyền thực thi luôn được Backend kiểm chứng lại. |
| **V. Đa ngôn ngữ (i18n)** | Không hardcode chuỗi hiển thị | **PASS** | Tên hiển thị của tất cả Permissions, Groups và Roles được định nghĩa song ngữ hoàn chỉnh trong `vi.json` và `en.json`. |
| **VI. Backend-Authoritative** | Toàn quyền kiểm soát tại Backend | **PASS** | Kiểm tra quyền (Authorization), gán vai trò, phân bổ chi nhánh và xác thực token diễn ra 100% tại tầng Application và Domain của Backend. |
| **VII. UI/UX Concept Hệ Màu** | Nhận diện thương hiệu theo phân hệ | **PASS** | 5 vai trò được định hướng phân bổ vào 3 không gian giao diện: Khách hàng (Đỏ `#990000`/Đen), Quản trị (Trắng/Cyan `#00FFFF`/Đen), Lễ tân (Xanh lá `#99FF99`/Xám `#808080`). |
| **VIII. Brand Naming** | Chuẩn hóa thương hiệu `Booking Hotel` | **PASS** | Tên nhóm quyền gốc là `BookingHotel`, tiêu đề hiển thị i18n là `Booking Hotel`. |
| **IX. Bảo Mật Phiên & Unlock** | Mở khóa brute-force phân tầng | **PASS** | Tách quyền `UnlockUser` phân tầng: System Admin mở khóa Host; Tenant Admin mở khóa tài khoản nhân viên Tenant. |

---

## 4. Project Structure & Code Mapping

### Tài liệu Đặc tả & Thiết kế (Documentation Artifacts)
```text
BookingHotel/specs/003-prepare-permissions/
├── spec.md                       # Đặc tả yêu cầu chức năng (Feature Spec)
├── checklists/
│   └── requirements.md           # Thẩm định chất lượng đặc tả (Quality Checklist)
├── plan.md                       # Kế hoạch kiến trúc kỹ thuật (Tài liệu này)
├── research.md                   # Báo cáo nghiên cứu giải pháp kỹ thuật (Phase 0)
├── data-model.md                 # Thiết kế mô hình dữ liệu & ma trận quyền (Phase 1)
├── contracts/
│   └── permissions-matrix.md     # Ma trận chi tiết Quyền hạn - Vai trò (Phase 1)
├── quickstart.md                 # Hướng dẫn kiểm thử & chạy thử nghiệm (Phase 1)
└── tasks.md                      # Danh mục đầu việc triển khai (Phase 2 - speckit.tasks)
```

### Cấu trúc Mã nguồn Triển khai (Source Code Layout)
```text
BookingHotel/
├── src/
│   ├── BookingHotel.Application.Contracts/
│   │   └── Permissions/
│   │       ├── BookingHotelPermissions.cs                 # Định nghĩa cây constants phân cấp
│   │       └── BookingHotelPermissionDefinitionProvider.cs # Đăng ký cây quyền & MultiTenancySides
│   │
│   ├── BookingHotel.Domain.Shared/
│   │   ├── Identity/
│   │   │   └── BookingHotelRoleConsts.cs                 # Hằng số tên 5 vai trò chuẩn
│   │   └── Localization/BookingHotel/
│   │       ├── vi.json                                   # Tài nguyên song ngữ tiếng Việt
│   │       └── en.json                                   # Tài nguyên song ngữ tiếng Anh
│   │
│   ├── BookingHotel.Domain/
│   │   ├── Identity/
│   │   │   └── UserHotelAssignment.cs                    # Thực thể phân bổ nhân sự - chi nhánh khách sạn
│   │   └── Data/
│   │       └── BookingHotelDataSeedContributor.cs        # Tự động seed 5 roles & ma trận quyền
│   │
│   ├── BookingHotel.EntityFrameworkCore/
│   │   ├── EntityFrameworkCore/
│   │   │   ├── BookingHotelDbContext.cs                  # DbSet<UserHotelAssignment>
│   │   │   └── BookingHotelDbContextModelCreatingExtensions.cs # Fluent API mapping & Indexing
│   │   └── Migrations/
│   │       └── [Timestamp]_AddUserHotelAssignment.cs     # EF Core Code-First Migration
│   │
│   └── BookingHotel.HttpApi.Host/
│       └── appsettings.json
│
└── test/
    ├── BookingHotel.Application.Tests/
    │   └── Permissions/
    │       └── BookingHotelPermissionDefinitionTests.cs  # Unit tests kiểm tra cây quyền & MultiTenancy
    └── BookingHotel.Domain.Tests/
        └── Data/
            └── BookingHotelDataSeedContributorTests.cs   # Unit tests kiểm tra tự động seed 5 vai trò
```

---

## 5. Phased Execution Plan

### Phase 0: Outline & Research (Hoàn thành)
- Nghiên cứu giải pháp thiết kế Permission Tree trong ABP Framework 10.7 với `MultiTenancySides`.
- Nghiên cứu cơ chế Data Seeding thông qua `IDataSeedContributor` cho 5 Roles chuẩn.
- Nghiên cứu giải pháp phân quyền dữ liệu chi nhánh (Data Authorization Scoping) qua `UserHotelAssignment`.
- Đóng gói tài liệu `research.md`.

### Phase 1: Design & Contracts (Hoàn thành)
- Thiết kế chi tiết cấu trúc dữ liệu, các thuộc tính và bảng quan hệ trong `data-model.md`.
- Xây dựng ma trận phân quyền hoàn chỉnh trong `contracts/permissions-matrix.md`.
- Soạn thảo tài liệu hướng dẫn kiểm thử và kích hoạt trong `quickstart.md`.

### Phase 2: Implementation Breakdown (Chuyển giao cho `/speckit.tasks`)
- Lập danh sách công việc chi tiết chia theo từng tầng kiến trúc DDD.
- Triển khai mã nguồn Backend, Data Seeder, Migration và kiểm thử tự động xUnit.
