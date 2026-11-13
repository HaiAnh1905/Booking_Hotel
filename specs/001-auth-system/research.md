# Technical Research: RM-001 - Xác Thực & Đăng Nhập/Đăng Ký Hệ Thống

**Feature**: `001-auth-system`  
**Date**: 2026-11-11  
**Status**: Completed  

---

## 1. Kiến Trúc Xác Thực (Authentication Engine)

### Bối cảnh & Hiện trạng hệ thống
- Dự án `BookingHotel` sử dụng **.NET 10** và **ABP Framework 10.7**.
- Project `BookingHotel.HttpApi.Host` đã tích hợp sẵn module `AbpAccountWebOpenIddictModule` và `OpenIddict.Server.AspNetCore`.
- CSDL quan hệ lưu trữ thực thể `IdentityUser` thông qua `BookingHotelDbContext`.

### Quyết định Kỹ thuật:
- **Đăng nhập bằng Email**: Mặc định ASP.NET Core Identity hỗ trợ tìm kiếm người dùng bằng cả `UserName` và `NormalizedEmail`. Chúng ta sẽ triển khai `IAuthAppService.LoginWithEmailAsync` trong tầng `Application`:
  - Tiếp nhận `Email` và `Password`.
  - Chuẩn hóa email (`NormalizeEmail`), truy vấn `IdentityUser` qua `IIdentityUserRepository` hoặc `UserManager<IdentityUser>`.
  - Kiểm tra trạng thái tài khoản: `IsActive == true`, không bị khóa `LockoutEnd <= DateTimeOffset.UtcNow`.
  - Sinh JWT token an toàn thông qua OpenIddict Server hoặc ABP token generation pipeline.

---

## 2. Cơ Chế Chống Brute-Force Phân Tầng (Tiered Lockout Algorithm)

### Yêu cầu đặc tả:
- **Giai đoạn 1 (Lần 1 - 15)**: Cho phép sai tối đa 5 lần mỗi chu kỳ phạt:
  - Sai lần 5: Khóa 5 phút.
  - Sai lần 10: Khóa 10 phút.
  - Sai lần 15: Khóa 15 phút.
- **Giai đoạn 2 (Từ lần 15 trở đi)**: Ngưỡng sai cho phép tự động **giảm xuống còn 3 lần** mỗi chu kỳ phạt:
  - Sai lần 18 (+3 lần): Khóa 20 phút.
  - Sai lần 21 (+3 lần): Khóa 25 phút.
  - Sai lần 24 (+3 lần): Khóa tối đa 30 phút.
- **Vô hiệu hóa vĩnh viễn**: Nếu sau khi mở khóa ở mốc 30 phút mà tiếp tục sai đủ 3 lần (lần 27) $\rightarrow$ Ngay lập tức đặt `IsActive = false`.

### Quyết định Kỹ thuật:
- **Tạo Domain Service riêng**: `BruteForceManager` (trong `BookingHotel.Domain/Identity/BruteForceManager.cs`).
- **Mở rộng thuộc tính `IdentityUser`**:
  - `FailedAttemptsInCurrentCycle` (int): Số lần sai tích lũy trong chu kỳ hiện tại.
  - `TotalFailedAttempts` (int): Tổng số lần sai liên tiếp từ trước đến nay.
  - `CurrentLockoutMinutes` (int): Mức phạt khóa hiện tại (0, 5, 10, 15, 20, 25, 30).
  - `LockoutEnd` (DateTimeOffset?): Đã có sẵn trong `IdentityUser`.
  - `IsActive` (bool): Đã có sẵn trong ABP `IdentityUser`.
  - `DeactivatedReason` (string?): Lý do vô hiệu hóa.
- **Tránh Race Condition & Giả mạo**: Mọi tính toán diễn ra trong Transaction Backend, ghi nhận đồng thời `IdentitySecurityLog` của ABP để kiểm toán bảo mật.

---

## 3. Xác Thực Google (OAuth 2.0 / OpenID Connect)

### Quyết định Kỹ thuật:
- Cấu hình Google External Provider trong `BookingHotelHttpApiHostModule.cs`:
  ```csharp
  context.Services.AddAuthentication()
      .AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
      {
          options.ClientId = configuration["Authentication:Google:ClientId"];
          options.ClientSecret = configuration["Authentication:Google:ClientSecret"];
      });
  ```
- Cung cấp API endpoint chuyên dụng cho SPA:
  - Nhận `idToken` từ Google Sign-In SDK phía Angular.
  - Backend sử dụng `Google.Apis.Auth` (`GoogleJsonWebSignature.ValidateAsync(idToken)`) để xác thực token từ Google.
  - Nếu Email đã tồn tại $\rightarrow$ Đăng nhập ngay.
  - Nếu Email mới $\rightarrow$ Bắt đầu luồng đăng ký Onboarding 3 bước với thông tin Email và Tên đã trích xuất từ Google.

---

## 4. Quản Lý Phiên Đăng Ký Đa Bước (Multi-step Onboarding)

### Thách thức:
- Đăng ký chia làm 3 bước tuần tự (Bước 1: Email $\rightarrow$ Bước 2: Tên $\rightarrow$ Bước 3: SĐT).
- Làm sao ngăn chặn người dùng bỏ qua bước (Step Skipping) hoặc giả mạo dữ liệu giữa các bước?

### Quyết định Kỹ thuật:
- Sử dụng **`IDistributedCache<UserRegistrationStateCacheItem>`** của ABP:
  - Khi hoàn tất Bước 1: Backend sinh một `RegistrationToken` (Guid ngẫu nhiên dạng mã hóa) làm Cache Key với thời gian sống (TTL) 15 phút.
  - Cache Item lưu trữ: `Email`, `PasswordHash` (nếu là đăng ký thường) hoặc `GoogleSub` (nếu là Google), `CurrentStep = Step1Completed`.
  - Bước 2: Client gửi `RegistrationToken` + `FullName`. Backend kiểm tra token hợp lệ và `CurrentStep == Step1Completed` $\rightarrow$ lưu `FullName`, cập nhật `CurrentStep = Step2Completed`.
  - Bước 3: Client gửi `RegistrationToken` + `PhoneNumber`. Backend kiểm tra `CurrentStep == Step2Completed` $\rightarrow$ tạo bản ghi `IdentityUser` trong CSDL $\rightarrow$ Xóa Cache Item $\rightarrow$ Cấp JWT token đăng nhập.

---

## 5. Chức Năng Quên Mật Khẩu & Kiểm Tra Captcha

### Thách thức:
- Ngăn chặn bot tự động spam API gửi hàng loạt email gây quá tải và đưa domain của khách sạn vào danh sách đen (Blacklist Spam).

### Quyết định Kỹ thuật:
- **Dịch vụ Captcha Validator** (`ICaptchaValidator` trong `BookingHotel.Domain/Security/ICaptchaValidator.cs`):
  - Hỗ trợ **Cloudflare Turnstile** (ưu tiên hàng đầu: không làm phiền người dùng, bảo mật cực cao) và tương thích fallback với **Google reCAPTCHA v3**.
  - Client gửi `CaptchaToken` kèm `Email`.
  - Backend gửi HTTP request ngầm (server-to-server) đến `https://challenges.cloudflare.com/turnstile/v0/siteverify` hoặc `https://www.google.com/recaptcha/api/siteverify` với `SecretKey`.
  - Nếu Captcha không hợp lệ $\rightarrow$ Trả về `400 Bad Request` ngay lập tức, không thực hiện bất kỳ thao tác CSDL hay gửi mail nào.
- **Password Reset Token**:
  - Tận dụng `UserManager.GeneratePasswordResetTokenAsync(user)` của ASP.NET Core Identity.
  - Gửi email qua `IEmailSender` của ABP với liên kết có chứa token (mã hóa URL-safe).
  - Link chỉ có hiệu lực tối đa **15 phút**.

---

## 6. Giao Diện & Hệ Màu Concept (UI/UX Theming)

### Yêu cầu Hiến pháp (Nguyên tắc VII):
- **Portal Khách hàng (Customer Booking Portal)**:
  - Màu chủ đạo: **Đỏ sẫm (`#990000`)** phối **Đen (`#000000`)** trên nền sáng ấm sang trọng.
  - Phong cách: Tối giản, tập trung vào form nhập, có chỉ báo tiến trình trực quan cho 3 bước đăng ký (Step 1 of 3: Thông tin tài khoản $\rightarrow$ Step 2 of 3: Họ và tên $\rightarrow$ Step 3 of 3: Số điện thoại liên hệ).
- **Phân tách giao diện Angular**:
  - `src/app/auth/login/`
  - `src/app/auth/register/` (component đa bước: `step1-account`, `step2-name`, `step3-phone`)
  - `src/app/auth/forgot-password/` (kèm Turnstile/reCAPTCHA widget)
  - `src/app/auth/reset-password/`

