# API Contract: Phê Duyệt Ngoại Lệ Tác Nghiệp (Operational Exception Approvals)

**Feature Branch**: `006-role-based-management`  
**Date**: 2026-11-13  
**Status**: Completed  
**Spec**: [specs/006-role-based-management/spec.md](../spec.md)

---

## 1. Tổng Quan API Phê Duyệt Ngoại Lệ

Cung cấp quy trình làm việc bất đồng bộ cho phép Lễ tân gửi yêu cầu ngoại lệ tác nghiệp (chiết khấu tiền phòng vượt chuẩn, miễn phạt hủy phòng, nâng hạng phòng) và Hotel Manager xem xét, phê duyệt hoặc từ chối trực tiếp từ bảng điều hành cơ sở.

- **Base Path**: `/api/app/operational-approval`
- **Authentication**: Bearer JWT Token
- **Multi-Tenancy**: Header `__tenant` (hoặc claim `tenantid` từ JWT)

---

## 2. Danh Sách Endpoint

| Method | Endpoint | Quyền yêu cầu (Permission) | Mô tả |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/app/operational-approval` | `BookingHotel.Bookings.Default` | Tra cứu danh sách phiếu yêu cầu phê duyệt có phân trang và bộ lọc. |
| `GET` | `/api/app/operational-approval/{id}` | `BookingHotel.Bookings.Default` | Lấy thông tin chi tiết một phiếu yêu cầu phê duyệt. |
| `POST` | `/api/app/operational-approval` | `BookingHotel.Bookings.Default` | Lễ tân tạo mới một phiếu yêu cầu phê duyệt ngoại lệ tác nghiệp. |
| `POST` | `/api/app/operational-approval/{id}/approve` | `BookingHotel.Bookings.ApproveException` | Hotel Manager chấp thuận phiếu yêu cầu ngoại lệ. |
| `POST` | `/api/app/operational-approval/{id}/reject` | `BookingHotel.Bookings.ApproveException` | Hotel Manager từ chối phiếu yêu cầu ngoại lệ (kèm lý do). |
| `POST` | `/api/app/operational-approval/{id}/cancel` | `BookingHotel.Bookings.Default` | Lễ tân tự hủy phiếu yêu cầu do mình tạo (khi còn `Pending`). |

---

## 3. Data Transfer Objects (DTOs)

### 3.1. Enums (`BookingHotel.Domain.Shared/Approvals`)

```csharp
public enum ApprovalTicketType
{
    DiscountOverride = 1,      // Chiết khấu tiền phòng vượt khung quy định
    PenaltyWaiver = 2,         // Miễn trừ phí phạt hủy cọc / hủy phòng
    EarlyCheckIn = 3,          // Miễn phí nhận phòng sớm
    LateCheckOut = 4,          // Miễn phí trả phòng muộn
    RoomUpgrade = 5            // Nâng hạng phòng miễn phí
}

public enum ApprovalTicketStatus
{
    Pending = 0,               // Đang chờ Hotel Manager phê duyệt
    Approved = 1,              // Đã được chấp thuận
    Rejected = 2,              // Bị từ chối
    Cancelled = 3              // Lễ tân đã tự hủy phiếu
}
```

---

### 3.2. Tra Cứu Danh Sách Phiếu (`GetApprovalTicketListDto`)

#### Request Query Parameters:
```text
GET /api/app/operational-approval?hotelId=4d5e...&status=0&ticketType=1&skipCount=0&maxResultCount=10
```

| Tham số | Kiểu | Bắt buộc | Mô tả |
| :--- | :--- | :---: | :--- |
| `hotelId` | `Guid?` | Không | Lọc theo ID cơ sở khách sạn (Hotel Manager có thể lọc theo chi nhánh). |
| `status` | `ApprovalTicketStatus?` | Không | Lọc theo trạng thái phiếu (`Pending`, `Approved`, `Rejected`, `Cancelled`). |
| `ticketType` | `ApprovalTicketType?` | Không | Lọc theo loại ngoại lệ. |
| `skipCount` | `int` | Không | Vị trí bắt đầu phân trang (Mặc định: 0). |
| `maxResultCount` | `int` | Không | Số lượng bản ghi trên một trang (Mặc định: 10, Tối đa: 100). |

#### Response (200 OK):
```json
{
  "totalCount": 1,
  "items": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "tenantId": "c0a80123-0000-0000-0000-000000000001",
      "hotelId": "4d5e8241-1111-2222-3333-444455556666",
      "hotelName": "Booking Hotel Riverside Saigon",
      "ticketType": 1,
      "ticketTypeName": "DiscountOverride",
      "status": 0,
      "statusName": "Pending",
      "requestedByStaffId": "e1f2a3b4-5555-6666-7777-888899990000",
      "requestedByStaffName": "Nguyen Van Le Tan",
      "bookingId": "8f7e6d5c-4444-3333-2222-11110000aaaa",
      "folioId": "1a2b3c4d-9999-8888-7777-666655554444",
      "roomNumber": "P.302",
      "amount": 250000.0,
      "discountPercent": 15.0,
      "reason": "Khách VIP kỷ niệm ngày cưới đề nghị chiết khấu phòng 15%",
      "reviewedByManagerId": null,
      "reviewedByManagerName": null,
      "reviewedAt": null,
      "rejectionReason": null,
      "creationTime": "2026-11-13T10:15:30Z"
    }
  ]
}
```

---

### 3.3. Tạo Mới Phiếu Ngoại Lệ (`CreateApprovalTicketDto`)

#### Request Body:
```json
{
  "hotelId": "4d5e8241-1111-2222-3333-444455556666",
  "ticketType": 1,
  "bookingId": "8f7e6d5c-4444-3333-2222-11110000aaaa",
  "folioId": "1a2b3c4d-9999-8888-7777-666655554444",
  "roomNumber": "P.302",
  "amount": 250000.0,
  "discountPercent": 15.0,
  "reason": "Khách VIP kỷ niệm ngày cưới đề nghị chiết khấu phòng 15%"
}
```

#### Validation Rules:
- `hotelId`: Required, không được rỗng `Guid.Empty`.
- `ticketType`: Required, thuộc các giá trị hợp lệ trong `ApprovalTicketType`.
- `reason`: Required, độ dài từ 5 đến 1000 ký tự.
- `discountPercent`: Nếu có giá trị, phải nằm trong khoảng $0.01 \le x \le 100.00$.
- `amount`: Nếu có giá trị, phải $\ge 0$.

#### Response (201 Created):
Trả về đối tượng `OperationalApprovalTicketDto` với `Status = Pending`.

---

### 3.4. Phê Duyệt Phiếu Ngoại Lệ (Approve)

#### Request:
```text
POST /api/app/operational-approval/{id}/approve
```

#### Validation & Authorization:
- Người dùng bắt buộc có quyền `BookingHotelPermissions.Bookings.ApproveException`.
- Khách sạn của phiếu (`ticket.HotelId`) phải thuộc danh sách các khách sạn mà người dùng hiện tại được phân công phụ trách. Nếu không, trả về lỗi `403 Forbidden` (`BookingHotel:CrossHotelOperationForbidden`).
- Phiếu phải đang ở trạng thái `Pending`. Nếu không, trả về lỗi `400 Bad Request` (`BookingHotel:TicketNotInPendingState`).

#### Response (200 OK):
Trả về đối tượng `OperationalApprovalTicketDto` với `Status = Approved`, `ReviewedAt = Clock.Now`.

---

### 3.5. Từ Chối Phiếu Ngoại Lệ (Reject)

#### Request Body:
```json
{
  "rejectionReason": "Mức chiết khấu 15% vượt hạn mức tối đa cho phép của chi nhánh trong mùa cao điểm."
}
```

#### Validation & Authorization:
- Người dùng bắt buộc có quyền `BookingHotelPermissions.Bookings.ApproveException`.
- `rejectionReason`: Required, độ dài từ 5 đến 1000 ký tự.
- Phiếu phải đang ở trạng thái `Pending`.

#### Response (200 OK):
Trả về đối tượng `OperationalApprovalTicketDto` với `Status = Rejected`, `RejectionReason = ...`.

---

## 4. Mã Lỗi Nghiệp Vụ (Business Error Codes)

| Mã lỗi (Error Code) | HTTP Status | Thông báo hiển thị (Localization Key) |
| :--- | :---: | :--- |
| `BookingHotel:TicketNotFound` | 404 | `Phiếu yêu cầu phê duyệt không tồn tại trong hệ thống.` |
| `BookingHotel:TicketNotInPendingState` | 400 | `Phiếu này đã được xử lý hoặc đã hủy, không thể thay đổi trạng thái.` |
| `BookingHotel:CrossHotelOperationForbidden` | 403 | `Bạn không có quyền phụ trách khách sạn này để phê duyệt yêu cầu.` |
| `BookingHotel:RejectionReasonRequired` | 400 | `Bắt buộc phải nhập lý do khi từ chối yêu cầu phê duyệt ngoại lệ.` |
| `BookingHotel:OnlyCreatorCanCancelTicket` | 403 | `Chỉ nhân sự tạo phiếu mới có quyền hủy phiếu này.` |

