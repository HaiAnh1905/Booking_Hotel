# API Contracts: RM-012 Phân hệ Tác nghiệp Lễ tân

**Base Route Prefix**: `/api/app/frontdesk`  
**Authentication**: JWT Bearer (`[Authorize]`)  
**Multi-Tenancy & Property Scoping**: Mọi endpoint đều bắt buộc truyền `hotelId` và kiểm tra `CheckFrontdeskHotelAccessAsync(hotelId)` qua bảng `UserHotelAssignment` (hoặc `TenantAdmin` trong cùng Tenant).  
**Error Standard**: ABP `BusinessException` có bản địa hóa song ngữ `vi.json` / `en.json`.

---

## 1. Frontdesk Dashboard API (`/api/app/frontdesk/dashboard`)

**Controller**: `FrontdeskDashboardController` → `IFrontdeskDashboardAppService`  
**Permission**: `BookingHotelPermissions.Bookings.Default`

### `GET /api/app/frontdesk/dashboard/overview?hotelId={guid}`
Trả về tổng quan ca trực trong ngày cho cơ sở được phân công.

**Response `FrontdeskDashboardDto`**:
```json
{
  "hotelId": "guid",
  "hotelName": "string",
  "hotelCode": "string",
  "businessDate": "2026-11-25",
  "totalActiveBookings": 14,
  "pendingConfirmationCount": 3,
  "expectedArrivalsTodayCount": 5,
  "inHouseGuestsCount": 6,
  "expectedDeparturesTodayCount": 2,
  "todayArrivals": [ /* FrontdeskQuickBookingItemDto[] */ ],
  "todayDepartures": [ /* FrontdeskQuickBookingItemDto[] */ ],
  "pendingBookings": [ /* FrontdeskQuickBookingItemDto[] */ ]
}
```

**`FrontdeskQuickBookingItemDto`**:
- `bookingId: Guid`, `bookingCode: string`, `guestFullName: string`, `guestPhone: string`, `roomTypeName: string`, `assignedRoomId: Guid?`, `assignedRoomNumber: string?`, `checkInDate: DateTime`, `checkOutDate: DateTime`, `bookingStatus: BookingStatus`, `roomAssignmentStatus: RoomAssignmentStatus`, `isIdentityVerified: bool`.

---

## 2. Frontdesk Room Rack API (`/api/app/frontdesk/room-rack`)

**Controller**: `FrontdeskRoomRackController` → `IFrontdeskRoomRackAppService`  
**Permission**: `BookingHotelPermissions.Rooms.Default`  
**Scope Note**: 100% **Read-Only** (không có endpoint sửa phòng, đóng phòng hay cập nhật Housekeeping).

### `GET /api/app/frontdesk/room-rack`
**Query Parameters**:
- `hotelId: Guid` (required)
- `floor?: int`
- `roomTypeId?: Guid`
- `rackStatus?: FrontdeskRoomRackStatus` (`Ready=1`, `Reserved=2`, `Occupied=3`, `NotReady=4`, `Unavailable=5`)
- `search?: string`

**Response `FrontdeskRoomRackDto`**:
```json
{
  "hotelId": "guid",
  "totalRooms": 20,
  "readyCount": 10,
  "reservedCount": 3,
  "occupiedCount": 5,
  "notReadyCount": 1,
  "unavailableCount": 1,
  "floors": [1, 2, 3],
  "rooms": [
    {
      "roomId": "guid",
      "roomNumber": "101",
      "floor": 1,
      "building": "A",
      "roomTypeId": "guid",
      "roomTypeName": "Deluxe King",
      "rackStatus": 3,
      "occupancyStatus": 3,
      "operationalStatus": 1,
      "isReadyForCheckIn": false,
      "unavailableReason": null,
      "currentBooking": {
        "bookingId": "guid",
        "bookingCode": "BH-20261125-001",
        "guestFullName": "Nguyễn Văn A",
        "guestPhone": "0901234567",
        "checkInDate": "2026-11-25T14:00:00",
        "checkOutDate": "2026-11-27T12:00:00",
        "bookingStatus": 3,
        "isIdentityVerified": true
      }
    }
  ]
}
```

---

## 3. Frontdesk Reservation & Stay API (`/api/app/frontdesk/reservations`)

**Controller**: `FrontdeskReservationController` → `IFrontdeskReservationAppService`  
**Class Permission**: `BookingHotelPermissions.Bookings.Default`

### 3.1 `GET /api/app/frontdesk/reservations`
Danh sách booking phân trang (chỉ đọc các booking thuộc `hotelId`).

**Query `GetFrontdeskReservationsInputDto`**:
- `hotelId: Guid` (required)
- `keyword?: string` (mã booking, tên, SĐT, email)
- `bookingStatus?: BookingStatus`
- `roomAssignmentStatus?: RoomAssignmentStatus`
- `fromDate?: DateTime`
- `toDate?: DateTime`
- `arrivalTodayOnly?: bool`
- `departureTodayOnly?: bool`
- `skipCount: int = 0`, `maxResultCount: int = 20`

**Response**: `PagedResultDto<FrontdeskReservationListItemDto>`.

### 3.2 `GET /api/app/frontdesk/reservations/{id}?hotelId={guid}`
Chi tiết đầy đủ một booking (bao gồm thông tin tài chính ở chế độ chỉ đọc).

**Response**: `FrontdeskReservationDetailDto` (gồm tất cả trường cơ bản + `roomTypeId`, `roomTypeName`, `assignedRoomId`, `assignedRoomNumber`, `assignedRoomFloor`, `assignedRoomIsReady`, `actualCheckInTime`, `actualCheckOutTime`, `isIdentityVerified`, `guestIdentityNumberMasked`, `specialRequests`, `frontdeskNotes`, `cancellationReason`, `totalAmount`, `depositAmount`, `paymentStatus`, `creationTime`, `lastModificationTime`).

### 3.3 `GET /api/app/frontdesk/reservations/{id}/available-rooms?hotelId={guid}`
Liệt kê danh sách phòng vật lý trống, sẵn sàng, thuộc đúng hạng phòng của booking và không trùng lịch `RoomAssignment` hay `RoomClosure` trong khoảng `[CheckInDate, CheckOutDate)`. *(Fallback tương thích booking cũ: ưu tiên `Booking.RoomTypeId` → `RoomAssignment.RoomTypeId` gần nhất; nếu cả hai đều `null`, trả về tất cả phòng sẵn sàng không trùng lịch của cơ sở và tự đồng bộ `Booking.RoomTypeId` khi gán phòng.)*

**Response**: `List<FrontdeskAvailableRoomDto>` (`roomId`, `roomNumber`, `floor`, `building`, `roomTypeId`, `roomTypeName`).

### 3.4 `POST /api/app/frontdesk/reservations/{id}/confirm`
**Permission**: `BookingHotelPermissions.Bookings.Default` (kiểm tra trạng thái `PendingConfirmation` + quỹ phòng hạng phòng còn khả dụng).  
**Body**: `{ "hotelId": "guid" }`  
**Response**: `FrontdeskReservationDetailDto`.

### 3.5 `POST /api/app/frontdesk/reservations/{id}/assign-room`
Gán phòng mới hoặc đổi phòng trước/trong khi lưu trú (kiểm tra trùng lịch nguyên tử).  
**Permission**: `BookingHotelPermissions.Bookings.Default`  
**Body `AssignFrontdeskRoomDto`**:
```json
{
  "hotelId": "guid",
  "roomId": "guid"
}
```
**Response**: `FrontdeskReservationDetailDto`.

### 3.6 `PUT /api/app/frontdesk/reservations/{id}`
Cập nhật thông tin vận hành cho phép của booking (không cho phép sửa giá, gói giá, tổng tiền, trạng thái thanh toán).  
**Permission**: `BookingHotelPermissions.Bookings.Default`  
**Body `UpdateFrontdeskReservationDto`**:
```json
{
  "hotelId": "guid",
  "guestFullName": "string (required, 2..128)",
  "guestPhone": "string (required, 8..20)",
  "guestEmail": "string? (email, <=128)",
  "specialRequests": "string? (<=2000)",
  "frontdeskNotes": "string? (<=2000)"
}
```
**Response**: `FrontdeskReservationDetailDto`.

### 3.7 `POST /api/app/frontdesk/reservations/{id}/cancel`
Hủy booking ở trạng thái `PendingConfirmation` hoặc `Confirmed` kèm lý do bắt buộc, tự động giải phóng phòng đã gán.  
**Permission**: `BookingHotelPermissions.Bookings.Cancel`  
**Body `CancelFrontdeskReservationDto`**:
```json
{
  "hotelId": "guid",
  "reason": "string (required, 5..1000)"
}
```
**Response**: `FrontdeskReservationDetailDto`.

### 3.8 `POST /api/app/frontdesk/reservations/{id}/check-in`
Xác minh thông tin khách (bắt buộc số CCCD/Hộ chiếu → tự động masked giữ 4 ký tự cuối), gán phòng nếu chưa gán/đổi phòng sẵn sàng, chuyển `BookingStatus -> CheckedIn`, lưu `ActualCheckInTime`, và chuyển `Room.OccupancyStatus -> Occupied`.  
**Permission**: `BookingHotelPermissions.Bookings.CheckIn`  
**Body `FrontdeskCheckInDto`**:
```json
{
  "hotelId": "guid",
  "roomId": "guid (required)",
  "guestFullName": "string (required, 2..128)",
  "guestPhone": "string (required, 8..20)",
  "guestEmail": "string? (email, <=128)",
  "identityNumber": "string (required, 6..32)",
  "frontdeskNotes": "string? (<=2000)"
}
```
**Response**: `FrontdeskReservationDetailDto`.

### 3.9 `POST /api/app/frontdesk/reservations/{id}/check-out`
Hoàn tất trả phòng: chuyển `BookingStatus -> Completed`, lưu `ActualCheckOutTime`, đóng `RoomAssignment`, chuyển `Room.OccupancyStatus -> Vacant` (hoặc `Reserved` nếu đã có đơn tiếp theo) và `Room.HousekeepingStatus -> Clean` (sẵn sàng ngay theo Q2).  
**Permission**: `BookingHotelPermissions.Bookings.CheckOut`  
**Body `FrontdeskCheckOutDto`**:
```json
{
  "hotelId": "guid",
  "frontdeskNotes": "string? (<=2000)"
}
```
**Response**: `FrontdeskReservationDetailDto`.

---

## 4. Frontdesk Guest Management API (`/api/app/frontdesk/guests`)

**Controller**: `FrontdeskGuestController` → `IFrontdeskGuestAppService`  
**Permission**: `BookingHotelPermissions.Bookings.Default` (phạm vi khách gắn với booking tại cơ sở của Lễ tân).  
**Quy ước `guestKey` an toàn URL**:
- `id:{guid}` khi khách có `GuestId` hợp lệ (không rỗng).
- `phone:{digitsOnly}` (chỉ giữ chữ số, loại bỏ `+`, khoảng trắng, dấu gạch) khi khách chưa có `GuestId` (fallback `booking:{bookingId}` nếu thiếu SĐT).
- Truyền qua query param `guestKey` ở `GET /detail` và `PUT /detail` để tránh mọi lỗi encoding trong path segment; phản hồi trả về `guestKey` mới nhất sau khi cập nhật SĐT.

### 4.1 `GET /api/app/frontdesk/guests`
**Query**: `hotelId: Guid` (required), `keyword?: string`, `skipCount: int = 0`, `maxResultCount: int = 20`.  
**Response**: `PagedResultDto<FrontdeskGuestListItemDto>` (`guestKey`, `guestId?`, `fullName`, `phoneNumber`, `email`, `identityNumberMasked`, `isIdentityVerified`, `totalBookingsCount`, `activeBookingsCount`, `lastCheckInDate`).

### 4.2 `GET /api/app/frontdesk/guests/detail?hotelId={guid}&guestKey={string}`
Hồ sơ khách hàng kèm danh sách lịch sử booking tại cơ sở (`bookings: FrontdeskGuestBookingItemDto[]`).

### 4.3 `PUT /api/app/frontdesk/guests/detail?guestKey={string}`
Cập nhật thông tin khách hàng và đồng bộ sang các booking đang hoạt động (`PendingConfirmation`, `Confirmed`, `CheckedIn`) của khách tại cơ sở. Trả về `FrontdeskGuestDetailDto` kèm `guestKey` mới nhất (nếu SĐT thay đổi).  
**Body `UpdateFrontdeskGuestDto`**:
```json
{
  "hotelId": "guid",
  "fullName": "string (required, 2..128)",
  "phoneNumber": "string (required, 8..20)",
  "email": "string? (email, <=128)",
  "identityNumber": "string? (6..32; nếu nhập mới máy chủ tự động masked giữ 4 ký tự cuối)"
}
```
**Response**: `FrontdeskGuestDetailDto`.

---

## 5. Mã lỗi nghiệp vụ chuẩn (Error Codes)

| Error Code | Điều kiện kích hoạt |
| --- | --- |
| `BookingHotel:CrossHotelOperationForbidden` | `hotelId` không nằm trong danh sách `UserHotelAssignment` hoạt động của Lễ tân |
| `BookingHotel:FrontdeskInvalidBookingTransition` | Thao tác không hợp lệ với trạng thái hiện tại (ví dụ Check-in booking chưa `Confirmed`, Check-out booking chưa `CheckedIn`, Hủy booking đã `CheckedIn`/`Completed`) |
| `BookingHotel:FrontdeskRoomUnavailable` | Phòng được chọn đang ngưng hoạt động, bảo trì, bị khóa (`Blocked`) hoặc đang có khách |
| `BookingHotel:FrontdeskRoomTypeMismatch` | Phòng vật lý được chọn không thuộc hạng phòng đã đặt của booking |
| `BookingHotel:FrontdeskRoomOverlapConflict` | Phòng đã bị gán cho booking khác hoặc nằm trong lịch đóng phòng (`RoomClosure`) giao cắt khoảng ngày lưu trú |
| `BookingHotel:FrontdeskNoAvailableInventory` | Không còn quỹ phòng khả dụng cho hạng phòng này trong khoảng ngày để xác nhận booking |
| `BookingHotel:FrontdeskIdentityRequired` | Thiếu hoặc số giấy tờ tùy thân không hợp lệ (< 6 ký tự) khi Check-in |
| `BookingHotel:FrontdeskCancelReasonRequired` | Thiếu hoặc lý do hủy quá ngắn (< 5 ký tự) |
