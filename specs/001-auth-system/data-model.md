# Data Model & Architecture Design: RM-001

**Feature**: `001-auth-system`  
**Date**: 2026-11-11  
**Status**: Completed  

---

## 1. Mở Rộng Thực Thể `IdentityUser` (AppUser Extension)

Tận dụng cơ chế ABP Module Entity Extension: định nghĩa thuộc tính mở rộng tại `BookingHotel.Domain.Shared/BookingHotelModuleExtensionConfigurator.cs` và cấu hình ánh xạ EF Core tại `BookingHotel.EntityFrameworkCore/EntityFrameworkCore/BookingHotelEfCoreEntityExtensionMappings.cs`.

### Các trường dữ liệu mở rộng:

| Tên trường | Kiểu dữ liệu | Mặc định | Ý nghĩa & Quy tắc |
| :--- | :--- | :--- | :--- |
| `FailedAttemptsInCurrentCycle` | `int` | `0` | Số lần sai tích lũy trong chu kỳ hiện tại (tối đa 5 ở Giai đoạn 1; tối đa 3 ở Giai đoạn 2). |
| `TotalFailedAttempts` | `int` | `0` | Tổng số lần sai liên tiếp từ trước đến nay. |
| `CurrentLockoutMinutes` | `int` | `0` | Thời gian khóa phạt hiện tại (0, 5, 10, 15, 20, 25, 30). |
| `LockoutEnd` | `DateTimeOffset?` | `null` | *(Có sẵn)* Thời điểm kết thúc khóa phạt tạm thời. |
| `IsActive` | `bool` | `true` | *(Có sẵn)* Đặt thành `false` khi tài khoản bị vô hiệu hóa do brute-force. |
| `DeactivatedReason` | `string?` | `null` | Lý do vô hiệu hóa (ví dụ: `BruteForceLimitExceeded`). |
| `GoogleId` | `string?` | `null` | ID tài khoản Google (`sub` claim) nếu người dùng liên kết tài khoản Google. |

---

## 2. Mô Hình Dữ Liệu Bộ Nhớ Đệm (Distributed Cache - Onboarding State)

Quản lý trạng thái đăng ký 3 bước qua `IDistributedCache<UserRegistrationStateCacheItem>`.

```csharp
public class UserRegistrationStateCacheItem
{
    public Guid RegistrationId { get; set; }
    public string Email { get; set; } = default!;
    public string? PasswordHash { get; set; }
    public string? GoogleId { get; set; }
    public string? FullName { get; set; }
    public string? PhoneNumber { get; set; }
    public RegistrationStep CurrentStep { get; set; }
    public DateTime CreatedAt { get; set; }
}

public enum RegistrationStep
{
    Step1Completed = 1, // Đã nhập Email/Pass hoặc xác thực Google
    Step2Completed = 2, // Đã nhập Tên
    Completed = 3       // Đã nhập SĐT, đã tạo user trong CSDL
}
```
* **Cache Key**: `reg_flow_{RegistrationId}`
* **Thời gian sống (TTL)**: 15 phút (Sliding/Absolute Expiration).

---

## 3. Sơ Đồ Chuyển Đổi Trạng Thái Brute-Force (State Machine)

```mermaid
stateDiagram-v2
    [*] --> Active: Tài khoản mới tạo / Đăng nhập thành công

    state "Active (Bình thường)" as Active {
        [*] --> AttemptCycle1
        AttemptCycle1: Lần sai 1..4 (Báo số lần còn lại)
        AttemptCycle1 --> Lock5m: Sai lần thứ 5
    }

    state "Khóa tạm 5 phút" as Lock5m
    Lock5m --> AttemptCycle2: Hết 5 phút
    
    state "Chu kỳ 2 (5 lần sai)" as AttemptCycle2 {
        [*] --> Count6to9: Lần sai 6..9
        Count6to9 --> Lock10m: Sai lần thứ 10
    }

    state "Khóa tạm 10 phút" as Lock10m
    Lock10m --> AttemptCycle3: Hết 10 phút

    state "Chu kỳ 3 (5 lần sai)" as AttemptCycle3 {
        [*] --> Count11to14: Lần sai 11..14
        Count11to14 --> Lock15m: Sai lần thứ 15
    }

    state "Khóa tạm 15 phút" as Lock15m
    Lock15m --> Tier2Tightened: Hết 15 phút

    state "Giai đoạn 2: Siết chặt (3 lần sai/chu kỳ)" as Tier2Tightened {
        state "Chu kỳ 4 (3 lần sai)" as AttemptCycle4 {
            [*] --> Count16to17: Lần sai 16..17
            Count16to17 --> Lock20m: Sai lần thứ 18 (+3)
        }
        state "Khóa tạm 20 phút" as Lock20m
        Lock20m --> AttemptCycle5: Hết 20 phút

        state "Chu kỳ 5 (3 lần sai)" as AttemptCycle5 {
            [*] --> Count19to20: Lần sai 19..20
            Count19to20 --> Lock25m: Sai lần thứ 21 (+3)
        }
        state "Khóa tạm 25 phút" as Lock25m
        Lock25m --> AttemptCycle6: Hết 25 phút

        state "Chu kỳ 6 (3 lần sai)" as AttemptCycle6 {
            [*] --> Count22to23: Lần sai 22..23
            Count22to23 --> Lock30m: Sai lần thứ 24 (+3, Tối đa)
        }
    }

    state "Khóa tạm 30 phút (Mức tối đa)" as Lock30m
    Lock30m --> FinalWarning: Hết 30 phút

    state "Chu kỳ kiểm tra cuối (3 lần sai)" as FinalWarning {
        [*] --> Count25to26: Lần sai 25..26
        Count25to26 --> Deactivated: Sai lần thứ 27 (+3)
    }

    state "VÔ HIỆU HÓA TÀI KHOẢN (IsActive = false)" as Deactivated
    Deactivated --> Active: Admin xác minh & Mở khóa lại
```

---

## 4. Sơ Đồ Luồng Đăng Ký 3 Bước (Registration Flow)

```mermaid
sequenceDiagram
    autonumber
    actor User as Khách hàng
    participant UI as Angular Frontend
    participant API as AuthAppService (Backend)
    participant Cache as Distributed Cache
    participant DB as BookingHotelDbContext

    alt Đăng ký thông thường
        User->>UI: Nhập Email, Password, ConfirmPassword
        UI->>API: POST /api/auth/register/step1 (Email, Password)
    else Đăng ký qua Google
        User->>UI: Chọn "Đăng ký với Google"
        UI->>User: Xác thực Google Consent
        User->>UI: Trả về Google ID Token
        UI->>API: POST /api/auth/register/google-init (idToken)
    end

    API->>DB: Kiểm tra Email tồn tại?
    Note over API: Nếu tồn tại -> Báo lỗi 400
    API->>Cache: Lưu phiên Onboarding (Token, Email, Hash/GoogleId)
    API-->>UI: 200 OK { registrationToken, email, prefilledName, nextStep: "Step2" }

    UI->>User: Hiển thị Màn hình Bước 2 (Nhập Tên)
    User->>UI: Nhập Họ và Tên (ví dụ: "Nguyễn Văn A")
    UI->>API: POST /api/auth/register/step2 (registrationToken, fullName)
    API->>Cache: Cập nhật FullName, chuyển step -> Step2Completed
    API-->>UI: 200 OK { nextStep: "Step3" }

    UI->>User: Hiển thị Màn hình Bước 3 (Nhập SĐT)
    User->>UI: Nhập Số điện thoại (ví dụ: "0901234567")
    UI->>API: POST /api/auth/register/step3 (registrationToken, phoneNumber)
    API->>DB: Tạo IdentityUser mới (Email, Name, Phone, IsActive=true, Role=Customer)
    API->>Cache: Xóa RegistrationToken
    API->>API: Sinh JWT Token đăng nhập
    API-->>UI: 200 OK { accessToken, userInfo }
    UI->>User: Chuyển hướng vào Portal Đặt phòng (Booking Home)
```

