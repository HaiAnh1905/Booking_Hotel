import { inject } from '@angular/core';
import { CanActivateFn, Router, UrlTree } from '@angular/router';
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

/**
 * FirstLoginPasswordChangeGuard:
 * Kiểm tra xem tài khoản nhân sự có cờ should_change_password trong JWT Token hay không.
 * Nếu có (nhân sự mới được tạo bởi TenantAdmin/SystemAdmin), bắt buộc điều hướng tới /auth/change-password.
 */
export const firstLoginPasswordChangeGuard: CanActivateFn = (route, state): boolean | UrlTree => {
  const oAuthStorage = inject(OAuthStorage);
  const router = inject(Router);

  const storageToken = oAuthStorage.getItem('access_token');
  const localToken = typeof localStorage !== 'undefined' ? localStorage.getItem('access_token') : null;
  const token = storageToken || localToken;

  if (!token || token === 'undefined' || token === 'null') {
    return true;
  }

  const payload = parseJwt(token);
  const shouldChange = payload?.should_change_password === 'true';

  if (shouldChange && !state.url.includes('/auth/change-password')) {
    return router.createUrlTree(['/auth/change-password']);
  }

  return true;
};

