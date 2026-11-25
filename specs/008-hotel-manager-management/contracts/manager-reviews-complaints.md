# API Contract: Đánh Giá & Xử Lý Khiếu Nại (Reviews & Complaints)

**Namespace Backend**: `BookingHotel.Reviews`  
**Base Route**: `/api/app/manager/feedback`  
**Yêu cầu quyền**: `[Authorize(BookingHotelPermissions.Reviews.Reply)]` + Xác thực `CheckHotelManagerAccessAsync(hotelId)`

---

## 1. Quản Lý Đánh Giá & Đăng Phản Hồi Chính Thức

### 1.1. Lấy danh sách đánh giá kèm điểm theo 4 tiêu chí
- **HTTP Method**: `GET`
- **Endpoint**: `/api/app/manager/feedback/reviews?hotelId={hotelId}&minScore={minScore}`

### Response (200 OK):
```json
{
  "averageScores": {
    "overall": 4.6,
    "cleanliness": 4.8,
    "staff": 4.7,
    "amenities": 4.5,
    "location": 4.4
  },
  "totalReviews": 82,
  "items": [
    {
      "reviewId": "rv-1111-2222-3333-4444",
      "bookingCode": "BK-20261110-008",
      "guestFullName": "Nguyễn Văn Hùng",
      "overallScore": 4.5,
      "cleanlinessScore": 5,
      "staffScore": 5,
      "amenitiesScore": 4,
      "locationScore": 4,
      "comment": "Khách sạn sạch sẽ, nhân viên lễ tân chu đáo, đồ ăn sáng phong phú.",
      "creationTime": "2026-11-12T09:30:00Z",
      "officialReply": "Cảm ơn quý khách đã tin tưởng lựa chọn The Passion Lux Airport. Rất hân hạnh được phục vụ quý khách trong kỳ nghỉ tiếp theo!",
      "repliedByName": "Nguyễn Thị B (Hotel Manager)",
      "repliedAt": "2026-11-12T14:20:00Z"
    }
  ]
}
```

### 1.2. Đăng phản hồi chính thức của khách sạn
- **HTTP Method**: `POST`
- **Endpoint**: `/api/app/manager/feedback/reviews/{reviewId}/reply`
- **Request Body**:
```json
{
  "hotelId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "replyContent": "Chân thành cảm ơn góp ý của quý khách về cách âm phòng. Chúng tôi đã tiến hành kiểm tra lại hệ thống gioăng cửa để nâng cao trải nghiệm lưu trú."
}
```

---

## 2. Quản Lý Khiếu Nại & Luồng SLA 48h Kèm Leo Thang

### 2.1. Lấy danh sách khiếu nại tại cơ sở
- **HTTP Method**: `GET`
- **Endpoint**: `/api/app/manager/feedback/complaints?hotelId={hotelId}&status={status}`

### Response (200 OK):
```json
{
  "totalCount": 5,
  "items": [
    {
      "id": "cp-1234-5678-90ab-cdef",
      "title": "Nước nóng phòng tắm không ổn định vào buổi tối",
      "guestFullName": "Vũ Đình Trọng",
      "guestPhone": "0934567890",
      "bookingCode": "BK-20261115-012",
      "priority": "High",
      "status": 2,
      "statusText": "InInvestigation",
      "creationTime": "2026-11-18T10:00:00Z",
      "slaDeadline": "2026-11-20T10:00:00Z",
      "hoursRemaining": 20.5,
      "isOverdue": false,
      "escalatedAt": null
    }
  ]
}
```

### 2.2. Cập nhật trạng thái xử lý khiếu nại
- **HTTP Method**: `POST`
- **Endpoint**: `/api/app/manager/feedback/complaints/{id}/update-status`
- **Request Body**:
```json
{
  "hotelId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "targetStatus": 3,
  "resolutionNotes": "Kỹ thuật đã thay thế van nhiệt tự động mới; quản lý đã gặp trực tiếp tặng voucher giảm giá 20% cho lần ở kế tiếp, khách hàng hài lòng đóng khiếu nại."
}
```