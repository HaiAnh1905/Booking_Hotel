# Quickstart & Verification Guide: RM-001 (Auth System)

**Feature**: `001-auth-system`  
**Date**: 2026-11-11  

---

## 1. Cấu Hình Môi Trường Cần Thiết (`appsettings.json`)

Trong file `src/BookingHotel.HttpApi.Host/appsettings.json`, bổ sung các cấu hình sau:

```json
{
  "Authentication": {
    "Google": {
      "ClientId": "YOUR_GOOGLE_CLIENT_ID.apps.googleusercontent.com",
      "ClientSecret": "YOUR_GOOGLE_CLIENT_SECRET"
    }
  },
  "Captcha": {
    "Provider": "CloudflareTurnstile", // hoặc "GoogleReCaptcha"
    "SiteKey": "0x4AAAAAA...",
    "SecretKey": "0x4AAAAAA...",
    "VerifyUrl": "https://challenges.cloudflare.com/turnstile/v0/siteverify"
  }
}
```

---

## 2. Các Bước Triển Khai Kiểm Thử Tự Động (Integration Tests)

### Kịch bản 1: Kiểm thử phòng thủ Brute-Force phân tầng
```bash
dotnet test test/BookingHotel.Application.Tests --filter "FullyQualifiedName~BruteForce_TieredLockout_Tests"
```
1. Tạo user test `test_bruteforce@bookinghotel.com`.
2. Gửi 4 request login sai: Kỳ vọng trả về `401 Unauthorized` kèm số lần thử còn lại giảm dần từ 4 về 1.
3. Gửi request login sai lần thứ 5: Kỳ vọng `423 Locked` và `LockoutEnd` sau đúng 5 phút.
4. Giả lập hết hạn khóa 5 phút (Fast-forward thời gian test).
5. Gửi tiếp 5 request sai liên tiếp: Kỳ vọng lần thứ 10 khóa đúng 10 phút.
6. Giả lập tiếp tục đạt mốc 15 lần sai: Đợt khóa là 15 phút.
7. Sau mốc 15 lần: Kiểm tra ngưỡng mới giảm xuống còn 3 lần:
   - Gửi 3 request sai tiếp theo: Kỳ vọng khóa 20 phút.
   - Gửi 3 request sai tiếp theo: Kỳ vọng khóa 25 phút.
   - Gửi 3 request sai tiếp theo: Kỳ vọng khóa 30 phút (mức trần).
8. Giả lập hết hạn đợt 30 phút: Gửi tiếp 3 request sai:
   - Kỳ vọng: `403 Forbidden` (`IsActive = false`), tài khoản bị vô hiệu hóa hoàn toàn.

---

### Kịch bản 2: Kiểm thử Luồng Đăng ký 3 Bước (Multi-step Onboarding)
```bash
dotnet test test/BookingHotel.Application.Tests --filter "FullyQualifiedName~RegistrationFlow_Tests"
```
1. Gọi `RegisterStep1Async` với email `newuser@hotel.com` và mật khẩu hợp lệ:
   - Kỳ vọng: Trả về `RegistrationToken` và `nextStep = "Step2"`.
2. Thử gọi trực tiếp `RegisterStep3Async` bỏ qua Bước 2:
   - Kỳ vọng: Lỗi `400 Bad Request` (Invalid Step).
3. Gọi `RegisterStep2Async` với Họ tên "Trần Văn B":
   - Kỳ vọng: Trả về `200 OK` và `nextStep = "Step3"`.
4. Gọi `RegisterStep3Async` với SĐT "0987654321":
   - Kỳ vọng: `IdentityUser` được tạo thành công trong DB, trả về JWT Access Token.

---

### Kịch bản 3: Kiểm thử Quên Mật Khẩu với Captcha
```bash
dotnet test test/BookingHotel.Application.Tests --filter "FullyQualifiedName~ForgotPassword_Captcha_Tests"
```
1. Gọi API `SendPasswordResetCodeAsync` với `CaptchaToken = "INVALID_TOKEN"`:
   - Kỳ vọng: `400 Bad Request` ("Xác minh bảo mật Captcha không thành công").
2. Gọi API với Mock Captcha Validator trả về `true`:
   - Kỳ vọng: `200 OK`, hàm gửi email `IEmailSender` được gọi với reset token có hạn 15 phút.

