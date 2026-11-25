# Phase 0 Research: Quản Lý Gói Giá Chuỗi Cho Admin Tenant (RM-009)

## 1. Nghiên Cứu Mô Hình Dữ Liệu Hiện Có
- **Thực thể `RatePlan`** (`BookingHotel.Domain.Rooms.RatePlan`):
  - Kế thừa: `FullAuditedAggregateRoot<Guid>, IMultiTenant`.
  - Các trường hiện có:
    - `Guid? TenantId`: Định danh Tenant / Chuỗi (ABP tự động áp dụng QueryFilter).
    - `string Name`: Tên hiển thị của gói giá.
    - `string Code`: Mã định danh gói (ví dụ: `STANDARD`, `BED_AND_BREAKFAST`).
    - `string Description`: Mô tả quyền lợi và chính sách của gói.
    - `bool IsRefundable`: Có được phép hoàn hủy không.
    - `bool IncludesBreakfast`: Có bao gồm ăn sáng không.
    - `int CancellationCutoffHours`: Thời hạn được hủy miễn phí trước giờ nhận phòng.
    - `decimal DefaultPriceAdjustmentPercent`: Tỷ lệ % điều chỉnh giá cơ bản so với giá hạng phòng.
  - Tình trạng CSDL: Bảng `AppRatePlans` đã được tạo qua migration `20261119081755_AddHotelManagerOperationsEntities` trong DbContext.
  - Kết luận: **Không cần tạo thêm Entity hay Migration CSDL mới**.

## 2. Nghiên Cứu Cơ Chế Đồng Bộ Xuống Khách Sạn Chi Nhánh
- **Thực thể `HotelRatePlan`** (`BookingHotel.Domain.Rooms.HotelRatePlan`):
  - Khóa ngoại: `RatePlanId`, `HotelId`, `RoomTypeId`.
  - Khi Hotel Manager mở trang `/manager/rates`:
    - `ManagerRateAppService.GetRatePlansAsync` thực hiện:
      ```csharp
      var masterPlans = await _ratePlanRepository.GetListAsync();
      var hotelPlans = await _hotelRatePlanRepository.GetListAsync(hp => hp.HotelId == hotelId && hp.RoomTypeId == roomTypeId);
      ```
    - Bất kỳ `RatePlan` nào thuộc `CurrentTenant.Id` đều tự động được load ra.
    - Khi Admin Chuỗi thêm mới một `RatePlan`, API này lập tức trả về gói mới cho Hotel Manager với trạng thái mặc định `IsActive = true`.
- **Cơ chế khi Xóa mềm gói giá**:
  - `RatePlan` kế thừa `FullAuditedAggregateRoot` nên phương thức `_ratePlanRepository.DeleteAsync(ratePlan)` sẽ kích hoạt Soft Delete (`IsDeleted = true`).
  - Trong `TenantRatePlanAppService.DeleteAsync`, cần bổ sung logic cập nhật toàn bộ bản ghi `HotelRatePlan` liên kết sang `IsActive = false` để đảm bảo các chi nhánh không tiếp tục mở bán gói đã bị xóa.

## 3. Nghiên Cứu Giao Diện Admin Tenant (Frontend)
- Phân hệ Admin Tenant hiện có các trang tại `angular/src/app/admin/tenant/`:
  - `tenant-dashboard`: Bảng điều khiển
  - `hotels`: Quản lý danh sách cơ sở
  - `staff`: Quản lý nhân sự & phân quyền
  - `bookings`: Giám sát đặt phòng toàn chuỗi
  - `reports`: Báo cáo hiệu suất chuỗi
  - `settings`: Cấu hình tổ chức
- Sidebar Menu nằm trong `tenant-admin-layout.component.html`:
  - Cần thêm 1 mục `Quản lý Gói Giá` với link `/tenant/rate-plans` và icon `fa-solid fa-tags`.
- Route cấu hình trong `tenant.routes.ts`:
  - Cần thêm route `{ path: 'rate-plans', loadComponent: ... }` có bảo vệ bằng `tenantAdminGuard`.
