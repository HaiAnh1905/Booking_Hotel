# Tasks: RM-002 - Thay Thế Form Auth Mặc Định ABP Bằng Form Tùy Chỉnh RM-001

**Branch**: `002-replace-default-auth`  
**Prerequisites**: [plan.md](plan.md), [spec.md](spec.md), [research.md](research.md), [data-model.md](data-model.md), [contracts/auth-routing-contracts.md](contracts/auth-routing-contracts.md), [quickstart.md](quickstart.md)  
**Input**: Design documents from `specs/002-replace-default-auth/`  

---

## Format: `[ID] [P?] [Story] Description`
- **[P]**: Có thể thực thi song song (khác file, không phụ thuộc nhau).
- **[Story]**: Gắn với User Story tương ứng (`[US1]`, `[US2]`, `[US3]`, `[US4]`).
- Mọi mô tả đều có đường dẫn file cụ thể trong dự án.

---

## Phase 1: Setup & Core Infrastructure

**Mục đích**: Khởi tạo service can thiệp tầng xác thực và cấu hình Dependency Injection trên Angular SPA.

- [x] T001 Khởi tạo file dịch vụ `CustomAuthService` tại `angular/src/app/auth/services/custom-auth.service.ts` kế thừa `AbpOAuthService` từ gói `@abp/ng.oauth`.
- [x] T002 [P] Cấu hình Dependency Injection trong `angular/src/app/app.config.ts` để ghi đè `AuthService` bằng `CustomAuthService` ({ provide: AuthService, useClass: CustomAuthService }).

---

## Phase 2: User Story 1 - Chuyển Hướng Nút Đăng Nhập & Header LeptonX Vào Form Mới (Priority: P1) 🎯 MVP

**Mục tiêu**: Bất kỳ thao tác kích hoạt đăng nhập nào trên SPA (Header LeptonX, Home banner, Route Guards) đều mở form tùy chỉnh `/auth/login`.

### Implementation for User Story 1
- [x] T003 [US1] Triển khai phương thức `navigateToLogin(queryParams?: Params)` trong `angular/src/app/auth/services/custom-auth.service.ts` sử dụng `Router.navigate(['/auth/login'], { queryParams })`.
- [x] T004 [US1] Kiểm tra và cập nhật nút đăng nhập `login()` trong `angular/src/app/home/home.component.ts` để đảm bảo chuyển hướng chính xác đến `/auth/login`.
- [x] T005 [P] [US1] Kiểm tra thủ công và xác nhận việc nhấp nút Login trên Header LeptonX Theme tự động mở `/auth/login` mà không reload trang và không chuyển hướng sang backend.

**Checkpoint US1**: Hoàn thành User Story 1, nút Login toàn hệ thống đã dẫn thẳng vào form RM-001.

---

## Phase 3: User Story 2 - Đồng Bộ Phiên Đăng Nhập Với ABP Session & LeptonX (Priority: P1) 🎯 MVP

**Mục tiêu**: Người dùng sau khi đăng nhập thành công tại `/auth/login` được cập nhật tức thời phiên làm việc, avatar, quyền hạn trong Angular SPA không cần reload trang; và đăng xuất về trang chủ an toàn.

### Implementation for User Story 2
- [x] T006 [US2] Tiêm `OAuthStorage` và `ConfigStateService` vào `angular/src/app/auth/login/login.component.ts`.
- [x] T007 [US2] Cập nhật logic xử lý sau khi đăng nhập Email thành công trong `angular/src/app/auth/login/login.component.ts`: lưu `access_token` và `expires_at` vào `OAuthStorage`, gọi `ConfigStateService.refreshAppState()` và điều hướng về `returnUrl` (hoặc `/`).
- [x] T008 [US2] Cập nhật logic xử lý sau khi đăng nhập Google thành công `handleGoogleSignIn()` trong `angular/src/app/auth/login/login.component.ts`: lưu token vào `OAuthStorage` và gọi `refreshAppState()`.
- [x] T009 [US2] Triển khai phương thức `logout()` trong `angular/src/app/auth/services/custom-auth.service.ts`: gọi `clearOAuthStorage(this.injector)`, kích hoạt `ConfigStateService.refreshAppState()` và điều hướng về trang chủ `['/']` dưới vai trò khách vãng lai.
- [x] T010 [P] [US2] Kiểm tra luồng Đăng nhập và Đăng xuất: xác nhận avatar/tên người dùng xuất hiện trên Header sau khi đăng nhập và biến mất sau khi đăng xuất mà không reload toàn bộ trang.

**Checkpoint US2**: Hoàn thành User Story 2, phiên đăng nhập đồng bộ mượt mà 100% với ABP Session.

---

## Phase 4: User Story 3 - Gỡ Bỏ Module `@abp/ng.account` & Chặn Tuyến Đường Cũ (Priority: P2)

**Mục tiêu**: Loại bỏ hoàn toàn bundle `@abp/ng.account` và tự động redirect mọi URL cũ `/account/*` sang `/auth/*`.

### Implementation for User Story 3
- [x] T011 [US3] Gỡ bỏ load route `@abp/ng.account` khỏi `angular/src/app/app.routes.ts`.
- [x] T012 [US3] Thiết lập các route chuyển hướng (Redirects) trong `angular/src/app/app.routes.ts`:
  - `/account/login` $\rightarrow$ `/auth/login`
  - `/account/register` $\rightarrow$ `/auth/register`
  - `/account/forgot-password` $\rightarrow$ `/auth/forgot-password`
  - `/account/reset-password` $\rightarrow$ `/auth/reset-password`
- [x] T013 [P] [US3] Kiểm tra điều hướng khi người dùng gõ trực tiếp URL `/account/login` trên thanh địa chỉ trình duyệt, đảm bảo chuyển tiếp tức thì sang `/auth/login`.

**Checkpoint US3**: Hoàn thành User Story 3, loại bỏ hoàn toàn các form cũ của ABP trên Angular.

---

## Phase 5: User Story 4 - Tự Động Chuyển Hướng Từ Backend Razor Pages Về Angular (Priority: P2)

**Mục tiêu**: Ngăn chặn người dùng truy cập thẳng vào form Razor Pages mặc định của backend tại `/Account/Login`.

### Implementation for User Story 4
- [x] T014 [US4] Tạo Razor View rỗng `Login.cshtml` tại `src/BookingHotel.HttpApi.Host/Pages/Account/Login.cshtml`.
- [x] T015 [US4] Triển khai `LoginModel` (`PageModel`) tại `src/BookingHotel.HttpApi.Host/Pages/Account/Login.cshtml.cs`: đọc cấu hình `App:AngularUrl`, tự động trả về HTTP 302 Redirect về `http://localhost:4200/auth/login` kèm query parameter `returnUrl`.
- [x] T016 [P] [US4] Kiểm thử truy cập trực tiếp `https://localhost:44386/Account/Login` trên backend và xác nhận nhận được mã 302 chuyển hướng về frontend Angular.

**Checkpoint US4**: Hoàn thành User Story 4, backend Razor Page được khóa và chuyển hướng an toàn.

---

## Phase 6: User Story 5 - Quy Chuẩn Độ Phức Tạp Mật Khẩu & Checklist Trực Quan (Priority: P1) 🎯

**Mục tiêu**: Bắt buộc mật khẩu có ít nhất 8 ký tự, ít nhất 1 chữ viết hoa, 1 chữ viết thường, 1 chữ số và 1 ký tự đặc biệt trên tất cả các luồng tạo và đặt lại mật khẩu; hiển thị Checklist trực quan thời gian thực trên giao diện.

### Implementation for User Story 5
- [x] T017 [P] [US5] Cập nhật `BookingHotelConsts.AdminPasswordDefaultValue` trong `src/BookingHotel.Domain/BookingHotelConsts.cs` từ `1q2w3E*` thành `1q2w3E*#` (8 ký tự, đáp ứng 100% chuẩn độ phức tạp mới).
- [x] T018 [P] [US5] Bổ sung tài nguyên chuỗi thông báo lỗi và 5 tiêu chí mật khẩu trong `src/BookingHotel.Domain.Shared/Localization/BookingHotel/vi.json` và `en.json`.
- [x] T019 [US5] Bổ sung DataAnnotation `[RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).{8,}$")]` trên `Password` trong `src/BookingHotel.Application.Contracts/Auth/Dtos/RegisterStep1InputDto.cs`.
- [x] T020 [P] [US5] Bổ sung DataAnnotation `[RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).{8,}$")]` trên `NewPassword` trong `src/BookingHotel.Application.Contracts/Auth/Dtos/ResetPasswordInputDto.cs`.
- [x] T021 [US5] Tạo custom validator `passwordComplexityValidator` tại `angular/src/app/auth/validators/password-complexity.validator.ts` kiểm tra chi tiết 5 cờ trạng thái (`hasMinLength`, `hasUpperCase`, `hasLowerCase`, `hasNumeric`, `hasSpecial`).
- [x] T022 [US5] Cập nhật Form Đăng ký Bước 1 (`register.component.ts`, `register.component.html`, `register.component.scss`): áp dụng `passwordComplexityValidator`, bổ sung khối giao diện Checklist 5 tiêu chuẩn trực quan (sáng xanh `✓` hoặc mờ `✗` thời gian thực khi gõ phím).
- [x] T023 [P] [US5] Cập nhật Form Đặt lại mật khẩu (`reset-password.component.ts`, `reset-password.component.html`, `reset-password.component.scss`): áp dụng `passwordComplexityValidator` và khối giao diện Checklist 5 tiêu chuẩn trực quan.
- [x] T024 [P] [US5] Bổ sung unit test backend trong `test/BookingHotel.Application.Tests/Auth/` kiểm tra các trường hợp mật khẩu không hợp lệ (< 8 ký tự, thiếu hoa, thiếu thường, thiếu số, thiếu ký tự đặc biệt) bị từ chối với validation error.

**Checkpoint US5**: Hoàn thành User Story 5, quy chuẩn độ phức tạp mật khẩu hoạt động đồng bộ 100% trên cả Frontend và Backend.

---

## Phase 7: User Story 6 - Yêu Cầu Đăng Nhập Thủ Công Sau Khi Đăng Ký Thành Công (Priority: P1) 🎯

**Mục tiêu**: Người dùng sau khi đăng ký xong Bước 3 không được tự động đăng nhập; màn hình Bước 4 hiển thị thông báo thành công cùng nút "Quay lại Đăng nhập" dẫn tới `/auth/login` để người dùng tự đăng nhập thủ công vào hệ thống.

### Implementation for User Story 6
- [x] T028 [P] [US6] Bổ sung các chuỗi bản dịch song ngữ `Auth:BackToLogin`, `Auth:RegistrationSuccessTitle`, `Auth:RegistrationSuccessMessage`, `Auth:PleaseLoginToContinue` trong `src/BookingHotel.Domain.Shared/Localization/BookingHotel/vi.json` và `en.json`.
- [x] T029 [US6] Cập nhật phương thức `onStep3Submit()` trong `angular/src/app/auth/register/register.component.ts`: loại bỏ việc ghi token vào `OAuthStorage` (`setItem`) và loại bỏ gọi `refreshAppState()`.
- [x] T030 [US6] Cập nhật giao diện Màn hình Hoàn tất Bước 4 trong `angular/src/app/auth/register/register.component.html`: thay nút "Truy cập Trang chủ" bằng nút "Quay lại Đăng nhập" (`routerLink="/auth/login"`), kèm thông điệp chúc mừng và hướng dẫn đăng nhập.
- [x] T031 [P] [US6] Kiểm tra thủ công luồng hoàn tất đăng ký: xác nhận Header LeptonX giữ nguyên trạng thái khách vãng lai và nút "Quay lại Đăng nhập" điều hướng mượt mà về `/auth/login`.

**Checkpoint US6**: Hoàn thành User Story 6, quy trình đăng nhập lại sau khi đăng ký hoạt động chuẩn mực và tuân thủ Hiến pháp Điều IX.

---

## Phase 8: User Story 7 - Ngăn Chặn Người Dùng Đã Đăng Nhập Truy Cập Lại Trang Đăng Nhập (Priority: P1) 🎯

**Mục tiêu**: Người dùng sau khi đăng nhập thành công nếu cố ý truy cập vào `/auth/login` (hoặc các form auth khác) sẽ bị Route Guard hoặc logic khởi tạo trang chặn lại và tự động chuyển hướng về Trang chủ (`/`).

### Implementation for User Story 7
- [x] T035 [US7] Khởi tạo Route Guard `guestGuard` (`CanActivateFn`) tại `angular/src/app/auth/guards/guest.guard.ts`: kiểm tra `OAuthStorage` và `AuthService.isAuthenticated`, trả về `UrlTree(['/'])` nếu đã đăng nhập.
- [x] T036 [US7] Đăng ký `canActivate: [guestGuard]` trên các tuyến đường trong `angular/src/app/auth/auth.routes.ts` (`login`, `register`, `forgot-password`, `reset-password`).
- [x] T037 [US7] Bổ sung logic phòng vệ trong `LoginComponent.ngOnInit()` và `RegisterComponent.ngOnInit()`: nếu đã phát hiện token thì `this.router.navigate(['/'])`.

**Checkpoint US7**: Hoàn thành User Story 7, tính toàn vẹn phiên làm việc được bảo vệ 2 lớp, người dùng đã đăng nhập luôn được giữ ở Trang chủ hoặc khu vực nghiệp vụ.

---

## Phase 9: Hoàn Thiện & Nghiệm Thu Tổng Thể (Build Quality & Final Verification)

**Mục đích**: Kiểm tra biên dịch production, đảm bảo không có cảnh báo/lỗi, và xác nhận 100% kịch bản trong `quickstart.md`.

- [x] T032 [P] Chạy `npm run build` trên Angular SPA để xác minh biên dịch 0 lỗi, 0 cảnh báo.
- [x] T033 [P] Chạy toàn bộ test suite backend (`dotnet test`) để xác nhận tất cả các unit tests pass 100%.
- [x] T034 Đối chiếu và nghiệm thu Kịch bản 7 trong [quickstart.md](quickstart.md) (Xác nhận sau khi hoàn tất đăng ký, người dùng bấm nút "Quay lại Đăng nhập" để tự đăng nhập thủ công vào hệ thống).
- [x] T038 Đối chiếu và nghiệm thu Kịch bản 8 trong [quickstart.md](quickstart.md) (Xác nhận người dùng đã đăng nhập khi truy cập `/auth/login` bị chặn và đưa về Trang chủ).

