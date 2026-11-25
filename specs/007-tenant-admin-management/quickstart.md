# Quickstart Validation Guide: RM-007 - Quản Trị Vận Hành Chuỗi & Phân Quyền Tenant Admin

**Feature**: `007-tenant-admin-management` | **Date**: 2026-11-18  
**Specs Reference**: [spec.md](spec.md) | **Contracts**: [contracts/](contracts/)

---

## 1. Điều Kiện Tiên Quyết (Prerequisites)

1. CSDL đã được nạp dữ liệu nền tảng từ Wave 0: Chạy `DbMigrator` thành công.
2. Có sẵn ít nhất 1 tài khoản quản trị viên chuỗi (`TenantAdmin`) thuộc một Tenant đang hoạt động (ví dụ: `TenantId != null`).
3. Dịch vụ Backend đang chạy tại `https://localhost:44322` và Angular Frontend chạy tại `http://localhost:4200`.

---

## 2. Kịch Bản Xác Minh Cuối Cùng (End-to-End Validation Scenarios)

### Kịch Bản 1: Tạo Mới Khách Sạn & Kiểm Tra Ràng Buộc Hotel Code
1. Đăng nhập vào hệ thống bằng tài khoản `TenantAdmin`.
2. Truy cập màn hình Quản lý Khách sạn tại `/tenant/hotels`.
3. Bấm "Thêm mới Khách sạn", nhập:
   - Tên: `Booking Hotel Nha Trang Ocean`
   - Mã: `BH-NHA-01`
   - Địa chỉ: `78 Trần Phú, Nha Trang`
   - SĐT: `02583888999`, Email: `nhatrang@bookinghotel.vn`, Xếp hạng: `5 sao`, Số phòng: `100`.
4. Bấm "Lưu" $\rightarrow$ Khách sạn xuất hiện trên danh sách với huy hiệu Xanh lá "Hoạt động".
5. **Kiểm tra trùng lặp**: Bấm thêm mới lần nữa và nhập lại đúng mã `BH-NHA-01` $\rightarrow$ Hệ thống lập tức báo lỗi validation `HotelCodeAlreadyExists` và không cho phép lưu.

---

### Kịch Bản 2: Tạm Ngừng Cơ Sở (Định Kỳ vs Khẩn Cấp)
1. Chọn cơ sở `BH-NHA-01` trên bảng, mở modal "Tạm ngừng cơ sở":
2. **Thử nghiệm Định kỳ vi phạm thời gian**: Chọn loại "Tạm ngừng định kỳ", chọn ngày bắt đầu cách hiện tại 10 ngày $\rightarrow$ Hệ thống chặn lại với thông báo "Tạm ngừng định kỳ bắt buộc phải đặt lịch trước tối thiểu 30 ngày".
3. **Thử nghiệm Định kỳ hợp lệ**: Chọn ngày bắt đầu cách hiện tại 45 ngày $\rightarrow$ Lưu thành công, hệ thống lên lịch và kích hoạt cutoff nhận đơn.
4. **Thử nghiệm Khẩn cấp**: Chọn loại "Tạm ngừng khẩn cấp", nhập lý do "Sự cố chập điện buồng máy đang khắc phục" $\rightarrow$ Cơ sở chuyển sang trạng thái Tạm ngừng ngay lập tức, gửi thông báo đến khách hàng và nhân sự.

---

### Kịch Bản 3: Tạo Nhân Viên Gán Vai Trò Kép (Kiêm Nhiệm) & Chuyển Đổi Không Gian
1. Truy cập phân hệ Quản lý Nhân sự tại `/tenant/staff`.
2. Bấm "Tạo tài khoản nhân viên", nhập:
   - Họ tên: `Trần Văn Kiêm Nhiệm`
   - Email: `kiemnhiem@bookinghotel.vn`, SĐT: `0912345678`, Mật khẩu: `P@ssword2026!`
   - Tích chọn CẢ HAI vai trò: `HotelManager` và `Receptionist`.
   - Chọn khách sạn phân công: Quản lý chi nhánh `BH-HAN-01`, Lễ tân cơ sở chính `BH-HAN-01`.
3. Bấm "Lưu" $\rightarrow$ Nhân viên hiển thị trên bảng với cả 2 huy hiệu `HotelManager` và `Receptionist`.
4. Đăng nhập bằng tài khoản `kiemnhiem@bookinghotel.vn`:
   - Lần đầu đăng nhập: Hệ thống nhận diện cờ `ShouldChangePasswordOnNextLogin` và điều hướng bắt buộc tới màn hình đổi mật khẩu.
   - Sau khi đổi mật khẩu: Hệ thống tự động chuyển vào `/manager/dashboard`.
   - Quan sát Topbar: Nút `Portal Switcher Pill` hiển thị rõ ràng, bấm nút $\rightarrow$ chuyển đổi tức thì sang `/frontdesk/room-rack` mà không cần đăng xuất.

---

### Kịch Bản 4: Kiểm Tra Chống Leo Thang Đặc Quyền (Anti-Privilege Escalation)
1. Thử gửi POST request lên `/api/app/identity-user-management/tenant-staff` với payload chứa `roles: ["SystemAdmin"]` bằng công cụ API / Swagger.
2. **Kết quả mong đợi**: Backend trả về mã lỗi `403 Forbidden` (`CannotAssignPlatformOrHostRoles`).

---

### Kịch Bản 5: Kiểm Tra Dashboard & So Sánh Hiệu Suất Theo RevPAR
1. Truy cập trang `/tenant/dashboard`.
2. Kiểm tra các thẻ KPI: Tổng khách sạn, Tổng nhân viên, Doanh thu dòng tiền thực thu (Cash Basis), Tỷ lệ lấp đầy.
3. Kiểm tra bảng so sánh cơ sở:
   - Danh sách chi nhánh được sắp xếp mặc định theo chỉ số **RevPAR** giảm dần.
   - Thử nghiệm click vào các tiêu đề cột khác (Doanh thu, Occupancy, ADR) $\rightarrow$ Bảng tự động sắp xếp lại mượt mà.

---

### Kịch Bản 6: Giám Sát Đặt Phòng Hợp Nhất (Read-Only)
1. Truy cập `/tenant/bookings`.
2. Kiểm tra danh sách booking từ tất cả các chi nhánh hiển thị đầy đủ.
3. Sử dụng bộ lọc: Chọn chi nhánh `BH-HAN-01` và trạng thái `Confirmed`.
4. Mở xem chi tiết một đơn đặt phòng: Hiển thị đầy đủ thông tin khách, tiền cọc, và dòng thời gian Audit Trail.
5. Xác nhận không có bất kỳ nút "Check-in" hay "Check-out" nào xuất hiện trên màn hình này.

---

### Kịch Bản 7: Cấu Hình Tổ Chức & Kiểm Tra Chính Sách Hybrid
1. Truy cập `/tenant/settings`.
2. Cập nhật tên chuỗi, hotline và logo thương hiệu mới.
3. Thiết lập Giờ Check-in chuẩn chuỗi `14:00` và Check-out chuẩn chuỗi `12:00`.
4. Bấm "Lưu cấu hình" $\rightarrow$ Logo và tên hiển thị cập nhật ngay trên Topbar.
5. Vào `/tenant/hotels`, mở sửa chi nhánh `BH-DAD-01` (khu nghỉ dưỡng), đổi giờ Check-in riêng sang `15:00` và lưu thành công (Ghi đè hợp lệ).
6. Kiểm tra chính sách Hủy phòng: Bị khóa cứng ở cấp chi nhánh, chỉ cho phép chỉnh sửa tập trung tại `/tenant/settings`.
