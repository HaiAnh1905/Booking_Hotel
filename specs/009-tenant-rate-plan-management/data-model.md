# Phase 1 Data Model: Quản Lý Gói Giá Chuỗi (RM-009)

## 1. Entity: `RatePlan` (Bảng `AppRatePlans`)

```mermaid
erDiagram
    RatePlan ||--o{ HotelRatePlan : "được áp dụng tại chi nhánh"
    Hotel ||--o{ HotelRatePlan : "cấu hình mở bán"
    RoomType ||--o{ HotelRatePlan : "gán theo hạng phòng"

    RatePlan {
        Guid Id PK
        Guid TenantId FK "Chuỗi khách sạn sở hữu"
        string Name "Tên hiển thị gói giá (ví dụ: Gói Kèm Buffet Sáng)"
        string Code "Mã định danh duy nhất trong Tenant (ví dụ: BED_AND_BREAKFAST)"
        string Description "Mô tả chi tiết quyền lợi và quy định"
        bool IsRefundable "Cho phép hoàn hủy tiền phòng hay không"
        bool IncludesBreakfast "Bao gồm bữa ăn sáng hay không"
        int CancellationCutoffHours "Thời hạn được hủy miễn phí trước giờ check-in (giờ)"
        decimal DefaultPriceAdjustmentPercent "Tỷ lệ % điều chỉnh giá cơ sở (+10, -10, 0)"
        bool IsDeleted "Trạng thái xóa mềm"
        DateTime CreationTime
        Guid CreatorId
    }

    HotelRatePlan {
        Guid Id PK
        Guid TenantId FK
        Guid HotelId FK
        Guid RoomTypeId FK
        Guid RatePlanId FK
        bool IsActive "Mở bán / Ngừng bán tại cơ sở"
        decimal CustomBasePrice "Giá ghi đè cơ sở (nếu có)"
        decimal PriceMultiplier "Hệ số giá"
    }
```

## 2. Validation & Business Rules
1. **Mã gói (`Code`)**:
   - Bắt buộc (`[Required]`), tối đa 32 ký tự.
   - Định dạng: Chữ in hoa, số, dấu gạch dưới (tự động `.ToUpper().Trim()`).
   - Duy nhất (`Unique`) trong phạm vi Tenant (`CurrentTenant.Id`).
2. **Tên gói (`Name`)**:
   - Bắt buộc (`[Required]`), tối đa 128 ký tự.
3. **Chính sách hoàn hủy (`IsRefundable`, `CancellationCutoffHours`)**:
   - Nếu `IsRefundable == false`, `CancellationCutoffHours` mặc định = 0.
   - Nếu `IsRefundable == true`, `CancellationCutoffHours` phải $\ge 0$.
4. **Tỷ lệ điều chỉnh giá (`DefaultPriceAdjustmentPercent`)**:
   - Giá trị thuộc khoảng: $-99.00 \le x \le 500.00$.
5. **Soft Delete Policy**:
   - Khi xóa, cập nhật `IsDeleted = true`.
   - Cập nhật toàn bộ các bản ghi `HotelRatePlan` tương ứng sang `IsActive = false`.
