# Tasks: RM-010 – Quản Lý Phòng Khách Sạn (Room Management)

**Input**: Design documents from `/specs/010-room-management/` (`plan.md`, `spec.md`)  
**Prerequisites**: `plan.md` (required), `spec.md` (required)  
**Organization**: Tasks được phân chia tuần tự theo từng Phase và User Story để đảm bảo khả năng phát triển độc lập, kiểm thử từng phần và tuân thủ nghiêm ngặt 22 Quy tắc nghiệp vụ (BR-01 đến BR-22) cùng Hiến pháp dự án.

## Format: `- [ ] [ID] [P?] [Story?] Description with exact file path`

- **[P]**: Có thể thực hiện song song (khác file, không phụ thuộc vào task chưa hoàn thành).
- **[Story]**: Đánh dấu User Story tương ứng (`[US1]`, `[US2]`, `[US3]`, `[US4]`, `[US5]`). Không gắn nhãn story cho Setup, Foundational, hoặc Polish.
- Đường dẫn file mã nguồn chính xác trong mọi mô tả task.

---

## Phase 1: Setup & Domain Modeling (Shared Infrastructure, Enums, Entities & Migrations)

**Mục đích**: Hoàn thiện toàn bộ các kiểu liệt kê nghiệp vụ mở rộng, các thực thể Aggregate Root và Entity mới trong Domain Layer, cấu hình EF Core Fluent API, tạo Migration và bổ sung từ điển bản địa hóa song ngữ.

- [x] T001 [P] Mở rộng các enum buồng phòng trong `BookingHotel/src/BookingHotel.Domain.Shared/Rooms/RoomEnums.cs`:
  - Khai báo `RoomOccupancyStatus` (`Vacant = 1`, `Reserved = 2`, `Occupied = 3`, `Blocked = 4`)
  - Cập nhật `RoomOperationalStatus` (`Active = 1`, `UnderMaintenance = 2`, `OutOfService = 3`)
  - Khai báo `RoomClosureType` (`Planned = 1`, `Emergency = 2`)
  - Khai báo `RoomClosureStatus` (`Pending = 1`, `Active = 2`, `Resolved = 3`, `Cancelled = 4`)
  - Khai báo `AffectedBookingResolutionType` (`RoomChange = 1`, `RoomUpgrade = 2`, `Refund = 3`, `OtherResolution = 4`)
- [x] T002 [P] Mở rộng Aggregate Root `Room` bổ sung các trường: `OccupancyStatus` (`RoomOccupancyStatus`), `IsActive` (`bool`, mặc định true), `Building` (`string?`), `Description` (`string?`) trong `BookingHotel/src/BookingHotel.Domain/Rooms/Room.cs`
- [x] T003 [P] Tạo Aggregate Root `RoomClosure` kế thừa `FullAuditedAggregateRoot<Guid>, IMultiTenant` quản lý các trường `HotelId`, `RoomId`, `ClosureType`, `Reason`, `IncidentDescription`, `StartDate`, `EndDate`, `EstimatedRecoveryTime`, `Status`, `RecoveryConfirmedBy`, `RecoveryConfirmedTime` trong `BookingHotel/src/BookingHotel.Domain/Rooms/RoomClosure.cs`
- [x] T004 [P] Tạo Entity `AffectedBookingResolution` kế thừa `FullAuditedEntity<Guid>, IMultiTenant` quản lý các trường `RoomClosureId`, `BookingId`, `ResolutionType`, `AlternativeRoomId`, `ResolvedBy`, `ResolvedTime`, `Notes`, `IsCustomerNotified` trong `BookingHotel/src/BookingHotel.Domain/Rooms/AffectedBookingResolution.cs`
- [x] T005 Khai báo `DbSet<RoomClosure>` và `DbSet<AffectedBookingResolution>` trong `BookingHotelDbContext` cùng cấu hình Fluent API, Foreign Keys, Indexes cho `RoomClosure` và `AffectedBookingResolution` trong `BookingHotel/src/BookingHotel.EntityFrameworkCore/EntityFrameworkCore/BookingHotelDbContextModelCreatingExtensions.cs`
- [x] T006 Tạo EF Core Migration `AddRoomManagementAndClosures` và áp dụng vào cơ sở dữ liệu qua `BookingHotel.DbMigrator` trong `BookingHotel/src/BookingHotel.EntityFrameworkCore/Migrations/`
- [x] T007 [P] Bổ sung 100% key bản địa hóa song ngữ cho phân hệ Room Management (tiền tố `RoomManagement:*` và `Rooms:*`) trong `BookingHotel/src/BookingHotel.Domain.Shared/Localization/BookingHotel/vi.json` và `en.json`

---

## Phase 2: Foundational (Application Contracts, DTOs & Permissions)

**Mục đích**: Khai báo hằng số quyền hạt nhân, định nghĩa toàn bộ hợp đồng DTOs và hoàn thiện interface `IManagerRoomAppService` trước khi hiện thực hóa logic nghiệp vụ.

- [x] T008 [P] Khai báo hằng số quyền hạt nhân: `BookingHotelPermissions.Rooms.ManageClosures`, `BookingHotelPermissions.Rooms.ResolveIncidents` và đăng ký trong `BookingHotel/src/BookingHotel.Domain.Shared/Permissions/BookingHotelPermissions.cs` và `BookingHotel/src/BookingHotel.Application.Contracts/Permissions/BookingHotelPermissionDefinitionProvider.cs`
- [x] T009 [P] Xây dựng các DTOs cho Room Dashboard: `RoomDashboardDto`, `RoomOccupancyBreakdownDto`, `RoomOperationalBreakdownDto` trong `BookingHotel/src/BookingHotel.Application.Contracts/Manager/Rooms/ManagerRoomDashboardDtos.cs`
- [x] T010 [P] Nâng cấp và mở rộng DTOs cho Quản lý phòng vật lý: `PhysicalRoomDto` (kèm `OccupancyStatus`, `IsActive`, `Building`, `Description`), `CreatePhysicalRoomDto`, `UpdatePhysicalRoomDto`, `DeactivateRoomDto` trong `BookingHotel/src/BookingHotel.Application.Contracts/Manager/Rooms/ManagerRoomDtos.cs`
- [x] T011 [P] Xây dựng các DTOs cho Quản lý đóng phòng và giải quyết sự cố: `CheckAffectedBookingsInputDto`, `AffectedBookingDto`, `CreatePlannedClosureDto`, `CreateEmergencyClosureDto`, `RoomClosureDto`, `ResolveAffectedBookingDto`, `ConfirmRoomRecoveryDto`, `AlternativeRoomSuggestionDto` trong `BookingHotel/src/BookingHotel.Application.Contracts/Manager/Rooms/ManagerRoomClosureDtos.cs`
- [x] T012 [P] Khai báo các phương thức mới trong interface `IManagerRoomAppService` (`GetDashboardOverviewAsync`, `GetPhysicalRoomsAsync`, `CreatePhysicalRoomAsync`, `UpdatePhysicalRoomAsync`, `DeactivateRoomAsync`, `DeletePhysicalRoomAsync`, `CheckAffectedBookingsAsync`, `CreatePlannedClosureAsync`, `CreateEmergencyClosureAsync`, `GetClosureHistoryAsync`, `ResolveAffectedBookingAsync`, `GetAlternativeRoomSuggestionsAsync`, `ConfirmRoomRecoveryAsync`) trong `BookingHotel/src/BookingHotel.Application.Contracts/Manager/Rooms/IManagerRoomAppService.cs`
- [x] T013 [P] Cập nhật và mở rộng Angular Data Service `ManagerRoomService` với các interfaces DTOs và API methods tương ứng trong `BookingHotel/angular/src/app/admin/services/manager/manager-room.service.ts`

**Checkpoint**: Nền tảng Domain, Entity, DbContext, Migration, DTOs và Service Interface hoàn tất.

---

## Phase 3: User Story 1 - Dashboard Tổng Quan Buồng Phòng & Chỉ Số Thời Gian Thực (Priority: P1) 🎯 MVP

**Mục tiêu**: Cung cấp các chỉ số đo lường tức thời (Real-time Metrics) giúp Hotel Manager nắm bắt sức khỏe buồng phòng theo thời gian thực (Total, Occupied, Vacant, Reserved, Unavailable) và phân rã 2 chiều Occupancy/Operational.

**Independent Test**: Mở trang `/manager/rooms`, kiểm tra thẻ KPI hiển thị khớp với số lượng phòng thực tế và trạng thái booking hiện tại của khách sạn được chọn; chuyển đổi cơ sở khác xác nhận số liệu cập nhật độc lập.

### Implementation for User Story 1
- [x] T014 [P] [US1] Triển khai phương thức `GetDashboardOverviewAsync(Guid hotelId)` trong `ManagerRoomAppService`:
  - Kiểm tra quyền truy cập cơ sở qua `CheckHotelManagerAccessAsync(hotelId)` (BR-01, Multi-Tenant).
  - Tính toán các chỉ số thời gian thực: `TotalRooms`, `OccupiedRooms`, `VacantRooms`, `ReservedRooms`, `UnavailableRooms`.
  - Phân rã dữ liệu 2 chiều trạng thái: `OccupancyBreakdown` (Vacant, Reserved, Occupied, Blocked) và `OperationalBreakdown` (Active, UnderMaintenance, OutOfService).
  - Áp dụng nguyên tắc tính toán động theo thời điểm hiện tại `Clock.Now` (BR-07, BR-08).
  File: `BookingHotel/src/BookingHotel.Application/Manager/Rooms/ManagerRoomAppService.cs`
- [x] T015 [US1] Bổ sung REST API endpoint `GET /api/app/manager/rooms/dashboard-overview` có phân quyền trong `BookingHotel/src/BookingHotel.HttpApi/Controllers/Manager/ManagerRoomController.cs`
- [x] T016 [US1] Xây dựng component `RoomDashboardComponent` hiển thị các thẻ KPI buồng phòng (Total, Occupied, Vacant, Reserved, Unavailable) và thanh tỷ lệ phân bổ trực quan trạng thái Occupancy & Operational trong `BookingHotel/angular/src/app/admin/manager/rooms/room-dashboard/room-dashboard.component.ts` (kèm HTML & SCSS)

**Checkpoint**: User Story 1 hoàn tất - Bảng điều khiển KPI buồng phòng thời gian thực hoạt động chính xác.

---

## Phase 4: User Story 2 - Quản Lý Danh Mục Phòng Thực Tế & Lưới Thẻ Theo Tầng (Priority: P1)

**Mục tiêu**: Hotel Manager xem danh sách phòng theo bố cục Lưới Thẻ theo Tầng (Room Cards grouped by Floor), tìm kiếm và lọc đa chiều, tạo mới/chỉnh sửa phòng, thực hiện Deactivate an toàn (chặn nếu có khách/booking tương lai) và Xóa phòng an toàn (chỉ cho phép xóa khi chưa từng phát sinh booking).

**Independent Test**: Tạo phòng mới số 501 tầng 5; thử tạo trùng số 501 trong cùng khách sạn -> bị từ chối; chuyển phòng sang Deactivate khi đang có booking -> hệ thống chặn và thông báo lỗi; xóa phòng chưa có booking -> thành công; xóa phòng đã có lịch sử booking -> hệ thống chặn và hướng dẫn Deactivate.

### Implementation for User Story 2
- [x] T017 [P] [US2] Triển khai phương thức `GetPhysicalRoomsAsync` trong `ManagerRoomAppService`: Hỗ trợ lọc đa chiều theo Tầng (`Floor`), Hạng phòng (`RoomTypeId`), `OccupancyStatus`, `OperationalStatus`, và tìm kiếm `RoomNumber` trong `BookingHotel/src/BookingHotel.Application/Manager/Rooms/ManagerRoomAppService.cs`
- [x] T018 [US2] Triển khai phương thức `CreatePhysicalRoomAsync` và `UpdatePhysicalRoomAsync` trong `ManagerRoomAppService`: Kiểm tra trùng số phòng (`RoomNumber`) trong phạm vi khách sạn (BR-02), kiểm tra `RoomTypeId` thuộc cùng Hotel (BR-03), chuẩn hóa dữ liệu trong `BookingHotel/src/BookingHotel.Application/Manager/Rooms/ManagerRoomAppService.cs`
- [x] T019 [US2] Triển khai phương thức `DeactivateRoomAsync` trong `ManagerRoomAppService`: Thực hiện quy tắc BR-06 kiểm tra phòng không được `Occupied` và không có booking tương lai chưa giải quyết; cập nhật `IsActive = false` an toàn trong `BookingHotel/src/BookingHotel.Application/Manager/Rooms/ManagerRoomAppService.cs`
- [x] T020 [US2] Triển khai phương thức `DeletePhysicalRoomAsync` trong `ManagerRoomAppService`: Thực hiện quy tắc BR-05 kiểm tra phòng chưa từng phát sinh bất kỳ booking hay giao dịch nào; nếu đã có booking, ném ngoại lệ nghiệp vụ `BookingHotel:CannotDeleteRoomWithBookingHistory` và hướng dẫn người dùng chuyển sang Deactivate trong `BookingHotel/src/BookingHotel.Application/Manager/Rooms/ManagerRoomAppService.cs`
- [x] T021 [US2] Cập nhật REST API endpoints cho Physical Rooms (`GET`, `POST`, `PUT`, `POST /{id}/deactivate`, `DELETE /{id}`) trong `BookingHotel/src/BookingHotel.HttpApi/Controllers/Manager/ManagerRoomController.cs`
- [x] T022 [P] [US2] Tái cấu trúc và nâng cấp giao diện `RoomListComponent` sang định dạng **Lưới Thẻ theo Tầng (Room Cards grouped by Floor)**:
  - Gom nhóm các phòng theo từng Floor trực quan phản ánh sơ đồ buồng phòng thực tế.
  - Hiển thị đầy đủ 2 huy hiệu: Trạng thái sử dụng (Occupancy Badge) và Trạng thái vận hành (Operational Badge).
  - Tích hợp thanh tìm kiếm theo số phòng và bộ lọc đa chiều (Tầng, Hạng phòng, Occupancy, Operational).
  - Tích hợp nút thao tác Deactivate / Chỉnh sửa / Xóa cho từng thẻ phòng.
  File: `BookingHotel/angular/src/app/admin/manager/rooms/room-list/room-list.component.ts` (kèm HTML & SCSS)
- [x] T023 [US2] Nâng cấp modal thêm mới/chỉnh sửa phòng bổ sung các trường bắt buộc (dấu sao đỏ `*` theo Điều khoản XI): Số phòng `*`, Hạng phòng `*`, Tầng `*`, Tòa nhà/Phân khu (`Building`), Mô tả phòng trong `BookingHotel/angular/src/app/admin/manager/rooms/room-modal/room-status-modal.component.ts`
- [x] T024 [US2] Xây dựng dialog xác nhận Deactivate phòng an toàn với cảnh báo ảnh hưởng kinh doanh và hiển thị thông báo lỗi khi vi phạm ràng buộc booking trong `BookingHotel/angular/src/app/admin/manager/rooms/room-list/room-list.component.ts`

**Checkpoint**: User Story 2 hoàn tất - Quản lý danh mục phòng, giao diện Lưới Thẻ theo Tầng và quy tắc Deactivate/Delete an toàn vận hành hoàn hảo.

---

## Phase 5: User Story 3 - Lập Lịch Ngừng Khai Thác Theo Kế Hoạch (Planned Closure & Conflict Detection) (Priority: P2)

**Mục tiêu**: Hotel Manager chủ động lập lịch khóa phòng trong tương lai cho các kế hoạch bảo dưỡng định kỳ hoặc chính sách kinh doanh; hệ thống tự động quét và cảnh báo các booking bị trùng lặp thời gian lưu trú để người quản lý chủ động xử lý trước khi lưu (BR-12, BR-13, BR-21).

**Independent Test**: Chọn phòng 301, chọn khoảng thời gian bảo trì 3 ngày tới; hệ thống gọi API quét xung đột và hiển thị danh sách 2 booking có khách đặt trước; xác nhận lưu lịch; kiểm tra phòng bị khóa không cho nhận booking mới trong khoảng ngày đó (BR-22).

### Implementation for User Story 3
- [x] T025 [P] [US3] Triển khai phương thức `CheckAffectedBookingsAsync` trong `ManagerRoomAppService`:
  - Nhận vào danh sách `RoomIds`, `StartDate`, `EndDate`.
  - Quét kiểm tra giao cắt thời gian $[T_{start}, T_{end}]$ với toàn bộ booking chưa bị hủy hoặc từ chối theo công thức BR-11.
  - Trả về danh sách chi tiết các booking bị ảnh hưởng (Mã booking, Tên khách, SĐT, Khoảng ngày lưu trú, Phòng gán).
  File: `BookingHotel/src/BookingHotel.Application/Manager/Rooms/ManagerRoomAppService.cs`
- [x] T026 [US3] Triển khai phương thức `CreatePlannedClosureAsync` trong `ManagerRoomAppService`:
  - Kiểm tra hợp lệ thời gian `StartDate < EndDate` trong tương lai (BR-12).
  - Tạo các bản ghi `RoomClosure` với `ClosureType = Planned`, `Status = Active`.
  - Đánh dấu các bản ghi `AffectedBookingResolution` với trạng thái chờ xử lý (BR-13) mà không tự ý hủy booking hay hoàn tiền (BR-21).
  File: `BookingHotel/src/BookingHotel.Application/Manager/Rooms/ManagerRoomAppService.cs`
- [x] T027 [US3] Bổ sung REST API endpoints `/api/app/manager/rooms/closures/check-affected-bookings` và `/api/app/manager/rooms/closures/planned` trong `BookingHotel/src/BookingHotel.HttpApi/Controllers/Manager/ManagerRoomController.cs`
- [x] T028 [US3] Xây dựng Modal Lập lịch Planned Closure `PlannedClosureModalComponent`:
  - Form chọn phòng, chọn khoảng thời gian bắt đầu và kết thúc, nhập lý do bảo dưỡng.
  - Tự động gọi API `check-affected-bookings` khi thay đổi ngày để hiển thị danh sách booking bị trùng lặp ngay trên giao diện trước khi người dùng bấm Lưu.
  - Hiển thị cảnh báo xác nhận không tự ý hủy booking khách hàng.
  File: `BookingHotel/angular/src/app/admin/manager/rooms/room-closure-modal/planned-closure-modal.component.ts` (kèm HTML & SCSS)

**Checkpoint**: User Story 3 hoàn tất - Lập lịch Planned Closure và cơ chế quét xung đột booking hoạt động chuẩn xác.

---

## Phase 6: User Story 4 - Ngừng Khai Thác Khẩn Cấp & Điều Phối Giải Quyết Booking Sự Cố (Emergency Closure & Resolution) (Priority: P1)

**Mục tiêu**: Hotel Manager kích hoạt ngừng khai thác khẩn cấp khi xảy ra sự cố đột xuất (hỏng điện nước, sự cố kết cấu, cháy nổ...); phòng lập tức chuyển sang `OutOfService`, hệ thống quét booking bị ảnh hưởng, tự động đẩy Background Job gửi email cảnh báo cho khách hàng và bắn thông báo điều hành cho Tenant Admin & Lễ tân; hỗ trợ màn hình điều phối 1-click Đổi phòng (cùng hạng), Nâng hạng (hạng cao hơn), Hoàn tiền có lưu vết kiểm toán đầy đủ.

**Independent Test**: Kích hoạt Emergency Closure cho phòng 204: bỏ trống lý do -> hệ thống chặn (BR-14); nhập lý do hỏng ống nước -> phòng chuyển sang `OutOfService` ngay lập tức; kiểm tra Background Job được kích hoạt gửi email cho khách; mở modal điều phối booking: chọn Đổi phòng -> danh sách gợi ý 1-click chỉ hiện các phòng trống cùng hạng trong khách sạn; lưu vết điều phối thành công.

### Implementation for User Story 4
- [x] T029 [P] [US4] Triển khai Background Job `EmergencyClosureNotificationJob` kế thừa `AsyncBackgroundJob<EmergencyClosureNotificationArgs>`:
  - Nhận danh sách booking bị ảnh hưởng và thông tin sự cố.
  - Gửi email thông báo tự động cho từng khách hàng qua `IEmailSender` theo mẫu chuyên biệt.
  - Cập nhật trường `IsCustomerNotified = true` trong `AffectedBookingResolution` và lưu log kiểm toán trạng thái gửi (BR-15, BR-20).
  File: `BookingHotel/src/BookingHotel.Application/Manager/Rooms/Jobs/EmergencyClosureNotificationJob.cs`
- [x] T030 [US4] Triển khai phương thức `CreateEmergencyClosureAsync` trong `ManagerRoomAppService`:
  - Ràng buộc bắt buộc nhập lý do `Reason` (BR-14) và mô tả sự cố `IncidentDescription`.
  - Tự động đổi trạng thái vận hành của toàn bộ phòng liên quan sang `OutOfService` (BR-08).
  - Quét trích xuất toàn bộ booking giao cắt $[T_{start}, T_{end}]$ và tạo các bản ghi `AffectedBookingResolution`.
  - Đẩy tác vụ gửi email vào hàng đợi nền `IBackgroundJobManager.EnqueueAsync<EmergencyClosureNotificationArgs>` nhằm đảm bảo phản hồi tức thời cho Hotel Manager.
  - Phát sự kiện cảnh báo hệ thống (Notification) tới Tenant Admin và nhân viên Lễ tân (Receptionist) trực thuộc Hotel (BR-16).
  File: `BookingHotel/src/BookingHotel.Application/Manager/Rooms/ManagerRoomAppService.cs`
- [x] T031 [P] [US4] Triển khai phương thức gợi ý phòng thay thế `GetAlternativeRoomSuggestionsAsync(Guid hotelId, Guid bookingId, AffectedBookingResolutionType resolutionType)`:
  - Lấy thông tin booking (khoảng ngày, hạng phòng gốc `BasePrice`).
  - Lọc ra các phòng vật lý khả dụng trong cùng khách sạn thỏa mãn 6 điều kiện khả dụng (BR-11).
  - Nếu `resolutionType == RoomChange`: Lọc phòng cùng hạng phòng (`RoomTypeId`).
  - Nếu `resolutionType == RoomUpgrade`: Lọc phòng thuộc các hạng phòng có `BasePrice` cao hơn trong cùng khách sạn.
  File: `BookingHotel/src/BookingHotel.Application/Manager/Rooms/ManagerRoomAppService.cs`
- [x] T032 [US4] Triển khai phương thức `ResolveAffectedBookingAsync` trong `ManagerRoomAppService`:
  - Cho phép chọn 1 trong 4 phương án: `RoomChange`, `RoomUpgrade`, `Refund`, `OtherResolution` (BR-17).
  - Cập nhật `AlternativeRoomId`, `ResolvedBy = CurrentUser.Id`, `ResolvedTime = Clock.Now`, `Notes`.
  - Cập nhật gán phòng mới trong `RoomAssignment` nếu chọn Đổi phòng hoặc Nâng hạng.
  File: `BookingHotel/src/BookingHotel.Application/Manager/Rooms/ManagerRoomAppService.cs`
- [x] T033 [US4] Bổ sung REST API endpoints `/api/app/manager/rooms/closures/emergency`, `/api/app/manager/rooms/closures/{id}/alternative-suggestions`, `/api/app/manager/rooms/closures/{closureId}/resolve-booking` trong `BookingHotel/src/BookingHotel.HttpApi/Controllers/Manager/ManagerRoomController.cs`
- [x] T034 [P] [US4] Xây dựng Modal Kích hoạt Emergency Closure `EmergencyClosureModalComponent`:
  - Cảnh báo viền đỏ phong cách khẩn cấp, bắt buộc nhập Lý do sự cố (`Reason` `*`), Mô tả chi tiết (`*`), và Thời gian dự kiến khắc phục.
  - Nút xác nhận kích hoạt kèm xác thực hai bước để tránh bấm nhầm.
  File: `BookingHotel/angular/src/app/admin/manager/rooms/room-closure-modal/emergency-closure-modal.component.ts` (kèm HTML & SCSS)
- [x] T035 [US4] Xây dựng Màn hình / Modal Điều phối giải quyết booking sự cố `AffectedBookingsModalComponent`:
  - Danh sách toàn bộ booking bị ảnh hưởng bởi đợt đóng phòng khẩn cấp kèm thông tin khách hàng, phòng gốc, trạng thái gửi email (`IsCustomerNotified`).
  - Bộ chọn phương án giải quyết (Đổi phòng, Nâng hạng, Hoàn tiền, Dàn xếp khác).
  - Danh sách dropdown gợi ý 1-click các phòng trống thỏa mãn điều kiện khi chọn Đổi phòng hoặc Nâng hạng.
  - Form nhập ghi chú giải quyết và nút lưu kết quả tức thời.
  File: `BookingHotel/angular/src/app/admin/manager/rooms/affected-bookings-modal/affected-bookings-modal.component.ts` (kèm HTML & SCSS)

**Checkpoint**: User Story 4 hoàn tất - Đóng phòng khẩn cấp, gửi thông báo tự động và điều phối giải quyết sự cố 1-click hoạt động trơn tru.

---

## Phase 7: User Story 5 - Nghiệm Thu Khôi Phục Phòng & Lịch Sử Đóng Phòng Kiểm Toán (Priority: P2)

**Mục tiêu**: Hotel Manager nghiệm thu khôi phục phòng về trạng thái `Active` sau khi khắc phục xong sự cố (có kiểm tra chéo nếu còn closure khác thì giữ nguyên không khả dụng - BR-18); tra cứu toàn bộ lịch sử đóng phòng và vết kiểm toán các phương án điều phối booking (BR-19).

**Independent Test**: Khôi phục phòng đang có 1 Planned Closure khác hiệu lực -> phòng vẫn tiếp tục không khả dụng; khôi phục phòng không còn closure nào khác -> phòng trở về `Active`; vào màn hình lịch sử tra cứu -> hiển thị đầy đủ vết ai tạo, ai khôi phục, chi tiết giải quyết booking.

### Implementation for User Story 5
- [x] T036 [P] [US5] Triển khai phương thức `ConfirmRoomRecoveryAsync(Guid closureId, ConfirmRoomRecoveryDto input)` trong `ManagerRoomAppService`:
  - Cập nhật trạng thái bản ghi `RoomClosure` sang `Resolved`, lưu `RecoveryConfirmedBy = CurrentUser.Id` và `RecoveryConfirmedTime = Clock.Now`.
  - Thực hiện kiểm tra chéo (BR-18): Kiểm tra xem phòng có còn bản ghi `RoomClosure` nào khác (`Planned` hoặc `Emergency`) đang có hiệu lực hay không.
  - Nếu không còn Closure nào khác đang hiệu lực, cập nhật phòng về trạng thái `Active`. Nếu vẫn còn Closure khác, giữ nguyên phòng ở trạng thái không khả dụng.
  File: `BookingHotel/src/BookingHotel.Application/Manager/Rooms/ManagerRoomAppService.cs`
- [x] T037 [US5] Triển khai phương thức `GetClosureHistoryAsync(Guid hotelId, RoomClosureType? closureType, DateTime? fromDate, DateTime? toDate)` trong `ManagerRoomAppService`:
  - Truy xuất toàn bộ lịch sử đóng phòng kèm thông tin kiểm toán đầy đủ: loại closure, thời gian bắt đầu/kết thúc, người tạo, người nghiệm thu khôi phục, danh sách booking bị ảnh hưởng và phương án giải quyết đã lưu (BR-19).
  File: `BookingHotel/src/BookingHotel.Application/Manager/Rooms/ManagerRoomAppService.cs`
- [x] T038 [US5] Bổ sung REST API endpoints `/api/app/manager/rooms/closures/{closureId}/confirm-recovery` và `/api/app/manager/rooms/closures/history` trong `BookingHotel/src/BookingHotel.HttpApi/Controllers/Manager/ManagerRoomController.cs`
- [x] T039 [P] [US5] Xây dựng Component Tra cứu Lịch sử Đóng Phòng & Vết Kiểm Toán `RoomClosureHistoryComponent`:
  - Tab hoặc trang tra cứu lịch sử đóng phòng lọc theo khoảng thời gian và loại hình closure (Planned / Emergency).
  - Chi tiết lịch sử hiển thị vết nghiệm thu khôi phục, người xác nhận, nhật ký điều phối booking và trạng thái thông báo khách hàng.
  - Nút thao tác "Xác nhận khôi phục phòng" trực tiếp cho các Closure đang ở trạng thái `Active` đã sửa chữa xong.
  File: `BookingHotel/angular/src/app/admin/manager/rooms/closure-history/room-closure-history.component.ts` (kèm HTML & SCSS)

**Checkpoint**: User Story 5 hoàn tất - Nghiệm thu khôi phục phòng và lịch sử đóng phòng kiểm toán hoạt động đầy đủ.

---

## Phase 8: Polish, Quality Assurance & Verification

**Mục đích**: Hoàn thiện toàn bộ các chi tiết cross-cutting, kiểm tra tuân thủ 22 Quy tắc nghiệp vụ BR-01 đến BR-22 và Hiến pháp dự án, xác minh tính toàn vẹn của mã nguồn qua biên dịch 0 lỗi.

- [x] T040 Rà soát và kiểm tra 100% việc tuân thủ 22 Quy tắc nghiệp vụ (BR-01 đến BR-22) trong các luồng xử lý phòng, closure và booking.
- [x] T041 [P] Rà soát 100% giao diện Angular đảm bảo nguyên tắc Zero Hardcoded Text (Điều khoản V Hiến pháp); toàn bộ nhãn, tiêu đề, modal, thông báo lỗi và toastr đều lấy từ ABP Localization.
- [x] T042 [P] Rà soát 100% các form nhập liệu đảm bảo quy chuẩn Dấu sao đỏ `*` cho các trường bắt buộc (Điều khoản XI Hiến pháp).
- [x] T043 [P] Cập nhật các key bản địa hóa dự phòng trong `BookingHotel/angular/src/app/shared/localization/fallback-localizations.ts`.
- [x] T044 Kiểm tra biên dịch Backend: Thực thi `dotnet build --no-restore -m:1` xác nhận 0 lỗi.
- [x] T045 Kiểm tra biên dịch Frontend: Thực thi `npx tsc -p tsconfig.app.json --noEmit` xác nhận 0 lỗi.
- [x] T046 Kiểm thử nghiệm thu End-to-End các kịch bản chính:
  - Xem Dashboard KPI buồng phòng và lưới phòng gom nhóm theo Tầng.
  - Thêm, sửa, deactivate phòng an toàn (thử deactivate phòng đang có khách -> chặn thành công).
  - Lập lịch Planned Closure: quét trước booking trùng ngày -> hiển thị danh sách cảnh báo.
  - Kích hoạt Emergency Closure: phòng chuyển ngay sang `OutOfService`, hệ thống quét booking, gửi mail cảnh báo chạy nền, bắn thông báo cho Tenant Admin & Lễ tân.
  - Điều phối giải quyết booking: click gợi ý đổi phòng cùng hạng / nâng hạng miễn phí sang hạng cao hơn -> lưu vết kiểm toán thành công.
  - Nghiệm thu khôi phục phòng: xác nhận nghiệm thu -> phòng trở lại `Active`.

---

## Thứ Tự Thực Thi & Các Cơ Hội Triển Khai Song Song (Parallel Execution)

1. **Tuần tự cơ bản**: Phase 1 (Setup) $\rightarrow$ Phase 2 (Foundational) $\rightarrow$ Các User Story (Phase 3 đến Phase 7) $\rightarrow$ Phase 8 (Polish & Verification).
2. **Cơ hội song song trong cùng Phase**:
   - Trong Phase 1: T001, T002, T003, T004, T007 có thể viết song song độc lập.
   - Trong Phase 2: T008, T009, T010, T011, T012, T013 có thể tạo song song độc lập.
   - Trong các User Story: Các task đánh dấu `[P]` (DTO/Job/Angular Component) có thể làm song song với logic Application Service.
3. **Mỗi User Story tuân thủ**: Entity/DTO $\rightarrow$ AppService logic $\rightarrow$ Controller $\rightarrow$ Angular Service $\rightarrow$ Component UI.
