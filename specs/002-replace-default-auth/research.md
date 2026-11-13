# Nghiên Cứu Kỹ Thuật (Phase 0 Research): RM-002 - Thay Thế Form Auth Mặc Định ABP Bằng Form Tùy Chỉnh RM-001

**Feature Branch**: `002-replace-default-auth`  
**Date**: 2026-11-11  
**Status**: Completed  

---

## 1. Mục Tiêu Nghiên Cứu
Xác định giải pháp tối ưu và sạch nhất (cleanest architecture) để:
1. Chuyển hướng tập trung mọi thao tác "Login" trên Angular SPA (LeptonX Header, Route Guards, 401 Interceptors) về component `/auth/login` mà không cần reload trang.
2. Đồng bộ Access Token nhận được từ `AuthAppService` vào hệ thống lưu trữ chuẩn của ABP (`OAuthStorage`) và nạp lại cấu hình ứng dụng (`ConfigStateService.refreshAppState()`) mà không gây chớp giật hay reload trình duyệt.
3. Chặn và chuyển hướng triệt để các route cũ của module `@abp/ng.account` sang `/auth/*`.
4. Xử lý backend HTTP 302 Redirect từ endpoint `Account/Login` sang Angular URL `http://localhost:4200/auth/login`.

---

## 2. Phân Tích Cơ Chế Điều Hướng Của ABP Angular & LeptonX Theme

### 2.1. Chuỗi Gọi Mặc Định (Default Call Chain)
Trong kiến trúc ABP Angular 10.7 với theme LeptonX Lite:
```
[User clicks "Login" on Header]
       │
       ▼
AuthService.navigateToLogin()
       │
       ▼ (provided by provideAbpOAuth() -> AbpOAuthService)
AbpOAuthService.navigateToLogin()
       │
       ▼ (AuthCodeFlowStrategy)
OAuthService.initLoginFlow() -> Redirects to https://localhost:44386/Account/Login (Backend Razor Page)
```

### 2.2. Điểm Can Thiệp Tối Ưu (Optimal Interception Point)
Trong `provideAbpOAuth()`, service `AuthService` được đăng ký như sau:
```typescript
{
    provide: AuthService,
    useClass: AbpOAuthService
}
```
Nhờ cơ chế Dependency Injection của Angular, bất kỳ provider nào được khai báo sau trong danh sách `providers` của `app.config.ts` sẽ ghi đè provider trước đó.
Do đó, ta tạo `CustomAuthService extends AbpOAuthService`:
- `override navigateToLogin(queryParams?: Params)`: Sử dụng `Router.navigate(['/auth/login'], { queryParams })`.
- `override logout(queryParams?: Params)`: Gọi `clearOAuthStorage(this.injector)`, kích hoạt `configStateService.refreshAppState()` và điều hướng về trang chủ `['/']`.

**Đánh giá**:
- **Ưu điểm**: Tất cả các component trong hệ thống (Header LeptonX, AuthGuard, AsyncAuthGuard, HTTP Error Interceptor khi gặp 401) đều tiêm `AuthService` và tự động sử dụng `CustomAuthService`. Không cần can thiệp lẻ tẻ từng component.
- **Rủi ro**: Không có. `AbpOAuthService` vẫn giữ nguyên toàn bộ logic quản lý token, refresh token và authentication state.

---

## 3. Phân Tích Đồng Bộ Token & Session State (Zero-Reload Session Sync)

### 3.1. Cấu Trúc Lưu Trữ `OAuthStorage`
Trong ABP Angular, `OAuthStorage` mặc định lưu trữ các khóa sau trong `localStorage`:
- `access_token`: Chuỗi JWT token được ký bởi `JwtTokenGenerator` backend.
- `expires_at`: Mốc thời gian Unix (milli-giây) hết hạn của token.
- `refresh_token`: Chuỗi refresh token (nếu có).

### 3.2. Cập Nhật Cấu Hình `ConfigStateService.refreshAppState()`
Phương thức `ConfigStateService.refreshAppState()` gửi GET request tới `/api/abp/application-configuration`:
- Request này được `OAuthApiInterceptor` tự động đính kèm header `Authorization: Bearer <access_token>` từ `OAuthStorage`.
- Backend giải mã claims, trả về:
  - `currentUser`: `{ id, userName, email, roles: ['Customer'] }`.
  - `auth.grantedPolicies`: Danh sách quyền hạn.
- Khi Observable phát ra kết quả (emits), Angular Signals và RxJS Observables trong toàn bộ app (bao gồm LeptonX User Menu) lập tức cập nhật:
  - Nút "Login" trên Header biến mất.
  - Avatar, Tên người dùng và Menu Hồ sơ xuất hiện.
  - Người dùng được chuyển hướng tức thì tới `returnUrl` (hoặc `/`) mà **không cần reload trình duyệt (0 reload)**.

---

## 4. Phân Tích Gỡ Bỏ `@abp/ng.account` & Xử Lý Định Tuyến

Trong `angular/src/app/app.routes.ts`:
- **Trước**:
  ```typescript
  {
    path: 'account',
    loadChildren: () => import('@abp/ng.account').then(c => c.createRoutes()),
  }
  ```
- **Sau**:
  Xóa bỏ import `@abp/ng.account`, thay bằng các route redirect:
  ```typescript
  { path: 'account/login', redirectTo: 'auth/login', pathMatch: 'full' },
  { path: 'account/register', redirectTo: 'auth/register', pathMatch: 'full' },
  { path: 'account/forgot-password', redirectTo: 'auth/forgot-password', pathMatch: 'full' },
  { path: 'account/reset-password', redirectTo: 'auth/reset-password', pathMatch: 'full' },
  ```
- **Lợi ích**: Giảm kích thước bundle biên dịch Angular (loại bỏ chunk `abp-ng-account`), ngăn chặn hoàn toàn việc người dùng truy cập giao diện cũ.

---

## 5. Phân Tích Backend Redirect (`Account/Login`)

Trong ASP.NET Core Razor Pages, các trang Razor Page nằm trong project host (`src/BookingHotel.HttpApi.Host/Pages`) luôn có độ ưu tiên cao hơn các trang nhúng trong Virtual File System của ABP modules (`Volo.Abp.Account.Web.OpenIddict`).

Bằng cách tạo file `Pages/Account/Login.cshtml.cs`:
```csharp
public class LoginModel : PageModel
{
    private readonly IConfiguration _configuration;
    public LoginModel(IConfiguration configuration) => _configuration = configuration;

    public IActionResult OnGet(string? returnUrl = null)
    {
        var angularUrl = _configuration["App:AngularUrl"]?.TrimEnd('/') ?? "http://localhost:4200";
        var targetUrl = $"{angularUrl}/auth/login";
        if (!string.IsNullOrEmpty(returnUrl))
        {
            targetUrl += $"?returnUrl={Uri.EscapeDataString(returnUrl)}";
        }
        return Redirect(targetUrl);
    }
}
```
Mọi request truy cập trực tiếp vào backend endpoint `/Account/Login` sẽ lập tức nhận mã HTTP 302 chuyển hướng về giao diện Angular SPA mới.

---

## 6. Nghiên Cứu Quy Chuẩn Độ Phức Tạp Mật Khẩu (Password Complexity Policy)

### 6.1. Tiêu Chuẩn Bảo Mật NIST & OWASP
Quy chuẩn mật khẩu hiện đại yêu cầu:
1. Độ dài tối thiểu >= 8 ký tự.
2. Đa dạng hóa tập ký tự: Chữ in hoa (`[A-Z]`), chữ in thường (`[a-z]`), chữ số (`[0-9]`), và ký tự đặc biệt (`[^a-zA-Z0-9]`).
3. Xác thực kép (Client-side & Server-side):
   - Phía Frontend Angular: Cung cấp phản hồi thị giác ngay khi người dùng đang nhập để nâng cao trải nghiệm người dùng (UX) và giảm thiểu số lần gửi form thất bại.
   - Phía Backend .NET: Kiểm tra bất biến (Invariant Check) qua DataAnnotations và cấu hình ASP.NET Core Identity Password Options.

### 6.2. Cấu Hình IdentityOptions Trong ABP Framework
Trong tầng Domain/Host, ABP Identity hỗ trợ cấu hình tiêu chuẩn:
```csharp
Configure<IdentityOptions>(options =>
{
    options.Password.RequiredLength = 8;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireDigit = true;
    options.Password.RequireNonAlphanumeric = true;
});
```
Kết hợp cùng DataAnnotation `[RegularExpression]` trên DTO, hệ thống đảm bảo 100% không cho phép tạo mật khẩu yếu.

---

## 7. Nghiên Cứu Quy Trình Xác Thực Lại Sau Đăng Ký (Post-Registration Explicit Login)

### 7.1. Lý Do Kiến Trúc & An Ninh Thông Tin
Trong các hệ thống đặt phòng khách sạn và dịch vụ trực tuyến:
1. **Xác nhận ghi nhớ mật khẩu**: Việc yêu cầu người dùng tự đăng nhập lại một lần sau khi đăng ký giúp xác nhận người dùng đã ghi nhớ mật khẩu vừa tạo, tránh trường hợp gõ nhầm khi đăng ký rồi sau này không thể đăng nhập lại.
2. **Ngăn chặn phiên đăng nhập ngoài ý muốn**: Nếu người dùng đăng ký trên thiết bị công cộng (quầy lễ tân, internet café, thiết bị dùng chung), việc tự động đăng nhập sẽ tạo ra rủi ro lộ phiên làm việc.
3. **Tuân thủ Hiến pháp Điều IX**: "Không tự động đăng nhập bất kỳ tài khoản nào, tuyệt đối không prefill mật khẩu, chỉ duy trì phiên khi người dùng chủ động chọn Remember Me".

### 7.2. Phân Tích Kỹ Thuật (Frontend & Backend Impact)
- **Tầng Backend (`AuthAppService.RegisterStep3Async`)**:
  - Backend tiếp tục tạo tài khoản `IdentityUser`, gán role `Customer`, sinh access token và trả về `LoginResultDto`. Giữ nguyên DTO để đảm bảo tính tương thích ngược cho các client khác (mobile apps, 3rd party API).
- **Tầng Frontend (`RegisterComponent`)**:
  - Tại callback `next` của `registerStep3`: Loại bỏ lệnh ghi vào `OAuthStorage` (`this.oAuthStorage.setItem(...)`) và lệnh gọi `configState.refreshAppState()`.
  - Chuyển `currentStep = 4` (Màn hình Thành công).
  - Màn hình Thành công hiển thị thông điệp hướng dẫn và nút bấm CTA `Quay lại Đăng nhập` (`routerLink="/auth/login"`).
  - Khi người dùng nhấp vào nút này, trình duyệt điều hướng tới `/auth/login` với các ô nhập liệu sạch (clean input), sẵn sàng cho người dùng tự đăng nhập.

---

## 8. Kết Luận
Giải pháp kỹ thuật đã rõ ràng, hoàn toàn khả thi, không phá vỡ bất kỳ quy chuẩn nào của ABP Framework và đảm bảo 100% nguyên tắc Hiến pháp (Clean Architecture, DDD, Backend Authoritative).

