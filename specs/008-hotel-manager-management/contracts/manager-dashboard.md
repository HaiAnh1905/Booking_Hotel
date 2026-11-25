# API Contract: Bảng Điều Khiển Vận Hành Cơ Sở (Manager Dashboard)

**Namespace Backend**: `BookingHotel.ManagerDashboard`  
**Base Route**: `/api/app/manager/dashboard`  
**Yêu cầu quyền**: `[Authorize(BookingHotelPermissions.Hotels.Default)]` + Xác thực `CheckHotelManagerAccessAsync(hotelId)`

---

## 1. Lấy Số Liệu Tổng Quan Thời Gian Thực (Realtime KPIs)

- **HTTP Method**: `GET`
- **Endpoint**: `/api/app/manager/dashboard/kpis`
- **Query Parameters**:
  - `hotelId: Guid` (Bắt buộc) — Mã định danh cơ sở đang quản lý

### Response (200 OK):
```json
{
  "hotelId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "hotelName": "Khách sạn The Passion Lux Airport",
  "hotelCode": "PLA-HCM-1",
  "totalRooms": 50,
  "availableRooms": 12,
  "occupiedRooms": 30,
  "reservedRooms": 4,
  "maintenanceRooms": 2,
  "outOfServiceRooms": 2,
  "availableInventory": 48,
  "occupancyRate": 62.5,
  "todayArrivalsCount": 8,
  "todayDeparturesCount": 6,
  "actionItemsCount": 3,
  "todayRoomRevenue": 45000000.0,
  "todayCollectedRevenue": 52000000.0,
  "weekRoomRevenue": 280000000.0,
  "monthRoomRevenue": 1150000000.0
}
```

---

## 2. Lấy Danh Sách Khách Sắp Đến Trong Ngày (Today Arrivals)

- **HTTP Method**: `GET`
- **Endpoint**: `/api/app/manager/dashboard/today-arrivals`
- **Query Parameters**:
  - `hotelId: Guid` (Bắt buộc)
  - `maxResultCount: int` (Mặc định 10)

### Response (200 OK):
```json
{
  "totalCount": 8,
  "items": [
    {
      "bookingId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
      "bookingCode": "BK-20261119-001",
      "guestFullName": "Trần Văn An",
      "guestPhone": "0912345678",
      "roomTypeName": "Deluxe King",
      "assignedRoomNumber": "304",
      "paymentStatus": 2,
      "estimatedArrivalTime": "14:00",
      "specialRequests": "Phòng tầng cao, không hút thuốc"
    }
  ]
}
```

---

## 3. Lấy Các Hạng Mục Cần Xử Lý Gấp (Action Items)

- **HTTP Method**: `GET`
- **Endpoint**: `/api/app/manager/dashboard/action-items`
- **Query Parameters**: `hotelId: Guid`

### Response (200 OK):
```json
{
  "pendingConfirmationBookings": 2,
  "unassignedArrivalsToday": 1,
  "overdueHousekeepingRooms": 3,
  "openComplaintsNearSla": 1
}
```