# Implementation Plan: RM-001 - Hệ Thống Xác Thực & Đăng Nhập/Đăng Ký Đa Kênh Bảo Mật

**Branch**: `001-auth-system` | **Date**: 2026-11-11 | **Spec**: [specs/001-auth-system/spec.md](spec.md)  
**Input**: Feature specification from `specs/001-auth-system/spec.md`  

---

## 1. Summary

Xây dựng hệ thống xác thực và quản lý tài khoản người dùng toàn diện cho `BookingHotel` đáp ứng:
1. **Đăng nhập linh hoạt**: Hỗ trợ form Email + Mật khẩu và liên kết tài khoản Google OAuth 2.0 / OpenIddict.
2. **Quy trình đăng ký 3 bước (Multi-step Onboarding)**: Đăng ký thường hoặc bằng Google với luồng tuần tự:
   $$\text{Bước 1: Tài khoản/Email} \rightarrow \text{Bước 2: Họ và Tên} \rightarrow \text{Bước 3: Số điện thoại}$$
   Quản lý phiên đăng ký bằng `IDistributedCache` và `RegistrationToken` (thời hạn 15 phút), chống nhảy cóc hoặc làm giả dữ liệu.
3. **Phòng thủ Brute-force lũy tiến phân tầng**:
   - Giai đoạn 1 (Lần sai 1 đến 15): Ngưỡng 5 lần sai/chu kỳ $\rightarrow$ Phạt tăng dần 5m, 10m, 15m.
   - Giai đoạn 2 (Từ lần sai thứ 15 trở đi): Ngưỡng siết chặt còn 3 lần sai/chu kỳ $\rightarrow$ Phạt tăng dần 20m, 25m, 30m.
   - Vô hiệu hóa vĩnh viễn (`IsActive = false`): Nếu sau mốc khóa 30m tiếp tục sai 3 lần tiếp theo.
4. **Quên & Đặt lại mật khẩu có Captcha**: Form quên mật khẩu bắt buộc xác minh Captcha bảo vệ (Cloudflare Turnstile / Google reCAPTCHA) tại Backend trước khi cấp phép gửi email đặt lại mật khẩu với token an toàn (hạn 15 phút).
5. **Giao diện nhận diện thương hiệu**: Áp dụng Concept màu Đỏ sẫm (`#990000`) và Đen (`#000000`) cho Portal Đặt phòng của Khách hàng.

---

## 2. Technical Context

- **Language / Version**: C# .NET 10, TypeScript 5.x / Angular 19+.
- **Primary Dependencies**:
  - ABP Framework 10.7 (Account, Identity, OpenIddict, DistributedCaching).
  - ASP.NET Core Identity & OpenIddict Server.
  - `Google.Apis.Auth` (Xác thực Google ID Token).
  - Cloudflare Turnstile / Google reCAPTCHA (Xác minh chống bot tại Backend).
- **Storage**: Relational DB (SQL Server / PostgreSQL) quản lý thông qua EF Core Migrations & `BookingHotel.DbMigrator`.
- **Testing**: xUnit, Shouldly, ABP TestBase (Domain Tests & Application Integration Tests).
- **Target Platform**: ASP.NET Core Web API Host & Angular SPA Web Application.
- **Performance Goals**: Phản hồi xác thực < 500ms; đánh giá và cập nhật trạng thái brute force < 100ms.
- **Constraints (Constitution)**: Toàn bộ quyết định trạng thái tài khoản và tính toán thời gian khóa phạt diễn ra 100% tại Backend (*Backend-Authoritative*).

---

## 3. Constitution Check

*GATE: Kiểm tra tính tuân thủ 7 Nguyên tắc tối cao trong `constitution.md`*

| Điều khoản Hiến pháp | Tiêu chí đánh giá | Trạng thái | Giải pháp kỹ thuật tương ứng |
| :--- | :--- | :---: | :--- |
| **I. Phân lớp DDD & ABP** | Đúng tầng, không trộn lẫn trách nhiệm | **PASS** | Logic khóa brute-force đặt tại `BookingHotel.Domain` (`BruteForceManager`); DTOs tại `Application.Contracts`; điều phối tại `Application`; mapping CSDL tại `EntityFrameworkCore`. |
| **II. Contract-First & Security** | DTO validate, bảo mật mặc định | **PASS** | Mọi DTO đều có DataAnnotation validation (`[EmailAddress]`, `[Phone]`, `[StringLength]`); băm mật khẩu chuẩn ASP.NET Core Identity; kiểm tra quyền. |
| **III. EF Core Migrations** | Không sửa DB bằng tay | **PASS** | Mở rộng thuộc tính `IdentityUser` thông qua `BookingHotelEfCoreEntityExtensionMappings` và chạy qua `BookingHotel.DbMigrator`. |
| **IV. Chuẩn hóa Angular Frontend** | Reactive Forms, Thin Client | **PASS** | Sử dụng Reactive Forms, Client chỉ gửi dữ liệu đầu vào và nhận đếm ngược thời gian từ Backend; không tự quyết định thời gian khóa. |
| **V. Đa ngôn ngữ (i18n)** | Không hardcode chuỗi | **PASS** | Toàn bộ thông báo lỗi lockout, số lần thử còn lại, thông báo Captcha định nghĩa song ngữ trong `vi.json` và `en.json`. |
| **VI. Backend-Authoritative** | Toàn quyền kiểm soát tại Backend | **PASS** | Kiểm tra mật khẩu, bộ đếm chu kỳ, thời gian phạt (`LockoutEnd`), xác thực Captcha và vô hiệu hóa tài khoản (`IsActive = false`) thực hiện 100% tại Backend. |
| **VII. UI Concept Khách hàng** | Đỏ sẫm (`#990000`) & Đen (`#000000`) | **PASS** | Giao diện đăng nhập, đăng ký 3 bước và quên mật khẩu trên Angular áp dụng phong cách sang trọng với tông màu `#990000` và `#000000`. |

---

## 4. Project Structure & Code Mapping

### Tài liệu Đặc tả (Documentation)
```text
specs/001-auth-system/
├── spec.md               # Đặc tả tính năng và kịch bản kiểm thử
├── plan.md               # Kế hoạch kiến trúc và kỹ thuật (file này)
├── research.md           # Nghiên cứu kỹ thuật chuyên sâu (Phase 0)
├── data-model.md         # Sơ đồ CSDL, Cache & State Machine (Phase 1)
├── quickstart.md         # Hướng dẫn chạy & kiểm thử nhanh cho lập trình viên
└── contracts/
    └── auth-contracts.md # Đặc tả REST API, DTOs & Mã lỗi HTTP
```

### Mã nguồn Thực thi (Source Code Layout)

```text
BookingHotel/
├── src/
│   ├── BookingHotel.Domain.Shared/
│   │   ├── BookingHotelModuleExtensionConfigurator.cs  # Đăng ký ExtraProperties cho IdentityUser
│   │   └── Localization/BookingHotel/
│   │       ├── vi.json                                # Bản dịch thông báo tiếng Việt
│   │       └── en.json                                # Bản dịch thông báo tiếng Anh
│   │
│   ├── BookingHotel.Domain/
│   │   ├── Identity/
│   │   │   ├── BruteForceManager.cs                   # Logic kiểm tra brute force phân tầng
│   │   │   └── IBruteForceManager.cs
│   │   └── Security/
│   │       ├── ICaptchaValidator.cs                   # Interface xác minh Captcha
│   │       └── CloudflareTurnstileValidator.cs        # Triển khai xác minh Turnstile/reCAPTCHA
│   │
│   ├── BookingHotel.Application.Contracts/
│   │   └── Auth/
│   │       ├── IAuthAppService.cs                     # API Interface chính
│   │       ├── Dtos/
│   │       │   ├── LoginWithEmailDto.cs
│   │       │   ├── LoginResultDto.cs
│   │       │   ├── RegisterStep1InputDto.cs
│   │       │   ├── RegisterStep1ResultDto.cs
│   │       │   ├── RegisterStep2InputDto.cs
│   │       │   ├── RegisterStep3InputDto.cs
│   │       │   ├── GoogleAuthInputDto.cs
│   │       │   ├── ForgotPasswordInputDto.cs
│   │       │   └── ResetPasswordInputDto.cs
│   │       └── ErrorCodes/
│   │           └── BookingHotelAuthErrorCodes.cs      # Mã lỗi nghiệp vụ
│   │
│   ├── BookingHotel.Application/
│   │   └── Auth/
│   │       ├── AuthAppService.cs                      # Triển khai các luồng xác thực & đăng ký
│   │       └── UserRegistrationStateCacheItem.cs      # Đối tượng Cache phiên Onboarding
│   │
│   ├── BookingHotel.EntityFrameworkCore/
│   │   ├── EntityFrameworkCore/
│   │   │   └── BookingHotelEfCoreEntityExtensionMappings.cs # Ánh xạ ExtraProperties vào bảng CSDL
│   │   └── Migrations/
│   │       └── xxxx_AddIdentityUserBruteForceFields.cs      # EF Core Migration
│   │
│   ├── BookingHotel.HttpApi/
│   │   └── Controllers/
│   │       └── AuthController.cs                      # REST Controller công khai (nếu tùy biến)
│   │
│   ├── BookingHotel.HttpApi.Host/
│   │   ├── appsettings.json                           # Cấu hình Google ClientId/Secret & Captcha Secret
│   │   └── BookingHotelHttpApiHostModule.cs           # Đăng ký Google Auth & HTTP Client cho Captcha
│   │
│   └── angular/
│       └── src/app/
│           └── auth/
│               ├── login/                             # Màn hình đăng nhập Email/Google (Theme đỏ/đen)
│               ├── register/                          # Component Onboarding 3 bước (Wizard/Tabs)
│               │   ├── step1-account/
│               │   ├── step2-name/
│               │   └── step3-phone/
│               ├── forgot-password/                   # Màn hình quên mật khẩu + Captcha widget
│               ├── reset-password/                    # Màn hình đặt lại mật khẩu từ link email
│               └── services/
│                   └── auth-proxy.service.ts          # Angular Service gọi Backend API
│
└── test/
    ├── BookingHotel.Domain.Tests/
    │   └── Identity/
    │       └── BruteForceManager_Tests.cs             # Unit test thuật toán phân tầng
    └── BookingHotel.Application.Tests/
        └── Auth/
            ├── AuthAppService_Login_Tests.cs          # Test đăng nhập, brute-force lockout
            ├── AuthAppService_Registration_Tests.cs   # Test 3 bước Onboarding
            └── AuthAppService_PasswordReset_Tests.cs  # Test quên mật khẩu & Captcha
```

---

## 5. Implementation Phases (Lộ Trình Triển Khai)

### Giai đoạn 1: Thiết lập Mô hình Dữ liệu & Domain Services (Backend Domain)
- [ ] Mở rộng thuộc tính cho `IdentityUser` trong `BookingHotelModuleExtensionConfigurator.cs` (`FailedAttemptsInCurrentCycle`, `TotalFailedAttempts`, `CurrentLockoutMinutes`, `DeactivatedReason`, `GoogleId`).
- [ ] Triển khai `IBruteForceManager` & `BruteForceManager`:
  - Thuật toán Giai đoạn 1: 5 lần sai/chu kỳ $\rightarrow$ 5m, 10m, 15m.
  - Thuật toán Giai đoạn 2: 3 lần sai/chu kỳ $\rightarrow$ 20m, 25m, 30m.
  - Vô hiệu hóa tài khoản: Sau đợt 30m, sai tiếp 3 lần $\rightarrow$ `IsActive = false`.
- [ ] Triển khai `ICaptchaValidator` với Cloudflare Turnstile & Google reCAPTCHA.
- [ ] Viết Unit Test tại `BookingHotel.Domain.Tests` cho `BruteForceManager`.

### Giai đoạn 2: Cấu hình EF Core Mapping & Database Migration
- [ ] Cấu hình ánh xạ cột CSDL trong `BookingHotelEfCoreEntityExtensionMappings.cs`.
- [ ] Tạo Migration mới bằng EF Core CLI: `dotnet ef migrations add AddIdentityUserBruteForceFields`.
- [ ] Chạy `BookingHotel.DbMigrator` để cập nhật cấu trúc database.

### Giai đoạn 3: Tầng Hợp Đồng & Ứng Dụng (Contracts & Application Service)
- [ ] Định nghĩa DTOs và `IAuthAppService` trong `BookingHotel.Application.Contracts`.
- [ ] Bổ sung mã lỗi nghiệp vụ và dịch song ngữ trong `vi.json` và `en.json`.
- [ ] Triển khai `AuthAppService` trong `BookingHotel.Application`:
  - `LoginWithEmailAsync` kết hợp `BruteForceManager`.
  - `LoginWithGoogleAsync` & `RegisterGoogleInitAsync` dùng `GoogleJsonWebSignature`.
  - Luồng đăng ký 3 bước: `RegisterStep1Async` $\rightarrow$ `RegisterStep2Async` $\rightarrow$ `RegisterStep3Async` qua `IDistributedCache`.
  - `SendPasswordResetCodeAsync` (kiểm tra Captcha, sinh reset token, gọi `IEmailSender`).
  - `ResetPasswordAsync` (cập nhật mật khẩu, reset bộ đếm lockout).
- [ ] Cấu hình OAuth Google và Captcha keys trong `BookingHotel.HttpApi.Host/appsettings.json`.

### Giai đoạn 4: Xây dựng Giao diện Frontend Angular
- [ ] Tạo module / standalone components trong `angular/src/app/auth/`:
  - Màn hình Đăng nhập (Email/Password & Nút Google Sign-in).
  - Màn hình Đăng ký Wizard 3 bước: Step 1 (Tài khoản) $\rightarrow$ Step 2 (Tên) $\rightarrow$ Step 3 (SĐT) kèm thanh tiến trình.
  - Màn hình Quên mật khẩu tích hợp widget Captcha (Turnstile/reCAPTCHA).
  - Màn hình Đặt lại mật khẩu từ email link.
- [ ] Áp dụng Design System: Bảng màu Đỏ sẫm (`#990000`) và Đen (`#000000`), thông báo đếm ngược thời gian lockout khi bị khóa tạm thời.

### Giai đoạn 5: Kiểm Thử Tích Hợp & Hoàn Thiện (Verification & Testing)
- [ ] Viết Integration Tests trong `BookingHotel.Application.Tests` (Cover toàn bộ kịch bản trong `specs/001-auth-system/quickstart.md`).
- [ ] Kiểm tra thực tế luồng đăng ký 3 bước và đăng nhập Google trên trình duyệt.
- [ ] Rà soát an ninh, audit log và hoàn tất gate checklist.

---

## 6. Complexity Tracking

> *Không có vi phạm hiến pháp nào cần giải trình. Toàn bộ giải pháp kiến trúc tuân thủ 100% 7 Nguyên tắc cốt lõi.*
