# API Contract: Quản Lý Nhân Sự & Phân Bổ Vai Trò (Tenant Staff & Role Management)

**Feature**: `007-tenant-admin-management` | **Date**: 2026-11-18  
**Base Route**: `/api/app/identity-user-management`

---

## 1. Endpoints

### 1.1. Lấy danh sách nhân viên toàn tổ chức
- **Method**: `GET`
- **Route**: `/api/app/identity-user-management`
- **Authorization**: `[Authorize(IdentityPermissions.Users.Default)]`
- **Query Parameters**:
  - `Filter` (string, optional): Tìm theo tên, username, email, SĐT.
  - `RoleName` (string, optional): Lọc theo vai trò (`HotelManager`, `Receptionist`).
  - `HotelId` (Guid?, optional): Lọc nhân viên công tác tại chi nhánh cụ thể.
  - `SkipCount` (int, default: 0), `MaxResultCount` (int, default: 10).
- **Response `200 OK`**:
  ```json
  {
    "totalCount": 12,
    "items": [
      {
        "id": "a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d",
        "userName": "manager_hanoi",
        "name": "Văn Quản Lý",
        "surname": "Nguyễn",
        "email": "manager.hanoi@bookinghotel.vn",
        "phoneNumber": "0912345678",
        "isActive": true,
        "isLockedOut": false,
        "roles": ["HotelManager", "Receptionist"],
        "assignedHotels": [
          {
            "hotelId": "e3b0c442-98fc-1c14-9af0-2a3b4c5d6e7f",
            "hotelName": "Booking Hotel Hà Nội Grand",
            "hotelCode": "BH-HAN-01",
            "roleName": "HotelManager",
            "isPrimary": false,
            "isActive": true
          },
          {
            "hotelId": "e3b0c442-98fc-1c14-9af0-2a3b4c5d6e7f",
            "hotelName": "Booking Hotel Hà Nội Grand",
            "hotelCode": "BH-HAN-01",
            "roleName": "Receptionist",
            "isPrimary": true,
            "isActive": true
          }
        ]
      }
    ]
  }
  ```

---

### 1.2. Tạo tài khoản nhân viên mới (Hỗ trợ vai trò đơn hoặc kiêm nhiệm)
- **Method**: `POST`
- **Route**: `/api/app/identity-user-management/tenant-staff`
- **Authorization**: `[Authorize(IdentityPermissions.Users.Create)]`
- **Request Body (`CreateTenantStaffDto`)**:
  ```json
  {
    "userName": "staff_danang",
    "name": "Thị Lễ Tân",
    "surname": "Trần",
    "email": "letan.danang@bookinghotel.vn",
    "phoneNumber": "0987654321",
    "password": "StrongPassword123!@#",
    "roles": ["HotelManager", "Receptionist"],
    "managerHotelIds": ["e3b0c442-98fc-1c14-9af0-2a3b4c5d6e7f"],
    "primaryReceptionistHotelId": "e3b0c442-98fc-1c14-9af0-2a3b4c5d6e7f"
  }
  ```
- **Validation & Anti-Privilege Escalation Rules**:
  - `roles`: Bắt buộc chỉ được chứa các giá trị trong whitelist: `["HotelManager"]`, `["Receptionist"]`, hoặc `["HotelManager", "Receptionist"]`.
  - Nếu `roles` chứa `SystemAdmin` hoặc role bất kỳ ngoài Tenant: Ném ngoại lệ `403 Forbidden` (`CannotAssignPlatformOrHostRoles`).
  - Mật khẩu: Tuân thủ chính sách mật khẩu mạnh ($\ge 8$ ký tự, 1 hoa, 1 thường, 1 số, 1 ký tự đặc biệt).
  - Tự động gán cờ `ShouldChangePasswordOnNextLogin = true`.
  - Nếu có role `Receptionist`, bắt buộc cung cấp `primaryReceptionistHotelId` hợp lệ thuộc Tenant.
- **Response `200 OK`**: Trả về `UserWithAssignmentDto`.

---

### 1.3. Điều chuyển chi nhánh công tác (Transfer Branch)
- **Method**: `POST`
- **Route**: `/api/app/identity-user-management/{id}/transfer-branch`
- **Authorization**: `[Authorize(BookingHotelPermissions.Hotels.AssignStaff)]`
- **Request Body (`TransferBranchDto`)**:
  ```json
  {
    "fromHotelId": "e3b0c442-98fc-1c14-9af0-2a3b4c5d6e7f",
    "toHotelId": "f4c1d553-09ad-2d25-0be1-3b4c5d6e7f8a",
    "roleName": "Receptionist",
    "note": "Điều chuyển nhân sự hỗ trợ khai trương chi nhánh mới."
  }
  ```
- **Xử lý nghiệp vụ**:
  - Bản ghi phân công cũ tại `fromHotelId` được cập nhật `IsActive = false`, `UnassignedAt = Clock.Now`.
  - Tạo bản ghi mới tại `toHotelId` với `IsActive = true`, `AssignedAt = Clock.Now`.
  - Lưu nhật ký kiểm toán không thể xóa sửa.
- **Response `200 OK`**: Thành công.

---

### 1.4. Khóa / Mở khóa & Mở khóa Brute-Force tài khoản nhân viên
- **Method**: `POST`
- **Route**: `/api/app/identity-user-management/{id}/toggle-active`
- **Authorization**: `[Authorize(BookingHotelPermissions.Identity.UnlockUser)]`
- **Response `200 OK`**: Đổi trạng thái `IsActive = true/false`.
- **Mở khóa Brute-Force (`/api/app/identity-user-management/{id}/unlock-brute-force`)**:
  - Reset toàn bộ bộ đếm vi phạm brute-force về 0, xóa `LockoutEnd`, khôi phục quyền đăng nhập ngay lập tức cho nhân viên nội bộ Tenant.
