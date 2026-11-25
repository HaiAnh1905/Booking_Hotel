# Phase 0 Research & Architectural Decisions: RM-012 Phân hệ Tác nghiệp Lễ tân

**Feature**: `012-receptionist-frontdesk-portal`  
**Date**: 2026-11-25  
**Status**: Resolved (Zero `NEEDS CLARIFICATION` remaining)

---

## Decision 1: Tách namespace Backend riêng `Frontdesk` thay vì mở rộng `Manager*AppService`

- **Decision**: Tạo nhóm Application Services và API Controllers độc lập cho phân hệ Lễ tân tại `BookingHotel.Application/Frontdesk/`, `BookingHotel.Application.Contracts/Frontdesk/`, và `BookingHotel.HttpApi/Controllers/Frontdesk/`:
  - `FrontdeskDashboardAppService` (`/api/app/frontdesk/dashboard`)
  - `FrontdeskRoomRackAppService` (`/api/app/frontdesk/room-rack`)
  - `FrontdeskReservationAppService` (`/api/app/frontdesk/reservations`)
  - `FrontdeskGuestAppService` (`/api/app/frontdesk/guests`)
  - Kế thừa lớp nền `FrontdeskAppServiceBase` với hàm `CheckFrontdeskHotelAccessAsync(Guid hotelId)`.
- **Rationale**:
  - Đảm bảo nguyên tắc **Least Privilege** và tách biệt hoàn toàn biên giới bảo mật với `Manager*AppService` (vốn chứa quản trị giá phòng, đóng phòng, quản lý đánh giá, báo cáo doanh thu, Reject/No-Show).
  - Mọi endpoint trong `Frontdesk` chỉ yêu cầu đúng các quyền vốn đã cấp cho `Receptionist` (`Bookings.Default`, `Bookings.CheckIn`, `Bookings.CheckOut`, `Bookings.Cancel`, `Rooms.Default`) — không cần nới rộng quyền nguy hiểm như `Rooms.Edit` hay `Guests.Default` (của Manager) hay `Hotels.Default`.
  - Hỗ trợ cả tài khoản `Receptionist` thuần túy, tài khoản kiêm nhiệm `HotelManager + Receptionist`, và `TenantAdmin` (kiểm tra `UserHotelAssignment` cho nhân viên, hoặc quyền TenantAdmin trong cùng Tenant).
- **Alternatives considered**:
  - *Mở lại quyền trên `ManagerBookingAppService` & `ManagerGuestAppService`*: Bị loại vì làm mờ ranh giới phân quyền giữa Manager và Receptionist, tăng rủi ro leo thang đặc quyền (Broken Access Control).

---

## Decision 2: Trích xuất nghiệp vụ cốt lõi vào `FrontdeskStayDomainService` tại tầng Domain

- **Decision**: Xây dựng `FrontdeskStayDomainService` (kế thừa `DomainService`) trong `BookingHotel.Domain/Bookings/FrontdeskStayDomainService.cs` để kiểm soát toàn bộ bất biến nghiệp vụ và quy tắc chuyển trạng thái:
  1. **Chuẩn hóa & Che giấy tờ tùy thân (`MaskIdentityNumber`)**: Kiểm tra định dạng hợp lệ (tối thiểu 6 ký tự chữ/số sau khi bỏ khoảng trắng), che toàn bộ ký tự đầu bằng `*` và chỉ giữ lại 4 ký tự cuối (ví dụ `079201001234` → `********1234`). Nếu chuỗi đầu vào đã ở dạng masked hợp lệ từ trước (kết thúc bằng 4 ký tự chữ/số và tổng độ dài ≥ 6), giữ nguyên để cho phép xác nhận lại mà không làm mất 4 ký tự cuối.
  2. **Kiểm tra chuyển trạng thái Booking (`ValidateCanConfirm`, `ValidateCanCancel`, `ValidateCanCheckIn`, `ValidateCanCheckOut`)**:
     - Confirm: chỉ hợp lệ khi `BookingStatus == PendingConfirmation`.
     - Cancel: chỉ hợp lệ khi `BookingStatus is PendingConfirmation or Confirmed` và `CancellationReason` có độ dài ≥ 5 ký tự.
     - Check-in: chỉ hợp lệ khi `BookingStatus == Confirmed`, có số giấy tờ định danh hợp lệ, và phòng vật lý được gán đang ở trạng thái sẵn sàng (`IsActive == true`, `OperationalStatus == Active`, `OccupancyStatus != Occupied && OccupancyStatus != Blocked`).
     - Check-out: chỉ hợp lệ khi `BookingStatus == CheckedIn`.
  3. **Kiểm tra khả dụng & chống trùng lịch phòng (`ValidateRoomAssignmentConflict`)**: Kiểm tra giao cắt khoảng ngày `[FromDate, ToDate)` với các `RoomAssignment` đang `IsActive == true` của booking khác và các `RoomClosure` ở trạng thái `Pending` hoặc `Active`.
- **Rationale**: Tuân thủ Hiến pháp Nguyên tắc I, V, VI (DDD, Backend-Authoritative, và viết Unit Test độc lập trong `BookingHotel.Domain.Tests` không cần CSDL).
- **Alternatives considered**:
  - *Đặt toàn bộ logic kiểm tra trong AppService*: Khó viết unit test cô lập cho từng quy tắc chuyển trạng thái và dễ rò rỉ logic lặp lại giữa màn hình Reservation và Check-in/Check-out.

---

## Decision 3: Mở rộng tối giản bảng `AppBookings` qua EF Core Code-First Migration

- **Decision**: Bổ sung 6 cột mới (tương thích ngược 100%, không phá vỡ dữ liệu booking hiện có) vào thực thể `Booking`:
  - `RoomTypeId` (`Guid?`): Lưu hạng phòng được đặt ngay cả khi booking chưa được gán phòng vật lý cụ thể.
  - `ActualCheckInTime` (`DateTime?`): Mốc thời gian Lễ tân thực hiện Check-in thực tế.
  - `ActualCheckOutTime` (`DateTime?`): Mốc thời gian Lễ tân thực hiện Check-out thực tế.
  - `IsIdentityVerified` (`bool`, default `false`): Cờ đánh dấu đã đối chiếu và xác minh giấy tờ định danh của khách khi nhận phòng.
  - `CancellationReason` (`string?`, max length `1000`): Lý do hủy booking bắt buộc khi Lễ tân thực hiện hủy.
  - `FrontdeskNotes` (`string?`, max length `2000`): Ghi chú tác nghiệp của quầy Lễ tân (tách biệt với `SpecialRequests` của khách và `ContractNotes` của hợp đồng).
- **Rationale**:
  - Hiện tại `Booking` chưa có `ActualCheckInTime`, `ActualCheckOutTime`, `IsIdentityVerified`, `CancellationReason`, `FrontdeskNotes`, và `RoomTypeId` (hiện `RoomTypeId` chỉ nằm trên `RoomAssignment`, nên booking chưa gán phòng không thể lọc phòng trống đúng hạng phòng!).
  - Thêm các cột này bằng một EF Core Migration sạch (`AddFrontdeskStayTrackingToBookings`) và chạy qua `BookingHotel.DbMigrator` tuân thủ tuyệt đối Hiến pháp Nguyên tắc III.
- **Alternatives considered**:
  - *Dùng `ExtraProperties` JSON của ABP*: Khó truy vấn/index bằng EF Core LINQ cho Dashboard và lọc theo `RoomTypeId` hoặc trạng thái xác minh.

---

## Decision 4: Đồng bộ trạng thái phòng sau Gán phòng, Hủy, Check-in và Check-out (Theo Q1 & Q2)

- **Decision**:
  - **Khi Gán / Đổi phòng**:
    - Vô hiệu hóa bản ghi `RoomAssignment` cũ (`IsActive = false`), tạo `RoomAssignment` mới (`IsActive = true`), đặt `Booking.RoomAssignmentStatus = Assigned`.
    - Nếu phòng mới đang `Vacant` (và không `Blocked`), chuyển `Room.OccupancyStatus = Reserved` (nếu booking đang `Confirmed`) hoặc `Occupied` (nếu đổi phòng trong khi đang `CheckedIn`).
    - Phòng cũ được tính toán lại `OccupancyStatus`: nếu còn booking `CheckedIn` → `Occupied`; nếu còn booking `Confirmed` → `Reserved`; ngược lại → `Vacant`.
  - **Khi Hủy booking (Q1 = Option A)**:
    - Chuyển `BookingStatus = Cancelled`, lưu `CancellationReason`, đóng mọi `RoomAssignment` đang mở và tính lại `Room.OccupancyStatus` cho phòng vừa giải phóng.
  - **Khi Check-in**:
    - Nếu đầu vào có `RoomId` mới khác phòng hiện gán (hoặc booking chưa gán phòng), thực hiện kiểm tra xung đột & gán phòng nguyên tử trong cùng Unit of Work.
    - Cập nhật `GuestFullName`, `GuestPhone`, `GuestEmail`, `GuestIdentityNumberMasked = MaskIdentityNumber(...)`, `IsIdentityVerified = true`, `BookingStatus = CheckedIn`, `ActualCheckInTime = Clock.Now`, `Room.OccupancyStatus = Occupied`.
  - **Khi Check-out (Q2 = Option A)**:
    - Chuyển `BookingStatus = Completed`, `ActualCheckOutTime = Clock.Now`, đóng `RoomAssignment` hiện hành (`IsActive = false`).
    - Đặt `Room.HousekeepingStatus = HousekeepingStatus.Clean` (sẵn sàng đón khách ngay, không kẹt trạng thái Chờ dọn vì Housekeeping nằm ngoài phạm vi RM-012) và tính lại `Room.OccupancyStatus` (`Reserved` nếu đã có booking `Confirmed` tiếp theo được gán vào phòng này, ngược lại `Vacant`).
- **Rationale**: Đáp ứng 100% đồng bộ trạng thái (SC-004) và đúng lựa chọn Q1 (A) + Q2 (A).
- **Alternatives considered**:
  - *Đổi `HousekeepingStatus = Dirty` sau Check-out*: Đã loại theo quyết định Q2 = Option A.

---

## Decision 5: Kiến trúc Frontend Angular cho 5 phân khu Lễ tân (Concept `#99FF99` / `#808080`)

- **Decision**:
  - Tổ chức 5 trang độc lập (standalone components, `ChangeDetectionStrategy.OnPush`, Angular Signals) dưới `angular/src/app/admin/frontdesk/`:
    1. `dashboard/` → `FrontdeskDashboardComponent` (`/frontdesk/dashboard`) — trang đích mặc định mới của phân hệ Lễ tân.
    2. `room-rack/` → `RoomRackComponent` (`/frontdesk/room-rack`) — nâng cấp loại bỏ nút Housekeeping `quickClean`, hiển thị trạng thái sử dụng & sẵn sàng chỉ đọc kèm ngăn xem nhanh chi tiết phòng/booking và lối tắt đi Check-in / Check-out / Xem chi tiết booking.
    3. `reservations/` → `FrontdeskReservationsComponent` (`/frontdesk/reservations`) — danh sách phân trang, bộ lọc đa tiêu chí (sử dụng `<bh-searchable-select>`), ngăn/modal chi tiết booking (giá chỉ đọc), modal Xác nhận / Gán-Đổi phòng / Cập nhật thông tin vận hành / Hủy booking có lý do bắt buộc.
    4. `guests/` → `FrontdeskGuestsComponent` (`/frontdesk/guests`) — danh sách khách hàng phân trang, tìm kiếm, ngăn chi tiết hồ sơ + lịch sử booking tại cơ sở, modal cập nhật thông tin khách (tự động che số CCCD/Hộ chiếu ở máy chủ).
    5. `check-in-out/` → `FrontdeskCheckInOutComponent` (`/frontdesk/check-in-out`, kèm route alias `/frontdesk/check-in` và `/frontdesk/check-out`) — màn hình 2 tab tác nghiệp nhanh (Nhận phòng / Trả phòng) với modal xác minh thông tin khách (đánh dấu `*` đỏ chuẩn Hiến pháp XI), bộ chọn phòng sẵn sàng trực tiếp khi chưa gán phòng, cảnh báo nhận/trả sớm/muộn.
  - Cập nhật `ReceptionistLayoutComponent` sidebar chỉ hiển thị đúng 5 mục menu chuẩn RM-012 (gỡ bỏ `Folios` khỏi thanh menu vì nằm ngoài phạm vi RM-012).
  - Cập nhật `role-based-redirect.guard.ts`: tài khoản `Receptionist` đăng nhập tự động điều hướng về `/frontdesk/dashboard`.
- **Rationale**: Đáp ứng chính xác `FR-001` đến `FR-024`, giữ giao diện gọn nhẹ, tốc độ cao đúng tinh thần *Frontdesk Speed Operation* và không lộ bất kỳ chức năng ngoài phạm vi nào.
- **Alternatives considered**:
  - *Giữ lại menu Folios và nút Quick Clean*: Vi phạm tuyên bố Out-of-Scope của RM-012 và Q2.
