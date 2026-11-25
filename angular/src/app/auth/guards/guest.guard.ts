import { inject } from '@angular/core';
import { CanActivateFn, Router, UrlTree } from '@angular/router';
import { AuthService, ConfigStateService } from '@abp/ng.core';
import { OAuthStorage } from 'angular-oauth2-oidc';
import { getDefaultUrlForRoles, getUserRoles } from '../../admin/guards/role-based-redirect.guard';

/**
 * GuestGuard (Unauthenticated Guard):
 * Ngăn chặn người dùng đã đăng nhập truy cập vào trang đăng nhập hoặc các luồng auth của khách.
 * Nếu phát hiện phiên làm việc hợp lệ (token trong OAuthStorage hoặc AuthService.isAuthenticated),
 * Guard sẽ điều hướng người dùng về Trang chính tương ứng với vai trò của họ.
 */
export const guestGuard: CanActivateFn = (): boolean | UrlTree => {
  const authService = inject(AuthService);
  const oAuthStorage = inject(OAuthStorage);
  const configState = inject(ConfigStateService);
  const router = inject(Router);

  const storageToken = oAuthStorage.getItem('access_token');
  const localToken = typeof localStorage !== 'undefined' ? localStorage.getItem('access_token') : null;
  const token = storageToken || localToken;
  const hasAccessToken = !!token && token !== 'undefined' && token !== 'null';
  const isAuthenticated = authService.isAuthenticated || hasAccessToken;

  if (isAuthenticated) {
    const roles = getUserRoles(oAuthStorage, configState);
    const targetUrl = getDefaultUrlForRoles(roles);
    return router.parseUrl(targetUrl);
  }

  return true;
};

