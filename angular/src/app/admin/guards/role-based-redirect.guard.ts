import { inject } from '@angular/core';
import { CanActivateFn, Router, UrlTree } from '@angular/router';
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

export function getUserTenantId(oAuthStorage: OAuthStorage, configState: ConfigStateService): string | null {
  // 1. Lấy từ ConfigStateService của ABP
  const currentUser = configState.getOne('currentUser');
  if (currentUser?.tenantId) {
    return currentUser.tenantId;
  }
  const currentTenant = configState.getOne('currentTenant');
  if (currentTenant?.id) {
    return currentTenant.id;
  }

  // 2. Lấy từ JWT Claims
  const storageToken = oAuthStorage.getItem('access_token');
  const localToken = typeof localStorage !== 'undefined' ? localStorage.getItem('access_token') : null;
  const token = storageToken || localToken;

  if (token && token !== 'undefined' && token !== 'null') {
    const payload = parseJwt(token);
    if (payload) {
      const tid = payload['tenantid'] 
        || payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/tenantid']
        || payload['TenantId'];
      if (tid) {
        return tid;
      }
    }
  }

  return null;
}

export function getUserRoles(oAuthStorage: OAuthStorage, configState: ConfigStateService): string[] {
  const rolesSet = new Set<string>();

  // 1. Lấy từ ConfigStateService của ABP
  const currentUser = configState.getOne('currentUser');
  if (currentUser?.roles && Array.isArray(currentUser.roles)) {
    currentUser.roles.forEach((r: string) => rolesSet.add(r));
  }

  // 2. Lấy từ JWT Claims
  const storageToken = oAuthStorage.getItem('access_token');
  const localToken = typeof localStorage !== 'undefined' ? localStorage.getItem('access_token') : null;
  const token = storageToken || localToken;

  if (token && token !== 'undefined' && token !== 'null') {
    const payload = parseJwt(token);
    if (payload) {
      const jwtRole = payload['role'] || payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];
      if (Array.isArray(jwtRole)) {
        jwtRole.forEach((r: string) => rolesSet.add(r));
      } else if (typeof jwtRole === 'string') {
        rolesSet.add(jwtRole);
      }
    }
  }

  // Nếu người dùng có TenantId và mang role 'admin', tự động nhận diện vai trò TenantAdmin
  const tenantId = getUserTenantId(oAuthStorage, configState);
  if (tenantId && rolesSet.has('admin') && !rolesSet.has('TenantAdmin')) {
    rolesSet.add('TenantAdmin');
  }

  return Array.from(rolesSet);
}

/**
 * Phân luồng URL mặc định theo vai trò nhân sự / quản trị:
 * - SystemAdmin (Host) -> /admin/dashboard
 * - TenantAdmin (Chuỗi khách sạn) -> /tenant/dashboard
 * - HotelManager -> /manager/dashboard
 * - Receptionist -> /frontdesk/room-rack
 * - Customer / Vãng lai -> '/'
 */
export function getDefaultUrlForRoles(roles: string[], returnUrl?: string, isTenantUser?: boolean): string {
  // Nếu có returnUrl cụ thể (không phải trang chủ '/', '/admin', hay luồng auth) thì ưu tiên đưa người dùng đến trang họ yêu cầu
  if (returnUrl && returnUrl !== '/' && returnUrl !== '/admin' && returnUrl !== '/admin/' && !returnUrl.startsWith('/auth')) {
    return returnUrl;
  }

  // Nếu người dùng thuộc Tenant
  if (isTenantUser) {
    if (roles.includes('TenantAdmin') || roles.includes('admin')) {
      return '/tenant/dashboard';
    }
    if (roles.includes('HotelManager')) {
      return '/manager/dashboard';
    }
    if (roles.includes('Receptionist')) {
      return '/frontdesk/dashboard';
    }
    return '/';
  }

  // Nếu là tài khoản Host
  if (roles.includes('SystemAdmin') || roles.includes('admin')) {
    return '/admin/dashboard';
  }
  if (roles.includes('TenantAdmin')) {
    return '/tenant/dashboard';
  }
  if (roles.includes('HotelManager')) {
    return '/manager/dashboard';
  }
  if (roles.includes('Receptionist')) {
    return '/frontdesk/dashboard';
  }

  return '/';
}

/**
 * RoleBasedRedirectGuard:
 * Tự động phân luồng điều hướng người dùng tại tuyến đường /admin gốc về Portal phù hợp:
 * - SystemAdmin -> /admin/dashboard
 * - TenantAdmin -> /tenant/dashboard
 * - HotelManager (kể cả khi kiêm nhiệm Lễ tân) -> /manager/dashboard
 * - Receptionist -> /frontdesk/room-rack
 * - Customer / Vãng lai -> /
 */
export const roleBasedRedirectGuard: CanActivateFn = (route, state): boolean | UrlTree => {
  const router = inject(Router);
  const oAuthStorage = inject(OAuthStorage);
  const configState = inject(ConfigStateService);

  const storageToken = oAuthStorage.getItem('access_token');
  const localToken = typeof localStorage !== 'undefined' ? localStorage.getItem('access_token') : null;
  const token = storageToken || localToken;

  if (!token || token === 'undefined' || token === 'null') {
    return router.createUrlTree(['/auth/login'], { queryParams: { returnUrl: state.url } });
  }

  const tenantId = getUserTenantId(oAuthStorage, configState);
  const isTenantUser = !!tenantId;
  const roles = getUserRoles(oAuthStorage, configState);

  // Nếu là Quản trị Chuỗi (TenantAdmin hoặc admin trong Tenant)
  if (roles.includes('TenantAdmin') || (isTenantUser && roles.includes('admin'))) {
    return router.createUrlTree(['/tenant/dashboard']);
  }

  // Nếu người dùng là SystemAdmin của Host:
  if (!isTenantUser && (roles.includes('SystemAdmin') || roles.includes('admin'))) {
    if (state.url === '/admin' || state.url === '/admin/') {
      return router.createUrlTree(['/admin/dashboard']);
    }
    return true;
  }

  // Nếu là Quản lý Cơ sở (HotelManager)
  if (roles.includes('HotelManager')) {
    return router.createUrlTree(['/manager/dashboard']);
  }

  // Nếu là Lễ tân (Receptionist)
  if (roles.includes('Receptionist')) {
    return router.createUrlTree(['/frontdesk/dashboard']);
  }

  // Khách hàng hoặc không có vai trò quản lý
  return router.createUrlTree(['/']);
};
