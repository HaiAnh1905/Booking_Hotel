# API Contract: Quản Lý & Điều Phối Đặt Phòng (Booking Operations)

**Namespace Backend**: `BookingHotel.Bookings`  
**Base Route**: `/api/app/manager/bookings`  
**Yêu cầu quyền**: `[Authorize(BookingHotelPermissions.Bookings.Default)]` + Xác thực `CheckHotelManagerAccessAsync(hotelId)`

---

## 1. Tìm Kiếm & Tra Cứu Danh Sách Đặt Phòng

- **HTTP Method**: `GET`
- **Endpoint**: `/api/app/manager/bookings`
- **Query Parameters**:
  - `hotelId: Guid` (Bắt buộc)
  - `bookingCode: string?`
  - `guestKeyword: string?` (Họ tên hoặc số điện thoại)
  - `bookingStatus: BookingStatus?`
  - `paymentStatus: PaymentStatus?`
  - `fromDate: DateTime?`
  - `toDate: DateTime?`
  - `skipCount: int` (Mặc định 0)
  - `maxResultCount: int` (Mặc định 20)

### Response (200 OK):
```json
{
  "totalCount": 128,
  "items": [
    {
      "id": "bk-1111-2222-3333-4444",
      "bookingCode": "BK-20261119-001",
      "guestFullName": "Nguyễn Văn An",
      "guestPhone": "0987654321",
      "guestEmail": "an.nguyen@example.com",
      "roomTypeName": "Deluxe King",
      "assignedRoomNumber": "304",
      "checkInDate": "2026-11-20T14:00:00Z",
      "checkOutDate": "2026-11-22T12:00:00Z",
      "totalAmount": 3600000.0,
      "depositAmount": 1800000.0,
      "bookingStatus": 2,
      "paymentStatus": 2,
      "roomAssignmentStatus": 2,
      "isDirectBooking": false,
      "creationTime": "2026-11-19T08:30:00Z"
    }
  ]
}
```

---

## 2. Tạo Đặt Phòng Trực Tiếp (Direct / Corporate Booking)

- **HTTP Method**: `POST`
- **Endpoint**: `/api/app/manager/bookings/direct-booking`
- **Yêu cầu quyền**: `[Authorize(BookingHotelPermissions.Bookings.Create)]`

### Request Body:
```json
{
  "hotelId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "guestFullName": "Lê Văn Thịnh",
  "guestPhone": "0912987654",
  "guestEmail": "thinh.le@corporatetech.vn",
  "guestIdentityNumber": "079095012345",
  "roomTypeId": "rt-1111-2222-3333-444455556666",
  "checkInDate": "2026-11-25T14:00:00Z",
  "checkOutDate": "2026-11-28T12:00:00Z",
  "numberOfRooms": 3,
  "agreedRatePerNight": 1600000.0,
  "depositAmountReceived": 2000000.0,
  "contractNotes": "Hợp đồng khách đoàn công ty TechCorp - quyết toán hóa đơn GTGT sau khi trả phòng",
  "specialRequests": "Xếp các phòng cùng tầng 4"
}
```

### Response (201 Created):
```json
{
  "bookingId": "bk-5555-6666-7777-8888",
  "bookingCode": "BK-20261119-DIR-002",
  "bookingStatus": 2,
  "paymentStatus": 2,
  "totalAmount": 14400000.0,
  "depositAmount": 2000000.0,
  "isDirectBooking": true
}
```

---

## 3. Xác Nhận Hoặc Từ Chối Đặt Phòng

### 3.1. Xác nhận booking
- **HTTP Method**: `POST`
- **Endpoint**: `/api/app/manager/bookings/{id}/confirm`
- **Request Body**:
```json
{
  "hotelId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

### 3.2. Từ chối booking
- **HTTP Method**: `POST`
- **Endpoint**: `/api/app/manager/bookings/{id}/reject`
- **Request Body**:
```json
{
  "hotelId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "reason": "Hết phòng phù hợp yêu cầu do sự cố kỹ thuật đột xuất"
}
```

---

## 4. Đánh Dấu Khách Không Đến (No-Show Settlement)

- **HTTP Method**: `POST`
- **Endpoint**: `/api/app/manager/bookings/{id}/mark-no-show`
- **Yêu cầu quyền**: `[Authorize(BookingHotelPermissions.Bookings.MarkNoShow)]`

### Request Body:
```json
{
  "hotelId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "waivePenalty": true,
  "penaltyAmountOverride": 0.0,
  "waivePenaltyReason": "Khách bị hủy chuyến bay do bão cấp 10 (Mã chuyến bay VN240), đã liên hệ lễ tân báo trước"
}
```

### Response (200 OK):
```json
{
  "bookingId": "bk-1111-2222-3333-4444",
  "bookingStatus": 7,
  "paymentStatus": 4,
  "penaltyAmountApplied": 0.0,
  "refundAmountInitiated": 1800000.0,
  "releasedRoomNumbers": ["304"],
  "auditLogId": "al-9999-8888-7777-6666"
}
```