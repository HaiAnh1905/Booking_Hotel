# Quickstart Validation Guide: RM-012 Phân hệ Tác nghiệp Lễ tân

**Feature**: `012-receptionist-frontdesk-portal`  
**References**: [spec.md](./spec.md) · [data-model.md](./data-model.md) · [contracts/frontdesk-api.md](./contracts/frontdesk-api.md)

---

## 1. Điều kiện tiên quyết (Prerequisites)

- SQL Server đang chạy và chuỗi kết nối trỏ đúng CSDL `BookingHotel`.
- Đã có ít nhất 1 Tenant, 1 cơ sở khách sạn (`Hotel`), ít nhất 1 hạng phòng (`RoomType`) và vài phòng vật lý (`Room` trạng thái `Active`).
- Đã có 1 tài khoản vai trò **Receptionist** được phân công vào cơ sở đó (`UserHotelAssignment`), và 1 vài booking mẫu (hoặc tạo từ nền tảng/seed) ở các trạng thái `PendingConfirmation`, `Confirmed`.

---

## 2. Bước cập nhật CSDL & Kiểm thử tự động

### 2.1 Áp dụng EF Core Migration
Chạy từ thư mục `BookingHotel/`:
```powershell
dotnet ef migrations add AddFrontdeskStayTrackingToBookings --project src/BookingHotel.EntityFrameworkCore --startup-project src/BookingHotel.DbMigrator
dotnet build src/BookingHotel.DbMigrator/BookingHotel.DbMigrator.csproj
Set-Location src/BookingHotel.DbMigrator
dotnet run --no-build
```
**Kết quả kỳ vọng**: Bảng `AppBookings` có thêm 6 cột mới (`RoomTypeId`, `ActualCheckInTime`, `ActualCheckOutTime`, `IsIdentityVerified`, `CancellationReason`, `FrontdeskNotes`) và index `IX_AppBookings_Tenant_Hotel_RoomType`.

### 2.2 Chạy Unit Tests & Integration Tests Backend
```powershell
dotnet test test/BookingHotel.Domain.Tests/BookingHotel.Domain.Tests.csproj --filter "FullyQualifiedName~FrontdeskStayDomainServiceTests"
dotnet test test/BookingHotel.EntityFrameworkCore.Tests/BookingHotel.EntityFrameworkCore.Tests.csproj --filter "FullyQualifiedName~Frontdesk"
```
**Kết quả kỳ vọng**: 100% test PASS (bao gồm mask CCCD/Hộ chiếu, chống trùng lịch gán phòng, Check-in/Check-out đồng bộ trạng thái phòng, chặn hủy/check-in sai trạng thái, và chặn truy cập liên cơ sở).

### 2.3 Kiểm tra biên dịch Frontend Angular
```powershell
Set-Location BookingHotel/angular
npx ngc -p tsconfig.app.json --noEmit
npm run build
```
**Kết quả kỳ vọng**: Không có lỗi TypeScript hoặc Angular template, `vi.json` / `en.json` hợp lệ.

---

## 3. Kịch bản xác minh End-to-End (Manual E2E)

1. **Điều hướng & Dashboard (US1)**:
   - Đăng nhập bằng tài khoản `Receptionist` → hệ thống tự chuyển tới `/frontdesk/dashboard`.
   - Kiểm tra thanh điều hướng bên có đúng 5 mục: **Tổng quan**, **Sơ đồ phòng**, **Quản lý đặt phòng**, **Khách lưu trú**, **Check-in / Check-out** (không còn mục Hóa đơn hay nút Quản trị Housekeeping/Giá phòng).
   - Xác nhận 4 thẻ KPI khớp với số liệu thực từ CSDL; nếu cơ sở chưa có booking → hiển thị `0` và `<bh-empty-state>`.
2. **Reservation Management (US2)**:
   - Mở `/frontdesk/reservations`, thử lọc theo mã booking, tên khách, trạng thái và khoảng ngày.
   - Chọn 1 booking `PendingConfirmation` → bấm **Xác nhận** → trạng thái đổi sang `Confirmed`.
   - Bấm **Gán phòng** → chỉ thấy phòng trống, sẵn sàng, đúng hạng phòng → chọn phòng và lưu → kiểm tra Room Rack đổi phòng đó sang `Đã đặt`.
   - Sửa yêu cầu đặc biệt & ghi chú lễ tân → lưu thành công; xác nhận các trường Tổng tiền / Tiền cọc ở chế độ chỉ đọc.
   - Thử **Hủy booking** với lý do hợp lệ → trạng thái đổi `Cancelled` và phòng vừa gán được giải phóng. Xác nhận không có nút Từ chối / No-Show.
3. **Check-in / Check-out (US3)**:
   - Mở `/frontdesk/check-in-out` tab **Nhận phòng**, chọn booking `Confirmed` dự kiến đến hôm nay.
   - Nhập số CCCD `079201001234`, chọn phòng sẵn sàng → bấm **Xác nhận Check-in** → kiểm tra booking đổi `CheckedIn`, `ActualCheckInTime` được ghi nhận, số CCCD hiển thị `********1234` kèm huy hiệu Đã xác minh, phòng trên Room Rack chuyển sang **Có khách** (`Occupied`).
   - Chuyển sang tab **Trả phòng**, chọn booking đang lưu trú → bấm **Xác nhận Check-out** → kiểm tra booking đổi `Completed`, `ActualCheckOutTime` được ghi nhận, phòng trên Room Rack quay về **Trống & Sẵn sàng đón khách ngay**.
4. **Room Rack (US4)**:
   - Mở `/frontdesk/room-rack`, chuyển giữa chế độ **Lưới** và **Bảng**, lọc theo tầng (tự động sinh) và trạng thái.
   - Click vào phòng **Có khách** → ngăn chi tiết hiển thị thông tin booking gắn kèm và nút đi nhanh tới Check-out; xác nhận không có nút dọn phòng Housekeeping hay cấu hình bảo trì.
5. **Guest Management (US5)**:
   - Mở `/frontdesk/guests`, tìm khách vừa Check-in, mở chi tiết thấy lịch sử booking tại cơ sở.
   - Cập nhật SĐT/email/số CCCD mới → lưu → kiểm tra thông tin khách được cập nhật đồng bộ trên booking đang hoạt động và số CCCD được che đúng 4 số cuối.
6. **Kiểm tra Bảo mật & Song ngữ (Security & i18n)**:
   - Chuyển đổi ngôn ngữ VI ↔ EN → 100% nhãn, trạng thái, thông báo đều đổi mượt mà, không còn chữ cứng.
   - Dùng DevTools thử gọi API sang `hotelId` của cơ sở khác hoặc gọi API sửa giá (`/api/app/manager/rooms/pricing/...`) / tạo phòng (`/api/app/manager/rooms/physical-rooms`) bằng JWT của Lễ tân → đều bị chặn `403 Forbidden` / `CrossHotelOperationForbidden`.
