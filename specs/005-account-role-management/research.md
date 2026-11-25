# Technical Research & Architecture Decisions: RM-005 - Quản Lý Tài Khoản & Phân Quyền Vai Trò

**Feature Branch**: `005-account-role-management`  
**Date**: 2026-11-13  
**Status**: Completed  
**Spec**: [specs/005-account-role-management/spec.md](spec.md)

---

## 1. Nghiên Cứu Mở Rộng Module ABP Identity (Identity Extension Strategy)

### Bối cảnh & Vấn đề
ABP Framework 10.7 cung cấp sẵn module `Volo.Abp.Identity` với các thực thể cốt lõi `IdentityUser`, `IdentityRole`, `IdentityUserManager`, và `IdentityUserAppService`. Tuy nhiên, nghiệp vụ khách sạn đòi hỏi:
- Phân bổ chi nhánh làm việc (`UserHotelAssignment`).
- Ràng buộc vai trò chặt chẽ theo phân tầng Multi-Tenancy (Tenant Admin chỉ được gán `HotelManager` hoặc `Receptionist`).
- Quy định 1-1 cho Lễ tân và 1-n cho Quản lý.
- Mở khóa an ninh Brute-Force (`UnlockUser`) kết hợp xóa cache.
- Bắt buộc đổi mật khẩu ở lần đăng nhập đầu tiên (`ShouldChangePasswordOnNextLogin`).

### Quyết định Kiến trúc
- **Giải pháp**: Xây dựng service mở rộng `IdentityUserManagementAppService` kế thừa `ApplicationService` và điều phối trực tiếp `IdentityUserManager`, `IdentityRoleManager`, `IRepository<UserHotelAssignment>`, và `IBruteForceManager`.
- **Lý do lựa chọn**:
  - Tận dụng 100% tính năng an toàn có sẵn của ABP: Password hashing (PBKDF2/Argon2), Security Stamp, Two-Factor Authentication, Audit Logging và OpenIddict Token Generation.
  - Không phá vỡ cấu trúc bảng `AbpUsers` hay xung đột với các module khác của hệ thống.
  - Dễ dàng mở rộng DTOs chuyên biệt cho nghiệp vụ khách sạn (`CreateUserWithAssignmentDto`, `UserWithAssignmentDto`).
- **Các giải pháp thay thế đã loại bỏ**:
  - *Viết bảng người dùng riêng hoàn toàn*: Bị loại vì lặp lại mã nguồn, tốn công bảo trì và mất tích hợp với OpenIddict.
  - *Chỉ validate trên Angular Frontend*: Bị loại vì vi phạm Hiến pháp Điều khoản VI (Backend-Authoritative).

---

## 2. Mô Hình Phân Bổ Chi Nhánh Khách Sạn (UserHotelAssignment Architecture)

### Bối cảnh & Vấn đề
Mỗi nhân viên khách sạn cần được giới hạn phạm vi truy cập dữ liệu (Data Scoping):
- `HotelManager`: Quản lý 1 hoặc nhiều chi nhánh trong cùng Tenant (`1-n`).
- `Receptionist`: Gắn cố định duy nhất 1 chi nhánh tại một thời điểm (`1-1`).
- Khi chuyển chi nhánh hoặc hủy phân công, hệ thống cần xử lý như thế nào để bảo toàn dữ liệu lịch sử?

### Quyết định Kiến trúc (Theo Clarifications Q2 & Q6)
- **Giải pháp**: Nâng cấp thực thể `UserHotelAssignment` kế thừa `FullAuditedEntity<Guid>` kết hợp giao diện `IMultiTenant`:
  - `IsActive` (bool): `true` = đang có hiệu lực; `false` = đã hủy phân công/ngừng phụ trách.
  - `AssignedAt` (DateTime): Thời điểm bắt đầu phân công.
  - `UnassignedAt` (DateTime?): Thời điểm hủy phân công / thu hồi quyền.
- **Cơ chế lưu vết lịch sử (Audit-tracked assignment)**:
  - Khi Tenant Admin cập nhật danh sách chi nhánh mới cho nhân viên, các bản ghi chi nhánh bị loại bỏ sẽ được cập nhật `IsActive = false` và `UnassignedAt = Clock.Now`.
  - Tuyệt đối **không xóa cứng (Hard Delete)** bản ghi liên kết khỏi cơ sở dữ liệu.
  - Các truy vấn tác nghiệp buồng phòng hiện tại chỉ lọc trên các bản ghi có `IsActive == true`. Toàn bộ đơn đặt phòng, hóa đơn Folio mà nhân viên đã lập tại cơ sở trước đây vẫn đối chiếu được về ID nhân viên và cơ sở lịch sử mà không bị đứt gãy dữ liệu (Foreign Key Integrity).

---

## 3. Chính Sách Khởi Tạo Mật Khẩu & Đổi Mật Khẩu Lần Đầu (Initial Password & First-Login)

### Bối cảnh & Vấn đề
Theo Hiến pháp Điều khoản IX, tuyệt đối không được điền sẵn mật khẩu (*Zero Credential Pre-filling*). Đồng thời, trong Wave 0, hệ thống chưa bắt buộc cấu hình hạ tầng gửi email SMTP bên ngoài.

### Quyết định Kiến trúc (Theo Clarifications Q1 & Q5)
- **Giải pháp**:
  - Khi Tenant Admin tạo tài khoản nhân sự:
    - Tenant Admin tự nhập mật khẩu ban đầu trực tiếp trên form.
    - Mật khẩu phải vượt qua bộ kiểm tra chính sách mật khẩu mạnh (>= 8 ký tự, 1 chữ hoa, 1 chữ thường, 1 chữ số, 1 ký tự đặc biệt) đồng bộ với RM-002.
    - Hệ thống tự động gán cờ `ShouldChangePasswordOnNextLogin = true` cho tài khoản người dùng (`IdentityUser`).
  - Khi nhân viên đăng nhập lần đầu tiên:
    - API đăng nhập (`AuthAppService`) trả về JWT Token chứa claim `should_change_password: true`.
    - Angular Route Guard nhận diện claim này và tự động chuyển hướng người dùng tới màn hình `/auth/change-password`.
    - Người dùng bị chặn hoàn toàn khỏi việc truy cập các chức năng vận hành cho đến khi hoàn tất đổi mật khẩu cá nhân mới.
- **Lý do lựa chọn**: Bảo đảm tính an toàn bảo mật cao nhất, trao quyền kiểm soát bí mật tài khoản cho nhân viên và loại bỏ sự phụ thuộc vào máy chủ gửi mail trong giai đoạn nền tảng Wave 0.

---

## 4. Tích Hợp Mở Khóa Tài Khoản & Bộ Phòng Thủ Brute-Force (Unlock User Integration)

### Bối cảnh & Vấn đề
Trong tính năng RM-001, hệ thống đã trang bị cơ chế Brute-force phân tầng:
- Sau 15 lần sai siết còn 3 lần/chu kỳ; sau 30 phút sai tiếp 3 lần sẽ vô hiệu hóa tài khoản (`IsActive = false`).
- Tài khoản bị vô hiệu hóa cần cơ chế mở khóa an toàn bởi người có thẩm quyền.

### Quyết định Kiến trúc (Theo Clarifications Q3)
- **Giải pháp**:
  - Cung cấp phương thức `UnlockUserAsync(Guid userId)` được bảo vệ bởi quyền `BookingHotelPermissions.Identity.UnlockUser`.
  - Phân định thẩm quyền 2 tầng nghiêm ngặt:
    - System Admin (`TenantId == null`): Chỉ mở khóa cho tài khoản cấp Host (`user.TenantId == null`).
    - Tenant Admin (`TenantId == CurrentTenant.Id`): Chỉ mở khóa cho tài khoản nhân sự thuộc Tenant của mình (`user.TenantId == CurrentTenant.Id`).
  - Thao tác mở khóa thực hiện tuần tự các bước:
    1. Cập nhật `user.SetIsActive(true)`.
    2. Đặt lại thời gian khóa `user.LockoutEnd = null`.
    3. Đặt lại số lần đếm sai `user.ResetAccessFailedCount()`.
    4. Gọi `BruteForceManager.ResetAttemptsAsync(user.Email)` để xóa toàn bộ lịch sử vi phạm trong Distributed Cache (`IDistributedCache`).
    5. Ghi nhật ký an ninh vào bảng `SecurityLogs` với nội dung `Action = "UserUnlockedByAdmin"`.
- **Lý do lựa chọn**: Khôi phục trạng thái hoạt động tức thì cho nhân viên mà không cần khởi động lại dịch vụ hoặc can thiệp thủ công vào cơ sở dữ liệu.

---

## 5. Tùy Biến Giao Diện Identity Chuẩn Concept Quản Trị RM-004 (Angular Architecture)

### Bối cảnh & Vấn đề
Mô-đun `@abp/ng.identity` mặc định của ABP sử dụng bảng và popup tiêu chuẩn của theme LeptonX, không phản ánh đúng nhận diện thương hiệu của dự án `Booking Hotel` (Hiến pháp Điều khoản VII & VIII) và không có giao diện chọn chi nhánh phân công.

### Quyết định Kiến trúc
- **Giải pháp**: Xây dựng bộ Standalone Components độc lập đặt tại `angular/src/app/identity/`:
  - `UserListComponent`:
    - Tích hợp bên trong `ManagementLayoutComponent`.
    - Sử dụng `BhDataTableComponent` hiển thị danh sách người dùng phân trang, tìm kiếm theo tên/email, lọc theo vai trò và chi nhánh.
    - Hiển thị huy hiệu trạng thái `BhStatusBadgeComponent` (Xanh cho `Active`, Đỏ cho `Locked/Inactive`).
    - Nút tác vụ nổi bật: Sửa, Mở khóa tài khoản (chỉ hiện khi bị khóa), Xóa mềm.
  - `UserModalComponent`:
    - Sử dụng `BhModalComponent` với backdrop mờ và phím ESC an toàn.
    - Dropdown chọn vai trò lọc nghiêm ngặt (chỉ hiển thị `HotelManager` và `Receptionist` khi người dùng là Tenant Admin).
    - Bộ chọn chi nhánh khách sạn:
      - Khi chọn `HotelManager`: Cho phép chọn nhiều chi nhánh (checkbox list hoặc multi-select).
      - Khi chọn `Receptionist`: Khóa cứng chỉ cho phép chọn duy nhất 1 chi nhánh (radio list hoặc single-select dropdown).
    - Mật khẩu: Hai ô nhập Mật khẩu và Xác nhận mật khẩu kèm thanh kiểm tra checklist mật khẩu mạnh trực quan.
  - Dịch vụ thông báo Toastr `BhToastrService` phát thông báo thành công hoặc cảnh báo lỗi vi phạm.
- **Tuân thủ WCAG 2.1 AA**: Mọi văn bản trên nền đen/sáng đều đảm bảo tỷ lệ tương phản $\ge 4.5:1$.

---

## 6. Chống Leo Thang Đặc Quyền (Anti-Privilege Escalation & Whitelist Validation)

### Bối cảnh & Vấn đề
Lỗ hổng leo thang đặc quyền (Privilege Escalation) xảy ra khi một quản trị viên cấp dưới (Tenant Admin) cố tình gán quyền quản trị cao nhất (`SystemAdmin`) cho chính mình hoặc tạo tài khoản có quyền cấp Host để can thiệp vào toàn bộ nền tảng SaaS.

### Quyết định Kiến trúc
- **Giải pháp**: Áp dụng quy tắc kiểm tra Whitelist đa tầng tại Backend:
  ```csharp
  if (CurrentTenant.Id.HasValue)
  {
      // Ngăn chặn gán vai trò ngoài thẩm quyền của Tenant
      var allowedRoles = new[] { BookingHotelRoleConsts.HotelManager, BookingHotelRoleConsts.Receptionist };
      foreach (var roleName in input.RoleNames)
      {
          if (!allowedRoles.Contains(roleName))
          {
              throw new BusinessException("BookingHotel:CannotAssignUnauthorizedRole")
                  .WithData("RoleName", roleName);
          }
      }

      // Ngăn chặn thao tác trên tài khoản thuộc Tenant khác hoặc cấp Host
      if (user.TenantId != CurrentTenant.Id)
      {
          throw new BusinessException("BookingHotel:CrossTenantOperationForbidden");
      }
  }
  ```
- **Bảo vệ 5 vai trò chuẩn**: Cả 5 vai trò `SystemAdmin`, `TenantAdmin`, `HotelManager`, `Receptionist`, `Customer` đều được thiết lập thuộc tính `IsStatic = true` trong CSDL, ngăn chặn hoàn toàn việc xóa vai trò hoặc thay đổi tên vai trò chuẩn.

