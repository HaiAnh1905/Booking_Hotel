# Hướng Dẫn Khởi Chạy & Kiểm Thử Nhanh (Quickstart Guide): RM-008 - Hotel Manager Console

**Feature**: `008-hotel-manager-management`  
**Nhánh Git**: `008-hotel-manager-management`  
**Mục tiêu**: Hướng dẫn thiết lập môi trường, chạy migration, nạp dữ liệu mẫu và thực hiện 9 kịch bản kiểm thử tích hợp (End-to-End) xác minh toàn diện tính năng dành cho vai trò Hotel Manager.

---

## 1. Yêu Cầu Môi Trường & Chuẩn Bị

1. **Backend**:
   - .NET 10 SDK
   - SQL Server 2022+ / LocalDB (`localhost\SQLEXPRESS` hoặc `(localdb)\mssqllocaldb`)
   - Visual Studio 2022 / VS Code
2. **Frontend**:
   - Node.js 20.x+ / PNPM 9.x+
   - Angular CLI 19+
3. **Tài Khoản Kiểm Thử Đã Tạo Sẵn (Seeded Users)**:
   - `manager.b@bookinghotel.com` (Mật khẩu: `Manager@123`) — Được phân công quản lý `PLA-HCM-1` (Khách sạn The Passion Lux Airport) và `HS-VT-1` (Sóng Homestay Vũng Tàu).
   - `tenant.admin@bookinghotel.com` (Mật khẩu: `Admin@123`) — Quản trị viên Tenant.

---

## 2. Các Bước Khởi Chạy Hệ Thống

### Bước 1: Áp dụng Migration CSDL
```powershell
cd d:\DFT\Booking_Hotel\BookingHotel\src\BookingHotel.DbMigrator
dotnet run
```

### Bước 2: Khởi chạy Backend API Host
```powershell
cd d:\DFT\Booking_Hotel\BookingHotel\src\BookingHotel.HttpApi.Host
dotnet run
```
*API Swagger khả dụng tại: `https://localhost:44322/swagger`*

### Bước 3: Khởi chạy Ứng dụng Angular
```powershell
cd d:\DFT\Booking_Hotel\BookingHotel\angular
pnpm start
```
*Giao diện người dùng khả dụng tại: `http://localhost:4200`*

---

## 3. Kịch Bản Xác Minh Kỹ Thuật (Step-by-Step Verification)

### Kịch Bản 1: Độc Lập Ngữ Cảnh Cơ Sở Đa Tab & Chặn Can Thiệp Trái Phép
1. Đăng nhập bằng tài khoản `manager.b@bookinghotel.com`.
2. Mở Tab 1: Chọn cơ sở `PLA-HCM-1` trên Property Selector Topbar $\rightarrow$ Kiểm tra Dashboard hiển thị số liệu của `PLA-HCM-1`.
3. Mở Tab 2: Chọn cơ sở `HS-VT-1` $\rightarrow$ Kiểm tra Tab 2 hiển thị số liệu của `HS-VT-1`, quay lại Tab 1 F5 kiểm tra vẫn giữ nguyên `PLA-HCM-1` (không bị ghi đè chéo).
4. Mở công cụ Developer Tools / Postman, gửi request API với `hotelId` của một cơ sở khác không thuộc quyền quản lý $\rightarrow$ Backend lập tức chặn và trả về HTTP **403 Forbidden** (`BookingHotel:CrossHotelOperationForbidden`).

### Kịch Bản 2: Tách Biệt 2 Chiều Trạng Thái Buồng Phòng & Quy Tắc Bán Phòng
1. Truy cập `/manager/rooms`:
   - Phòng 301 có trạng thái: `OperationalStatus = Available`, `HousekeepingStatus = Dirty`.
2. Kiểm tra nhãn trạng thái hiển thị "Chờ dọn dẹp" (màu cam nhạt), nút Check-in bị vô hiệu hóa cho đến khi lễ tân/buồng phòng chuyển sang `Clean` hoặc `Inspected`.

### Kịch Bản 3: Chuyển Phòng Sang `OutOfService` Kèm Kiểm Tra Xung Đột
1. Thử chuyển phòng 304 sang `OutOfService` trong khoảng ngày 20/11 - 25/11 trong khi phòng 304 đã có booking được gán vào ngày 22/11.
2. Hệ thống cảnh báo đỏ và chặn không cho chuyển (`BookingHotel:ConflictingBookingExists`), yêu cầu điều chuyển booking sang phòng khác trước.
3. Chuyển thành công một phòng không có booking: Kiểm tra `AvailableInventory` trên Dashboard tự động giảm 1 phòng và có bản ghi `AuditLog` lưu lại lý do cùng ngày kết thúc dự kiến.

### Kịch Bản 4: Quản Lý Gói Giá RatePlan & Ghi Đè Giá Ngày (Decision Q1)
1. Vào `/manager/rates`: Quan sát danh mục RatePlan chuẩn của chuỗi (*Standard Flexible*, *Non-Refundable Promo*, *Breakfast Included*).
2. Kích hoạt gói "Non-Refundable Promo" cho loại phòng "Deluxe King" với giá 1.550.000 VNĐ.
3. Thiết lập bảng giá `DailyRateOverride` cho đêm Giáng sinh 24/12 tăng lên 2.400.000 VNĐ $\rightarrow$ Lưu thành công và kiểm tra giá áp dụng chuẩn xác.

### Kịch Bản 5: Tạo Đặt Phòng Trực Tiếp (Decision Q4)
1. Vào `/manager/bookings`, bấm nút "Tạo Đặt Phòng Mới" (`Bookings.Create`).
2. Nhập thông tin khách đoàn doanh nghiệp, chọn loại phòng Deluxe, áp dụng mức giá hợp đồng 1.600.000 VNĐ/đêm, nhập tiền cọc hợp đồng 2.000.000 VNĐ.
3. Bấm "Tạo đặt phòng" $\rightarrow$ Hệ thống tạo thành công booking với cờ `IsDirectBooking = true`, trạng thái `Confirmed` và `PartiallyPaid`.

### Kịch Bản 6: Đánh Dấu No-Show & Điều Chỉnh Miễn Phạt (Decision Q2)
1. Chọn một đơn đặt phòng quá giờ check-in mà khách không đến.
2. Bấm "Xác nhận Khách không đến (No-Show)".
3. Màn hình modal hiển thị mức phạt chuẩn theo Tenant; Manager tích chọn "Miễn phạt do sự cố bất khả kháng" (0 VNĐ), nhập lý do: "Chuyến bay VN240 bị bão hủy" và bấm Xác nhận.
4. Kiểm tra phòng được giải phóng phân bổ, trạng thái đơn chuyển `NoShow`, lập lệnh hoàn cọc và lưu vết kiểm toán `AuditLog`.

### Kịch Bản 7: Bảo Mật Dữ Liệu Khách Hàng & Ghi Chú Có Audit
1. Vào `/manager/guests`: Quan sát danh sách khách hàng; số CCCD/Passport được che bảo mật (`***-***-5678`), chỉ hiển thị 4 số cuối.
2. Tạo Ghi chú nội bộ cho khách VIP: "Thích phòng yên tĩnh, dị ứng hải sản" kèm cờ Alert đỏ.
3. Kiểm tra thông tin người tạo được gắn tự động từ `CurrentUser.Id` và không thể bị sửa đổi trái phép.

### Kịch Bản 8: Tính Toán Báo Cáo Chuẩn Quốc Tế (Occupancy, ADR, RevPAR)
1. Vào `/manager/reports`: Chọn kỳ báo cáo từ 01/11 đến 18/11.
2. Kiểm tra các chỉ số tự động khớp với công thức USALI:
   - `AvailableInventory = Tổng phòng - Phòng OutOfService`.
   - `Occupancy Rate = (Occupied / AvailableInventory) * 100%`.
   - `RevPAR = RoomRevenue / AvailableInventory = ADR * OccupancyRate`.
3. Kiểm tra phân rã dòng tiền: Tiền phòng thuần (`RoomRevenue`), Dịch vụ phụ trợ (`ServiceRevenue`) và Tiền thực thu (`CollectedRevenue`).

### Kịch Bản 9: Xử Lý Khiếu Nại & Cảnh Báo Quá Hạn SLA 48h (Decision Q3)
1. Vào `/manager/feedback`: Xem danh sách khiếu nại.
2. Một khiếu nại chưa xử lý quá 48h tự động hiển thị nhãn đỏ `Overdue SLA` và trạng thái chuyển sang `Escalated`.
3. Đăng nhập tài khoản `tenant.admin@bookinghotel.com` $\rightarrow$ Nhận được thông báo cảnh báo leo thang khiếu nại từ cơ sở.