# Tasks: RM-003 - Chuẩn Bị Phân Quyền Hệ Thống & Vai Trò Người Dùng (Role-Based Permissions Preparation)

**Input**: Design documents from `/specs/003-prepare-permissions/` (`plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/permissions-matrix.md`, `quickstart.md`)  
**Prerequisites**: `plan.md` (required), `spec.md` (required), `research.md`, `data-model.md`, `contracts/permissions-matrix.md`  
**Organization**: Tasks được nhóm theo từng User Story để đảm bảo tính độc lập và khả năng kiểm thử độc lập cho từng vai trò.

## Format: `- [ ] [ID] [P?] [Story?] Description with file path`

- **[P]**: Có thể thực hiện song song (khác file, không phụ thuộc vào task chưa hoàn thành).
- **[Story]**: Đánh dấu User Story tương ứng (`[US1]`, `[US2]`, `[US3]`, `[US4]`, `[US5]`).
- Đường dẫn file mã nguồn chính xác trong mô tả task.

---

## Phase 1: Setup (Shared Infrastructure)

**Mục đích**: Khởi tạo hằng số định danh vai trò chuẩn và bộ tài nguyên bản địa hóa song ngữ làm nền tảng dùng chung.

- [x] T001 Khởi tạo file hằng số định danh 5 vai trò chuẩn `BookingHotelRoleConsts.cs` trong `BookingHotel/src/BookingHotel.Domain.Shared/Identity/BookingHotelRoleConsts.cs`
- [x] T002 Bổ sung chuỗi bản địa hóa tiếng Việt cho 5 vai trò, tên nhóm quyền và các quyền hạt nhân trong `BookingHotel/src/BookingHotel.Domain.Shared/Localization/BookingHotel/vi.json`
- [x] T003 [P] Bổ sung chuỗi bản địa hóa tiếng Anh cho 5 vai trò, tên nhóm quyền và các quyền hạt nhân trong `BookingHotel/src/BookingHotel.Domain.Shared/Localization/BookingHotel/en.json`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Mục đích**: Xây dựng hạ tầng cây hằng số phân quyền, thực thể phân bổ chi nhánh và khung sườn Data Seeder trước khi nạp ma trận quyền cụ thể.

**⚠️ CRITICAL**: Toàn bộ công việc của các User Story phụ thuộc vào việc hoàn tất Phase 2.

- [x] T004 Khởi tạo danh mục hằng số quyền hạt nhân phân cấp `BookingHotelPermissions.cs` trong `BookingHotel/src/BookingHotel.Application.Contracts/Permissions/BookingHotelPermissions.cs`
- [x] T005 Khởi tạo lớp đăng ký phân quyền và gán cờ `MultiTenancySides` (`Host`, `Tenant`, `Both`) trong `BookingHotel/src/BookingHotel.Application.Contracts/Permissions/BookingHotelPermissionDefinitionProvider.cs`
- [x] T006 [P] Tạo thực thể phân bổ chi nhánh `UserHotelAssignment.cs` kế thừa `CreationAuditedEntity<Guid>, IMultiTenant` trong `BookingHotel/src/BookingHotel.Domain/Identity/UserHotelAssignment.cs`
- [x] T007 Cấu hình ánh xạ DbContext và chỉ mục duy nhất compound `(TenantId, UserId, HotelId)` trong `BookingHotel/src/BookingHotel.EntityFrameworkCore/EntityFrameworkCore/BookingHotelDbContextModelCreatingExtensions.cs`
- [x] T008 [P] Tạo EF Core Code-First Migration bổ sung bảng `AppUserHotelAssignments` trong `BookingHotel/src/BookingHotel.EntityFrameworkCore/Migrations/`
- [x] T009 Triển khai khung sườn bộ nạp dữ liệu `BookingHotelDataSeedContributor.cs` kế thừa `IDataSeedContributor, ITransientDependency` trong `BookingHotel/src/BookingHotel.Domain/Data/BookingHotelDataSeedContributor.cs`

**Checkpoint**: Nền tảng phân quyền và thực thể phân bổ chi nhánh đã sẵn sàng - có thể tiến hành triển khai các User Story theo thứ tự ưu tiên.

---

## Phase 3: User Story 1 - Phân quyền & Quản trị Nền tảng (System / Host Admin) (Priority: P1) 🎯 MVP

**Mục tiêu**: Cung cấp vai trò `System Admin` cấp Host có toàn quyền quản lý vòng đời Tenants, xem toàn bộ audit logs và mở khóa các tài khoản Host bị khóa brute-force.

**Independent Test**: Chạy unit test xác nhận nhóm quyền `Tenants` chỉ cấp phát cho `MultiTenancySides.Host`, vai trò `SystemAdmin` được tạo ở cấp Host và sở hữu toàn quyền quản trị nền tảng.

### Tests for User Story 1
- [x] T010 [P] [US1] Unit test kiểm tra đăng ký quyền nhóm `Tenants` và `UnlockUser` gán đúng cờ `MultiTenancySides.Host` trong `BookingHotel/test/BookingHotel.Application.Tests/Permissions/BookingHotelPermissionDefinitionTests.cs`
- [x] T011 [P] [US1] Unit test kiểm tra tự động seed vai trò `SystemAdmin` và cấp quyền Host khi `TenantId == null` trong `BookingHotel/test/BookingHotel.Domain.Tests/Data/BookingHotelDataSeedContributorTests.cs`

### Implementation for User Story 1
- [x] T012 [US1] Triển khai logic seeding vai trò `SystemAdmin` và gán ma trận quyền `Tenants.*` cùng `Identity.UnlockUser` cấp Host trong `BookingHotel/src/BookingHotel.Domain/Data/BookingHotelDataSeedContributor.cs`
- [x] T013 [US1] Xác thực phân quyền `BookingHotelPermissions.Identity.UnlockUser` cho Quản trị viên hệ thống mở khóa tài khoản Host trong `BookingHotel/src/BookingHotel.Application/Auth/AuthAppService.cs`

**Checkpoint**: User Story 1 hoàn tất và có thể kiểm thử độc lập ở cấp độ Host Admin.

---

## Phase 4: User Story 2 - Phân quyền & Quản trị Doanh nghiệp Khách sạn (Tenant Admin) (Priority: P1)

**Mục tiêu**: Cung cấp vai trò `Tenant Admin` quản lý các cơ sở khách sạn trực thuộc, gán vai trò nhân sự trong tenant, thiết lập chính sách chung và mở khóa tài khoản nhân sự nội bộ.

**Independent Test**: Chạy unit test xác nhận vai trò `TenantAdmin` được seed trong Tenant, sở hữu toàn quyền quản trị khách sạn chi nhánh và có thẩm quyền mở khóa tài khoản nhân sự thuộc Tenant mà không thể truy cập cấp Host.

### Tests for User Story 2
- [x] T014 [P] [US2] Unit test kiểm tra seeding vai trò `TenantAdmin` và ma trận quyền Tenant khi `TenantId != null` trong `BookingHotel/test/BookingHotel.Domain.Tests/Data/BookingHotelDataSeedContributorTests.cs`
- [x] T015 [P] [US2] Unit test kiểm tra phân tầng mở khóa brute-force: Tenant Admin mở khóa được cho nhân viên Tenant và bị từ chối khi thao tác trên tài khoản Host trong `BookingHotel/test/BookingHotel.Application.Tests/Auth/AuthAppService_Unlock_Tests.cs`

### Implementation for User Story 2
- [x] T016 [US2] Triển khai logic seeding vai trò `TenantAdmin` và gán ma trận quyền Doanh nghiệp (Hotels, Rooms, Bookings, Folios, Reports.Consolidated) trong `BookingHotel/src/BookingHotel.Domain/Data/BookingHotelDataSeedContributor.cs`
- [x] T017 [US2] Triển khai phương thức mở khóa tài khoản nhân sự nội bộ Tenant được phân quyền `UnlockUser` cho Tenant Admin trong `BookingHotel/src/BookingHotel.Application/Auth/AuthAppService.cs`

**Checkpoint**: User Story 1 và User Story 2 cùng hoạt động độc lập và bảo đảm phân cách dữ liệu giữa các Tenant.

---

## Phase 5: User Story 3 - Phân quyền Vận hành Cơ sở Khách sạn (Hotel Manager) (Priority: P2)

**Mục tiêu**: Cung cấp vai trò `Hotel Manager` quản lý buồng phòng, hạng phòng, bảng giá bán theo mùa, duyệt ngoại lệ hoàn hủy và xem báo cáo doanh thu theo cơ chế phân công đa chi nhánh `1-n`.

**Independent Test**: Chạy unit test xác nhận vai trò `HotelManager` được cấp đúng các quyền buồng phòng và bảng giá (không có quyền cấu hình chuỗi), đồng thời bộ lọc dữ liệu lọc đúng danh sách `HotelId` được phân công.

### Tests for User Story 3
- [x] T018 [P] [US3] Unit test kiểm tra seeding vai trò `HotelManager` và ma trận quyền buồng phòng/giá bán/báo cáo cơ sở trong `BookingHotel/test/BookingHotel.Domain.Tests/Data/BookingHotelDataSeedContributorTests.cs`
- [x] T019 [P] [US3] Unit test kiểm tra quy tắc phân bổ đa chi nhánh (1-n) và lọc dữ liệu cơ sở cho Hotel Manager trong `BookingHotel/test/BookingHotel.Domain.Tests/Identity/UserHotelAssignmentTests.cs`

### Implementation for User Story 3
- [x] T020 [US3] Triển khai logic seeding vai trò `HotelManager` và ma trận quyền vận hành cơ sở trong `BookingHotel/src/BookingHotel.Domain/Data/BookingHotelDataSeedContributor.cs`
- [x] T021 [US3] Triển khai dịch vụ `UserHotelAssignmentAppService.cs` hỗ trợ Tenant Admin phân công một hoặc nhiều chi nhánh cho Hotel Manager trong `BookingHotel/src/BookingHotel.Application/Identity/UserHotelAssignmentAppService.cs`

**Checkpoint**: Hotel Manager vận hành độc lập trên các chi nhánh được giao phụ trách.

---

## Phase 6: User Story 4 - Phân quyền Tác nghiệp Quầy Lễ tân (Receptionist) (Priority: P2)

**Mục tiêu**: Cung cấp vai trò `Receptionist` chuyên biệt cho quầy lễ tân: sơ đồ buồng phòng thời gian thực, Check-in, Check-out, tạo booking tại quầy, quản lý Folio, minibar và cập nhật phòng sạch/bẩn với ràng buộc cố định duy nhất 1 chi nhánh (1-1).

**Independent Test**: Chạy unit test xác nhận vai trò `Receptionist` sở hữu các quyền tác nghiệp quầy (bị chặn quyền sửa đơn giá gốc và xóa đơn), và tài khoản chỉ gắn cố định với duy nhất 1 chi nhánh khách sạn.

### Tests for User Story 4
- [x] T022 [P] [US4] Unit test kiểm tra seeding vai trò `Receptionist` và ma trận quyền tác nghiệp quầy trong `BookingHotel/test/BookingHotel.Domain.Tests/Data/BookingHotelDataSeedContributorTests.cs`
- [x] T023 [P] [US4] Unit test kiểm tra ràng buộc duy nhất 1 chi nhánh hoạt động (1-1) cho Receptionist trong `BookingHotel/test/BookingHotel.Domain.Tests/Identity/UserHotelAssignmentTests.cs`

### Implementation for User Story 4
- [x] T024 [US4] Triển khai logic seeding vai trò `Receptionist` và ma trận quyền quầy lễ tân trong `BookingHotel/src/BookingHotel.Domain/Data/BookingHotelDataSeedContributor.cs`
- [x] T025 [US4] Áp dụng quy tắc kiểm tra ràng buộc duy nhất 1 cơ sở khách sạn khi gán chi nhánh cho tài khoản Receptionist trong `BookingHotel/src/BookingHotel.Application/Identity/UserHotelAssignmentAppService.cs`

**Checkpoint**: Lễ tân có không gian phân quyền quầy tiếp đón trực tiếp, không chồng chéo với quyền quản lý.

---

## Phase 7: User Story 5 - Phân quyền Trải nghiệm Đặt phòng Cá nhân (Customer) (Priority: P3)

**Mục tiêu**: Cung cấp vai trò `Customer` dùng chung toàn hệ thống (`TenantId = null`), cho phép khách hàng tự tra cứu phòng trống, đặt phòng trực tuyến, thanh toán và hủy phòng hợp lệ trên mọi chi nhánh khách sạn.

**Independent Test**: Chạy unit test xác minh tài khoản đăng ký mới tự động được gán vai trò `Customer` toàn cầu, có quyền đặt phòng/hủy phòng và bị chặn 100% khi cố gọi vào các API quản trị nội bộ.

### Tests for User Story 5
- [x] T026 [P] [US5] Unit test kiểm tra seeding vai trò `Customer` ở cấp Host (`TenantId = null`, `IsDefault = true`) trong `BookingHotel/test/BookingHotel.Domain.Tests/Data/BookingHotelDataSeedContributorTests.cs`
- [x] T027 [P] [US5] Unit test xác thực vai trò Customer bị từ chối (403 Forbidden) khi gọi các endpoint quản trị khách sạn/buồng phòng trong `BookingHotel/test/BookingHotel.Application.Tests/Permissions/BookingHotelPermissionSecurityTests.cs`

### Implementation for User Story 5
- [x] T028 [US5] Triển khai logic seeding vai trò `Customer` toàn cầu và cấp quyền tự phục vụ (`Bookings.Create`, `Bookings.Cancel`, `Folios.View`) trong `BookingHotel/src/BookingHotel.Domain/Data/BookingHotelDataSeedContributor.cs`
- [x] T029 [US5] Tích hợp tự động gán vai trò `Customer` vào tài khoản người dùng khi hoàn tất đăng ký 3 bước tại `RegisterStep3Async` trong `BookingHotel/src/BookingHotel.Application/Auth/AuthAppService.cs`

**Checkpoint**: Toàn bộ 5 vai trò chuẩn đã được phân quyền và hoạt động độc lập, xuyên suốt.

---

## Phase 8: Polish & Cross-Cutting Concerns

**Mục đích**: Hoàn thiện kiểm tra toàn diện, rà soát tính nhất quán tài nguyên và xác thực kịch bản chạy thử nghiệm.

- [x] T030 [P] Rà soát đồng bộ 100% các key chuỗi hiển thị phân quyền giữa `vi.json` và `en.json` trong `BookingHotel/src/BookingHotel.Domain.Shared/Localization/BookingHotel/`
- [x] T031 Thực thi toàn bộ 3 kịch bản kiểm thử trong `quickstart.md` bằng lệnh `dotnet test`
- [x] T032 Chạy kiểm thử công cụ `BookingHotel.DbMigrator` để xác nhận schema CSDL và dữ liệu vai trò/quyền được khởi tạo trơn tru

---

## Dependencies & Execution Order

### Thứ tự Thực thi giữa các Phase
```text
Phase 1: Setup ──► Phase 2: Foundational ──► Phase 3: US1 (Host Admin - MVP)
                                          ├──► Phase 4: US2 (Tenant Admin)
                                          ├──► Phase 5: US3 (Hotel Manager)
                                          ├──► Phase 6: US4 (Receptionist)
                                          └──► Phase 7: US5 (Customer)
                                                    │
                                                    ▼
                                          Phase 8: Polish & Validation
```

### Cơ hội Triển khai Song song (Parallel Opportunities)
- **Phase 1**: `T002` (`vi.json`) và `T003` (`en.json`) có thể làm song song.
- **Phase 2**: `T006` (`UserHotelAssignment.cs`) và `T008` (Migration) có thể chạy song song với `T004`/`T005` (Permissions).
- **Phase 3 $\rightarrow$ Phase 7 (User Stories)**:
  - Sau khi hoàn thành Phase 2, các bài kiểm thử unit tests `[P]` của từng User Story có thể viết song song.
  - Các User Story có thể được phân chia cho các lập trình viên thực hiện độc lập mà không bị xung đột mã nguồn.

---

## Implementation Strategy

### MVP Đầu Tiên (User Story 1 - System Admin)
1. Hoàn tất Phase 1 (Setup) và Phase 2 (Foundational).
2. Triển khai Phase 3 (US1 - Host Admin).
3. Chạy `dotnet test --filter BookingHotelPermissionDefinitionTests` để kiểm tra MVP.

### Phát triển Tăng dần (Incremental Delivery)
1. **MVP**: Cây hằng số quyền + Vai trò System Admin cấp Host.
2. **Increment 2 (US2)**: Cấp phát quyền cho Tenant Admin và phân tầng mở khóa tài khoản.
3. **Increment 3 (US3 & US4)**: Vận hành cơ sở với Hotel Manager (1-n) và Receptionist (1-1) qua `UserHotelAssignment`.
4. **Increment 4 (US5)**: Khách hàng toàn cầu (Global Customer) tự động gán quyền khi đăng ký.
5. **Final Polish**: Kiểm thử toàn diện 100% test suite và chạy `DbMigrator`.

