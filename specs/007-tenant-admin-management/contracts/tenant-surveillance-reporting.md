# API Contract: Giám Sát Đặt Phòng & Báo Cáo Chuỗi (Surveillance & Consolidated Reports)

**Feature**: `007-tenant-admin-management` | **Date**: 2026-11-18  
**Base Routes**: `/api/app/tenant-dashboard`, `/api/app/tenant-bookings`, `/api/app/tenant-reports`

---

## 1. Bảng Điều Khiển Tổng Quan (Executive Dashboard)

### 1.1. Lấy chỉ số tổng quan chuỗi & Xếp hạng RevPAR
- **Method**: `GET`
- **Route**: `/api/app/tenant-dashboard/overview`
- **Authorization**: `[Authorize(BookingHotelPermissions.Reports.Consolidated)]`
- **Query Parameters**:
  - `StartDate` (DateTime?, optional, default: đầu tháng hiện tại).
  - `EndDate` (DateTime?, optional, default: hôm nay).
- **Response `200 OK`**:
  ```json
  {
    "chainName": "Booking Hotel Chain Vietnam",
    "totalHotelCount": 5,
    "operatingBranchCount": 4,
    "suspendedBranchCount": 1,
    "totalStaffCount": 45,
    "totalBookingCount": 1280,
    "monthlyCashRevenue": 3450000000.00,
    "averageOccupancyPercent": 76.5,
    "branches": [
      {
        "hotelId": "e3b0c442-98fc-1c14-9af0-2a3b4c5d6e7f",
        "name": "Booking Hotel Hà Nội Grand",
        "code": "BH-HAN-01",
        "rooms": 120,
        "occupancyPercent": 82.4,
        "monthlyRevenue": 1450000000.00,
        "adr": 1500000.00,
        "revPar": 1236000.00,
        "rank": 1
      },
      {
        "hotelId": "f4c1d553-09ad-2d25-0be1-3b4c5d6e7f8a",
        "name": "Booking Hotel Đà Nẵng Beach",
        "code": "BH-DAD-01",
        "rooms": 80,
        "occupancyPercent": 78.1,
        "monthlyRevenue": 980000000.00,
        "adr": 1400000.00,
        "revPar": 1093400.00,
        "rank": 2
      }
    ]
  }
  ```

---

## 2. Giám Sát Đặt Phòng Hợp Nhất (Consolidated Booking Surveillance - Read-Only)

### 2.1. Tra cứu danh sách đặt phòng toàn chuỗi
- **Method**: `GET`
- **Route**: `/api/app/tenant-bookings/surveillance`
- **Authorization**: `[Authorize(BookingHotelPermissions.Bookings.Default)]`
- **Query Parameters**:
  - `HotelId` (Guid?, optional): Lọc theo khách sạn.
  - `Status` (string?, optional): `New`, `Confirmed`, `CheckedIn`, `Completed`, `Cancelled`.
  - `PaymentStatus` (string?, optional): `Unpaid`, `PartiallyPaid`, `FullyPaid`, `Refunded`.
  - `FromDate`, `ToDate` (DateTime?, optional): Khoảng thời gian lưu trú.
  - `Filter` (string?, optional): Mã booking, tên khách, SĐT.
  - `SkipCount`, `MaxResultCount` (int).
- **Response `200 OK`**:
  ```json
  {
    "totalCount": 120,
    "items": [
      {
        "bookingId": "c8d7e6f5-a4b3-2c1d-0e9f-8a7b6c5d4e3f",
        "bookingCode": "BK-20261118-009",
        "hotelName": "Booking Hotel Hà Nội Grand",
        "hotelCode": "BH-HAN-01",
        "customerName": "Lê Hoàng Long",
        "customerPhone": "0901234567",
        "checkInDate": "2026-11-20T14:00:00Z",
        "checkOutDate": "2026-11-23T12:00:00Z",
        "roomTypeName": "Deluxe King Sea View",
        "status": "Confirmed",
        "paymentStatus": "FullyPaid",
        "totalAmount": 4500000.00,
        "creationTime": "2026-11-18T08:30:00Z"
      }
    ]
  }
  ```

---

## 3. Hệ Thống Báo Cáo Hợp Nhất (Tenant Reports)

### 3.1. Báo cáo Doanh thu chuỗi (Cash / Settlement Basis)
- **Method**: `GET`
- **Route**: `/api/app/tenant-reports/revenue`
- **Authorization**: `[Authorize(BookingHotelPermissions.Reports.Consolidated)]`
- **Query Parameters**: `StartDate`, `EndDate`, `GroupBy` (`Day`, `Month`, `Quarter`), `HotelId`.
- **Response `200 OK`**: Trả về tổng tiền phòng thực thu, doanh thu phụ phí, số tiền hoàn hủy phòng và doanh thu thuần.

### 3.2. Báo cáo Hiệu suất cơ sở (RevPAR Ranked Performance)
- **Method**: `GET`
- **Route**: `/api/app/tenant-reports/hotel-performance`
- **Authorization**: `[Authorize(BookingHotelPermissions.Reports.Consolidated)]`
- **Query Parameters**: `StartDate`, `EndDate`.
- **Response `200 OK`**: Trả về danh sách cơ sở xếp hạng theo RevPAR, tỷ lệ lấp đầy, ADR và tăng trưởng so với kỳ trước.

### 3.3. Xuất báo cáo (Export Excel / CSV)
- **Method**: `GET`
- **Route**: `/api/app/tenant-reports/export`
- **Authorization**: `[Authorize(BookingHotelPermissions.Reports.Consolidated)]`
- **Query Parameters**: `ReportType` (`Revenue`, `Occupancy`, `Performance`, `Cancellation`), `Format` (`Excel`, `Csv`).
- **Response `200 OK`**: Tệp tin nhị phân tải về (`application/vnd.openxmlformats-officedocument.spreadsheetml.sheet` hoặc `text/csv`).
