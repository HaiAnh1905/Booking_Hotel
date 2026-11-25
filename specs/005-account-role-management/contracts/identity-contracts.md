# API & Interface Contracts: RM-005 - Quản Lý Tài Khoản & Phân Quyền Vai Trò

**Feature Branch**: `005-account-role-management`  
**Date**: 2026-11-13  
**Status**: Completed  
**Spec**: [specs/005-account-role-management/spec.md](spec.md)

---

## 1. Danh Mục REST API Endpoints

Tất cả các API dưới đây đều yêu cầu xác thực Bearer JWT Token và thuộc nhóm dịch vụ `IdentityUserManagement`.

| Phương thức | Đường dẫn API | Quyền yêu cầu (Permission) | Mô tả |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/app/identity-user-management` | `AbpIdentity.Users` | Lấy danh sách người dùng phân trang, lọc theo vai trò, chi nhánh và trạng thái. |
| `GET` | `/api/app/identity-user-management/{id}` | `AbpIdentity.Users` | Lấy thông tin chi tiết một người dùng kèm danh sách chi nhánh đang phụ trách. |
| `POST` | `/api/app/identity-user-management` | `AbpIdentity.Users.Create` | Tạo mới tài khoản nhân sự kèm gán vai trò và phân bổ chi nhánh. |
| `PUT` | `/api/app/identity-user-management/{id}` | `AbpIdentity.Users.Update` | Cập nhật thông tin nhân sự và điều chỉnh chi nhánh phân công. |
| `DELETE` | `/api/app/identity-user-management/{id}` | `AbpIdentity.Users.Delete` | Xóa mềm tài khoản nhân viên (Soft Delete). |
| `POST` | `/api/app/identity-user-management/{id}/unlock` | `BookingHotel.Identity.UnlockUser` | Mở khóa an ninh tài khoản bị khóa do Brute-force. |
| `POST` | `/api/app/identity-user-management/{id}/toggle-status` | `AbpIdentity.Users.Update` | Kích hoạt hoặc tạm khóa tài khoản (`IsActive = !IsActive`). |
| `GET` | `/api/app/identity-user-management/assignable-roles` | `AbpIdentity.Users` | Lấy danh sách vai trò hợp lệ được phép gán (đã áp dụng Whitelist). |

---

## 2. Chi Tiết Các Data Transfer Objects (DTOs)

### 2.1. `CreateUserWithAssignmentDto`

DTO dùng để tạo tài khoản nhân sự mới kèm phân công cơ sở:

```json
{
  "userName": "receptionist_danang_01",
  "name": "Nguyễn Văn",
  "surname": "An",
  "email": "an.nguyen@dananghotel.com",
  "phoneNumber": "0912345678",
  "password": "Password123*#",
  "roleNames": [
    "Receptionist"
  ],
  "hotelIds": [
    "3fa85f64-5717-4562-b3fc-2c963f66afa6"
  ],
  "primaryHotelId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

- **Quy tắc Kiểm tra Tính hợp lệ (Validation Rules)**:
  - `userName`: Bắt buộc, độ dài từ 3 đến 256 ký tự.
  - `email`: Bắt buộc, định dạng email hợp lệ, độ dài tối đa 256 ký tự.
  - `password`: Bắt buộc, tối thiểu 8 ký tự, chứa ít nhất 1 chữ hoa, 1 chữ thường, 1 chữ số và 1 ký tự đặc biệt.
  - `roleNames`: Bắt buộc ít nhất 1 vai trò. Nếu người gọi là Tenant Admin, danh sách này **chỉ được phép** chứa `HotelManager` hoặc `Receptionist`.
  - `hotelIds`:
    - Nếu vai trò là `Receptionist`: Bắt buộc có đúng **1 phần tử** (ràng buộc 1-1 cố định).
    - Nếu vai trò là `HotelManager`: Chứa 1 hoặc nhiều phần tử (hỗ trợ 1-n).
  - Backend tự động thiết lập `ShouldChangePasswordOnNextLogin = true`.

---

### 2.2. `UpdateUserWithAssignmentDto`

DTO cập nhật thông tin và tái phân bổ chi nhánh:

```json
{
  "name": "Nguyễn Văn",
  "surname": "An",
  "email": "an.nguyen@dananghotel.com",
  "phoneNumber": "0987654321",
  "roleNames": [
    "Receptionist"
  ],
  "hotelIds": [
    "b8c6e3b2-4d1f-4a8e-9021-e7c65f98a123"
  ],
  "primaryHotelId": "b8c6e3b2-4d1f-4a8e-9021-e7c65f98a123",
  "concurrencyStamp": "a1b2c3d4..."
}
```

- **Hành vi xử lý (Behavior)**:
  - Các chi nhánh cũ không còn nằm trong `hotelIds` sẽ được cập nhật `IsActive = false` và `UnassignedAt = Clock.Now`.
  - Các chi nhánh mới được thêm vào với `IsActive = true` và `AssignedAt = Clock.Now`.
  - Chi nhánh đã tồn tại giữ nguyên trạng thái.

---

### 2.3. `UserWithAssignmentDto`

DTO trả về thông tin chi tiết của người dùng:

```json
{
  "id": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "userName": "receptionist_danang_01",
  "name": "Nguyễn Văn",
  "surname": "An",
  "email": "an.nguyen@dananghotel.com",
  "phoneNumber": "0912345678",
  "isActive": true,
  "lockoutEnd": null,
  "isLockedOut": false,
  "shouldChangePasswordOnNextLogin": true,
  "roles": [
    {
      "id": "e4d29f84-1845-42f8-9a91-4c6e28945ef1",
      "name": "Receptionist"
    }
  ],
  "assignedHotels": [
    {
      "hotelId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "hotelName": "Khách sạn Biển Đà Nẵng",
      "hotelCode": "HTL-DN-01",
      "isPrimary": true,
      "assignedAt": "2026-11-13T08:00:00Z"
    }
  ],
  "creationTime": "2026-11-13T08:00:00Z"
}
```

---

### 2.4. `UnlockUserDto` (API Mở khóa)

- **Request**:
  - `POST /api/app/identity-user-management/{id}/unlock`
  - Body: trống hoặc chứa lý do mở khóa:
  ```json
  {
    "reason": "Nhân viên quên mật khẩu khi giao ca và đã xác minh nhân thân trực tiếp."
  }
  ```
- **Response Success (200 OK)**:
  ```json
  {
    "success": true,
    "userId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
    "unlockedAt": "2026-11-13T08:30:00Z",
    "message": "Tài khoản đã được mở khóa an ninh thành công."
  }
  ```

---

## 3. Mã Lỗi Nghiệp Vụ Chuẩn Hóa (Standard Error Codes)

| Mã lỗi HTTP | Error Code | Thông báo hiển thị (`vi.json` / `en.json`) |
| :--- | :--- | :--- |
| `400 Bad Request` | `BookingHotel:ReceptionistSingleHotelRequired` | Nhân viên Lễ tân chỉ được phân công duy nhất 1 cơ sở khách sạn tại một thời điểm. |
| `403 Forbidden` | `BookingHotel:CannotAssignUnauthorizedRole` | Bạn không có thẩm quyền gán vai trò này cho người dùng. |
| `403 Forbidden` | `BookingHotel:CrossTenantOperationForbidden` | Thao tác trên tài khoản thuộc tổ chức/tenant khác bị từ chối truy cập. |
| `404 Not Found` | `BookingHotel:HotelNotFound` | Cơ sở khách sạn được chỉ định không tồn tại trong tổ chức của bạn. |
| `400 Bad Request` | `BookingHotel:WeakPassword` | Mật khẩu chưa đạt tiêu chuẩn bảo mật (tối thiểu 8 ký tự, gồm chữ hoa, chữ thường, số và ký tự đặc biệt). |
| `400 Bad Request` | `BookingHotel:UserAlreadyActive` | Tài khoản này đang ở trạng thái hoạt động bình thường, không cần mở khóa. |

