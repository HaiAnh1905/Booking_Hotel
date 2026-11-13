import { inject } from '@angular/core';
import { CanActivateFn, Router, UrlTree } from '@angular/router';
import { AuthService } from '@abp/ng.core';
import { OAuthStorage } from 'angular-oauth2-oidc';

/**
 * GuestGuard (Unauthenticated Guard):
 * Ngăn chặn người dùng đã đăng nhập truy cập vào trang đăng nhập hoặc các luồng auth của khách.
 * Nếu phát hiện phiên làm việc hợp lệ (token trong OAuthStorage hoặc AuthService.isAuthenticated),
 * Guard sẽ buộc điều hướng người dùng về Trang chủ ('/').
 */
export const guestGuard: CanActivateFn = (): boolean | UrlTree => {
  const authService = inject(AuthService);
  const oAuthStorage = inject(OAuthStorage);
  const router = inject(Router);

  const storageToken = oAuthStorage.getItem('access_token');
  const localToken = typeof localStorage !== 'undefined' ? localStorage.getItem('access_token') : null;
  const token = storageToken || localToken;
  const hasAccessToken = !!token && token !== 'undefined' && token !== 'null';
  const isAuthenticated = authService.isAuthenticated || hasAccessToken;

  if (isAuthenticated) {
    return router.createUrlTree(['/']);
  }

  return true;
};

