# Tasks: RM-008 - Thiết Kế Trang Quản Lý Cho Role Hotel Manager (Hotel Manager Operations & Property Console)

**Input**: Design documents from `/specs/008-hotel-manager-management/` (`plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/`, `quickstart.md`)  
**Prerequisites**: `plan.md` (required), `spec.md` (required), `research.md`, `data-model.md`, `contracts/`  
**Organization**: Tasks được phân chia tuần tự theo từng Phase và User Story để đảm bảo khả năng phát triển độc lập, kiểm thử từng phần và tuân thủ nghiêm ngặt 11 điều khoản của Hiến pháp dự án.

## Format: `- [ ] [ID] [P?] [Story?] Description with exact file path`

- **[P]**: Có thể thực hiện song song (khác file, không phụ thuộc vào task chưa hoàn thành).
- **[Story]**: Đánh dấu User Story tương ứng (`[US1]`, `[US2]`, `[US3]`, `[US4]`, `[US5]`, `[US6]`, `[US7]`). Không gắn nhãn story cho Setup, Foundational, hoặc Polish.
- Bao gồm đường dẫn file chính xác trong mọi mô tả task.

---

## Phase 1: Setup (Shared Infrastructure, Enums, Entities & Localization)

**Mục đích**: Xây dựng toàn bộ các kiểu liệt kê (Enums), thực thể miền (Domain Entities), cấu hình EF Core Fluent API, migration CSDL và hệ thống bản địa hóa song ngữ cho phân hệ Quản lý Khách sạn.

- [x] T001 [P] Khai báo các Enum buồng phòng: `RoomOperationalStatus` (`Available = 1`, `Occupied = 2`, `Reserved = 3`, `Maintenance = 4`, `OutOfService = 5`) và `HousekeepingStatus` (`Clean = 1`, `Dirty = 2`, `Cleaning = 3`, `Inspected = 4`) trong `BookingHotel/src/BookingHotel.Domain.Shared/Rooms/RoomEnums.cs`
- [x] T002 [P] Khai báo các Enum đặt phòng: `BookingStatus` (`PendingConfirmation = 1`, `Confirmed = 2`, `CheckedIn = 3`, `Completed = 4`, `Rejected = 5`, `Cancelled = 6`, `NoShow = 7`), `PaymentStatus` (`Unpaid = 1`, `PartiallyPaid = 2`, `Paid = 3`, `Refunded = 4`), và `RoomAssignmentStatus` (`Unassigned = 1`, `Assigned = 2`) trong `BookingHotel/src/BookingHotel.Domain.Shared/Bookings/BookingEnums.cs`
- [x] T003 [P] Khai báo Enum `ComplaintStatus` (`Open = 1`, `InInvestigation = 2`, `Resolved = 3`, `Escalated = 4`) trong `BookingHotel/src/BookingHotel.Domain.Shared/Complaints/ComplaintStatus.cs`
- [x] T004 Tạo các thực thể buồng phòng: `RoomType` (kế thừa `FullAuditedAggregateRoot<Guid>, IMultiTenant`), `Room` (kế thừa `FullAuditedAggregateRoot<Guid>, IMultiTenant`), `RatePlan` (kế thừa `FullAuditedAggregateRoot<Guid>, IMultiTenant`), `HotelRatePlan` (kế thừa `FullAuditedEntity<Guid>, IMultiTenant`), và `DailyRateOverride` trong thư mục `BookingHotel/src/BookingHotel.Domain/Rooms/`
- [x] T005 Tạo các thực thể đặt phòng: `Booking` (kế thừa `FullAuditedAggregateRoot<Guid>, IMultiTenant` quản lý 3 trường trạng thái, `IsDirectBooking`, `WaivePenaltyReason`, `PenaltyAmount`) và `RoomAssignment` (kế thừa `FullAuditedEntity<Guid>, IMultiTenant`) trong `BookingHotel/src/BookingHotel.Domain/Bookings/`
- [x] T006 [P] Tạo thực thể `InternalGuestNote` kế thừa `FullAuditedEntity<Guid>, IMultiTenant` lưu vết `HotelId`, `GuestId`, `NoteText`, `Category`, `IsAlert` trong `BookingHotel/src/BookingHotel.Domain/Guests/InternalGuestNote.cs`
- [x] T007 [P] Tạo các thực thể `Complaint` (kế thừa `FullAuditedAggregateRoot<Guid>, IMultiTenant` lưu `SlaDeadline`, `EscalatedAt`, `ResolutionNotes`) và `Review` (kế thừa `FullAuditedAggregateRoot<Guid>, IMultiTenant` lưu 4 tiêu chí đánh giá, `OfficialReply`) trong `BookingHotel/src/BookingHotel.Domain/Complaints/Complaint.cs` và `BookingHotel/src/BookingHotel.Domain/Reviews/Review.cs`
- [x] T008 Cấu hình EF Core Fluent API, Foreign Keys, Precisions và Indexes cho các thực thể mới trong `BookingHotel/src/BookingHotel.EntityFrameworkCore/EntityFrameworkCore/BookingHotelDbContextModelCreatingExtensions.cs`
- [x] T009 Tạo và thực thi EF Core Migration `AddHotelManagerOperationsEntities` áp dụng vào CSDL thông qua project `BookingHotel.DbMigrator` trong `BookingHotel/src/BookingHotel.EntityFrameworkCore/Migrations/`
- [x] T010 [P] Khai báo 100% bản dịch song ngữ cho phân hệ Hotel Manager (tiền tố `Manager:*`) bao gồm các nhãn buồng phòng, trạng thái vệ sinh, bảng giá, booking, khách hàng, khiếu nại SLA và báo cáo trong `BookingHotel/src/BookingHotel.Domain.Shared/Localization/BookingHotel/vi.json` và `en.json`

---

## Phase 2: Foundational (Backend Contracts, Permissions, Access Guard & Routing)

**Mục đích**: Hoàn thiện các hợp đồng DTOs, đăng ký quyền hạt nhân `BookingHotelPermissions`, xây dựng phương thức xác thực ngữ cảnh trung tâm tại Backend và khung giao diện đa tab trên Angular.

**⚠️ CRITICAL**: Toàn bộ các User Story tác nghiệp phía sau bắt buộc phải kế thừa phương thức bảo vệ ngữ cảnh cơ sở này.

- [x] T011 [P] Khai báo hằng số quyền hạt nhân: `BookingHotelPermissions.Rooms.Default`, `.Create`, `.Edit`, `.SetRates`, `.ChangeStatus`, `.UpdateHousekeeping`, `Bookings.Create`, `Bookings.MarkNoShow`, `Guests.Default`, `Guests.ManageNotes`, `Reports.HotelPerformance`, `Reviews.Reply`, `Reviews.ManageComplaints` và đăng ký trong `BookingHotel/src/BookingHotel.Application.Contracts/Permissions/BookingHotelPermissionDefinitionProvider.cs`
- [x] T012 [P] Xây dựng các DTOs cho các phân hệ của Manager: `ManagerDashboardDto`, `RoomDto`, `RoomTypeDto`, `RatePlanDto`, `DailyRateOverrideDto`, `ManagerBookingDto`, `CreateDirectBookingDto`, `MarkNoShowDto`, `GuestHistoryDto`, `InternalGuestNoteDto`, `HotelPerformanceReportDto`, `ReviewDto`, `ComplaintDto` trong `BookingHotel/src/BookingHotel.Application.Contracts/Manager/`
- [x] T013 [P] Khai báo Interfaces: `IManagerDashboardAppService`, `IManagerRoomAppService`, `IManagerBookingAppService`, `IManagerGuestAppService`, `IManagerReportAppService`, `IManagerFeedbackAppService` trong `BookingHotel/src/BookingHotel.Application.Contracts/Manager/`
- [x] T014 Xây dựng lớp trừu tượng `ManagerAppServiceBase` tích hợp phương thức xác thực ủy quyền cơ sở `CheckHotelManagerAccessAsync(Guid hotelId)` kiểm tra trong `UserHotelAssignment` (`IsActive == true`), ném lỗi HTTP 403 Forbidden `BookingHotel:CrossHotelOperationForbidden` nếu cố tình can thiệp chéo trong `BookingHotel/src/BookingHotel.Application/Manager/ManagerAppServiceBase.cs`
- [x] T015 [P] Xây dựng các Angular Data Services định kiểu mạnh: `ManagerDashboardService`, `ManagerRoomService`, `ManagerBookingService`, `ManagerGuestService`, `ManagerReportService`, `ManagerFeedbackService` trong `BookingHotel/angular/src/app/admin/services/manager/`
- [x] T016 Nâng cấp giao diện vỏ `HotelManagerLayoutComponent` tích hợp bộ chọn cơ sở (**Property Selector**) liên kết URL query `?hotelId=...` và lưu trạng thái vào `sessionStorage` để bảo đảm tính độc lập ngữ cảnh khi mở nhiều tab trong `BookingHotel/angular/src/app/admin/manager/hotel-manager-layout/hotel-manager-layout.component.ts` (kèm HTML & SCSS)
- [x] T017 Cấu hình định tuyến Angular hoàn chỉnh `manager.routes.ts` với các route con: `dashboard`, `hotel-profile`, `rooms`, `rates`, `bookings`, `guests`, `reports`, `feedback` được bảo vệ bởi `hotelManagerGuard` trong `BookingHotel/angular/src/app/admin/manager/manager.routes.ts`

**Checkpoint**: Nền tảng phân quyền, DTOs, cơ chế xác thực Backend Scoping và Layout Shell đa tab hoàn tất.

---

## Phase 3: User Story 1 - Chuyển Đổi Chi Nhánh & Bảng Điều Khiển Tổng Quan (Priority: P1) 🎯 MVP

**Mục tiêu**: Hotel Manager chuyển đổi an toàn giữa các khách sạn được phân công trên Topbar và theo dõi bảng điều khiển vận hành với các chỉ số chuẩn xác (phòng trống/đang ở/đã đặt/bảo trì, tỷ lệ lấp đầy loại trừ `OutOfService`, doanh thu ngày/tuần/tháng, booking sắp đến, và các công việc cần xử lý gấp).

**Independent Test**: Đăng nhập bằng tài khoản Quản lý, mở 2 tab chuyển đổi giữa `PLA-HCM-1` và `HS-VT-1` xác nhận dữ liệu độc lập; gửi API can thiệp `hotelId` trái phép xác nhận nhận HTTP 403; kiểm tra các thẻ KPI và biểu đồ doanh thu theo thời gian thực.

### Implementation for User Story 1
- [x] T018 [P] [US1] Xây dựng `ManagerDashboardAppService` triển khai tính toán các chỉ số KPI: `AvailableInventory = TotalRooms - OutOfService`, `OccupancyRate = (Occupied / AvailableInventory) * 100`, danh sách Today Arrivals và Action Items trong `BookingHotel/src/BookingHotel.Application/Manager/Dashboard/ManagerDashboardAppService.cs`
- [x] T019 [US1] Xây dựng REST API Controller `ManagerDashboardController` phơi bày endpoints `/api/app/manager/dashboard/*` trong `BookingHotel/src/BookingHotel.HttpApi/Controllers/Manager/ManagerDashboardController.cs`
- [x] T020 [US1] Nâng cấp màn hình `ManagerDashboardComponent` hiển thị thẻ KPI tương tác, biểu đồ phân tích doanh thu ngày/tuần/tháng, bảng Today Arrivals và danh sách việc cần xử lý gấp (Action Items) trong `BookingHotel/angular/src/app/admin/manager/manager-dashboard/manager-dashboard.component.ts` (kèm HTML & SCSS)

**Checkpoint**: User Story 1 hoàn tất - Bảng điều khiển vận hành và cơ chế chọn cơ sở đa tab hoạt động độc lập (MVP).

---

## Phase 4: User Story 2 - Quản Lý Buồng Phòng, Loại Phòng & Gói Giá (Priority: P1)

**Mục tiêu**: Hotel Manager quản lý danh mục Loại phòng (`RoomType`), danh sách phòng thực tế (`Room`), quản lý độc lập 2 chiều trạng thái vận hành và tình trạng vệ sinh, thực hiện đóng phòng (`OutOfService` / `Maintenance`) an toàn kèm kiểm tra xung đột booking, và cấu hình bảng giá đa tầng (kích hoạt RatePlan Tenant, cấu hình BasePrice và ghi đè giá ngày `DailyRateOverride`).

**Independent Test**: Tạo loại phòng mới; tạo danh sách phòng thực tế; cập nhật phòng sang `Dirty` và kiểm tra hệ thống chặn check-in; thử chuyển phòng sang `OutOfService` khi phòng đã có booking trong khoảng ngày và xác nhận hệ thống từ chối (`BookingHotel:ConflictingBookingExists`); kích hoạt RatePlan chuẩn và thiết lập giá ngày cuối tuần thành công.

### Implementation for User Story 2
- [x] T021 [P] [US2] Xây dựng `ManagerRoomAppService` triển khai CRUD Loại phòng (`RoomType`) và Phòng thực tế (`Room`), kèm quy tắc phòng sẵn sàng bán: $\text{Sellable} \iff (\text{Available}) \land (\text{Clean} \lor \text{Inspected})$ trong `BookingHotel/src/BookingHotel.Application/Manager/Rooms/ManagerRoomAppService.cs`
- [x] T022 [US2] Triển khai phương thức `ChangeOperationalStatusAsync` trong `ManagerRoomAppService`: Bắt buộc nhập `Reason` và `ExpectedEndDate`; kiểm tra phòng không được `Occupied` và không có `RoomAssignment` nào trong khoảng ngày; ghi vết `AuditLog` trong `BookingHotel/src/BookingHotel.Application/Manager/Rooms/ManagerRoomAppService.cs`
- [x] T023 [US2] Triển khai logic quản lý giá trong `ManagerRateAppService`: Lấy danh mục RatePlan chuẩn của Tenant, kích hoạt/vô hiệu hóa tại cơ sở (`ToggleRatePlanAsync`), cập nhật `BasePrice` và ghi đè giá theo ngày (`BatchOverrideDailyRatesAsync`) trong `BookingHotel/src/BookingHotel.Application/Manager/Rooms/ManagerRateAppService.cs`
- [x] T024 [US2] Xây dựng REST API Controllers `ManagerRoomController` và `ManagerRateController` trong `BookingHotel/src/BookingHotel.HttpApi/Controllers/Manager/`
- [x] T025 [P] [US2] Xây dựng màn hình danh sách buồng phòng `RoomListComponent` hiển thị lưới phòng phân theo tầng, huy hiệu trạng thái 2 chiều, bộ lọc trạng thái vận hành/vệ sinh trong `BookingHotel/angular/src/app/admin/manager/rooms/room-list/room-list.component.ts` (kèm HTML & SCSS)
- [x] T026 [US2] Xây dựng modal chuyển trạng thái phòng `RoomStatusModalComponent` hỗ trợ chuyển sang `Maintenance` hoặc `OutOfService` bắt buộc nhập lý do, ngày kết thúc và hiển thị cảnh báo xung đột nếu có trong `BookingHotel/angular/src/app/admin/manager/rooms/room-modal/room-status-modal.component.ts` (kèm HTML & SCSS)
- [x] T027 [US2] Xây dựng modal quản lý loại phòng `RoomTypeModalComponent` nhập tên, diện tích, sức chứa, tiện nghi và giá niêm yết chuẩn `BasePrice` trong `BookingHotel/angular/src/app/admin/manager/rooms/room-type-modal/room-type-modal.component.ts` (kèm HTML & SCSS)
- [x] T028 [US2] Xây dựng màn hình quản lý giá phòng `RatesManagementComponent` hiển thị danh mục RatePlan để kích hoạt/bỏ kích hoạt và bảng lịch điều chỉnh giá theo ngày `DailyRateOverride` trong `BookingHotel/angular/src/app/admin/manager/rates/rates-management.component.ts` (kèm HTML & SCSS)

**Checkpoint**: User Story 2 hoàn tất - Hệ thống quản lý buồng phòng 2 chiều trạng thái và bảng giá đa tầng hoạt động ổn định.

---

## Phase 5: User Story 3 - Quản Lý Thông Tin Hồ Sơ Khách Sạn & Tiện Ích (Priority: P1)

**Mục tiêu**: Hotel Manager xem và cập nhật hồ sơ khách sạn phụ trách (mô tả, hotline, email lễ tân, tiện ích Amenities, hình ảnh, quy định lưu trú, giờ check-in/out chi nhánh) với mã Hotel Code được bảo vệ bất biến.

**Independent Test**: Vào `/manager/hotel-profile`, kiểm tra mã `HotelCode` và Tên cơ sở bị khóa (Read-Only); cập nhật mô tả, tải lên hình ảnh tiện ích, sửa giờ Check-in thành 14:00, lưu thành công và kiểm tra thông tin hiển thị chính xác.

### Implementation for User Story 3
- [x] T029 [P] [US3] Xây dựng `ManagerHotelProfileAppService` cho phép lấy và cập nhật thông tin cơ sở (bảo vệ bất biến không cho sửa `Code` và `TenantId`) trong `BookingHotel/src/BookingHotel.Application/Manager/Hotels/ManagerHotelProfileAppService.cs`
- [x] T030 [US3] Xây dựng REST API Controller `ManagerHotelProfileController` phơi bày endpoints `/api/app/manager/hotel-profile/*` trong `BookingHotel/src/BookingHotel.HttpApi/Controllers/Manager/ManagerHotelProfileController.cs`
- [x] T031 [US3] Xây dựng màn hình hồ sơ khách sạn `HotelProfileComponent` hiển thị trường mã cơ sở ở dạng Read-Only, form cập nhật tiện ích Amenities, thư viện ảnh và giờ check-in/out trong `BookingHotel/angular/src/app/admin/manager/hotel-profile/hotel-profile.component.ts` (kèm HTML & SCSS)

**Checkpoint**: User Story 3 hoàn tất - Hồ sơ khách sạn được cập nhật an toàn với mã định danh cơ sở bất biến.

---

## Phase 6: User Story 4 - Quản Lý & Điều Phối Đặt Phòng (Booking Operations) (Priority: P2)

**Mục tiêu**: Hotel Manager tạo mới đơn đặt phòng trực tiếp (`Direct Booking`) cho khách đoàn/doanh nghiệp, tra cứu danh sách đặt phòng với 3 trạng thái độc lập (`BookingStatus`, `PaymentStatus`, `RoomAssignmentStatus`), xác nhận/từ chối booking, và xử lý No-Show giải phóng phòng theo ngày kèm quyền điều chỉnh giảm/miễn phạt có lưu `AuditLog`.

**Independent Test**: Tạo mới một đơn đặt phòng trực tiếp corporate cho 3 phòng thành công; tra cứu booking theo tên hoặc SĐT; thực hiện từ chối 1 booking kèm lý do; thực hiện No-Show cho đơn quá giờ: chọn miễn phạt (0 VNĐ), nhập lý do bất khả kháng, kiểm tra phòng được giải phóng và log kiểm toán được lưu.

### Implementation for User Story 4
- [x] T032 [P] [US4] Xây dựng `ManagerBookingAppService` triển khai tìm kiếm đặt phòng đa tiêu chí, tra cứu chi tiết và các phương thức `ConfirmAsync`, `RejectAsync(Guid id, RejectBookingDto input)` trong `BookingHotel/src/BookingHotel.Application/Manager/Bookings/ManagerBookingAppService.cs`
- [x] T033 [US4] Triển khai phương thức `CreateDirectBookingAsync(CreateDirectBookingDto input)` trong `ManagerBookingAppService`: Tạo booking mới với cờ `IsDirectBooking = true`, gán trạng thái `Confirmed` và tạo bản ghi gán phòng `RoomAssignment` trong `BookingHotel/src/BookingHotel.Application/Manager/Bookings/ManagerBookingAppService.cs`
- [x] T034 [US4] Triển khai phương thức `MarkNoShowAsync(Guid id, MarkNoShowDto input)` trong `ManagerBookingAppService`: Hủy `RoomAssignment` của đơn, tính mức phạt theo Tenant Policy hoặc áp dụng mức phạt điều chỉnh nếu `WaivePenalty == true`, lưu `WaivePenaltyReason` và `UserId` vào `AuditLog` trong `BookingHotel/src/BookingHotel.Application/Manager/Bookings/ManagerBookingAppService.cs`
- [x] T035 [US4] Xây dựng REST API Controller `ManagerBookingController` phơi bày endpoints `/api/app/manager/bookings/*` trong `BookingHotel/src/BookingHotel.HttpApi/Controllers/Manager/ManagerBookingController.cs`
- [x] T036 [P] [US4] Xây dựng màn hình danh sách đặt phòng `BookingListComponent` sử dụng `BhDataTableComponent`, hiển thị 3 huy hiệu trạng thái độc lập, bộ lọc đa chiều trong `BookingHotel/angular/src/app/admin/manager/bookings/booking-list/booking-list.component.ts` (kèm HTML & SCSS)
- [x] T037 [US4] Xây dựng modal chi tiết đặt phòng `BookingDetailModalComponent` hiển thị thông tin khách, chi tiết phòng gán, dòng tiền thanh toán và lịch sử thao tác trong `BookingHotel/angular/src/app/admin/manager/bookings/booking-detail-modal/booking-detail-modal.component.ts` (kèm HTML & SCSS)
- [x] T038 [US4] Xây dựng modal tạo đặt phòng trực tiếp `DirectBookingModalComponent` nhập thông tin khách đoàn, chọn loại phòng, đơn giá thỏa thuận và số tiền cọc nhận trước trong `BookingHotel/angular/src/app/admin/manager/bookings/direct-booking-modal/direct-booking-modal.component.ts` (kèm HTML & SCSS)
- [x] T039 [US4] Xây dựng modal xác nhận No-Show `NoShowModalComponent` hiển thị số tiền phạt chuẩn, checkbox điều chỉnh miễn phạt và textarea bắt buộc nhập lý do ngoại lệ trong `BookingHotel/angular/src/app/admin/manager/bookings/no-show-modal/no-show-modal.component.ts` (kèm HTML & SCSS)

**Checkpoint**: User Story 4 hoàn tất - Phân hệ đặt phòng với đầy đủ tác nghiệp tạo đơn trực tiếp và quyết toán No-Show hoạt động chính xác.

---

## Phase 7: User Story 5 - Quản Lý Khách Hàng Cơ Sở & Ghi Chú Có Audit (Priority: P2)

**Mục tiêu**: Hotel Manager xem danh sách khách hàng đã từng lưu trú tại cơ sở, xem lịch sử các lần ở, số CCCD/Passport được che bảo mật (`***-***-XXXX`), và quản lý Ghi chú nội bộ (`InternalGuestNotes`) có Audit Log đầy đủ.

**Independent Test**: Vào `/manager/guests`, kiểm tra danh sách khách: chỉ hiển thị khách từng ở cơ sở; CCCD hiển thị 4 số cuối; tạo ghi chú nội bộ cho khách VIP, kiểm tra thông tin người tạo hiển thị đúng và không bị lộ ra Customer Portal.

### Implementation for User Story 5
- [x] T040 [P] [US5] Xây dựng `ManagerGuestAppService` truy xuất danh sách khách hàng giới hạn nghiêm ngặt theo các booking của cơ sở, thực hiện che bảo mật số CCCD (`***-***-1234`), và lấy lịch sử lưu trú tại cơ sở trong `BookingHotel/src/BookingHotel.Application/Manager/Guests/ManagerGuestAppService.cs`
- [x] T041 [US5] Triển khai CRUD Ghi chú nội bộ `InternalGuestNotes` trong `ManagerGuestAppService` ghi nhận tự động `CreatorId`, `CreationTime` từ `CurrentUser` trong `BookingHotel/src/BookingHotel.Application/Manager/Guests/ManagerGuestAppService.cs`
- [x] T042 [US5] Xây dựng REST API Controller `ManagerGuestController` phơi bày endpoints `/api/app/manager/guests/*` trong `BookingHotel/src/BookingHotel.HttpApi/Controllers/Manager/ManagerGuestController.cs`
- [x] T043 [P] [US5] Xây dựng màn hình danh sách khách hàng `GuestListComponent` hiển thị số lượt ở, tổng chi tiêu tại khách sạn, số giấy tờ tùy thân che masked trong `BookingHotel/angular/src/app/admin/manager/guests/guest-list/guest-list.component.ts` (kèm HTML & SCSS)
- [x] T044 [US5] Xây dựng modal ghi chú nội bộ `GuestNotesModalComponent` hiển thị danh sách ghi chú có nhãn Audit, form thêm ghi chú mới và cờ cảnh báo VIP Alert trong `BookingHotel/angular/src/app/admin/manager/guests/guest-notes-modal/guest-notes-modal.component.ts` (kèm HTML & SCSS)

**Checkpoint**: User Story 5 hoàn tất - Dữ liệu khách hàng được bảo vệ an toàn và hệ thống ghi chú nội bộ có kiểm toán vận hành hoàn hảo.

---

## Phase 8: User Story 6 - Báo Cáo Hiệu Suất & Phân Rã Dòng Tiền Theo Chuẩn USALI (Priority: P2)

**Mục tiêu**: Hotel Manager xem các báo cáo phân tích hiệu suất chuyên sâu với công thức chuẩn quốc tế (Occupancy Rate, ADR, RevPAR, phân rã doanh thu phòng/dịch vụ/thực thu, tỷ lệ hủy phòng và thất thoát công suất do `OutOfService`).

**Independent Test**: Vào `/manager/reports`, chọn kỳ báo cáo; kiểm tra công thức tính `Occupancy = Occupied / (Total - OutOfService)`, `ADR = RoomRevenue / RoomsSold`, `RevPAR = RoomRevenue / AvailableInventory`; xuất dữ liệu báo cáo ra Excel/CSV.

### Implementation for User Story 6
- [x] T045 [P] [US6] Xây dựng `ManagerReportAppService` tính toán các chỉ số USALI: `OccupancyRate`, `ADR`, `RevPAR`, phân rã dòng tiền (`RoomRevenue`, `ServiceRevenue`, `CollectedRevenue`, `RefundAmount`), và báo cáo thất thoát công suất phòng `OutOfService` trong `BookingHotel/src/BookingHotel.Application/Manager/Reports/ManagerReportAppService.cs`
- [x] T046 [US6] Xây dựng REST API Controller `ManagerReportController` phơi bày endpoints `/api/app/manager/reports/*` trong `BookingHotel/src/BookingHotel.HttpApi/Controllers/Manager/ManagerReportController.cs`
- [x] T047 [US6] Xây dựng màn hình báo cáo hiệu suất cơ sở `ManagerReportsComponent` tích hợp bộ chọn khoảng ngày, các thẻ KPI chuẩn USALI, biểu đồ phân rã dòng tiền và nút xuất file Excel trong `BookingHotel/angular/src/app/admin/manager/reports/manager-reports.component.ts` (kèm HTML & SCSS)

**Checkpoint**: User Story 6 hoàn tất - Báo cáo quản trị cơ sở tính toán chính xác và minh bạch theo tiêu chuẩn quốc tế.

---

## Phase 9: User Story 7 - Đánh Giá & Xử Lý Khiếu Nại Của Khách Hàng (Priority: P3)

**Mục tiêu**: Hotel Manager xem danh sách đánh giá kèm điểm theo 4 tiêu chí, đăng phản hồi chính thức của khách sạn, và theo dõi tiến độ xử lý khiếu nại (Open $\rightarrow$ InInvestigation $\rightarrow$ Resolved) kèm cam kết SLA 48 giờ tự động leo thang (`Escalated`) lên Tenant Admin.

**Independent Test**: Vào `/manager/feedback`, xem đánh giá 4 tiêu chí và đăng phản hồi chính thức; cập nhật tiến độ khiếu nại sang `InInvestigation`; giả lập khiếu nại quá 48h kiểm tra hệ thống gắn nhãn cảnh báo `Overdue SLA` và gửi thông báo `Escalated` lên Tenant Admin.

### Implementation for User Story 7
- [x] T048 [P] [US7] Xây dựng `ManagerFeedbackAppService` lấy danh sách đánh giá kèm điểm trung bình 4 tiêu chí và triển khai phương thức đăng phản hồi chính thức `ReplyToReviewAsync` trong `BookingHotel/src/BookingHotel.Application/Manager/Feedback/ManagerFeedbackAppService.cs`
- [x] T049 [US7] Triển khai luồng khiếu nại trong `ManagerFeedbackAppService`: Tạo khiếu nại gán `SlaDeadline = Clock.Now.AddHours(48)`, cập nhật trạng thái `Open` $\rightarrow$ `InInvestigation` $\rightarrow$ `Resolved` kèm ghi chú giải quyết trong `BookingHotel/src/BookingHotel.Application/Manager/Feedback/ManagerFeedbackAppService.cs`
- [x] T050 [US7] Xây dựng Background Worker `ComplaintSlaCheckWorker` quét định kỳ các khiếu nại quá hạn SLA 48h mà chưa `Resolved`, tự động cập nhật `Status = Escalated`, gán `EscalatedAt` và phát event thông báo cho Tenant Admin trong `BookingHotel/src/BookingHotel.Application/Manager/Feedback/ComplaintSlaCheckWorker.cs`
- [x] T051 [US7] Xây dựng REST API Controller `ManagerFeedbackController` phơi bày endpoints `/api/app/manager/feedback/*` trong `BookingHotel/src/BookingHotel.HttpApi/Controllers/Manager/ManagerFeedbackController.cs`
- [x] T052 [P] [US7] Xây dựng màn hình quản lý đánh giá `ReviewListComponent` hiển thị điểm sao theo 4 tiêu chí, danh sách nhận xét và modal nhập phản hồi chính thức trong `BookingHotel/angular/src/app/admin/manager/feedback/review-list/review-list.component.ts` (kèm HTML & SCSS)
- [x] T053 [US7] Xây dựng màn hình xử lý khiếu nại `ComplaintListComponent` hiển thị đồng hồ đếm ngược SLA, nhãn cảnh báo đỏ `Overdue SLA`, huy hiệu `Escalated`, và modal cập nhật kết quả xử lý trong `BookingHotel/angular/src/app/admin/manager/feedback/complaint-list/complaint-list.component.ts` (kèm HTML & SCSS)

**Checkpoint**: User Story 7 hoàn tất - Phân hệ đánh giá và luồng khiếu nại SLA 48h tự động leo thang hoạt động chuẩn xác.

---

## Phase 10: Polish, Quality Assurance & Verification

**Mục đích**: Hoàn thiện toàn bộ các chi tiết cross-cutting, kiểm tra tuân thủ Hiến pháp dự án và xác minh tính toàn vẹn của mã nguồn.

- [x] T054 Thực hiện toàn bộ 9 kịch bản kiểm thử tích hợp (End-to-End) theo tài liệu `quickstart.md`
- [x] T055 [P] Rà soát 100% mã nguồn Angular và Backend đảm bảo chính sách **Zero Hardcoded Text** (Điều khoản V Hiến pháp); mọi chuỗi hiển thị đều truy xuất qua ABP Localization
- [x] T056 [P] Rà soát 100% các form nhập liệu đảm bảo quy chuẩn **Dấu sao đỏ `*`** cho trường bắt buộc (Điều khoản XI Hiến pháp)
- [x] T057 [P] Kiểm tra độ tương phản màu sắc đạt chuẩn WCAG 2.1 AA ($\ge 4.5:1$) theo Concept Quản lý (Điều khoản VII Hiến pháp)
- [x] T058 Thực thi biên dịch toàn bộ Solution: `dotnet build` và Angular `tsc --noEmit` đạt 0 Warning / 0 Error

---

## Thứ Tự Thực Thi & Các Cơ Hội Triển Khai Song Song (Parallel Execution)

1. **Tuần tự cơ bản**: Phase 1 (Setup) $\rightarrow$ Phase 2 (Foundational) $\rightarrow$ Các User Story (Phase 3 đến Phase 9) $\rightarrow$ Phase 10 (Polish).
2. **Cơ hội song song trong cùng Phase**:
   - Trong Phase 1: T001, T002, T003, T006, T007, T010 có thể tạo song song.
   - Trong Phase 2: T011, T012, T013, T015 có thể viết song song.
   - Sau khi Phase 2 hoàn tất: Các User Story có thể tiến hành độc lập theo từng nhóm lập trình viên (ví dụ: Developer A làm Buồng phòng US2, Developer B làm Đặt phòng US4, Developer C làm Giao diện Dashboard US1).
3. **Mỗi User Story tuân thủ**: Entity/DTO $\rightarrow$ AppService $\rightarrow$ Controller $\rightarrow$ Angular Service $\rightarrow$ Component UI.