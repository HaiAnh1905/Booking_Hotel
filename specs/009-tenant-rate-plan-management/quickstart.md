# Quickstart & Verification Guide: Quản Lý Gói Giá Chuỗi Cho Admin Tenant (RM-009)

Hướng dẫn kiểm thử và xác thực tính năng End-to-End từ phân hệ Quản trị Chuỗi (`TenantAdmin`) sang phân hệ Quản lý Khách sạn (`HotelManager`).

---

## 1. Kịch Bản 1: Xem & Tạo Mới Gói Giá Chuỗi Bên Tenant Admin

1. Đăng nhập hệ thống bằng tài khoản `TenantAdmin` (ví dụ: `admin_tenant` hoặc tài khoản có vai trò Quản trị viên Chuỗi).
2. Quan sát thanh menu điều hướng bên trái (Sidebar):
   - Thấy mục **"Quản lý Gói Giá"** (`/tenant/rate-plans`) nằm cùng cấp với *Khách sạn, Nhân sự, Đặt phòng, Báo cáo*.
3. Nhấp vào mục này:
   - Giao diện bảng danh sách gói giá hiển thị với các gói mặc định (`STANDARD`, `BED_AND_BREAKFAST`, `NON_REFUNDABLE`).
4. Nhấn nút **"+ Thêm gói giá mới"**:
   - Modal hiển thị đầy đủ các trường:
     - Tên gói giá `*`
     - Mã gói giá `*`
     - Mô tả
     - Checkbox "Bao gồm bữa sáng"
     - Checkbox "Cho phép hoàn hủy" (kèm trường "Thời hạn hủy miễn phí trước giờ nhận phòng")
     - % Điều chỉnh giá mặc định `*`
5. Nhập thông tin:
   - Tên: `Gói Cuối Tuần Thư Thái`
   - Mã: `WEEKEND_SPECIAL`
   - Bữa sáng: Có (`true`)
   - Hoàn hủy: Có (`true`), 24 giờ.
   - % Điều chỉnh: `10`%
6. Nhấn **Lưu**:
   - Thông báo toastr thành công hiển thị.
   - Bảng danh sách lập tức xuất hiện dòng mới `WEEKEND_SPECIAL`.

---

## 2. Kịch Bản 2: Kiểm Tra Đồng Bộ Sang Phân Hệ Hotel Manager

1. Chuyển sang tài khoản `HotelManager` hoặc đổi sang không gian Quản lý cơ sở (`/manager/dashboard`).
2. Vào trang **Quản Lý Giá Phòng** (`/manager/rates?hotelId=...`).
3. Chọn một hạng phòng bất kỳ (ví dụ: *Phòng đôi Superior*).
4. Quan sát cột bên trái **"Danh mục gói giá phòng (Rate Plans)"**:
   - Gói giá mới `Gói Cuối Tuần Thư Thái (WEEKEND_SPECIAL)` lập tức xuất hiện trong danh mục.
   - Trạng thái mặc định là Mở bán (`Active`).
   - Đơn giá hiển thị tự động bằng: $\text{Giá hạng phòng} \times (1 + 10\%)$.
5. Thử gạt công tắc Bật/Tắt mở bán để xác nhận tính độc lập tại cơ sở.

---

## 3. Kịch Bản 3: Chỉnh Sửa & Xóa Gói Giá Chuỗi

1. Quay lại trang `/tenant/rate-plans` với tài khoản `TenantAdmin`.
2. Tại dòng `WEEKEND_SPECIAL`, bấm nút ✏️ **Chỉnh sửa**:
   - Sửa % điều chỉnh giá thành `20`%.
   - Nhấn **Lưu** $\rightarrow$ Dữ liệu trên bảng cập nhật tức thì.
3. Bấm nút 🗑️ **Xóa**:
   - Hộp thoại xác nhận hiển thị: *"Bạn có chắc chắn muốn xóa gói giá này không?"*.
   - Xác nhận $\rightarrow$ Gói giá biến mất khỏi danh mục.
4. Quay lại trang `/manager/rates` của Quản lý cơ sở:
   - Gói `WEEKEND_SPECIAL` không còn xuất hiện trong danh sách mở bán.
