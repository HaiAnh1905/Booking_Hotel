# Tasks: RM-001 - Hệ Thống Xác Thực & Đăng Nhập/Đăng Ký Đa Kênh Bảo Mật

**Branch**: `001-auth-system`  
**Prerequisites**: [plan.md](plan.md), [spec.md](spec.md), [research.md](research.md), [data-model.md](data-model.md), [contracts/auth-contracts.md](contracts/auth-contracts.md)  
**Input**: Design documents from `specs/001-auth-system/`  

---

## Format: `[ID] [P?] [Story] Description`
- **[P]**: Có thể thực thi song song (khác file, không phụ thuộc nhau).
- **[Story]**: Gắn với User Story tương ứng (`[US1]`, `[US2]`, `[US3]`, `[US4]`, `[US5]`).
- Mọi mô tả đều có đường dẫn file cụ thể trong dự án.

---

## Phase 1: Setup & Shared Infrastructure

**Mục đích**: Khởi tạo cấu trúc dữ liệu mở rộng, tài nguyên ngôn ngữ song ngữ và hằng số lỗi dùng chung.

- [x] T001 Cấu hình mở rộng thuộc tính `IdentityUser` trong `src/BookingHotel.Domain.Shared/BookingHotelModuleExtensionConfigurator.cs` (`FailedAttemptsInCurrentCycle`, `TotalFailedAttempts`, `CurrentLockoutMinutes`, `DeactivatedReason`, `GoogleId`).
- [x] T002 [P] Khởi tạo tài nguyên chuỗi bản địa hóa cho bảo mật và đăng nhập trong `src/BookingHotel.Domain.Shared/Localization/BookingHotel/vi.json` và `src/BookingHotel.Domain.Shared/Localization/BookingHotel/en.json`.
- [x] T003 [P] Định nghĩa hằng số mã lỗi nghiệp vụ `BookingHotelAuthErrorCodes.cs` trong `src/BookingHotel.Application.Contracts/Auth/ErrorCodes/BookingHotelAuthErrorCodes.cs`.
- [x] T004 [P] Tạo cấu trúc thư mục auth và service gọi API `auth-proxy.service.ts` tại `angular/src/app/auth/services/auth-proxy.service.ts`.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Mục đích**: Cấu hình CSDL, EF Core Migrations, dịch vụ xác minh Captcha và Domain Service kiểm soát Brute-force.  
*⚠️ Bắt buộc hoàn thành trước khi triển khai các User Stories.*

- [x] T005 Cấu hình ánh xạ cột CSDL cho các ExtraProperties của `IdentityUser` trong `src/BookingHotel.EntityFrameworkCore/EntityFrameworkCore/BookingHotelEfCoreEntityExtensionMappings.cs`.
- [x] T006 Tạo EF Core Migration và chạy `BookingHotel.DbMigrator` để cập nhật CSDL (`AddIdentityUserBruteForceFields`).
- [x] T007 [P] Triển khai interface `ICaptchaValidator` và dịch vụ `CloudflareTurnstileValidator` tại `src/BookingHotel.Domain/Security/CloudflareTurnstileValidator.cs`.
- [x] T008 [P] Thêm cấu hình `Turnstile` và `Authentication:Google` vào file `src/BookingHotel.HttpApi.Host/appsettings.json`.
- [x] T009 Triển khai Domain Service `BruteForceManager` trong `src/BookingHotel.Domain/Identity/BruteForceManager.cs` thực hiện thuật toán phòng thủ phân tầng:
  - Giai đoạn 1 (Lần 1 - 15): Ngưỡng 5 lần sai/chu kỳ phạt $\rightarrow$ khóa lũy tiến 5m, 10m, 15m.
  - Giai đoạn 2 (Từ lần 15 trở đi): Ngưỡng siết chặt còn 3 lần sai/chu kỳ phạt $\rightarrow$ khóa lũy tiến 20m, 25m, 30m.
  - Vô hiệu hóa tài khoản: Sau đợt 30m, sai tiếp 3 lần $\rightarrow$ `IsActive = false`.
- [x] T010 [P] Viết Unit Tests tại `test/BookingHotel.Domain.Tests/Identity/BruteForceManager_Tests.cs` kiểm chứng toàn bộ các nhánh logic của thuật toán phân tầng.

**Checkpoint Foundation**: CSDL, Security Validator và BruteForceManager đã sẵn sàng.

---

## Phase 3: User Story 1 - Đăng nhập bằng Email với Chống Brute-Force Phân Tầng (Priority: P1) 🎯 MVP

**Mục tiêu**: Người dùng có thể đăng nhập bằng Email/Password an toàn, được bảo vệ bởi thuật toán Brute-Force phân tầng.

### Implementation for User Story 1
- [x] T011 [P] [US1] Tạo DTOs `LoginWithEmailDto.cs` và `LoginResultDto.cs` trong `src/BookingHotel.Application.Contracts/Auth/Dtos/`.
- [x] T012 [US1] Khai báo phương thức `LoginWithEmailAsync` trong `src/BookingHotel.Application.Contracts/Auth/IAuthAppService.cs`.
- [x] T013 [US1] Triển khai phương thức `LoginWithEmailAsync` trong `src/BookingHotel.Application/Auth/AuthAppService.cs` (tích hợp `BruteForceManager`, ghi log `IdentitySecurityLog`, kiểm tra trạng thái kích hoạt, sinh JWT token).
- [x] T014 [P] [US1] Viết Integration Tests tại `test/BookingHotel.Application.Tests/Auth/AuthAppService_Login_Tests.cs` (đăng nhập thành công, đăng nhập sai 5 lần đầu khóa 5m, đạt mốc 15 lần, giảm ngưỡng còn 3 lần, vô hiệu hóa sau mốc 30m).
- [x] T015 [US1] Xây dựng UI Component Đăng nhập tại `angular/src/app/auth/login/` (`login.component.ts`, `login.component.html`, `login.component.scss`) tuân thủ Theme Đỏ (`#990000`) và Đen (`#000000`), hiển thị số lần thử còn lại và đếm ngược thời gian khi bị khóa tạm thời.

**Checkpoint US1**: Hoàn thành User Story 1, kiểm thử độc lập luồng Đăng nhập Email và phòng thủ Brute-force.

---

## Phase 4: User Story 2 - Đăng Ký Tài Khoản Khách Hàng 3 Bước (Priority: P1) 🎯 MVP

**Mục tiêu**: Khách hàng mới đăng ký tài khoản qua biểu mẫu 3 bước trực quan (Email $\rightarrow$ Họ tên $\rightarrow$ SĐT) an toàn, chống bỏ qua bước.

### Implementation for User Story 2
- [x] T016 [P] [US2] Tạo DTOs đăng ký 3 bước: `RegisterStep1InputDto.cs`, `RegisterStep1ResultDto.cs`, `RegisterStep2InputDto.cs`, `RegisterStep3InputDto.cs` trong `src/BookingHotel.Application.Contracts/Auth/Dtos/`.
- [x] T017 [P] [US2] Định nghĩa đối tượng lưu cache phiên `UserRegistrationStateCacheItem.cs` trong `src/BookingHotel.Application/Auth/UserRegistrationStateCacheItem.cs`.
- [x] T018 [US2] Khai báo `RegisterStep1Async`, `RegisterStep2Async`, `RegisterStep3Async` trong `src/BookingHotel.Application.Contracts/Auth/IAuthAppService.cs`.
- [x] T019 [US2] Triển khai logic 3 bước đăng ký trong `src/BookingHotel.Application/Auth/AuthAppService.cs`:
  - Bước 1: Kiểm tra email duy nhất, lưu cache phiên Onboarding (`RegistrationToken`, TTL 15 phút).
  - Bước 2: Kiểm tra token, lưu Họ và Tên.
  - Bước 3: Kiểm tra token, validate định dạng SĐT di động Việt Nam/quốc tế $\rightarrow$ tạo `IdentityUser` trong CSDL $\rightarrow$ gán role `Customer` $\rightarrow$ xóa cache $\rightarrow$ cấp token đăng nhập.
- [x] T020 [P] [US2] Viết Integration Tests kiểm chứng luồng đăng ký 3 bước và chống Step Skipping tại `test/BookingHotel.Application.Tests/Auth/AuthAppService_Registration_Tests.cs`.
- [x] T021 [US2] Xây dựng UI Wizard Đăng ký 3 bước tại `angular/src/app/auth/register/` (`register.component.ts`, `step1-account/`, `step2-name/`, `step3-phone/`) với thanh tiến trình trực quan theo Theme Đỏ/Đen.

**Checkpoint US2**: Hoàn thành User Story 2, luồng Đăng ký 3 bước hoạt động độc lập và hoàn chỉnh.

---

## Phase 5: User Story 3 - Đăng Nhập & Đăng Ký Google One Tap (Priority: P2)

**Mục tiêu**: Người dùng có thể đăng nhập hoặc đăng ký một chạm bằng tài khoản Google (GIS / One Tap).

### Implementation for User Story 3
- [x] T022 [P] [US3] Tạo DTO `GoogleAuthInputDto.cs` trong `src/BookingHotel.Application.Contracts/Auth/Dtos/GoogleAuthInputDto.cs`.
- [x] T023 [US3] Khai báo `LoginWithGoogleAsync` và `RegisterGoogleInitAsync` trong `src/BookingHotel.Application.Contracts/Auth/IAuthAppService.cs`.
- [x] T024 [US3] Triển khai xác thực Google ID Token bằng `GoogleJsonWebSignature` trong `src/BookingHotel.Application/Auth/AuthAppService.cs`:
  - Tài khoản đã tồn tại: Cấp JWT đăng nhập ngay.
  - Tài khoản mới: Khởi tạo phiên Onboarding, tự động điền Tên ở Bước 2 $\rightarrow$ chuyển tiếp tới Bước 3 thu thập SĐT.
- [x] T025 [US3] Tích hợp Google Identity Services (GIS) One Tap button trên Angular SPA trong `angular/src/app/auth/login/` và `angular/src/app/auth/register/step1-account/`.
- [x] T026 [P] [US3] Viết Integration Tests với Mock Google Validator tại `test/BookingHotel.Application.Tests/Auth/AuthAppService_GoogleAuth_Tests.cs`.

**Checkpoint US3**: Hoàn thành User Story 3, đăng nhập/đăng ký bằng Google liền mạch không cần reload trang.

---

## Phase 6: User Story 4 - Quên Mật Khẩu với Cloudflare Turnstile (Priority: P2)

**Mục tiêu**: Người dùng có thể khôi phục mật khẩu an toàn, có Captcha chống bot spam email.

### Implementation for User Story 4
- [x] T027 [P] [US4] Tạo DTOs `ForgotPasswordInputDto.cs` và `ResetPasswordInputDto.cs` trong `src/BookingHotel.Application.Contracts/Auth/Dtos/`.
- [x] T028 [US4] Khai báo `SendPasswordResetCodeAsync` và `ResetPasswordAsync` trong `src/BookingHotel.Application.Contracts/Auth/IAuthAppService.cs`.
- [x] T029 [US4] Triển khai `SendPasswordResetCodeAsync` trong `src/BookingHotel.Application/Auth/AuthAppService.cs`:
  - Xác thực Captcha token qua `ICaptchaValidator` (Cloudflare Turnstile).
  - Sinh password reset token (hạn 15 phút), gửi mail qua `IEmailSender` (hoặc ghi log console trong môi trường Dev).
- [x] T030 [US4] Triển khai `ResetPasswordAsync` trong `src/BookingHotel.Application/Auth/AuthAppService.cs`:
  - Kiểm tra reset token, cập nhật mật khẩu mới, reset toàn bộ bộ đếm brute force về 0, hủy phiên cũ.
- [x] T031 [US4] Xây dựng UI Component Quên mật khẩu `forgot-password.component.ts` tại `angular/src/app/auth/forgot-password/` tích hợp widget Cloudflare Turnstile.
- [x] T032 [US4] Xây dựng UI Component Đặt lại mật khẩu `reset-password.component.ts` tại `angular/src/app/auth/reset-password/` nhận token từ query param và form mật khẩu mới.
- [x] T033 [P] [US4] Viết Integration Tests kiểm chứng luồng Quên mật khẩu và xác minh Captcha tại `test/BookingHotel.Application.Tests/Auth/AuthAppService_PasswordReset_Tests.cs`.

**Checkpoint US4**: Hoàn thành User Story 4, tính năng Quên & Đặt lại mật khẩu được bảo vệ bởi Captcha.

---

## Phase 7: User Story 5 - Quản Trị Viên Mở Khóa Tài Khoản (Priority: P3)

**Mục tiêu**: Cung cấp thẩm quyền độc quyền cho Quản trị viên (Admin) để mở khóa tài khoản bị vô hiệu hóa do brute-force.

### Implementation for User Story 5
- [x] T034 [P] [US5] Thêm quyền phân quyền `BookingHotelPermissions.Identity.UnlockUser` trong `src/BookingHotel.Application.Contracts/Permissions/BookingHotelPermissions.cs`.
- [x] T035 [US5] Triển khai phương thức `UnlockUserAsync(Guid userId)` trong `src/BookingHotel.Application/Auth/AuthAppService.cs` (`[Authorize(BookingHotelPermissions.Identity.UnlockUser)]`, đặt `IsActive = true`, reset `FailedAttemptsInCurrentCycle = 0`, `TotalFailedAttempts = 0`, `CurrentLockoutMinutes = 0`, ghi `IdentitySecurityLog`).
- [x] T036 [P] [US5] Viết Unit/Integration Test cho chức năng Admin Unlock tại `test/BookingHotel.Application.Tests/Auth/AdminUnlockUser_Tests.cs`.

**Checkpoint US5**: Hoàn thành User Story 5, Admin có khả năng mở khóa và khôi phục tài khoản an toàn.

---

## Phase 8: Hoàn Thiện UI/UX, Bản Địa Hóa Song Ngữ & Nghiệm Thu

**Mục đích**: Tinh chỉnh giao diện theo concept nhận diện thương hiệu của Hiến pháp, hoàn thiện bản dịch song ngữ và kiểm tra toàn diện.

- [x] T037 [P] Hoàn thiện bản dịch song ngữ `vi.json` và `en.json` cho toàn bộ nhãn form, cảnh báo số lần thử còn lại, đồng hồ đếm ngược và thông báo lỗi.
- [x] T038 [P] Tinh chỉnh Responsive SCSS/CSS cho toàn bộ phân hệ Auth trên Angular theo bảng màu Đỏ sẫm (`#990000`) và Đen (`#000000`) tại `angular/src/app/auth/`.
- [x] T039 Chạy toàn bộ test suite (`dotnet test`) để xác nhận 100% tests pass và kiểm tra đối chiếu các tiêu chí đo lường trong `quickstart.md`.

