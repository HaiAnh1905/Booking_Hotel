# Hướng Dẫn Kiểm Thử & Chạy Thử Nghiệm (Quickstart): RM-004

**Feature Branch**: `004-shared-ui-layout`  
**Date**: 2026-11-13  
**Status**: Ready for Verification  
**Spec Reference**: [spec.md](spec.md)

---

## 1. Yêu Cầu Môi Trường (Prerequisites)

- Node.js LTS (v20.x hoặc v22.x)
- Angular CLI v22+
- Trình duyệt Web hiện đại (Chrome / Edge / Firefox)
- Dự án Angular đặt tại: `BookingHotel/angular`

---

## 2. Các Lệnh Build & Chạy Thử Nghiệm

### 2.1 Biên dịch kiểm tra mã nguồn (Build Check)
Chạy lệnh build production để kiểm tra tính toàn vẹn cú pháp TypeScript và SCSS:
```powershell
Set-Location -Path "D:\DFT\Booking_Hotel\BookingHotel\angular"
npm run build
```
*Kết quả kỳ vọng: Quá trình build hoàn thành thành công trong khoảng 10-15s, không có lỗi biên dịch.*

### 2.2 Chạy môi trường phát triển (Dev Server)
Khởi động máy chủ phát triển Angular:
```powershell
Set-Location -Path "D:\DFT\Booking_Hotel\BookingHotel\angular"
npm start
```
Ứng dụng sẽ khả dụng tại: `http://localhost:4200/`

---

## 3. Quy Trình Kiểm Thử Độc Lập Các Kịch Bản (Independent Testing Walkthrough)

### Kịch Bản 1: Cổng Đặt Phòng & Trang Chủ Khách Hàng (Customer Landing Page)
1. Mở trình duyệt truy cập: `http://localhost:4200/`
2. **Kiểm tra Header**:
   - Logo thương hiệu hiển thị chữ đỏ **`Booking`** (`#990000`) và chữ đen **`Hotel`** (`#111111`), có khoảng cách rõ ràng giữa 2 chữ.
   - Nút tác vụ Đăng nhập / Đăng ký hiển thị góc phải.
3. **Kiểm tra Hero Search Banner**:
   - Form tìm kiếm phòng với các trường: Điểm đến/Khách sạn, Ngày nhận - Ngày trả phòng, Số khách.
   - Nút "Tìm Phòng" màu đỏ sẫm `#990000`, rê chuột vào chuyển sang `#7a0000`.
4. **Kiểm tra Tiện ích & Phòng mẫu**:
   - Các thẻ phòng mẫu (Deluxe, Suite, Family) hiển thị ảnh, giá tham khảo và huy hiệu tiện nghi.
5. **Kiểm tra Footer**:
   - Chân trang hiển thị đầy đủ bản quyền `Booking Hotel` và thông tin liên hệ.

### Kịch Bản 2: Bộ Chuyển Đổi Ngôn Ngữ Song Ngữ (vi/en Language Switcher)
1. Nhấp vào Dropdown ngôn ngữ trên Header (góc trên bên phải).
2. Chọn **"English"** $\rightarrow$ Xác nhận toàn bộ nhãn:
   - "Trang chủ" $\rightarrow$ "Home"
   - "Tìm Phòng" $\rightarrow$ "Search Rooms"
   - "Đăng nhập" $\rightarrow$ "Login"
   *Thời gian phản hồi diễn ra tức thì trong < 200ms mà không reload trang.*
3. Chọn lại **"Tiếng Việt"** $\rightarrow$ Hệ thống lập tức khôi phục Tiếng Việt chuẩn.
4. Đóng tab trình duyệt và mở lại $\rightarrow$ Ngôn ngữ được bảo lưu đúng từ `localStorage`.

### Kịch Bản 3: Khung Giao Diện Quản Trị & Vận Hành (Management Layout Shell)
1. Truy cập tuyến đường quản trị: `http://localhost:4200/admin`
2. **Kiểm tra Sidebar bên trái**:
   - Nền màu đen sâu `#000000`.
   - Danh sách menu quản trị có icon sắc nét.
   - Mục menu đang chọn được viền/nổi bật bằng màu Xanh Cyan (`#00FFFF`).
   - Nhấn nút toggle thu gọn (collapse button) $\rightarrow$ Sidebar thu gọn lại hiển thị icon, mở rộng diện tích làm việc.
3. **Kiểm tra Nội dung chính**:
   - Nền trắng sáng `#FFFFFF`, thanh Breadcrumb rõ ràng.

### Kịch Bản 4: Thư Viện UI Dùng Chung (Showcase)
1. Truy cập tuyến đường: `http://localhost:4200/admin/ui-showcase`
2. **Bảng dữ liệu (`BhDataTableComponent`)**:
   - Xem dữ liệu bảng mẫu, thử bấm chuyển trang (phân trang), sắp xếp theo cột.
3. **Hộp thoại xác nhận (`BhModalComponent`)**:
   - Bấm nút "Mở Modal Xác Nhận" $\rightarrow$ Modal hiện lên giữa màn hình với backdrop mờ, nhấn `ESC` hoặc nút Hủy để đóng an toàn.
4. **Thông báo nổi (`BhToastrService`)**:
   - Bấm các nút thử nghiệm "Toast Thành công", "Toast Lỗi", "Toast Cảnh báo", "Toast Thông tin" $\rightarrow$ Toast xuất hiện góc trên bên phải và tự ẩn sau 4 giây.
5. **Chỉ báo Tải & Dữ liệu trống**:
   - Thử nghiệm bật/tắt Spinner và xem khối Empty State.

### Kịch Bản 5: Sơ Đồ Buồng Phòng Lễ Tân (Room Rack Preview)
1. Truy cập tuyến đường: `http://localhost:4200/admin/room-rack-preview`
2. **Kiểm tra bảng màu theo Hiến pháp**:
   - Phòng Trống/Sạch (`Available`): Huy hiệu màu Xanh lá tươi `#99FF99` với chữ Đen `#000000`.
   - Phòng Bẩn/Chờ dọn (`Dirty`): Huy hiệu màu Xám `#808080` với chữ Trắng `#FFFFFF`.
   - Phòng Có khách (`Occupied`): Huy hiệu màu Đỏ sẫm `#990000` với chữ Trắng `#FFFFFF`.
   - Phòng Bảo trì (`Maintenance`): Huy hiệu màu Vàng cam `#FFA500` với chữ Đen `#000000`.
3. **Thử nghiệm Bộ lọc**:
   - Bấm lọc "Tất cả", "Phòng sạch", "Phòng bẩn", "Có khách" $\rightarrow$ Danh sách thẻ phòng lọc mượt mà theo trạng thái.

