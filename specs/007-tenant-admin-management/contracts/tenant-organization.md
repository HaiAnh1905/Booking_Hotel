# API Contract: Hồ Sơ Tổ Chức & Cấu Hình Chuỗi (Tenant Organization Profile & Policies)

**Feature**: `007-tenant-admin-management` | **Date**: 2026-11-18  
**Base Route**: `/api/app/tenant-organization`

---

## 1. Endpoints

### 1.1. Lấy thông tin hồ sơ tổ chức & chính sách chuỗi
- **Method**: `GET`
- **Route**: `/api/app/tenant-organization/profile`
- **Authorization**: `[Authorize(BookingHotelPermissions.Hotels.Default)]`
- **Response `200 OK`**:
  ```json
  {
    "tenantId": "c1a2b3c4-d5e6-7f8a-9b0c-1d2e3f4a5b6c",
    "organizationName": "Booking Hotel Chain Vietnam",
    "logoUrl": "/uploads/tenants/logo_chain_vn.png",
    "headquarterAddress": "Số 1 Đại lộ Thăng Long, Mễ Trì, Nam Từ Liêm, Hà Nội",
    "contactEmail": "contact@bookinghotel.vn",
    "hotline": "1900 6868",
    "websiteUrl": "https://bookinghotel.vn",
    "defaultCheckInTime": "14:00:00",
    "defaultCheckOutTime": "12:00:00",
    "cancellationPolicy": {
      "freeCancellationHours": 48,
      "partialPenaltyHours": 24,
      "partialPenaltyPercent": 50,
      "fullPenaltyHours": 0,
      "fullPenaltyPercent": 100,
      "description": "Miễn phí hủy phòng trước 48 giờ. Hủy trong vòng 24-48 giờ tính phí 50% tiền cọc. Hủy dưới 24 giờ tính phí 100%."
    }
  }
  ```

---

### 1.2. Cập nhật hồ sơ tổ chức & chính sách chuỗi
- **Method**: `PUT`
- **Route**: `/api/app/tenant-organization/profile`
- **Authorization**: `[Authorize(BookingHotelPermissions.Hotels.ManageBranches)]`
- **Request Body (`UpdateTenantOrganizationDto`)**:
  ```json
  {
    "organizationName": "Booking Hotel Hospitality Group",
    "logoUrl": "/uploads/tenants/new_logo.png",
    "headquarterAddress": "Tầng 25, Tòa nhà Keangnam Landmark, Hà Nội",
    "contactEmail": "support@bookinghotel.vn",
    "hotline": "1900 8888",
    "websiteUrl": "https://bookinghotel.vn",
    "defaultCheckInTime": "14:00:00",
    "defaultCheckOutTime": "12:00:00",
    "cancellationPolicy": {
      "freeCancellationHours": 48,
      "partialPenaltyHours": 24,
      "partialPenaltyPercent": 50,
      "fullPenaltyHours": 0,
      "fullPenaltyPercent": 100,
      "description": "Miễn phí hủy phòng trước 48 giờ."
    }
  }
  ```
- **Validation**:
  - `organizationName`: `[Required]`, max 256 ký tự.
  - `contactEmail`: `[Required]`, `[EmailAddress]`.
  - `hotline`: `[Required]`.
  - `defaultCheckInTime`, `defaultCheckOutTime`: Định dạng TimeSpan hợp lệ.
  - `cancellationPolicy`: Cấu trúc JSON tuân thủ schema chính sách chuỗi.
- **Response `200 OK`**: Trả về hồ sơ tổ chức đã cập nhật.
