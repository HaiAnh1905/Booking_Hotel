# API Contract: Báo Cáo Hiệu Suất Chuẩn Hóa Chi Nhánh (Reports & Analytics)

**Namespace Backend**: `BookingHotel.TenantReports`  
**Base Route**: `/api/app/manager/reports`  
**Yêu cầu quyền**: `[Authorize(BookingHotelPermissions.Reports.HotelPerformance)]` + Xác thực `CheckHotelManagerAccessAsync(hotelId)`

---

## 1. Báo Cáo Hiệu Suất Kinh Doanh (Occupancy, ADR, RevPAR)

- **HTTP Method**: `GET`
- **Endpoint**: `/api/app/manager/reports/performance`
- **Query Parameters**:
  - `hotelId: Guid` (Bắt buộc)
  - `fromDate: DateTime` (Bắt buộc)
  - `toDate: DateTime` (Bắt buộc)

### Response (200 OK):
```json
{
  "hotelId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "fromDate": "2026-11-01T00:00:00Z",
  "toDate": "2026-11-18T23:59:59Z",
  "totalPhysicalRooms": 50,
  "outOfServiceRoomNights": 36,
  "availableInventoryNights": 864,
  "occupiedRoomNights": 648,
  "occupancyRate": 75.0,
  "totalRoomRevenue": 1166400000.0,
  "averageDailyRate": 1800000.0,
  "revenuePerAvailableRoom": 1350000.0
}
```

---

## 2. Phân Tách Dòng Tiền Minh Bạch (Revenue Breakdown)

- **HTTP Method**: `GET`
- **Endpoint**: `/api/app/manager/reports/revenue-breakdown`
- **Query Parameters**:
  - `hotelId: Guid`
  - `fromDate: DateTime`
  - `toDate: DateTime`

### Response (200 OK):
```json
{
  "roomRevenue": 1166400000.0,
  "serviceRevenue": 154200000.0,
  "totalBilledRevenue": 1320600000.0,
  "collectedRevenue": 1285000000.0,
  "refundAmount": 18500000.0,
  "uncollectedBalance": 35600000.0
}
```

---

## 3. Thống Kê Hủy Phòng & Thất Thoát Công Suất (Cancellations & OutOfService Loss)

- **HTTP Method**: `GET`
- **Endpoint**: `/api/app/manager/reports/cancellations-loss`
- **Query Parameters**:
  - `hotelId: Guid`
  - `fromDate: DateTime`
  - `toDate: DateTime`

### Response (200 OK):
```json
{
  "totalBookings": 240,
  "cancelledCount": 18,
  "cancellationRate": 7.5,
  "noShowCount": 4,
  "noShowRate": 1.67,
  "waivedNoShowPenaltiesCount": 2,
  "totalPenaltiesCollected": 5400000.0,
  "outOfServiceLossNights": 36,
  "estimatedRevenueLoss": 64800000.0
}
```