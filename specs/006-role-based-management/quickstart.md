# Quickstart & Verification Guide: RM-006 - Phân Tách Không Gian & Thiết Kế Giao Diện Quản Lý Theo 4 Vai Trò

**Feature Branch**: `006-role-based-management`  
**Date**: 2026-11-13  
**Spec**: [specs/006-role-based-management/spec.md](spec.md)

---

## 1. Mục Đích & Điều Kiện Tiên Quyết

Tài liệu này cung cấp các kịch bản kiểm thử trực quan và tự động nhằm thẩm định tính chính xác của phân hệ Phân tách không gian giao diện quản lý theo 4 vai trò với 4 Root URL độc lập:
- **Admin System**: `/admin/*`
- **Admin Tenant**: `/tenant/*`
- **Hotel Manager**: `/manager/*`
- **Receptionist**: `/frontdesk/*`

### Điều kiện tiên quyết (Prerequisites):
1. **Cơ sở dữ liệu**: Đã nạp Migration và dữ liệu mẫu (`dotnet run --project BookingHotel.DbMigrator`).
2. **Backend**: Host API đang chạy tại `https://localhost:44322` (`dotnet run --project BookingHotel.HttpApi.Host`).
3. **Frontend**: Angular SPA đang chạy tại `http://localhost:4200` (`npm start`).

---

## 2. Tài Khoản & Phân Quyền Kiểm Thử Mẫu (Test Personas)

| Vai trò kiểm thử | Tên đăng nhập | Thuộc tính phân bổ | Mật khẩu mẫu | Portal tương ứng |
| :--- | :--- | :--- | :--- | :--- |
| **System Admin** | `admin` | Host scope (`TenantId = null`) | `1q2w3E*` | `/admin/dashboard` |
| **Tenant Admin** | `tenantadmin_hcm` | Tenant: `SaigonHotelChain` | `1q2w3E*` | `/tenant/dashboard` |
| **Hotel Manager** | `manager_riverside` | Phụ trách: `Riverside Hotel` & `Central Hotel` | `1q2w3E*` | `/manager/dashboard` |
| **Receptionist** | `receptionist_river` | Cố định: `Riverside Hotel` | `1q2w3E*` | `/frontdesk/room-rack` |
| **Nhân sự Kiêm nhiệm** | `dual_staff` | Sở hữu cả 2 roles: `HotelManager` & `Receptionist` | `1q2w3E*` | Mặc định `/manager/dashboard` |

---

## 3. Các Kịch Bản Xác Minh Chi Tiết (Verification Scenarios)

### Kịch bản 1: Định Tuyến Thông Minh Sau Đăng Nhập (`RoleBasedRedirectGuard`)
1. Truy cập `http://localhost:4200/auth/login`.
2. Đăng nhập bằng `admin` $\rightarrow$ Hệ thống tự động chuyển hướng về `/admin/dashboard`.
3. Đăng xuất, đăng nhập bằng `tenantadmin_hcm` $\rightarrow$ Tự động chuyển hướng về `/tenant/dashboard`.
4. Đăng xuất, đăng nhập bằng `manager_riverside` $\rightarrow$ Tự động chuyển hướng về `/manager/dashboard`.
5. Đăng xuất, đăng nhập bằng `receptionist_river` $\rightarrow$ Tự động chuyển hướng về `/frontdesk/room-rack`.
6. **Kết quả mong đợi**: Không có vai trò nào bị kẹt ở URL `/admin` hoặc chuyển hướng sai Portal.

---

### Kịch bản 2: Kiểm Tra Nhận Diện Giao Diện & Bảng Màu Theo Concept
1. **System Admin Layout** (`/admin/*`):
   - Kiểm tra Sidebar: Nền đen sâu `#000000`, điểm nhấn Cyan `#00FFFF`.
   - Kiểm tra Topbar / Header: Hiển thị Huy hiệu Host màu tím chàm `#7C3AED` (`System Admin - Host Control`).
2. **Tenant Admin Layout** (`/tenant/*`):
   - Kiểm tra Sidebar: Nền đen `#000000`, điểm nhấn Cyan `#00FFFF`.
   - Kiểm tra Topbar / Header: Hiển thị Huy hiệu Chuỗi màu vàng hổ phách `#F59E0B` (`Tenant Admin - SaigonHotelChain`).
3. **Hotel Manager Layout** (`/manager/*`):
   - Kiểm tra Topbar: Hiển thị Dropdown chọn chi nhánh (*Property Selector*) với 2 khách sạn: `Riverside Hotel` và `Central Hotel`.
4. **Receptionist Layout** (`/frontdesk/*`):
   - Kiểm tra Topbar / Sidebar: Gam màu Xanh lá tươi nhạt `#99FF99`, viền xám `#808080`.
   - Tương phản văn bản: 100% chữ trên nền `#99FF99` là chữ Đen đậm `#000000` ($\ge 11:1$, đạt WCAG 2.1 AA / AAA).

---

### Kịch bản 3: Chuyển Đổi Chi Nhánh (`PropertyContextService`) Cho Hotel Manager
1. Đăng nhập bằng `manager_riverside` và vào `/manager/rooms`.
2. Kiểm tra Dropdown trên Topbar: Khách sạn hiện tại là `Riverside Hotel`.
3. Chọn khách sạn `Central Hotel` từ dropdown:
   - Dữ liệu buồng phòng trên trang lập tức phản ứng và nạp danh sách phòng của `Central Hotel`.
   - URL vẫn giữ nguyên dạng: `http://localhost:4200/manager/rooms`.
4. Nhấn F5 (Refresh trình duyệt):
   - Trang tải lại vẫn giữ nguyên khách sạn `Central Hotel` (nhờ lưu `localStorage.getItem('bh_active_hotel_id')`).

---

### Kịch bản 4: Sơ Đồ Buồng Phòng Lễ Tân (Visual Matrix Grid + Table Toggle)
1. Đăng nhập bằng `receptionist_river` và vào `/frontdesk/room-rack`.
2. **Chế độ Visual Matrix Grid (Mặc định)**:
   - Các phòng hiển thị dưới dạng ô lưới trực quan với 4 màu:
     - Xanh lá tươi nhạt `#99FF99`: Phòng sạch / Trống.
     - Xám `#808080`: Phòng bẩn / Chờ dọn.
     - Đỏ nhạt `#FF6B6B`: Phòng đang có khách (Occupied).
     - Vàng cam `#FFD166`: Phòng đã đặt trước (Reserved).
3. **Chuyển đổi sang Chế độ Bảng**:
   - Nhấn nút Toggle `[ ☷ Lưới | ☰ Danh Sách ]`.
   - Giao diện chuyển mượt mà sang `BhDataTableComponent` có tìm kiếm số phòng, phân trang và bộ lọc trạng thái.

---

### Kịch bản 5: Quy Trình Duyệt Ngoại Lệ Tác Nghiệp (Approval Ticket Workflow)
1. **Lễ tân gửi yêu cầu**:
   - Đăng nhập bằng `receptionist_river`, mở chi tiết phòng P.302.
   - Bấm nút "Yêu cầu duyệt ngoại lệ" $\rightarrow$ Modal hiển thị.
   - Chọn loại: `Chiết khấu vượt khung (DiscountOverride)`, Tỷ lệ: `15%`, Lý do: `Khách kỷ niệm ngày cưới`.
   - Bấm "Gửi yêu cầu" $\rightarrow$ Toast hiển thị: `Phiếu yêu cầu #... đã được gửi tới Quản lý cơ sở`.
2. **Hotel Manager xem và phê duyệt**:
   - Đăng nhập bằng `manager_riverside`, mở `/manager/approvals`.
   - Thấy 1 phiếu ở trạng thái `Pending` từ khách sạn `Riverside Hotel`.
   - Bấm nút "Chấp thuận" (Approve) $\rightarrow$ Trạng thái chuyển sang `Approved`.
3. **Đối soát**:
   - Đăng nhập lại bằng `receptionist_river`, phiếu đã chuyển trạng thái `Approved` và mức chiết khấu 15% được tự động áp dụng.

---

### Kịch bản 6: Chuyển Đổi Không Gian Cho Nhân Sự Kiêm Nhiệm (Dual-Role Switcher)
1. Đăng nhập bằng tài khoản `dual_staff`.
2. Hệ thống chuyển hướng vào `/manager/dashboard` (ưu tiên quyền cao hơn).
3. Trên Topbar, kiểm tra sự xuất hiện của nút *Portal Switcher*:
   - `[ Quầy Lễ Tân (Frontdesk) 🡒 ]` (Nền `#99FF99`, Chữ Đen).
4. Nhấn nút:
   - Giao diện chuyển ngay sang `/frontdesk/room-rack` với màu sắc Lễ tân.
   - Trên Topbar của Lễ tân xuất hiện nút: `[ 🡐 Bàn Quản Lý (Manager) ]`.
5. Nhấn nút quay lại $\rightarrow$ Lập tức trở về `/manager/dashboard`.

---

### Kịch bản 7: Chống Xâm Phạm Ranh Giới Vai Trò & Thử Thách An Ninh (Anti-Tamper)
1. Đang ở phiên của `receptionist_river`, cố tình gõ trên thanh địa chỉ trình duyệt:
   - `http://localhost:4200/admin/tenants` $\rightarrow$ `systemAdminGuard` chặn, Toast báo lỗi 403, điều hướng về `/frontdesk/room-rack`.
   - `http://localhost:4200/admin/users` $\rightarrow$ Bị chặn, điều hướng về `/frontdesk/room-rack`.
   - `http://localhost:4200/tenant/staff` $\rightarrow$ `tenantAdminGuard` chặn, điều hướng về `/frontdesk/room-rack`.
   - `http://localhost:4200/manager/rooms` $\rightarrow$ `hotelManagerGuard` chặn, điều hướng về `/frontdesk/room-rack`.
2. Đang ở phiên của `manager_riverside`, cố tình truy cập:
   - `http://localhost:4200/admin/tenants` $\rightarrow$ Bị chặn 403.
   - `http://localhost:4200/tenant/hotels` $\rightarrow$ Bị chặn 403.
3. Gửi request REST API trực tiếp bằng Postman / curl mà không có quyền:
   - Lễ tân gọi `POST /api/app/operational-approval/{id}/approve` $\rightarrow$ Backend trả về HTTP `403 Forbidden` (`Authorization failed`).
   - Tenant Admin gọi API Host Tenant `POST /api/app/tenants` $\rightarrow$ Backend trả về HTTP `403 Forbidden`.
