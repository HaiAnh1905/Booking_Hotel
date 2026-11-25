# API Contract: Quản Lý Khách Hàng Cơ Sở & Ghi Chú Có Audit (Guests & Notes)

**Namespace Backend**: `BookingHotel.Guests`  
**Base Route**: `/api/app/manager/guests`  
**Yêu cầu quyền**: `[Authorize(BookingHotelPermissions.Guests.Default)]` + Xác thực `CheckHotelManagerAccessAsync(hotelId)`

---

## 1. Danh Sách Khách Hàng Cơ Sở (Đã từng lưu trú tại khách sạn)

- **HTTP Method**: `GET`
- **Endpoint**: `/api/app/manager/guests`
- **Query Parameters**:
  - `hotelId: Guid` (Bắt buộc)
  - `keyword: string?` (Tên hoặc số điện thoại)
  - `skipCount: int`
  - `maxResultCount: int`

### Response (200 OK):
```json
{
  "totalCount": 45,
  "items": [
    {
      "guestId": "gst-1111-2222-3333-4444",
      "fullName": "Nguyễn Hoàng Nam",
      "phoneNumber": "0909123456",
      "email": "nam.nguyen@example.com",
      "identityNumberMasked": "***-***-5678",
      "totalStaysAtThisHotel": 4,
      "totalSpentAtThisHotel": 15400000.0,
      "lastStayDate": "2026-10-15T00:00:00Z",
      "hasInternalNotes": true
    }
  ]
}
```

---

## 2. Chi Tiết Lịch Sử Lưu Trú Tại Cơ Sở

- **HTTP Method**: `GET`
- **Endpoint**: `/api/app/manager/guests/{guestId}/history?hotelId={hotelId}`

### Response (200 OK):
```json
{
  "guestId": "gst-1111-2222-3333-4444",
  "fullName": "Nguyễn Hoàng Nam",
  "stays": [
    {
      "bookingId": "bk-9999-8888-7777-6666",
      "bookingCode": "BK-20261012-003",
      "roomTypeName": "Deluxe King",
      "roomNumber": "402",
      "checkInDate": "2026-10-12T14:00:00Z",
      "checkOutDate": "2026-10-15T12:00:00Z",
      "totalAmount": 5400000.0,
      "bookingStatus": 4
    }
  ]
}
```

---

## 3. Quản Lý Ghi Chú Nội Bộ (Internal Guest Notes)

### 3.1. Lấy danh sách ghi chú nội bộ
- **HTTP Method**: `GET`
- **Endpoint**: `/api/app/manager/guests/{guestId}/notes?hotelId={hotelId}`

### Response (200 OK):
```json
{
  "items": [
    {
      "id": "nt-1234-5678-90ab-cdef",
      "category": "VIP",
      "noteText": "Khách VIP quen của Tổng Giám đốc; thích phòng yên tĩnh góc hành lang, dị ứng hải sản.",
      "isAlert": true,
      "creatorName": "Nguyễn Thị B (Hotel Manager)",
      "creationTime": "2026-08-10T10:15:00Z",
      "lastModifierName": null,
      "lastModificationTime": null
    }
  ]
}
```

### 3.2. Thêm mới ghi chú nội bộ
- **HTTP Method**: `POST`
- **Endpoint**: `/api/app/manager/guests/{guestId}/notes`
- **Yêu cầu quyền**: `[Authorize(BookingHotelPermissions.Guests.ManageNotes)]`

### Request Body:
```json
{
  "hotelId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "category": "Dietary",
  "noteText": "Khách ăn chay trường, yêu cầu chuẩn bị đồ ăn sáng chay riêng.",
  "isAlert": false
}
```