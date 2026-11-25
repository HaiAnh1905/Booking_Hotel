# API Contract: Quản Lý Cơ Sở Khách Sạn (Hotel Property Management)

**Feature**: `007-tenant-admin-management` | **Date**: 2026-11-18  
**Base Route**: `/api/app/hotel`

---

## 1. Endpoints

### 1.1. Lấy danh sách khách sạn chuỗi (Paged List)
- **Method**: `GET`
- **Route**: `/api/app/hotel`
- **Authorization**: `[Authorize(BookingHotelPermissions.Hotels.Default)]`
- **Query Parameters**:
  - `Filter` (string, optional): Từ khóa tìm kiếm (tên, mã, địa chỉ, thành phố).
  - `Status` (int?, optional): `1` = Operating, `2` = ScheduledSuspended, `3` = EmergencySuspended.
  - `SkipCount` (int, default: 0).
  - `MaxResultCount` (int, default: 10).
- **Response `200 OK`**:
  ```json
  {
    "totalCount": 2,
    "items": [
      {
        "id": "e3b0c442-98fc-1c14-9af0-2a3b4c5d6e7f",
        "name": "Booking Hotel Hà Nội Grand",
        "code": "BH-HAN-01",
        "address": "123 Đường Tràng Tiền",
        "city": "Hà Nội",
        "phone": "02439998888",
        "email": "hanoi@bookinghotel.vn",
        "starRating": 5,
        "roomCount": 120,
        "managerName": "Nguyễn Văn Quản Lý",
        "status": 1,
        "standardCheckInTime": "14:00:00",
        "standardCheckOutTime": "12:00:00",
        "bookingCutoffDays": 14,
        "creationTime": "2026-11-18T08:00:00Z"
      }
    ]
  }
  ```

---

### 1.2. Thêm mới cơ sở khách sạn
- **Method**: `POST`
- **Route**: `/api/app/hotel`
- **Authorization**: `[Authorize(BookingHotelPermissions.Hotels.Create)]`
- **Request Body (`CreateUpdateHotelDto`)**:
  ```json
  {
    "name": "Booking Hotel Đà Nẵng Beach Resort",
    "code": "BH-DAD-01",
    "address": "456 Đường Võ Nguyên Giáp",
    "city": "Đà Nẵng",
    "phone": "02363888999",
    "email": "danang@bookinghotel.vn",
    "starRating": 5,
    "roomCount": 80,
    "managerName": "",
    "status": 1,
    "standardCheckInTime": "15:00:00",
    "standardCheckOutTime": "11:30:00",
    "bookingCutoffDays": 14
  }
  ```
- **Validation**:
  - `name`: `[Required]`, max 256 ký tự.
  - `code`: `[Required]`, regex `^[A-Z0-9\-_]{3,20}$`, duy nhất trong Tenant. Nếu trùng trả về `400 Bad Request` kèm message `HotelCodeAlreadyExists`.
  - `phone`: `[Required]`, định dạng số điện thoại hợp lệ.
  - `email`: `[Required]`, `[EmailAddress]`.
  - `starRating`: `[Range(1, 5)]`.
- **Response `200 OK`**: Trả về `HotelDto` của cơ sở mới tạo.

---

### 1.3. Cập nhật thông tin khách sạn
- **Method**: `PUT`
- **Route**: `/api/app/hotel/{id}`
- **Authorization**: `[Authorize(BookingHotelPermissions.Hotels.Edit)]`
- **Request Body (`CreateUpdateHotelDto`)**: Các trường thông tin cơ sở cần cập nhật.
- **Response `200 OK`**: Trả về `HotelDto` đã cập nhật.

---

### 1.4. Tạm ngừng hoạt động cơ sở (Scheduled vs Emergency)
- **Method**: `POST`
- **Route**: `/api/app/hotel/{id}/suspend`
- **Authorization**: `[Authorize(BookingHotelPermissions.Hotels.ManageBranches)]`
- **Request Body (`SuspendHotelInputDto`)**:
  ```json
  {
    "suspensionType": 1,
    "startDate": "2026-12-25T00:00:00Z",
    "endDate": "2027-01-10T00:00:00Z",
    "reason": "Bảo dưỡng định kỳ hệ thống phòng cháy chữa cháy và vệ sinh toàn bộ điều hòa buồng phòng."
  }
  ```
- **Validation Rules**:
  - Nếu `suspensionType == 1` (Scheduled): `startDate` phải cách thời điểm hiện tại $\ge 30$ ngày. Nếu nhỏ hơn 30 ngày, trả về `400 Bad Request` (`ScheduledSuspensionRequires30DaysAdvance`).
  - Nếu `suspensionType == 2` (Emergency): Bắt buộc nhập `reason` dài $\ge 20$ ký tự. Hệ thống kích hoạt event bus thông báo khẩn cấp đến khách hàng và nhân sự chuỗi.
- **Response `200 OK`**:
  ```json
  {
    "hotelId": "e3b0c442-98fc-1c14-9af0-2a3b4c5d6e7f",
    "status": 2,
    "affectedBookingsCount": 15,
    "message": "Hotel suspension scheduled successfully."
  }
  ```

---

### 1.5. Kích hoạt lại cơ sở khách sạn
- **Method**: `POST`
- **Route**: `/api/app/hotel/{id}/activate`
- **Authorization**: `[Authorize(BookingHotelPermissions.Hotels.ManageBranches)]`
- **Response `200 OK`**: Trạng thái chuyển về `Operating = 1`, khách sạn tiếp nhận đặt phòng bình thường.

---

### 1.6. Phân công Quản lý khách sạn (Assign Hotel Manager)
- **Method**: `POST`
- **Route**: `/api/app/hotel/{id}/assign-manager`
- **Authorization**: `[Authorize(BookingHotelPermissions.Hotels.AssignStaff)]`
- **Request Body (`AssignHotelManagerDto`)**:
  ```json
  {
    "managerUserId": "a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d"
  }
  ```
- **Validation**:
  - `managerUserId` bắt buộc phải là nhân viên thuộc Tenant và có vai trò `HotelManager`.
- **Response `200 OK`**: Cập nhật `UserHotelAssignment` và hiển thị `ManagerName` trên cơ sở.
