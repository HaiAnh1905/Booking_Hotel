import { Injectable, inject } from '@angular/core';
import { Params, Router } from '@angular/router';
import { Observable, of } from 'rxjs';
import { AbpOAuthService, clearOAuthStorage } from '@abp/ng.oauth';
import { ConfigStateService } from '@abp/ng.core';
import { OAuthStorage } from 'angular-oauth2-oidc';

function parseJwt(token: string): any {
  try {
    const base64Url = token.split('.')[1];
    const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
    const jsonPayload = decodeURIComponent(
      atob(base64)
        .split('')
        .map(c => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2))
        .join('')
    );
    return JSON.parse(jsonPayload);
  } catch {
    return null;
  }
}

@Injectable({
  providedIn: 'root'
})
export class CustomAuthService extends AbpOAuthService {
  private router = inject(Router);

  /**
   * Ghi đè init(): Triệt tiêu hoàn toàn luồng OIDC Authorization Code / Refresh Token mặc định của ABP.
   * Hệ thống xác thực BookingHotel sử dụng mô hình JWT Standalone. Nếu gọi super.init(),
   * ABP OAuth sẽ kích hoạt tự động làm mới token qua OpenIddict (/connect/token),
   * gặp lỗi invalid_grant (400) và tự động gọi clearOAuthStorage() khiến người dùng bị logout khi nhấn F5.
   */
  override async init(): Promise<void> {
    if (typeof localStorage !== 'undefined') {
      const token = localStorage.getItem('access_token');
      const expiresAt = localStorage.getItem('expires_at');
      const storage = this.injector.get(OAuthStorage, null);

      if (token && token !== 'undefined' && token !== 'null') {
        const payload = parseJwt(token);
        // Tự động dọn dẹp các token cũ bị lỗi định dạng array (sub hoặc tenantid là mảng)
        if (payload && (Array.isArray(payload.sub) || Array.isArray(payload.tenantid))) {
          localStorage.removeItem('access_token');
          localStorage.removeItem('expires_at');
          if (storage) {
            storage.removeItem('access_token');
            storage.removeItem('expires_at');
          }
          this.navigateToLogin();
          return;
        }

        if (storage) {
          if (storage.getItem('access_token') !== token) {
            storage.setItem('access_token', token);
          }
          if (expiresAt && storage.getItem('expires_at') !== expiresAt) {
            storage.setItem('expires_at', expiresAt);
          }
        }
      }
    }

    (this as any).isInitialized = true;
    if ((this as any).resolveInitialized) {
      (this as any).resolveInitialized();
    }
    return Promise.resolve();
  }

  override get isAuthenticated(): boolean {
    const oAuthValid = super.isAuthenticated;
    if (oAuthValid) return true;
    if (typeof localStorage !== 'undefined') {
      const token = localStorage.getItem('access_token');
      const expiresAt = localStorage.getItem('expires_at');
      if (token && token !== 'undefined' && token !== 'null') {
        if (!expiresAt) return true;
        return parseInt(expiresAt, 10) > Date.now();
      }
    }
    return false;
  }

  override getAccessToken(): string {
    const token = super.getAccessToken();
    if (token && token !== 'undefined' && token !== 'null') return token;
    if (typeof localStorage !== 'undefined') {
      return localStorage.getItem('access_token') || '';
    }
    return '';
  }

  override getRefreshToken(): string {
    return '';
  }

  override getAccessTokenExpiration(): number {
    const exp = super.getAccessTokenExpiration();
    if (exp) return exp;
    if (typeof localStorage !== 'undefined') {
      const expiresAt = localStorage.getItem('expires_at');
      if (expiresAt) return parseInt(expiresAt, 10);
    }
    return Date.now() + 24 * 60 * 60 * 1000;
  }

  /**
   * Ngăn chặn việc gửi request refresh token lên endpoint OIDC /connect/token
   */
  override refreshToken(): Promise<any> {
    return Promise.resolve(null);
  }

  /**
   * Ghi đè điều hướng đăng nhập: chuyển hướng nội bộ Angular SPA tới /auth/login.
   * Nếu người dùng đang ở bất kỳ trang auth nào (ví dụ /auth/register, /auth/forgot-password,
   * /auth/reset-password, /auth/login), tuyệt đối không tự ý ngắt quãng trải nghiệm của họ.
   */
  override navigateToLogin(queryParams?: Params): void {
    const isAuthRoute = (typeof window !== 'undefined' && window.location.pathname.startsWith('/auth'))
      || this.router.url.startsWith('/auth');

    if (isAuthRoute) {
      return;
    }
    this.router.navigate(['/auth/login'], { queryParams });
  }

  /**
   * Ghi đè đăng xuất: xóa sạch token trong OAuthStorage, làm mới trạng thái phiên
   * và đưa người dùng về Trang chủ (/) dưới vai trò khách vãng lai.
   */
  override logout(queryParams?: Params): Observable<any> {
    clearOAuthStorage(this.injector);
    if (typeof localStorage !== 'undefined') {
      localStorage.removeItem('access_token');
      localStorage.removeItem('expires_at');
    }
    const configState = this.injector.get(ConfigStateService);
    configState.refreshAppState().subscribe();
    this.router.navigate(['/']);
    return of(true);
  }
}

