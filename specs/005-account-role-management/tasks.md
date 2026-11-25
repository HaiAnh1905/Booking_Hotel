# Tasks: RM-005 - Quản Lý Tài Khoản & Phân Quyền Vai Trò (Identity & Role Management)

**Input**: Design documents from `/specs/005-account-role-management/` (`plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/identity-contracts.md`, `quickstart.md`)  
**Prerequisites**: `plan.md` (required), `spec.md` (required), `research.md`, `data-model.md`, `contracts/`  
**Organization**: Tasks được nhóm theo từng Phase và User Story để đảm bảo tính độc lập triển khai và khả năng kiểm thử độc lập cho từng phân hệ.

## Format: `- [ ] [ID] [P?] [Story?] Description with file path`

- **[P]**: Có thể thực hiện song song (khác file, không phụ thuộc vào task chưa hoàn thành).
- **[Story]**: Đánh dấu User Story tương ứng (`[US1]`, `[US2]`, `[US3]`, `[US4]`, `[US5]`).
- Đường dẫn file mã nguồn chính xác trong mô tả task.

---

## Phase 1: Setup (Backend & Contracts Foundation)

**Mục đích**: Nâng cấp thực thể phân công chi nhánh `UserHotelAssignment` hỗ trợ lưu vết lịch sử, tạo EF Core Migration, định nghĩa các DTOs mở rộng và đồng bộ bản địa hóa song ngữ làm nền tảng.

- [x] T001 Mở rộng thực thể `UserHotelAssignment` kế thừa `FullAuditedEntity<Guid>` bổ sung `IsActive` (bool) và `UnassignedAt` (DateTime?) trong `BookingHotel/src/BookingHotel.Domain/Identity/UserHotelAssignment.cs`
- [x] T002 Cập nhật cấu hình EF Core Fluent API mapping và Indexing cho `UserHotelAssignment` trong `BookingHotel/src/BookingHotel.EntityFrameworkCore/EntityFrameworkCore/BookingHotelDbContextModelCreatingExtensions.cs`
- [x] T003 Tạo và thực thi EF Core Code-First Migration `UpdateUserHotelAssignmentAudit` cho bảng `UserHotelAssignments` trong `BookingHotel/src/BookingHotel.EntityFrameworkCore/Migrations/`
- [x] T004 [P] Định nghĩa các DTOs mở rộng quản trị nhân sự (`CreateUserWithAssignmentDto`, `UpdateUserWithAssignmentDto`, `UserWithAssignmentDto`, `UnlockUserDto`, `AssignedHotelDto`) trong `BookingHotel/src/BookingHotel.Application.Contracts/Identity/Dtos/`
- [x] T005 [P] Khai báo Interface `IIdentityUserManagementAppService` kế thừa `IApplicationService` trong `BookingHotel/src/BookingHotel.Application.Contracts/Identity/IIdentityUserManagementAppService.cs`
- [x] T006 [P] Bổ sung đầy đủ 100% các chuỗi bản địa hóa song ngữ cho phân hệ Identity (tiêu đề form, nhãn cột, tên vai trò, mã lỗi và thông báo mở khóa) trong `BookingHotel/src/BookingHotel.Domain.Shared/Localization/BookingHotel/vi.json` và `en.json`

---

## Phase 2: Foundational (Backend AppServices & Security Layer)

**Mục đích**: Triển khai tầng Application Service quản trị định danh, xử lý mở khóa an ninh Brute-Force, lưu vết phân công cơ sở và thiết lập API Controller.

**⚠️ CRITICAL**: Các User Story phía sau phụ thuộc vào các services nền tảng này.

- [x] T007 Triển khai `IdentityUserManagementAppService` kế thừa `ApplicationService` điều phối `IdentityUserManager`, `IdentityRoleManager`, `UserHotelAssignment` và `IBruteForceManager` trong `BookingHotel/src/BookingHotel.Application/Identity/IdentityUserManagementAppService.cs`
- [x] T008 Triển khai phương thức mở khóa tài khoản `UnlockUserAsync` tích hợp quyền `BookingHotelPermissions.Identity.UnlockUser`, xóa cache Brute-force và ghi nhật ký an ninh trong `IdentityUserManagementAppService.cs`
- [x] T009 Nâng cấp `UserHotelAssignmentAppService` hỗ trợ cơ chế lưu vết lịch sử phân công (soft unlinking với `IsActive = false` và `UnassignedAt = Clock.Now`) trong `BookingHotel/src/BookingHotel.Application/Identity/UserHotelAssignmentAppService.cs`
- [x] T010 Xây dựng API Controller `IdentityUserManagementController` cung cấp các REST endpoints theo hợp đồng `contracts/identity-contracts.md` trong `BookingHotel/src/BookingHotel.HttpApi/Controllers/IdentityUserManagementController.cs`
- [x] T011 [P] Xây dựng Angular service `IdentityUserManagementService` kết nối các API mở rộng quản trị nhân sự trong `BookingHotel/angular/src/app/identity/services/identity-user-management.service.ts`

**Checkpoint**: Nền tảng Backend Services và APIs đã sẵn sàng - tiến hành triển khai các User Story theo thứ tự ưu tiên.

---

## Phase 3: User Story 1 - Quản Trị Tài Khoản Cấp Nền Tảng & Mở Khóa An Ninh Host (System Admin) (Priority: P1) 🎯 MVP

**Mục tiêu**: System Admin có thể xem danh sách tài khoản cấp Host (`TenantId = null`), tạo tài khoản Host, và mở khóa các tài khoản Host bị vô hiệu hóa do Brute-force.

**Independent Test**: Đăng nhập với tài khoản System Admin (Host scope), mở trang Quản trị người dùng, tạo tài khoản Host mới và thực hiện mở khóa một tài khoản Host bị khóa brute-force (`UnlockUser`) thành công trong < 500ms.

### Tests for User Story 1 ⚠️
- [x] T012 [P] [US1] Xây dựng Unit Tests kiểm tra lọc người dùng cấp Host và mở khóa tài khoản Host trong `BookingHotel/test/BookingHotel.Application.Tests/Identity/IdentityUserManagementAppServiceTests.cs`

### Implementation for User Story 1
- [x] T013 [US1] Triển khai logic lọc người dùng cấp Host (`TenantId == null`) trong phương thức `GetListAsync` của `IdentityUserManagementAppService.cs`
- [x] T014 [US1] Xây dựng component danh sách người dùng `UserListComponent` hiển thị phân trang bằng `BhDataTableComponent` trong `BookingHotel/angular/src/app/identity/users/user-list.component.ts` (kèm HTML & SCSS)
- [x] T015 [US1] Tích hợp nút tác vụ "Mở khóa tài khoản" (`Unlock`) và modal xác nhận `BhModalComponent` kích hoạt `UnlockUserAsync` kèm thông báo `BhToastrService` trong `user-list.component.ts`
- [x] T016 [US1] Đăng ký tuyến đường quản trị tài khoản cấp Host `/admin/system/users` trong `BookingHotel/angular/src/app/admin/admin.routes.ts`

**Checkpoint**: User Story 1 hoàn tất - System Admin quản lý tài khoản Host và mở khóa an ninh độc lập thành công (MVP).

---

## Phase 4: User Story 2 - Quản Trị Nhân Sự Chuỗi & Phân Bổ Chi Nhánh Khách Sạn (Tenant Admin) (Priority: P1)

**Mục tiêu**: Tenant Admin quản lý nhân sự thuộc Tenant, tạo mới nhân viên (gán `HotelManager` hoặc `Receptionist`), phân công chi nhánh làm việc (`UserHotelAssignment`), đặt cờ `ShouldChangePasswordOnNextLogin = true`, và mở khóa nhân viên bị brute-force.

**Independent Test**: Đăng nhập bằng Tenant Admin Chuỗi A, tạo Quản lý khách sạn gán 2 chi nhánh, tạo Lễ tân gán 1 chi nhánh (thử gán 2 chi nhánh bị chặn), và mở khóa tài khoản Lễ tân thành công.

### Tests for User Story 2 ⚠️
- [x] T017 [P] [US2] Xây dựng Unit Tests cho Tenant Admin: Tạo nhân sự, gán vai trò, phân bổ chi nhánh và kiểm tra ràng buộc Lễ tân 1-1 trong `IdentityUserManagementAppServiceTests.cs`

### Implementation for User Story 2
- [x] T018 [US2] Triển khai phương thức `CreateWithAssignmentAsync` và `UpdateWithAssignmentAsync` trong `IdentityUserManagementAppService.cs` tự động gán `TenantId = CurrentTenant.Id` và `ShouldChangePasswordOnNextLogin = true`
- [x] T019 [US2] Xây dựng component modal tạo/sửa nhân sự `UserModalComponent` sử dụng `BhModalComponent` với dropdown chọn vai trò, bộ kiểm tra mật khẩu mạnh trực quan trong `BookingHotel/angular/src/app/identity/users/user-modal.component.ts` (kèm HTML & SCSS)
- [x] T020 [US2] Tích hợp bộ chọn chi nhánh phân công trong `UserModalComponent`: hỗ trợ multi-select cho `HotelManager` và single-select khóa cứng cho `Receptionist`
- [x] T021 [US2] Tích hợp bộ lọc chi nhánh và hiển thị danh sách nhân sự nội bộ Tenant trong `UserListComponent`
- [x] T022 [US2] Cập nhật điều hướng menu nhân sự trong `ManagementLayoutComponent` (`BookingHotel/angular/src/app/layout/management/management-layout.component.ts`)

**Checkpoint**: User Story 2 hoàn tất - Tenant Admin quản lý nhân sự chuỗi và phân bổ cơ sở làm việc chính xác.

---

## Phase 5: User Story 5 - Cơ Chế Chống Leo Thang Đặc Quyền & Bảo Vệ Toàn Vẹn Vai Trò (Priority: P1)

**Mục tiêu**: Ngăn chặn triệt để hành vi leo thang đặc quyền (Tenant Admin không thể gán `SystemAdmin` hoặc sửa quyền cốt lõi), bảo vệ 5 vai trò chuẩn bất biến (`IsStatic = true`).

**Independent Test**: Gửi request tạo người dùng với vai trò `SystemAdmin` từ Tenant Admin $\rightarrow$ Backend từ chối với lỗi 403 Forbidden. Kiểm tra 5 vai trò chuẩn đều có cờ `IsStatic = true`.

### Tests for User Story 5 ⚠️
- [x] T023 [P] [US5] Xây dựng Unit Tests kiểm tra bảo mật Whitelist vai trò: Từ chối khi Tenant Admin gán role ngoài `[HotelManager, Receptionist]` trong `IdentityUserManagementAppServiceTests.cs`

### Implementation for User Story 5
- [x] T024 [US5] Triển khai cơ chế Whitelist Role Check trong `IdentityUserManagementAppService.cs`: ném `BusinessException("BookingHotel:CannotAssignUnauthorizedRole")` nếu phát hiện vai trò ngoài danh mục cho phép
- [x] T025 [US5] Triển khai bảo vệ đa khách thuê Cross-Tenant: từ chối thao tác sửa/khóa/mở khóa người dùng nếu `user.TenantId != CurrentTenant.Id`
- [x] T026 [US5] Xây dựng component danh mục vai trò `RoleListComponent` hiển thị 5 vai trò chuẩn ở chế độ chỉ đọc (read-only) trong `BookingHotel/angular/src/app/identity/roles/role-list.component.ts` (kèm HTML)

**Checkpoint**: User Story 5 hoàn tất - Ranh giới phân quyền bất khả xâm phạm và hệ thống chống leo thang đặc quyền 100%.

---

## Phase 6: User Story 3 - Tra Cứu Hồ Sơ & Thông Tin Nhân Sự Phân Ca (Hotel Manager) (Priority: P2)

**Mục tiêu**: Hotel Manager tra cứu được danh sách nhân sự Lễ tân thuộc các chi nhánh mình được phân công phụ trách để sắp xếp ca trực, ở chế độ chỉ đọc.

**Independent Test**: Đăng nhập bằng Hotel Manager, mở màn hình ca trực chi nhánh: chỉ nhìn thấy danh sách Lễ tân của chi nhánh mình, không xuất hiện nút Tạo/Sửa/Xóa tài khoản.

- [x] T027 [US3] Triển khai phương thức tra cứu nhân viên theo chi nhánh phụ trách `GetStaffByHotelsAsync` trong `IdentityUserManagementAppService.cs`
- [x] T028 [US3] Cấu hình giao diện danh sách nhân sự ở chế độ chỉ xem (Read-only view) ẩn toàn bộ nút Tạo/Sửa/Xóa/Gán quyền khi người dùng đăng nhập là Hotel Manager trong `user-list.component.ts`

**Checkpoint**: User Story 3 hoàn tất - Hotel Manager tra cứu nhân sự phục vụ điều phối ca trực an toàn.

---

## Phase 7: User Story 4 - Tự Phục Vụ Hồ Sơ Cá Nhân & Quản Trị Khách Hàng Toàn Cầu (Customer) (Priority: P2)

**Mục tiêu**: Khách hàng toàn cầu tự quản lý hồ sơ cá nhân; bắt buộc đổi mật khẩu ở lần đăng nhập đầu tiên cho nhân sự có cờ `ShouldChangePasswordOnNextLogin = true`.

**Independent Test**: Đăng nhập bằng tài khoản nhân viên mới tạo $\rightarrow$ Hệ thống tự động chuyển hướng bắt buộc tới màn hình đổi mật khẩu trước khi cho phép vào các phân hệ vận hành.

- [x] T029 [US4] Tích hợp cờ `should_change_password` vào JWT Token claims và xử lý hạ cờ trong `AuthAppService` khi đổi mật khẩu thành công trong `BookingHotel/src/BookingHotel.Application/Auth/AuthAppService.cs`
- [x] T030 [US4] Xây dựng Angular Route Guard `FirstLoginPasswordChangeGuard` tự động điều hướng người dùng tới `/auth/change-password` nếu có cờ `should_change_password` trong `BookingHotel/angular/src/app/auth/guards/first-login-password-change.guard.ts`
- [x] T031 [US4] Hoàn thiện màn hình đổi mật khẩu `ChangePasswordComponent` tại `BookingHotel/angular/src/app/auth/change-password/` tích hợp checklist mật khẩu mạnh

**Checkpoint**: User Story 4 hoàn tất - Cơ chế tự đổi mật khẩu lần đầu và quản lý hồ sơ an toàn hoạt động liền mạch.

---

## Phase 8: Polish & Cross-Cutting Concerns

**Mục đích**: Hoàn thiện toàn diện các khía cạnh kiểm thử tự động, tối ưu tương phản WCAG 2.1 AA, kiểm tra bảo mật và tài liệu hóa.

- [x] T032 [P] Chạy toàn bộ Unit Tests của Application layer xác nhận 100% PASS cho phân hệ Identity
- [x] T033 [P] Chạy kiểm thử tự động Angular và build Angular xác nhận 0 lỗi biên dịch
- [x] T034 [P] Rà soát và đảm bảo tỷ lệ tương phản màu sắc WCAG 2.1 AA ($\ge 4.5:1$) trên toàn bộ giao diện Identity
- [x] T035 [P] Thực hiện đầy đủ 6 kịch bản kiểm thử trong `BookingHotel/specs/005-account-role-management/quickstart.md`
- [x] T036 Cập nhật trạng thái RM-005 sang `xong` trong `BookingHotel/docs/roadmap.md`

---

## Dependencies & Execution Order

### Phase Dependencies
- **Setup (Phase 1)**: Không phụ thuộc - thực thi ngay lập tức.
- **Foundational (Phase 2)**: Phụ thuộc vào Phase 1 hoàn tất - CHẶN toàn bộ User Stories phía sau.
- **User Stories (Phase 3+)**: Phụ thuộc vào Phase 2 hoàn tất.
  - Phase 3 (US1 - System Admin MVP) $\rightarrow$ Phase 4 (US2 - Tenant Admin) $\rightarrow$ Phase 5 (US5 - Anti-Privilege) $\rightarrow$ Phase 6 (US3 - Manager) $\rightarrow$ Phase 7 (US4 - Customer).
- **Polish (Phase 8)**: Phụ thuộc vào tất cả User Stories hoàn tất.

### Parallel Opportunities
- Các task đánh dấu `[P]` trong Phase 1 (T004, T005, T006) có thể thực hiện song song.
- Dịch vụ Angular T011 có thể phát triển song song với Backend Controller T010.
- Unit Tests T012, T017, T023 có thể viết song song độc lập.
- Các task Polish T032, T033, T034, T035 có thể chạy song song.

---

## Implementation Strategy

### MVP First (User Story 1 Only)
1. Hoàn thành Phase 1 (Setup CSDL & DTOs).
2. Hoàn thành Phase 2 (Backend Services & Unlock Logic).
3. Hoàn thành Phase 3 (User Story 1: System Admin quản lý tài khoản Host & Mở khóa brute-force).
4. **VALIDATE MVP**: Kiểm thử độc lập US1 qua kịch bản kiểm thử `quickstart.md`.

### Incremental Delivery
1. Setup + Foundation $\rightarrow$ Sẵn sàng nền tảng.
2. Thêm US1 $\rightarrow$ Kiểm thử độc lập $\rightarrow$ MVP Quản trị Host.
3. Thêm US2 $\rightarrow$ Kiểm thử độc lập $\rightarrow$ Quản trị nhân sự chuỗi & phân bổ chi nhánh.
4. Thêm US5 $\rightarrow$ Kiểm thử độc lập $\rightarrow$ Rào chắn chống leo thang quyền hoàn tất.
5. Thêm US3 & US4 $\rightarrow$ Kiểm thử độc lập $\rightarrow$ Đổi mật khẩu lần đầu & phân ca trực.
6. Polish $\rightarrow$ Hoàn tất 100% tính năng RM-005.

