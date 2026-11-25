# API Contracts: Quản Lý Gói Giá Chuỗi Cho Admin Tenant (RM-009)

## 1. REST Endpoints Overview

| Method | Endpoint | Description | Quyền yêu cầu |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/app/tenant/rate-plans` | Lấy danh sách toàn bộ gói giá của Tenant | Tenant Admin |
| `GET` | `/api/app/tenant/rate-plans/{id}` | Lấy thông tin chi tiết một gói giá | Tenant Admin |
| `POST` | `/api/app/tenant/rate-plans` | Tạo mới một gói giá chuỗi | Tenant Admin |
| `PUT` | `/api/app/tenant/rate-plans/{id}` | Cập nhật gói giá chuỗi | Tenant Admin |
| `DELETE` | `/api/app/tenant/rate-plans/{id}` | Xóa mềm gói giá và ngắt mở bán tại chi nhánh | Tenant Admin |

---

## 2. DTO Schemas

### `CreateUpdateRatePlanDto`
```csharp
public class CreateUpdateRatePlanDto
{
    [Required]
    [StringLength(128)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(32)]
    public string Code { get; set; } = string.Empty;

    [StringLength(512)]
    public string Description { get; set; } = string.Empty;

    public bool IsRefundable { get; set; } = true;

    public bool IncludesBreakfast { get; set; }

    [Range(0, 720)]
    public int CancellationCutoffHours { get; set; } = 24;

    [Range(-99, 500)]
    public decimal DefaultPriceAdjustmentPercent { get; set; }
}
```

### `RatePlanDto`
```csharp
public class RatePlanDto : FullAuditedEntityDto<Guid>
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsRefundable { get; set; }
    public bool IncludesBreakfast { get; set; }
    public int CancellationCutoffHours { get; set; }
    public decimal DefaultPriceAdjustmentPercent { get; set; }
}
```

---

## 3. Sample Requests & Responses

### 3.1. Tạo mới gói giá (`POST /api/app/tenant/rate-plans`)
**Request Body**:
```json
{
  "name": "Gói Nghỉ Dưỡng Gia Đình Kèm Buffet Sáng",
  "code": "FAMILY_BB",
  "description": "Bao gồm buffet sáng cho tối đa 4 khách, miễn phí hủy trước 48h",
  "isRefundable": true,
  "includesBreakfast": true,
  "cancellationCutoffHours": 48,
  "defaultPriceAdjustmentPercent": 15.0
}
```

**Response (200 OK)**:
```json
{
  "id": "7b095f93-0182-4682-89a3-5c7a421b8312",
  "name": "Gói Nghỉ Dưỡng Gia Đình Kèm Buffet Sáng",
  "code": "FAMILY_BB",
  "description": "Bao gồm buffet sáng cho tối đa 4 khách, miễn phí hủy trước 48h",
  "isRefundable": true,
  "includesBreakfast": true,
  "cancellationCutoffHours": 48,
  "defaultPriceAdjustmentPercent": 15.0,
  "creationTime": "2026-11-20T10:35:00Z"
}
```

### 3.2. Lỗi trùng mã gói (`400 Bad Request`)
```json
{
  "error": {
    "code": "BookingHotel:DuplicateRatePlanCode",
    "message": "Mã gói giá 'FAMILY_BB' đã tồn tại trong chuỗi."
  }
}
```
