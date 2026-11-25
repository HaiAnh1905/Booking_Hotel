# Quickstart & Validation Guide: RM-011 – Quản Lý Giá Phòng (Room Pricing Management)

**Feature**: `011-room-pricing-management`  
**Date**: 2026-11-25  
**References**: [spec.md](./spec.md) | [data-model.md](./data-model.md) | [manager-rate-api.md](./contracts/manager-rate-api.md)

---

## 1. Điều Kiện Tiên Quyết (Prerequisites)

- .NET 10 SDK và EF Core CLI (`dotnet ef`) đã cài đặt.
- Node.js 20+ và npm/Angular CLI cho thư mục `BookingHotel/angular`.
- Cơ sở dữ liệu đã áp dụng các migration từ RM-001 đến RM-010 và có sẵn tối thiểu:
  - 1 Tenant có ít nhất 2 cơ sở khách sạn (`Hotel A`, `Hotel B`).
  - 1 tài khoản `HotelManager` được phân công quản lý `Hotel A` (không quản lý `Hotel B`).
  - Ít nhất 2 Hạng phòng (`RoomType`) thuộc `Hotel A` và 2 Gói giá chuỗi (`RatePlan`) tạo từ RM-009.

---

## 2. Lệnh Build, Migration & Kiểm Thử (Commands)

Chạy từ thư mục `BookingHotel/`:

```powershell
# 1. Tạo và áp dụng EF Core Migration sau khi thêm RoomPriceOverride & PricingAuditLog
dotnet ef migrations add AddRoomPricingManagement --project src/BookingHotel.EntityFrameworkCore --startup-project src/BookingHotel.HttpApi.Host
dotnet run --project src/BookingHotel.DbMigrator

# 2. Build toàn bộ Backend Solution
dotnet build BookingHotel.sln

# 3. Chạy Unit Tests Domain (tính giá, làm tròn 1.000 VNĐ, tách/gộp xung đột, bảo toàn ngày quá khứ)
dotnet test test/BookingHotel.Domain.Tests --filter "FullyQualifiedName~RoomPricing"

# 4. Chạy Integration Tests Application (ManagerRateAppService, AuditLog, Cross-Hotel/Tenant isolation)
dotnet test test/BookingHotel.Application.Tests --filter "FullyQualifiedName~ManagerRate"

# 5. Kiểm tra Build & Lint Frontend Angular
Set-Location angular
npm run build
npm run lint
```

---

## 3. Kịch Bản Kiểm Chứng End-to-End (Acceptance Validation Scenarios)

### Scenario A — Chọn Hạng Phòng, Bật/Tắt Gói Giá & Sửa `Base Price` Chi Nhánh (US-1)
1. Đăng nhập bằng tài khoản `HotelManager` và mở màn hình `/manager/rates`.
2. **Kỳ vọng**: Section 1 hiển thị đúng Tên khách sạn, Mã cơ sở, Hạng phòng đầu tiên, Giá tham khảo, Số gói giá và Số lịch ghi đè.
3. Mở modal sửa `Base Price` của `Standard Rate`: xác nhận Tên gói, Mã gói, Chính sách ăn sáng, Chính sách hoàn hủy và Mô tả ở chế độ **chỉ đọc**; đổi `Base Price` từ `400.000 VNĐ` sang `450.000 VNĐ` và lưu.
4. **Kỳ vọng**: Các ngày chưa có override trên lịch tháng đổi ngay sang `450.000 VNĐ`; các ngày đã có override trước đó (cả `Fixed Price` và `Percentage`) **giữ nguyên tuyệt đối** mức giá đã chốt (BR-08).
5. Gạt tắt `Standard Rate` sang `Inactive` qua confirm modal (`BhConfirmService`) → thử tạo Bulk Adjustment cho gói này.
6. **Kỳ vọng**: Hệ thống chặn tạo điều chỉnh mới (BR-11), nhưng vẫn bảo toàn `Base Price` và các lịch ghi đè cũ.

### Scenario B — Lịch Giá Theo Tháng & Chỉnh Sửa Nhanh Theo Ngày (US-2)
1. Bật lại `Standard Rate`, điều hướng qua tháng hiện tại và tháng kế tiếp trên Section 3.
2. Click vào 1 ngày trong quá khứ (`< Today`): modal chi tiết mở ở chế độ **chỉ đọc** (không có nút lưu/sửa).
3. Click vào 1 ngày tương lai (`>= Today`): chuyển chế độ `Restrictions Only`, bật `CTA = true` (giữ nguyên giá) và lưu.
4. **Kỳ vọng**: Ô ngày trên lịch hiển thị ngay huy hiệu `CTA` cùng mức `Base Price` hiện hành mà không cần tải lại trang (SC-004).

### Scenario C — Điều Chỉnh Hàng Loạt, Lọc Thứ, Preview & Xử Lý Trùng Ngày (US-3)
1. Tại Section 4, chọn khoảng `[10/12 – 20/12]`, chế độ `Fixed Price = 2.000.000 VNĐ`, bật `CTA`, nhấn **Xem trước (Preview)** → xác nhận lưu.
2. Tiếp tục tạo đợt mới `[05/12 – 15/12]`, lọc thứ `T6, T7, CN`, chế độ `Percentage = +20%` (trên `Base Price = 450.000 VNĐ` → dự kiến `540.000 VNĐ`, kiểm tra làm tròn bội số `1.000 VNĐ`).
3. **Kỳ vọng**: Hộp thoại Preview liệt kê rõ các ngày trùng lặp trong giao cắt `[10/12 – 15/12]` và yêu cầu chọn `Replace Existing`, `Skip Existing` hoặc `Cancel`:
   - Chọn `Cancel` → không thay đổi DB.
   - Chọn `Skip Existing` → chỉ tạo override cho các ngày khớp T6–CN trong `[05/12 – 09/12]`.
   - Chọn `Replace Existing` → tách chính xác khoảng cũ và thay thế đúng các ngày trùng, không tồn tại 2 bản ghi trùng ngày (SC-003).
4. Thử nhập `Percentage = -100%` hoặc `Start Date < Today` hoặc `Start Date > End Date` → xác nhận bị chặn ở cả Frontend và Backend.

### Scenario D — Quản Lý Danh Sách Ghi Đè & Khoảng Vắt Ngang `Today` (US-4)
1. Tại Section 5, kiểm tra đầy đủ cột (`StartDate`, `EndDate`, `AdjustmentType`, `AdjustmentValue`, `CTA`, `CTD`, `CreationTime`, `CreatorName`, `View/Edit/Delete`).
2. Xóa 1 lịch ghi đè tương lai qua hộp thoại xác nhận → kiểm tra ô ngày trên lịch quay ngay về `Base Price`.
3. Kiểm thử khoảng vắt ngang ngày hôm nay (`StartDate < Today <= EndDate`): khi xóa, xác nhận phần `[StartDate .. Yesterday]` vẫn được bảo toàn nguyên vẹn và chỉ phần `[Today .. EndDate]` quay về `Base Price` (BR-05).

### Scenario E — Audit Log, Bảo Toàn Booking & Cô Lập Bảo Mật (US-5)
1. Nhấn nút **Lịch sử thay đổi giá** trên thanh tiêu đề để mở Modal/Drawer `PricingAuditLog`.
2. Lọc theo Gói giá, Loại thao tác và Khoảng ngày → xác nhận đủ cả 6 nhóm thao tác đã thực hiện ở Scenarios A–D kèm Người thực hiện, Thời điểm, Giá trị trước/sau và Phạm vi ngày (SC-006).
3. Kiểm tra 1 booking đã xác nhận trước đó có ngày lưu trú rơi vào ngày vừa ghi đè giá `2.000.000 VNĐ` → xác nhận `Booking.TotalAmount` giữ nguyên không đổi (SC-005).
4. Đổi sang khách sạn khác trên Property Switcher hoặc gọi API với `HotelId` của khách sạn không được phân công / bằng tài khoản `Receptionist` → xác nhận reset trạng thái UI / trả về `403 Forbidden` ở Backend (SC-007).
5. Chuyển ngôn ngữ `VI ↔ EN` → xác nhận 100% nhãn, nút, badge, modal, lỗi validation và bảng Audit Log dịch đầy đủ, không có text cứng.
