# API & UI Contracts: RM-011 – Quản Lý Giá Phòng (Room Pricing Management)

**Feature**: `011-room-pricing-management`  
**Base Route**: `/api/app/manager/rooms/pricing` *(Controller: `ManagerRateController`, Service: `IManagerRateAppService`)*  
**Authorization**: `[Authorize(BookingHotelPermissions.Rooms.SetRates)]` + `CheckHotelManagerAccessAsync(hotelId)` + Cross-Hotel / Cross-RoomType / Cross-Tenant validation trên 100% endpoints.

---

## 1. REST Endpoints Summary

| Method | Endpoint | Mô tả | Yêu cầu liên quan |
|---|---|---|---|
| `GET` | `/api/app/manager/rooms/pricing/context` | Lấy ngữ cảnh hạng phòng (Section 1 KPI + danh sách gói giá Section 2 kèm `BasePrice` và `EffectivePrice` ngày hôm nay) | FR-001, FR-002, FR-003 |
| `POST` | `/api/app/manager/rooms/pricing/rate-plans/toggle` | Bật/tắt trạng thái `Active`/`Inactive` của gói giá tại chi nhánh + ghi `PricingAuditLog` | FR-004, FR-017, BR-11 |
| `PUT` | `/api/app/manager/rooms/pricing/rate-plans/base-price` | Chỉnh sửa `Base Price` chi nhánh của cặp `RoomType + RatePlan` (không sửa override cũ) + ghi `PricingAuditLog` | FR-005, FR-017, BR-02, BR-08 |
| `GET` | `/api/app/manager/rooms/pricing/calendar` | Lấy lưới lịch giá theo tháng (`year`, `month`) cho cặp `RoomType + RatePlan` (Section 3) | FR-006, FR-007, BR-03 |
| `PUT` | `/api/app/manager/rooms/pricing/daily-quick-edit` | Chỉnh sửa nhanh giá và/hoặc `CTA`/`CTD` cho 1 ngày cụ thể (`>= Today`) từ modal chi tiết ngày | FR-007, FR-011, BR-05, BR-07 |
| `POST` | `/api/app/manager/rooms/pricing/bulk-preview` | Xem trước điều chỉnh giá hàng loạt (tính toán Backend-Authoritative: giá dự kiến, số ngày ảnh hưởng, danh sách ngày trùng lặp) | FR-009, FR-010, FR-012, FR-013 |
| `POST` | `/api/app/manager/rooms/pricing/bulk-apply` | Áp dụng điều chỉnh giá hàng loạt theo phương án xử lý trùng ngày (`ReplaceExisting` / `SkipExisting`) + ghi `PricingAuditLog` | FR-009–FR-013, FR-017 |
| `GET` | `/api/app/manager/rooms/pricing/overrides` | Danh sách lịch ghi đè (`Section 5`) thuộc cặp `RoomType + RatePlan` | FR-014 |
| `PUT` | `/api/app/manager/rooms/pricing/overrides/{id}` | Chỉnh sửa lịch ghi đè (hỗ trợ kiểm tra xung đột & tách ngày quá khứ nếu vắt ngang `Today`) + ghi `PricingAuditLog` | FR-015, FR-017, BR-05, BR-07 |
| `DELETE` | `/api/app/manager/rooms/pricing/overrides/{id}` | Xóa lịch ghi đè (chặn nếu `EndDate < Today`; tách giữ quá khứ nếu vắt ngang `Today`) + ghi `PricingAuditLog` | FR-016, FR-017, BR-05 |
| `GET` | `/api/app/manager/rooms/pricing/audit-logs` | Tra cứu phân trang `PricingAuditLog` lọc theo `RoomTypeId`, `RatePlanId`, `ActionType`, `FromDate`, `ToDate` | FR-017, US-5 |

> **Tương thích ngược**: Các endpoint cũ `/api/app/manager/rooms/rate-plans` và `/api/app/manager/rooms/rate-plans/toggle` có thể giữ redirect nội bộ hoặc chuyển hoàn toàn Angular `ManagerRateService` sang bộ contract `/api/app/manager/rooms/pricing/*` mới rõ ràng, nhất quán.

---

## 2. Contract Chi Tiết Từng Endpoint & DTO

### 2.1. `GET /context` — Ngữ cảnh Hạng phòng & Danh mục Gói giá (Section 1 & 2)

**Query Parameters**:
- `hotelId` (`Guid`, required)
- `roomTypeId` (`Guid`, required)

**Response (`RoomTypePricingContextDto`)**:
```json
{
  "hotelId": "guid",
  "hotelName": "Booking Hotel Saigon",
  "hotelCode": "BHSG",
  "roomTypeId": "guid",
  "roomTypeName": "Phòng đôi Superior",
  "referenceBasePrice": 400000,
  "ratePlanCount": 3,
  "activeRatePlanCount": 2,
  "activeOverrideCount": 5,
  "ratePlans": [
    {
      "ratePlanId": "guid",
      "code": "STD",
      "name": "Standard Rate",
      "ratePlanType": "Refundable",
      "description": "Giá tiêu chuẩn linh hoạt",
      "isRefundable": true,
      "cancellationCutoffHours": 24,
      "includesBreakfast": false,
      "defaultPriceAdjustmentPercent": 0,
      "basePrice": 400000,
      "effectivePrice": 2000000,
      "isTodayOverridden": true,
      "isActive": true,
      "overrideCount": 2
    }
  ]
}
```
> `effectivePrice` trên từng dòng Section 2 đại diện cho giá hiệu lực của ngày hôm nay (`Today`) tính theo BR-03.

---

### 2.2. `POST /rate-plans/toggle` — Bật/Tắt Gói giá Chi nhánh

**Request (`ToggleRoomRatePlanStatusDto`)**:
- `hotelId` (`Guid`, `[Required]`)
- `roomTypeId` (`Guid`, `[Required]`)
- `ratePlanId` (`Guid`, `[Required]`)
- `isActive` (`bool`, `[Required]`)

**Response**: `RoomRatePlanItemDto` (trạng thái mới sau khi lưu; đồng thời ghi `PricingAuditLog` loại `ToggleRatePlanStatus`).

---

### 2.3. `PUT /rate-plans/base-price` — Chỉnh sửa Giá Cơ Bản Chi Nhánh

**Request (`UpdateRoomRatePlanBasePriceDto`)**:
- `hotelId` (`Guid`, `[Required]`)
- `roomTypeId` (`Guid`, `[Required]`)
- `ratePlanId` (`Guid`, `[Required]`)
- `basePrice` (`decimal`, `[Required]`, `[Range(1, 1000000000)]`)

**Response**: `RoomRatePlanItemDto` (đồng thời ghi `PricingAuditLog` loại `UpdateBasePrice`; không thay đổi bất kỳ `RoomPriceOverride` nào).

---

### 2.4. `GET /calendar` — Lịch Giá Theo Tháng (Section 3)

**Query Parameters**:
- `hotelId` (`Guid`, required)
- `roomTypeId` (`Guid`, required)
- `ratePlanId` (`Guid`, required)
- `year` (`int`, required, `2000..2100`)
- `month` (`int`, required, `1..12`)

**Response (`MonthlyPricingCalendarDto`)**:
```json
{
  "hotelId": "guid",
  "roomTypeId": "guid",
  "ratePlanId": "guid",
  "ratePlanName": "Standard Rate",
  "isRatePlanActive": true,
  "basePrice": 400000,
  "year": 2026,
  "month": 11,
  "days": [
    {
      "applicableDate": "2026-11-25",
      "isPastDate": false,
      "isToday": true,
      "basePrice": 400000,
      "effectivePrice": 2000000,
      "isPriceOverridden": true,
      "hasRestrictionsOverride": true,
      "overrideId": "guid",
      "adjustmentType": 1,
      "adjustmentValue": 2000000,
      "isClosedToArrival": true,
      "isClosedToDeparture": false,
      "isOpenForSale": true
    }
  ]
}
```
> `isOpenForSale` = `isRatePlanActive` (nếu Rate Plan đang `Inactive` thì mọi ngày hiển thị trạng thái đóng bán của gói giá). `isPastDate` = `applicableDate < Today` (Frontend khóa nút Quick-Edit khi `isPastDate || !isRatePlanActive`).

---

### 2.5. `PUT /daily-quick-edit` — Chỉnh Sửa Nhanh Một Ngày Trên Lịch

**Request (`QuickEditDailyPricingDto`)**:
- `hotelId` (`Guid`, `[Required]`)
- `roomTypeId` (`Guid`, `[Required]`)
- `ratePlanId` (`Guid`, `[Required]`)
- `applicableDate` (`DateTime`, `[Required]`, must be `>= Today`)
- `adjustmentType` (`PriceAdjustmentType`, `[Required]`: `FixedPrice` | `Percentage` | `RestrictionsOnly`)
- `adjustmentValue` (`decimal?`, required when `FixedPrice`/`Percentage`)
- `isClosedToArrival` (`bool`)
- `isClosedToDeparture` (`bool`)
- `clearOverride` (`bool`, default `false` — cho phép khôi phục ngày đó về `Base Price` & bỏ CTA/CTD)

**Behavior**:
- Kiểm tra `applicableDate >= Today` (BR-05) và Rate Plan `IsActive` (BR-11).
- Thực thi tách/cập nhật chính xác cho duy nhất 1 ngày `applicableDate` (tự động giữ nguyên các ngày khác nếu ngày đó đang thuộc 1 khoảng nhiều ngày) và ghi `PricingAuditLog`.

**Response**: `DailyPricingCellDto` (ô ngày sau khi cập nhật).

---

### 2.6. `POST /bulk-preview` & `POST /bulk-apply` — Điều Chỉnh Hàng Loạt (Section 4)

**Request (`BulkPriceAdjustmentPreviewInputDto`)**:
```json
{
  "hotelId": "guid",
  "roomTypeId": "guid",
  "ratePlanIds": ["guid-1", "guid-2"],
  "startDate": "2026-12-05",
  "endDate": "2026-12-15",
  "daysOfWeek": [5, 6, 0],
  "adjustmentType": 2,
  "adjustmentValue": 20,
  "isClosedToArrival": false,
  "isClosedToDeparture": true
}
```
> `daysOfWeek`: `0 = Sunday .. 6 = Saturday` (chuẩn .NET `DayOfWeek`); nếu `null` hoặc rỗng thì mặc định áp dụng cả 7 ngày trong tuần.

**Preview Response (`BulkPriceAdjustmentPreviewDto`)**:
```json
{
  "hotelName": "Booking Hotel Saigon",
  "roomTypeName": "Phòng đôi Superior",
  "startDate": "2026-12-05",
  "endDate": "2026-12-15",
  "daysOfWeek": [5, 6, 0],
  "adjustmentType": 2,
  "adjustmentValue": 20,
  "isClosedToArrival": false,
  "isClosedToDeparture": true,
  "totalAffectedDaysAcrossPlans": 10,
  "hasAnyConflicts": true,
  "ratePlanPreviews": [
    {
      "ratePlanId": "guid-1",
      "ratePlanName": "Standard Rate",
      "ratePlanCode": "STD",
      "currentBasePrice": 400000,
      "projectedEffectivePrice": 480000,
      "matchingDatesCount": 5,
      "conflictingDates": ["2026-12-11", "2026-12-12"],
      "newDatesCount": 3
    }
  ]
}
```

**Apply Request (`ApplyBulkPriceAdjustmentDto`)**: kế thừa toàn bộ trường của `BulkPriceAdjustmentPreviewInputDto` + thêm:
- `conflictResolution` (`PriceConflictResolution`: `Cancel = 0` | `ReplaceExisting = 1` | `SkipExisting = 2`)

**Apply Response (`BulkPriceAdjustmentResultDto`)**:
- `appliedRatePlanCount` (`int`)
- `affectedDaysCount` (`int`)
- `replacedDaysCount` (`int`)
- `skippedDaysCount` (`int`)
- `createdOverrideIds` (`List<Guid>`)

---

### 2.7. `GET /overrides`, `PUT /overrides/{id}`, `DELETE /overrides/{id}` (Section 5)

**`GET /overrides` Query**: `hotelId`, `roomTypeId`, `ratePlanId`  
**Response**: `List<RoomPriceOverrideDto>`:
```json
[
  {
    "id": "guid",
    "hotelId": "guid",
    "roomTypeId": "guid",
    "ratePlanId": "guid",
    "startDate": "2026-12-05",
    "endDate": "2026-12-15",
    "adjustmentType": 1,
    "adjustmentValue": 2000000,
    "calculatedEffectivePrice": 2000000,
    "isClosedToArrival": true,
    "isClosedToDeparture": false,
    "creationTime": "2026-11-25T02:30:00Z",
    "creatorId": "guid",
    "creatorName": "Nguyen Van Manager",
    "isPastOnly": false,
    "isStraddlingToday": false
  }
]
```

**`PUT /overrides/{id}` Request (`UpdateRoomPriceOverrideDto`)**:
- `hotelId` (`Guid`, `[Required]`)
- `startDate` (`DateTime`, `[Required]`, must be `>= Today`)
- `endDate` (`DateTime`, `[Required]`, `>= startDate`)
- `adjustmentType` (`PriceAdjustmentType`, `[Required]`)
- `adjustmentValue` (`decimal?`)
- `isClosedToArrival` (`bool`)
- `isClosedToDeparture` (`bool`)
- `conflictResolution` (`PriceConflictResolution`, default `Cancel` — nếu phát hiện trùng với lịch khác thì trả danh sách ngày trùng hoặc xử lý theo lựa chọn của người dùng)

**`DELETE /overrides/{id}` Query**: `hotelId` (`Guid`, required)  
- Nếu `EndDate < Today` → 400 `BookingHotel:CannotModifyPastPricing`.
- Nếu `StartDate < Today && EndDate >= Today` → thu hẹp `EndDate = Today - 1`, giữ lịch sử quá khứ, ghi Audit Log cho khoảng `[Today .. oldEndDate]`.
- Nếu `StartDate >= Today` → soft-delete, ghi Audit Log cho khoảng `[StartDate .. EndDate]`.

---

### 2.8. `GET /audit-logs` — Tra Cứu Lịch Sử Thay Đổi Giá

**Query (`GetPricingAuditLogsInputDto` extends `PagedResultRequestDto`)**:
- `hotelId` (`Guid`, `[Required]`)
- `roomTypeId` (`Guid?`, optional)
- `ratePlanId` (`Guid?`, optional)
- `actionType` (`PricingAuditActionType?`, optional)
- `fromDate` (`DateTime?`, optional)
- `toDate` (`DateTime?`, optional)
- `skipCount` (`int`, default `0`)
- `maxResultCount` (`int`, default `20`, max `100`)

**Response**: `PagedResultDto<PricingAuditLogDto>` gồm `totalCount` và `items` (`id`, `roomTypeName`, `ratePlanCode`, `ratePlanName`, `actionType`, `actorName`, `actionTime`, `beforeValueJson`, `afterValueJson`, `affectedStartDate`, `affectedEndDate`, `summaryNote`).

---

## 3. Mã Lỗi Nghiệp Vụ Chuẩn Hóa (Error Codes & HTTP Status)

| Error Code | HTTP | Điều kiện kích hoạt |
|---|---|---|
| `BookingHotel:CrossHotelOperationForbidden` | `403` | `HotelId` không thuộc `UserHotelAssignment` hoặc `RoomType.HotelId != HotelId` (BR-13, BR-14) |
| `BookingHotel:InvalidDateRange` | `400` | `StartDate > EndDate` (BR-05) |
| `BookingHotel:CannotModifyPastPricing` | `400` | `StartDate < Today` khi tạo mới, hoặc sửa/xóa bản ghi có `EndDate < Today` (BR-05) |
| `BookingHotel:InvalidEffectivePrice` | `400` | Giá cơ bản, giá cố định hoặc giá sau tính `%` `<= 0` (BR-02, BR-06) |
| `BookingHotel:RatePlanInactive` | `400` | Tạo điều chỉnh giá / quick-edit trên Rate Plan đang `Inactive` (BR-11) |
| `BookingHotel:NoMatchingDaysInRange` | `400` | Bộ lọc `DaysOfWeek` không khớp ngày nào trong `[StartDate..EndDate]` |
| `BookingHotel:PriceOverrideConflictDetected` | `409` | Phát hiện ngày trùng khi `conflictResolution == Cancel` (kèm payload danh sách ngày trùng, BR-07) |
| `BookingHotel:RatePlanNotFound` | `404` | `RatePlanId` không tồn tại trong Tenant hiện tại |
