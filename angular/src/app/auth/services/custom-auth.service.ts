import { Injectable, inject } from '@angular/core';
import { Params, Router } from '@angular/router';
import { Observable, of } from 'rxjs';
import { AbpOAuthService, clearOAuthStorage } from '@abp/ng.oauth';
import { ConfigStateService } from '@abp/ng.core';

@Injectable({
  providedIn: 'root'
})
export class CustomAuthService extends AbpOAuthService {
  private router = inject(Router);

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

