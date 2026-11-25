# API Contract: Quản Lý Buồng Phòng, Loại Phòng & Bảng Giá (Rooms & Rates)

**Namespace Backend**: `BookingHotel.Rooms`  
**Base Route**: `/api/app/manager/rooms`  
**Yêu cầu quyền**: `[Authorize(BookingHotelPermissions.Rooms.Default)]` + Xác thực `CheckHotelManagerAccessAsync(hotelId)`

---

## 1. Quản Lý Loại Phòng (Room Types)

### 1.1. Lấy danh sách loại phòng của khách sạn
- **HTTP Method**: `GET`
- **Endpoint**: `/api/app/manager/rooms/room-types?hotelId={hotelId}`
- **Response (200 OK)**:
```json
{
  "totalCount": 3,
  "items": [
    {
      "id": "rt-1111-2222-3333-444455556666",
      "hotelId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "name": "Deluxe King",
      "description": "Phòng giường đôi hướng hồ bơi sang trọng",
      "standardOccupancy": 2,
      "maxOccupancy": 3,
      "basePrice": 1800000.0,
      "sizeSquareMeters": 35.5,
      "amenities": ["WiFi", "Bồn tắm", "Ban công", "SmartTV"],
      "totalRoomCount": 20
    }
  ]
}
```

### 1.2. Tạo mới hoặc cập nhật loại phòng
- **HTTP Method**: `POST` / `PUT`
- **Endpoint**: `/api/app/manager/rooms/room-types`
- **Request Body**:
```json
{
  "hotelId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "name": "Executive Suite",
  "description": "Phòng suite cao cấp có phòng khách riêng",
  "standardOccupancy": 2,
  "maxOccupancy": 4,
  "basePrice": 3200000.0,
  "sizeSquareMeters": 65.0,
  "amenities": ["WiFi", "Bồn tắm jacuzzi", "Phòng khách", "Máy pha cà phê"]
}
```

---

## 2. Quản Lý Phòng Thực Tế & Trạng Thái Buồng Phòng

### 2.1. Lấy danh sách phòng kèm 2 chiều trạng thái
- **HTTP Method**: `GET`
- **Endpoint**: `/api/app/manager/rooms/physical-rooms?hotelId={hotelId}&floor={floor}&operationalStatus={status}`
- **Response (200 OK)**:
```json
{
  "totalCount": 50,
  "items": [
    {
      "id": "rm-1234-5678-90ab-cdef12345678",
      "hotelId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "roomTypeId": "rt-1111-2222-3333-444455556666",
      "roomTypeName": "Deluxe King",
      "roomNumber": "304",
      "floor": 3,
      "operationalStatus": 1,
      "housekeepingStatus": 1,
      "isSellable": true,
      "currentOccupantName": null,
      "maintenanceReason": null,
      "maintenanceEndDate": null,
      "outOfServiceReason": null,
      "outOfServiceEndDate": null
    }
  ]
}
```

### 2.2. Chuyển trạng thái Vận hành (Maintenance / OutOfService kèm Guardrails)
- **HTTP Method**: `POST`
- **Endpoint**: `/api/app/manager/rooms/{id}/change-operational-status`
- **Request Body**:
```json
{
  "hotelId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "targetStatus": 5,
  "reason": "Sửa chữa hệ thống điều hòa trung tâm bị rò rỉ",
  "startDate": "2026-11-20T00:00:00Z",
  "expectedEndDate": "2026-11-25T23:59:59Z"
}
```
- **Xác thực Backend (Guardrails)**:
  - Nếu phòng đang `Occupied`: Trả về `400 Bad Request` (`BookingHotel:RoomCurrentlyOccupied`).
  - Nếu phòng đã có `RoomAssignment` trong khoảng `[startDate, expectedEndDate]`: Trả về `400 Bad Request` (`BookingHotel:ConflictingBookingExists`).
  - Ghi nhận `AuditLog` với `UserId` và lý do.

### 2.3. Cập nhật trạng thái vệ sinh buồng phòng
- **HTTP Method**: `POST`
- **Endpoint**: `/api/app/manager/rooms/{id}/update-housekeeping`
- **Request Body**:
```json
{
  "hotelId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "targetStatus": 4
}
```

---

## 3. Quản Lý Gói Giá (RatePlans) & Giá Theo Ngày (Daily Rates)

### 3.1. Lấy danh sách RatePlan Tenant và trạng thái kích hoạt tại cơ sở
- **HTTP Method**: `GET`
- **Endpoint**: `/api/app/manager/rooms/rate-plans?hotelId={hotelId}&roomTypeId={roomTypeId}`
- **Response (200 OK)**:
```json
{
  "items": [
    {
      "ratePlanId": "rp-aaaa-bbbb-cccc-dddd",
      "code": "FLEX",
      "name": "Standard Flexible",
      "description": "Giá tiêu chuẩn cho phép hủy miễn phí trước 24h",
      "isRefundable": true,
      "includesBreakfast": false,
      "isActiveAtHotel": true,
      "customBasePrice": 1800000.0
    },
    {
      "ratePlanId": "rp-eeee-ffff-1111-2222",
      "code": "NONREF",
      "name": "Non-Refundable Promo",
      "description": "Giảm 15% không hoàn tiền",
      "isRefundable": false,
      "includesBreakfast": false,
      "isActiveAtHotel": true,
      "customBasePrice": 1530000.0
    }
  ]
}
```

### 3.2. Bật/Tắt gói giá và cấu hình giá cơ sở
- **HTTP Method**: `POST`
- **Endpoint**: `/api/app/manager/rooms/rate-plans/toggle`
- **Request Body**:
```json
{
  "hotelId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "roomTypeId": "rt-1111-2222-3333-444455556666",
  "ratePlanId": "rp-eeee-ffff-1111-2222",
  "isActive": true,
  "customBasePrice": 1550000.0
}
```

### 3.3. Thiết lập ghi đè giá theo ngày (Daily Rate Override)
- **HTTP Method**: `POST`
- **Endpoint**: `/api/app/manager/rooms/daily-rates/batch-override`
- **Request Body**:
```json
{
  "hotelId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "roomTypeId": "rt-1111-2222-3333-444455556666",
  "fromDate": "2026-12-24",
  "toDate": "2026-12-25",
  "overridePrice": 2400000.0,
  "isClosedToArrival": false,
  "isClosedToDeparture": false
}
```