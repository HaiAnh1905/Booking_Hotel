# API Contracts & Data Transfer Objects (DTOs): RM-001

**Feature**: `001-auth-system`  
**Date**: 2026-11-11  
**Project**: `BookingHotel.Application.Contracts/Auth`  

---

## 1. DTOs Xác Thực & Đăng Nhập

### `LoginWithEmailDto`
```csharp
namespace BookingHotel.Auth.Dtos;

public class LoginWithEmailDto
{
    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = default!;

    [Required]
    [StringLength(128, MinimumLength = 6)]
    public string Password { get; set; } = default!;

    public bool RememberMe { get; set; } = false;
}
```

### `LoginResultDto`
```csharp
namespace BookingHotel.Auth.Dtos;

public class LoginResultDto
{
    public bool IsSuccess { get; set; }
    public string? AccessToken { get; set; }
    public string? TokenType { get; set; } = "Bearer";
    public int ExpiresIn { get; set; }
    public bool IsLockedOut { get; set; }
    public int? RemainingLockoutSeconds { get; set; }
    public int? RemainingAttemptsInCycle { get; set; }
    public bool IsDeactivated { get; set; }
    public string? Message { get; set; }
}
```

---

## 2. DTOs Đăng Ký Đa Bước (Onboarding)

### `RegisterStep1InputDto`
```csharp
namespace BookingHotel.Auth.Dtos;

public class RegisterStep1InputDto
{
    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = default!;

    [Required]
    [StringLength(128, MinimumLength = 8)]
    public string Password { get; set; } = default!;

    [Required]
    [Compare(nameof(Password))]
    public string ConfirmPassword { get; set; } = default!;
}
```

### `GoogleAuthInputDto`
```csharp
namespace BookingHotel.Auth.Dtos;

public class GoogleAuthInputDto
{
    [Required]
    public string IdToken { get; set; } = default!;
}
```

### `RegisterStep1ResultDto`
```csharp
namespace BookingHotel.Auth.Dtos;

public class RegisterStep1ResultDto
{
    public Guid RegistrationToken { get; set; }
    public string Email { get; set; } = default!;
    public string? PrefilledName { get; set; }
    public string NextStep { get; set; } = "Step2";
}
```

### `RegisterStep2InputDto`
```csharp
namespace BookingHotel.Auth.Dtos;

public class RegisterStep2InputDto
{
    [Required]
    public Guid RegistrationToken { get; set; }

    [Required]
    [StringLength(128, MinimumLength = 2)]
    public string FullName { get; set; } = default!;
}
```

### `RegisterStep3InputDto`
```csharp
namespace BookingHotel.Auth.Dtos;

public class RegisterStep3InputDto
{
    [Required]
    public Guid RegistrationToken { get; set; }

    [Required]
    [RegularExpression(@"^(\+84|0)(3[2-9]|5[6|8|9]|7[0|6-9]|8[1-9]|9[0-9])[0-9]{7}$", 
        ErrorMessage = "Số điện thoại không hợp lệ.")]
    [StringLength(20)]
    public string PhoneNumber { get; set; } = default!;
}
```

---

## 3. DTOs Quên & Đặt Lại Mật Khẩu (Kèm Captcha)

### `ForgotPasswordInputDto`
```csharp
namespace BookingHotel.Auth.Dtos;

public class ForgotPasswordInputDto
{
    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = default!;

    [Required]
    public string CaptchaToken { get; set; } = default!;
}
```

### `ResetPasswordInputDto`
```csharp
namespace BookingHotel.Auth.Dtos;

public class ResetPasswordInputDto
{
    [Required]
    public string UserId { get; set; } = default!;

    [Required]
    public string ResetToken { get; set; } = default!;

    [Required]
    [StringLength(128, MinimumLength = 8)]
    public string NewPassword { get; set; } = default!;

    [Required]
    [Compare(nameof(NewPassword))]
    public string ConfirmNewPassword { get; set; } = default!;
}
```

---

## 4. Đặc Tả Giao Diện Dịch Vụ Ứng Dụng (Service Interface)

```csharp
namespace BookingHotel.Auth;

public interface IAuthAppService : IApplicationService
{
    Task<LoginResultDto> LoginWithEmailAsync(LoginWithEmailDto input);

    Task<LoginResultDto> LoginWithGoogleAsync(GoogleAuthInputDto input);

    Task<RegisterStep1ResultDto> RegisterStep1Async(RegisterStep1InputDto input);

    Task<RegisterStep1ResultDto> RegisterGoogleInitAsync(GoogleAuthInputDto input);

    Task RegisterStep2Async(RegisterStep2InputDto input);

    Task<LoginResultDto> RegisterStep3Async(RegisterStep3InputDto input);

    Task SendPasswordResetCodeAsync(ForgotPasswordInputDto input);

    Task ResetPasswordAsync(ResetPasswordInputDto input);
}
```

---

## 5. Danh Mục Mã Lỗi HTTP & Lỗi Nghiệp Vụ

| HTTP Status | Mã lỗi / Tình huống | Mô tả |
| :--- | :--- | :--- |
| `200 OK` | `Success` | Thao tác thành công. |
| `400 Bad Request` | `Auth:010001` (EmailAlreadyExists) | Email đã được sử dụng. |
| `400 Bad Request` | `Auth:010002` (InvalidCaptcha) | Xác minh Captcha thất bại hoặc bị bot giả lập. |
| `400 Bad Request` | `Auth:010003` (InvalidOnboardingToken) | Phiên đăng ký không hợp lệ hoặc đã hết hạn 15 phút. |
| `401 Unauthorized`| `Auth:010004` (InvalidCredentials) | Sai email hoặc mật khẩu (kèm số lần thử còn lại). |
| `423 Locked` | `Auth:010005` (AccountTemporarilyLocked) | Tài khoản bị tạm khóa phạt (kèm số giây đếm ngược). |
| `403 Forbidden` | `Auth:010006` (AccountDeactivated) | Tài khoản đã bị vô hiệu hóa do vượt ngưỡng an toàn. |

