# Hợp Đồng Định Tuyến & Dịch Vụ (Routing & Service Contracts): RM-002

**Feature Branch**: `002-replace-default-auth`  
**Date**: 2026-11-11  
**Status**: Completed  

---

## 1. CustomAuthService Specification (Angular SPA)

**Đường dẫn**: `angular/src/app/auth/services/custom-auth.service.ts`

```typescript
import { Injectable, inject } from '@angular/core';
import { Router, Params } from '@angular/router';
import { Observable, of } from 'rxjs';
import { AbpOAuthService, clearOAuthStorage } from '@abp/ng.oauth';
import { ConfigStateService } from '@abp/ng.core';

@Injectable({
  providedIn: 'root'
})
export class CustomAuthService extends AbpOAuthService {
  private router = inject(Router);

  /**
   * Ghi đè điều hướng đăng nhập: chuyển hướng nội bộ SPA tới /auth/login
   */
  override navigateToLogin(queryParams?: Params): void {
    this.router.navigate(['/auth/login'], { queryParams });
  }

  /**
   * Ghi đè đăng xuất: xóa sạch token, reset app state và trở về Trang chủ dưới dạng khách
   */
  override logout(queryParams?: Params): Observable<any> {
    clearOAuthStorage(this.injector);
    const configState = this.injector.get(ConfigStateService);
    configState.refreshAppState().subscribe();
    this.router.navigate(['/']);
    return of(true);
  }
}
```

---

## 2. Cấu Hình Providers trong `app.config.ts`

**Đường dẫn**: `angular/src/app/app.config.ts`

```typescript
import { AuthService } from '@abp/ng.core';
import { CustomAuthService } from './auth/services/custom-auth.service';

export const appConfig: ApplicationConfig = {
  providers: [
    // ... các provider hiện tại
    provideAbpOAuth(),
    // Ghi đè AuthService bằng CustomAuthService
    {
      provide: AuthService,
      useClass: CustomAuthService
    },
    // ...
  ]
};
```

---

## 3. Cấu Hình Định Tuyến Chuyển Hướng trong `app.routes.ts`

**Đường dẫn**: `angular/src/app/app.routes.ts`

```typescript
export const APP_ROUTES: Routes = [
  {
    path: '',
    pathMatch: 'full',
    loadComponent: () => import('./home/home.component').then(c => c.HomeComponent),
  },
  {
    path: 'auth',
    loadChildren: () => import('./auth/auth.routes').then(m => m.AUTH_ROUTES),
  },
  // Chuyển hướng các route cũ của ABP Account sang phân hệ Auth mới
  { path: 'account/login', redirectTo: 'auth/login', pathMatch: 'full' },
  { path: 'account/register', redirectTo: 'auth/register', pathMatch: 'full' },
  { path: 'account/forgot-password', redirectTo: 'auth/forgot-password', pathMatch: 'full' },
  { path: 'account/reset-password', redirectTo: 'auth/reset-password', pathMatch: 'full' },
  {
    path: 'identity',
    loadChildren: () => import('@abp/ng.identity').then(c => c.createRoutes()),
  },
  // ...
];
```

---

## 4. Hợp Đồng Đồng Bộ Token trong `LoginComponent`

**Đường dẫn**: `angular/src/app/auth/login/login.component.ts`

```typescript
// Khi đăng nhập thành công:
this.authProxy.loginWithEmail(this.loginForm.value).subscribe({
  next: (res: LoginResultDto) => {
    this.isLoading = false;
    if (res.isSuccess && res.accessToken) {
      // 1. Lưu token vào OAuthStorage
      this.oAuthStorage.setItem('access_token', res.accessToken);
      if (res.expiresIn) {
        this.oAuthStorage.setItem('expires_at', '' + (Date.now() + res.expiresIn * 1000));
      }
      
      // 2. Kích hoạt cập nhật ứng dụng tức thời không reload trang
      this.configState.refreshAppState().subscribe({
        next: () => {
          const returnUrl = this.route.snapshot.queryParams['returnUrl'] || '/';
          this.router.navigateByUrl(returnUrl);
        },
        error: () => {
          this.router.navigate(['/']);
        }
      });
      return;
    }
    // ... xử lý lỗi và brute-force
  }
});
```

---

## 5. Hợp Đồng Chuyển Hướng Backend Razor Page

**Đường dẫn**: `src/BookingHotel.HttpApi.Host/Pages/Account/Login.cshtml.cs`

```csharp
using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;

namespace BookingHotel.Pages.Account;

public class LoginModel : PageModel
{
    private readonly IConfiguration _configuration;

    public LoginModel(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public IActionResult OnGet(string? returnUrl = null)
    {
        var angularUrl = _configuration["App:AngularUrl"]?.TrimEnd('/') ?? "http://localhost:4200";
        var redirectUrl = $"{angularUrl}/auth/login";

        if (!string.IsNullOrWhiteSpace(returnUrl))
        {
            redirectUrl += $"?returnUrl={Uri.EscapeDataString(returnUrl)}";
        }

        return Redirect(redirectUrl);
    }
}
```

---

## 6. Hợp Đồng Quy Chuẩn Mật Khẩu (Password Complexity Contracts)

### 6.1. Backend DTO Contract
```csharp
public class RegisterStep1InputDto
{
    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = default!;

    [Required]
    [StringLength(128, MinimumLength = 8)]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).{8,}$", 
        ErrorMessage = "::Auth:PasswordComplexityFailed")]
    public string Password { get; set; } = default!;

    [Required]
    [Compare(nameof(Password))]
    public string ConfirmPassword { get; set; } = default!;
}

public class ResetPasswordInputDto
{
    [Required]
    public string UserId { get; set; } = default!;

    [Required]
    public string ResetToken { get; set; } = default!;

    [Required]
    [StringLength(128, MinimumLength = 8)]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).{8,}$", 
        ErrorMessage = "::Auth:PasswordComplexityFailed")]
    public string NewPassword { get; set; } = default!;

    [Required]
    [Compare(nameof(NewPassword))]
    public string ConfirmNewPassword { get; set; } = default!;
}
```

### 6.2. Frontend Angular Password Complexity Validator
```typescript
import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

export const passwordComplexityValidator: ValidatorFn = (control: AbstractControl): ValidationErrors | null => {
  const value = control.value;
  if (!value) return null;

  const hasMinLength = value.length >= 8;
  const hasUpperCase = /[A-Z]/.test(value);
  const hasLowerCase = /[a-z]/.test(value);
  const hasNumeric = /[0-9]/.test(value);
  const hasSpecial = /[^a-zA-Z0-9]/.test(value);

  const isValid = hasMinLength && hasUpperCase && hasLowerCase && hasNumeric && hasSpecial;
  return isValid ? null : {
    passwordComplexity: {
      hasMinLength,
      hasUpperCase,
      hasLowerCase,
      hasNumeric,
      hasSpecial
    }
  };
};
```

---

## 7. Quy Chuẩn Xử Lý Hoàn Tất Đăng Ký (Post-Registration Contract)

### 7.1. Contract Xử Lý Bước 3 (`onStep3Submit`)
```typescript
  onStep3Submit(): void {
    if (this.step3Form.invalid) return;

    this.isLoading = true;
    this.errorMessage = '';
    this.cdr.detectChanges();

    this.authProxy.registerStep3({
      registrationToken: this.registrationToken,
      phoneNumber: this.step3Form.value.phoneNumber
    }).subscribe({
      next: (res) => {
        this.isLoading = false;
        // KHÔNG lưu token vào OAuthStorage
        // KHÔNG kích hoạt refreshAppState()
        this.currentStep = 4; // Chuyển sang Màn hình Thành công
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.isLoading = false;
        this.errorMessage = err?.error?.error?.message || 'Hoàn tất đăng ký không thành công.';
        this.cdr.detectChanges();
      }
    });
  }
```

### 7.2. Giao Diện Màn Hình Thành Công (Bước 4 Markup)
```html
<div *ngIf="currentStep === 4" class="success-screen">
  <div class="success-icon">🎉</div>
  <h3>{{ '::Auth:RegistrationSuccessTitle' | abpLocalization }}</h3>
  <p>{{ '::Auth:RegistrationSuccessMessage' | abpLocalization: step2Form.get('fullName')?.value }}</p>
  <p class="subtitle-msg">{{ '::Auth:PleaseLoginToContinue' | abpLocalization }}</p>
  <a routerLink="/auth/login" class="btn btn-primary btn-block">
    {{ '::Auth:BackToLogin' | abpLocalization }}
  </a>
</div>
```

---

## 8. Hợp Đồng Bảo Vệ Khách & Chặn Người Dùng Đã Đăng Nhập (GuestGuard Contract)

### 8.1. Guard Function: `guestGuard` (`angular/src/app/auth/guards/guest.guard.ts`)
```typescript
import { inject } from '@angular/core';
import { CanActivateFn, Router, UrlTree } from '@angular/router';
import { AuthService } from '@abp/ng.core';
import { OAuthStorage } from 'angular-oauth2-oidc';

export const guestGuard: CanActivateFn = (): boolean | UrlTree => {
  const authService = inject(AuthService);
  const oAuthStorage = inject(OAuthStorage);
  const router = inject(Router);

  const token = oAuthStorage.getItem('access_token');
  const hasAccessToken = !!token && token !== 'undefined' && token !== 'null';
  const isAuthenticated = authService.isAuthenticated || hasAccessToken;

  if (isAuthenticated) {
    return router.createUrlTree(['/']);
  }

  return true;
};
```

### 8.2. Cấu Hình Tuyến Đường (`angular/src/app/auth/auth.routes.ts`)
```typescript
export const AUTH_ROUTES: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'login' },
  { path: 'login', canActivate: [guestGuard], loadComponent: () => import('./login/login.component').then(c => c.LoginComponent) },
  { path: 'register', canActivate: [guestGuard], loadComponent: () => import('./register/register.component').then(c => c.RegisterComponent) },
  { path: 'forgot-password', canActivate: [guestGuard], loadComponent: () => import('./forgot-password/forgot-password.component').then(c => c.ForgotPasswordComponent) },
  { path: 'reset-password', canActivate: [guestGuard], loadComponent: () => import('./reset-password/reset-password.component').then(c => c.ResetPasswordComponent) }
];
```

