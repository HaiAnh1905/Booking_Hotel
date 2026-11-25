# Quickstart Validation Guide: RM-005 - Quản Lý Tài Khoản & Phân Quyền Vai Trò

**Feature Branch**: `005-account-role-management`  
**Date**: 2026-11-13  
**Status**: Ready for Verification  
**Spec**: [specs/005-account-role-management/spec.md](spec.md)

---

## 1. Yêu Cầu Tiên Quyết (Prerequisites)

1. Cơ sở dữ liệu đã được gieo dữ liệu ban đầu (Seeded Data) từ RM-003:
   - 5 vai trò chuẩn: `SystemAdmin`, `Customer`, `TenantAdmin`, `HotelManager`, `Receptionist`.
   - Một Tenant mẫu: `Default` (hoặc `TenantA`).
   - Hai khách sạn mẫu trong Tenant: `Khách sạn Biển Đà Nẵng` (ID: `HTL-01`) và `Khách sạn Phố Cổ Hội An` (ID: `HTL-02`).
2. Backend API Host đang chạy tại `https://localhost:44305` (hoặc cổng cấu hình).
3. Frontend Angular đang chạy tại `http://localhost:4200`.

---

## 2. Các Kịch Bản Kiểm Thử Xác Thực Chức Năng (Runnable Validation Scenarios)

### Kịch Bản 1: Tạo Tài Khoản Quản Lý Khách Sạn (Hotel Manager) với Đa Chi Nhánh

- **Mục tiêu**: Xác thực Tenant Admin tạo thành công Quản lý khách sạn và phân công phụ trách 2 cơ sở.
- **Các bước thực hiện**:
  1. Đăng nhập vào Angular với tài khoản `tenant_admin` của Chuỗi A.
  2. Điều hướng tới phân hệ Quản lý Nhân sự: `http://localhost:4200/admin/tenant/staff` (hoặc `/identity/users`).
  3. Nhấn nút **"Thêm nhân viên mới"** (`BhModalComponent` hiển thị).
  4. Nhập thông tin:
     - Tên đăng nhập: `manager_danang_hoian`
     - Email: `manager@chain-a.com`
     - Mật khẩu: `P@ssword2026!#`
     - Vai trò: Chọn `Hotel Manager`.
     - Phân công chi nhánh: Tích chọn cả 2 cơ sở `Khách sạn Biển Đà Nẵng` và `Khách sạn Phố Cổ Hội An`.
  5. Nhấn **"Lưu lại"**.
- **Kết quả kỳ vọng**:
  - Hệ thống tạo người dùng thành công, thông báo Toast xanh hiển thị.
  - Bản ghi xuất hiện trên bảng `BhDataTableComponent` với huy hiệu `Hotel Manager` và 2 nhãn chi nhánh.
  - CSDL có 2 bản ghi `UserHotelAssignment` với `IsActive = true`.

---

### Kịch Bản 2: Kiểm Tra Ràng Buộc Khóa Cứng 1 Chi Nhánh Cho Lễ Tân (Receptionist)

- **Mục tiêu**: Đảm bảo Lễ tân chỉ được gán duy nhất 1 chi nhánh; ngăn chặn việc chọn nhiều cơ sở.
- **Các bước thực hiện**:
  1. Trong form thêm nhân viên mới, nhập tên `receptionist_dn_01`.
  2. Chọn vai trò: `Receptionist`.
  3. Quan sát giao diện chọn chi nhánh: Giao diện tự động chuyển sang chế độ chọn đơn (Single select / Radio button).
  4. Cố tình gửi request API trực tiếp với `hotelIds = [HTL-01, HTL-02]`:
     ```bash
     curl -X POST https://localhost:44305/api/app/identity-user-management \
       -H "Authorization: Bearer <TOKEN>" \
       -H "Content-Type: application/json" \
       -d '{"userName":"rec_test","email":"rec@test.com","password":"P@ssword2026!#","roleNames":["Receptionist"],"hotelIds":["HTL-01","HTL-02"]}'
     ```
- **Kết quả kỳ vọng**:
  - API trả về mã lỗi `400 Bad Request`.
  - Thông báo lỗi: `Nhân viên Lễ tân chỉ được phân công duy nhất 1 cơ sở khách sạn tại một thời điểm.`

---

### Kịch Bản 3: Đổi Mật Khẩu Bắt Buộc Ở Lần Đăng Nhập Đầu Tiên

- **Mục tiêu**: Xác thực cờ `ShouldChangePasswordOnNextLogin = true` ngăn chặn tác nghiệp cho đến khi nhân viên đổi mật khẩu cá nhân.
- **Các bước thực hiện**:
  1. Đăng xuất khỏi tài khoản Quản trị viên.
  2. Tại trang Đăng nhập (`/auth/login`), nhập tài khoản mới tạo: `manager_danang_hoian` / `P@ssword2026!#`.
  3. Nhấn Đăng nhập.
- **Kết quả kỳ vọng**:
  - Hệ thống xác thực thành công nhưng không chuyển hướng vào Dashboard.
  - Router Guard tự động điều hướng tới `/auth/change-password` kèm thông báo: *"Đây là lần đăng nhập đầu tiên. Bạn vui lòng đổi mật khẩu mới để bảo mật tài khoản."*
  - Người dùng nhập mật khẩu mới: `NewSecret2026!#` $\rightarrow$ Hệ thống cập nhật mật khẩu, hạ cờ `ShouldChangePasswordOnNextLogin = false` và đưa người dùng vào `HotelManagerLayoutComponent`.

---

### Kịch Bản 4: Chống Leo Thang Đặc Quyền (Anti-Privilege Escalation Test)

- **Mục tiêu**: Xác thực Tenant Admin không thể tự cấp quyền `SystemAdmin` hoặc can thiệp cấp Host.
- **Các bước thực hiện**:
  1. Đăng nhập bằng tài khoản Tenant Admin.
  2. Mở Console trình duyệt hoặc Postman, gửi request POST tạo tài khoản với vai trò `SystemAdmin`:
     ```bash
     curl -X POST https://localhost:44305/api/app/identity-user-management \
       -H "Authorization: Bearer <TENANT_ADMIN_TOKEN>" \
       -H "Content-Type: application/json" \
       -d '{"userName":"hacker_admin","email":"hack@host.com","password":"P@ssword2026!#","roleNames":["SystemAdmin"],"hotelIds":[]}'
     ```
- **Kết quả kỳ vọng**:
  - Backend phát hiện vi phạm Whitelist, từ chối thực thi với mã lỗi `403 Forbidden`.
  - Mã lỗi nghiệp vụ: `BookingHotel:CannotAssignUnauthorizedRole`.
  - Không có tài khoản nào được tạo trong CSDL; sự kiện được ghi vào `AuditLogs`.

---

### Kịch Bản 5: Mở Khóa Tài Khoản Bị Khóa Do Brute-Force (`UnlockUser`)

- **Mục tiêu**: Kiểm tra quy trình mở khóa tài khoản nhân sự bị khóa an ninh.
- **Các bước thực hiện**:
  1. Dùng tài khoản `receptionist_dn_01` cố tình nhập sai mật khẩu liên tiếp 18+ lần tại form đăng nhập cho đến khi hệ thống báo tài khoản bị vô hiệu hóa.
  2. Đăng nhập lại bằng tài khoản `Tenant Admin`, mở màn hình Quản lý nhân sự.
  3. Tìm tài khoản `receptionist_dn_01`:
     - Huy hiệu hiển thị màu đỏ: `Bị khóa an ninh (Brute-Force)`.
     - Nút tác vụ hiển thị biểu tượng mở khóa: **"Mở khóa tài khoản"** (`Unlock`).
  4. Nhấn nút "Mở khóa tài khoản".
  5. Hộp thoại xác nhận `BhModalComponent` hiển thị $\rightarrow$ Nhấn **"Xác nhận mở khóa"**.
- **Kết quả kỳ vọng**:
  - Hệ thống gọi API `POST /api/app/identity-user-management/{id}/unlock`.
  - Thông báo Toast hiển thị thành công: *"Tài khoản đã được mở khóa an ninh thành công."*
  - Trạng thái tài khoản chuyển về xanh: `Đang hoạt động (Active)`.
  - Nhân viên dùng lại tài khoản và đăng nhập thành công ngay lập tức.

---

### Kịch Bản 6: Lưu Vết Lịch Sử Khi Hủy Phân Công Chi Nhánh (Soft Unlinking Test)

- **Mục tiêu**: Đảm bảo khi gỡ bỏ cơ sở, liên kết được đánh dấu `IsActive = false` và không bị xóa cứng.
- **Các bước thực hiện**:
  1. Tenant Admin mở hồ sơ Quản lý `manager_danang_hoian`, bỏ tích chọn cơ sở `Khách sạn Phố Cổ Hội An` (chỉ giữ lại `Khách sạn Biển Đà Nẵng`).
  2. Nhấn "Lưu lại".
  3. Kiểm tra trong cơ sở dữ liệu:
     ```sql
     SELECT Id, UserId, HotelId, IsActive, AssignedAt, UnassignedAt, IsDeleted 
     FROM UserHotelAssignments 
     WHERE UserId = '<MANAGER_USER_ID>';
     ```
- **Kết quả kỳ vọng**:
  - Bản ghi liên kết với `Khách sạn Phố Cổ Hội An` có:
    - `IsActive = 0 (false)`
    - `UnassignedAt IS NOT NULL`
    - `IsDeleted = 0 (false)`
  - Bản ghi liên kết với `Khách sạn Biển Đà Nẵng` có:
    - `IsActive = 1 (true)`
    - `UnassignedAt IS NULL`
  - Quản lý không còn nhìn thấy chi nhánh Hội An trong bộ chọn Topbar, nhưng các hóa đơn do Quản lý này duyệt trước đây tại Hội An vẫn giữ nguyên liên kết kiểm toán.

---

## 3. Lệnh Chạy Kiểm Thử Tự Động (Automated Test Execution)

Chạy bộ kiểm thử tự động xUnit cho phân hệ quản trị tài khoản và phân bổ chi nhánh:

```powershell
# Chạy toàn bộ Unit Tests của Application layer
dotnet test BookingHotel/test/BookingHotel.Application.Tests/BookingHotel.Application.Tests.csproj --filter "FullyQualifiedName~Identity"
```

