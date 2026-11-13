# Implementation Plan: RM-002 - Thay Thế Form Auth Mặc Định ABP Bằng Form Tùy Chỉnh RM-001 & Quy Chuẩn Mật Khẩu

**Branch**: `002-replace-default-auth` | **Date**: 2026-11-11 | **Spec**: [spec.md](spec.md)  
**Input**: Feature specification from `specs/002-replace-default-auth/spec.md`  

---

## Summary

Triển khai cơ chế thay thế triệt để các form đăng nhập/đăng ký mặc định của ABP Framework bằng phân hệ giao diện xác thực tùy chỉnh đã hoàn thiện ở RM-001, đồng thời thiết lập chính sách mật khẩu mạnh (Strong Password Policy):
1. **Frontend Angular**:
   - Cung cấp `CustomAuthService` kế thừa `AbpOAuthService` để ghi đè `navigateToLogin()` (chuyển hướng SPA tới `/auth/login`) và `logout()` (xóa token và điều hướng về trang chủ dưới dạng khách).
   - Đăng ký `CustomAuthService` thay thế `AuthService` trong `app.config.ts`.
   - Gỡ bỏ module `@abp/ng.account` khỏi `app.routes.ts` và thiết lập route redirects từ `/account/*` sang `/auth/*`, đặt `data: { layout: 'empty' }` cho toàn bộ nhánh auth.
   - Cập nhật `LoginComponent` để lưu token vào `OAuthStorage`, gọi `ConfigStateService.refreshAppState()` đồng bộ phiên đăng nhập không reload trang, loại bỏ triệt để browser autofill khi chưa chọn Remember Me.
   - Xây dựng `passwordComplexityValidator` và khối Checklist trực quan thời gian thực (5 tiêu chí: độ dài >= 8, chữ hoa, chữ thường, số, ký tự đặc biệt) trên form Đăng ký (`/auth/register`) và Đặt lại mật khẩu (`/auth/reset-password`).
   - Cập nhật luồng hoàn tất đăng ký: Không tự động lưu token hay tự động đăng nhập sau Bước 3. Màn hình Bước 4 hiển thị thông báo thành công và nút "Quay lại Đăng nhập" (`/auth/login`) để người dùng tự đăng nhập thủ công.
2. **Backend ASP.NET Core**:
   - Tạo Razor Page `Pages/Account/Login.cshtml.cs` và `Register.cshtml.cs` để tự động trả về HTTP 302 Redirect về URL Angular `/auth/*` khi có truy cập trực tiếp.
   - Áp dụng DataAnnotation `[RegularExpression]` kiểm tra độ phức tạp mật khẩu trên `RegisterStep1InputDto` và `ResetPasswordInputDto`.
   - Cập nhật mật khẩu admin seed trong `BookingHotelConsts.AdminPasswordDefaultValue` lên `1q2w3E*#` (8 ký tự).
   - Cung cấp chuỗi tài nguyên song ngữ trong `vi.json` và `en.json`.

---

## Technical Context

- **Language / Framework Versions**: .NET 10 (C# 13), ABP Framework 10.7, Angular 19 (Standalone Components, RxJS, TypeScript 5.6).
- **Primary Dependencies**: `@abp/ng.core`, `@abp/ng.oauth`, `@abp/ng.theme.lepton-x`, `angular-oauth2-oidc`, `Microsoft.AspNetCore.Mvc.RazorPages`.
- **Storage**: `OAuthStorage` (`localStorage`).
- **Testing**: `xUnit`, `dotnet test` (22/22 tests), Angular CLI `ng build`.
- **Target Platform**: Web SPA (Angular) + ASP.NET Core Web API / Host.
- **Performance Goals**: Điều hướng nội bộ trong < 50ms, đồng bộ phiên qua `refreshAppState()` < 200ms, không reload toàn trang, validate regex tức thời không giật lag.

---

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Nguyên tắc Hiến pháp (Constitution Principle) | Đánh giá Tuân thủ trong RM-002 | Trạng thái |
|:---|:---|:---:|
| **1. Clean Domain-Driven Design** | Phân tách rõ ràng giữa cấu hình host backend và client Angular. Không trộn lẫn business logic. | ✅ PASS |
| **2. Contract-First API & DTOs** | Bổ sung Regex validation trực tiếp trên DTOs `RegisterStep1InputDto` và `ResetPasswordInputDto`. Định nghĩa rõ hợp đồng `CustomAuthService`. | ✅ PASS |
| **3. Database Migrations Discipline** | Cập nhật hằng số seed password `BookingHotelConsts.AdminPasswordDefaultValue`, không thay đổi cấu trúc bảng DB, không cần migration mới. | ✅ PASS |
| **4. Angular UI Separation & Theme** | Duy trì theme Đỏ sẫm (`#990000`) và Đen (`#000000`); Checklist mật khẩu thiết kế đồng bộ với bảng màu thương hiệu. | ✅ PASS |
| **5. Localization First (i18n)** | Sử dụng các chuỗi bản dịch song ngữ `vi.json` / `en.json` cho toàn bộ thông báo lỗi và 5 tiêu chí mật khẩu. | ✅ PASS |
| **6. Backend-Authoritative Calculations** | Mọi xác thực tính hợp lệ và băm mật khẩu (hashing) do ASP.NET Core Identity & Backend làm chủ. | ✅ PASS |
| **7. Multi-Channel Brute-Force & Captcha** | Mọi luồng truy cập auth đều được dẫn về form RM-001 có sẵn Brute-force phân tầng và Turnstile Captcha. | ✅ PASS |
| **8. Brand Naming & Spacing** | Tên thương hiệu hiển thị luôn là `Booking Hotel` có dấu cách. | ✅ PASS |
| **9. Authentication & Session Security** | Mặc định không hiển thị tài khoản nào (kể cả admin), chỉ hiển thị khi có Remember Me (tối đa 15 ngày). | ✅ PASS |

---

## Project Structure

### Documentation (this feature)

```text
BookingHotel/specs/002-replace-default-auth/
├── spec.md                     # Tài liệu đặc tả yêu cầu & quyết định kỹ thuật
├── research.md                 # Nghiên cứu chuỗi gọi LeptonX, OAuthStorage, refreshAppState, NIST/OWASP password
├── data-model.md               # Mô hình lưu trữ token, vòng đời phiên và bảng ánh xạ chuyển hướng, ma trận mật khẩu
├── contracts/
│   └── auth-routing-contracts.md # Hợp đồng CustomAuthService, app.config, app.routes, DTO Regex, password validator
├── quickstart.md               # Kịch bản kiểm thử thủ công và lệnh nghiệm thu
├── tasks.md                    # Danh sách các task phân rã chi tiết
└── plan.md                     # File kế hoạch triển khai tổng thể này
```

### Source Code Files Modified / Created

```text
BookingHotel/
├── angular/
│   └── src/app/
│       ├── app.config.ts                           # Đăng ký CustomAuthService ghi đè AuthService
│       ├── app.routes.ts                           # Gỡ bỏ @abp/ng.account, thêm redirects /account/* -> /auth/*, layout: empty
│       ├── styles.scss                             # Loại bỏ Sidebar LeptonX trên toàn hệ thống
│       └── auth/
│           ├── services/
│           │   └── custom-auth.service.ts          # Kế thừa AbpOAuthService, ghi đè navigateToLogin & logout
│           ├── validators/
│           │   └── password-complexity.validator.ts# [NEW] Custom validator kiểm tra 5 tiêu chí mật khẩu
│           ├── login/
│           │   ├── login.component.ts              # OAuthStorage sync, refreshAppState(), triệt tiêu autofill
│           │   ├── login.component.html            # Traps autofill, readonly toggle
│           │   └── login.component.scss            # Triệt tiêu :-webkit-autofill
│           ├── register/
│           │   ├── register.component.ts           # Áp dụng passwordComplexityValidator
│           │   ├── register.component.html         # Bổ sung Password Checklist UI (5 tiêu chí)
│           │   └── register.component.scss         # Styling checklist mật khẩu
│           └── reset-password/
│               ├── reset-password.component.ts     # Áp dụng passwordComplexityValidator
│               ├── reset-password.component.html   # Bổ sung Password Checklist UI (5 tiêu chí)
│               └── reset-password.component.scss   # Styling checklist mật khẩu
├── src/
│   ├── BookingHotel.Domain/
│   │   └── BookingHotelConsts.cs                   # Cập nhật AdminPasswordDefaultValue = "1q2w3E*#" (8 ký tự)
│   ├── BookingHotel.Domain.Shared/
│   │   └── Localization/BookingHotel/
│   │       ├── vi.json                             # Bổ sung thông điệp lỗi và tiêu chí mật khẩu tiếng Việt
│   │       └── en.json                             # Bổ sung thông điệp lỗi và tiêu chí mật khẩu tiếng Anh
│   ├── BookingHotel.Application.Contracts/
│   │   └── Auth/Dtos/
│   │       ├── RegisterStep1InputDto.cs            # Bổ sung Regex validation attribute
│   │       └── ResetPasswordInputDto.cs            # Bổ sung Regex validation attribute
│   └── BookingHotel.HttpApi.Host/
│       └── Pages/
│           └── Account/
│               ├── Login.cshtml & Login.cshtml.cs       # Razor Page chuyển hướng 302 về Angular /auth/login
│               └── Register.cshtml & Register.cshtml.cs # Razor Page chuyển hướng 302 về Angular /auth/register
```

---

## Phased Execution Plan

### Phase 1: Core Routing & Session Sync (Completed)
- Triển khai `CustomAuthService` kế thừa `AbpOAuthService` ghi đè `navigateToLogin` và `logout`.
- Gỡ bỏ `@abp/ng.account`, thiết lập route redirects và gán `data: { layout: 'empty' }`.
- Đồng bộ `OAuthStorage` và `refreshAppState()` tại `LoginComponent` và `RegisterComponent`.
- Xử lý chặn triệt để browser autofill theo Hiến pháp Điều IX.
- Loại bỏ thanh Sidebar dọc LeptonX khỏi hệ thống theo RM-003.

### Phase 2: Password Complexity Backend Enforcement (Completed)
- Cập nhật hằng số `BookingHotelConsts.AdminPasswordDefaultValue` lên `1q2w3E*#`.
- Bổ sung DataAnnotation `[RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).{8,}$")]` vào:
  - `RegisterStep1InputDto.cs`
  - `ResetPasswordInputDto.cs`
- Bổ sung tài nguyên song ngữ cho lỗi độ phức tạp mật khẩu trong `vi.json` và `en.json`.
- Bổ sung 10 unit test backend trong `AuthAppService_Registration_Tests` và `AuthAppService_PasswordReset_Tests` kiểm tra 5 tiêu chí mật khẩu (100% passed).

### Phase 3: Password Complexity Frontend & Real-time Checklist UI (Completed)
- Tạo Angular validator `password-complexity.validator.ts` kiểm tra chi tiết từng tiêu chí (`hasMinLength`, `hasUpperCase`, `hasLowerCase`, `hasNumeric`, `hasSpecial`).
- Tích hợp validator vào `register.component.ts` (Step 1) và `reset-password.component.ts`.
- Thiết kế khối Checklist trực quan thời gian thực (5 items có icon `✓`/`✗` đổi màu linh hoạt) trên `register.component.html` và `reset-password.component.html`.
- Định kiểu SCSS viền xanh lá `is-valid` khi khớp và viền đỏ `is-invalid` thời gian thực khi không khớp.

### Phase 4: Post-Registration Explicit Login Flow (User Story 6 - In Progress)
- Cập nhật phương thức `onStep3Submit()` trong `angular/src/app/auth/register/register.component.ts`: loại bỏ việc ghi token vào `OAuthStorage` và loại bỏ gọi `refreshAppState()`.
- Cập nhật giao diện Bước 4 (Success Screen) trong `angular/src/app/auth/register/register.component.html`:
  - Thay nút liên kết Trang chủ (`routerLink="/"`) bằng nút hành động **"Quay lại Đăng nhập"** (`routerLink="/auth/login"`).
  - Cung cấp thông điệp hướng dẫn rõ ràng: *"Đăng ký thành công! Vui lòng đăng nhập để bắt đầu trải nghiệm dịch vụ."*
- Bổ sung các chuỗi bản dịch song ngữ `Auth:BackToLogin`, `Auth:RegistrationSuccessTitle`, `Auth:RegistrationSuccessMessage`, `Auth:PleaseLoginToContinue` trong `src/BookingHotel.Domain.Shared/Localization/BookingHotel/vi.json` và `en.json`.

### Phase 5: Verification & Quality Gates (Final Step)
- Chạy `npm run build` trên Angular SPA (xác nhận 0 errors, 0 warnings).
- Chạy `dotnet test` trên Backend .NET (xác nhận 32/32 tests passing 100%).
- Nghiệm thu Kịch bản 7 trong `quickstart.md` (Xác nhận sau khi đăng ký thành công, người dùng không bị tự động đăng nhập mà bấm "Quay lại Đăng nhập" để tự đăng nhập thủ công).
