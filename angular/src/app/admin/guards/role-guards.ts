import { inject } from '@angular/core';
import { CanActivateFn, Router, UrlTree } from '@angular/router';
import { ConfigStateService, LocalizationService } from '@abp/ng.core';
import { OAuthStorage } from 'angular-oauth2-oidc';
import { getUserRoles, getUserTenantId } from './role-based-redirect.guard';
import { BhToastrService } from '../../shared/components/toastr/bh-toastr.service';

function getFallbackUrlTree(router: Router, roles: string[], isTenantUser?: boolean): UrlTree {
  if (isTenantUser) {
    if (roles.includes('TenantAdmin') || roles.includes('admin')) {
      return router.createUrlTree(['/tenant/dashboard']);
    }
    if (roles.includes('HotelManager')) {
      return router.createUrlTree(['/manager/dashboard']);
    }
    if (roles.includes('Receptionist')) {
      return router.createUrlTree(['/frontdesk/room-rack']);
    }
    return router.createUrlTree(['/']);
  }

  if (roles.includes('SystemAdmin') || roles.includes('admin')) {
    return router.createUrlTree(['/admin/dashboard']);
  }
  if (roles.includes('TenantAdmin')) {
    return router.createUrlTree(['/tenant/dashboard']);
  }
  if (roles.includes('HotelManager')) {
    return router.createUrlTree(['/manager/dashboard']);
  }
  if (roles.includes('Receptionist')) {
    return router.createUrlTree(['/frontdesk/room-rack']);
  }
  return router.createUrlTree(['/']);
}

/**
 * Guard bảo vệ Portal Quản trị Nền tảng Host (/admin/*)
 * Chỉ duy nhất SystemAdmin của Host (không thuộc Tenant) có quyền truy cập
 */
export const systemAdminGuard: CanActivateFn = (route, state): boolean | UrlTree => {
  const router = inject(Router);
  const oAuthStorage = inject(OAuthStorage);
  const configState = inject(ConfigStateService);
  const toastr = inject(BhToastrService);
  const localization = inject(LocalizationService);

  const tenantId = getUserTenantId(oAuthStorage, configState);
  const isTenantUser = !!tenantId;
  const roles = getUserRoles(oAuthStorage, configState);

  // SystemAdmin PHẢI là tài khoản cấp Host (không có TenantId)
  if (!isTenantUser && (roles.includes('SystemAdmin') || roles.includes('admin'))) {
    return true;
  }

  toastr.warning(
    localization.instant('Guard:HostAccessDenied'),
    localization.instant('Common:AccessDenied')
  );
  return getFallbackUrlTree(router, roles, isTenantUser);
};

/**
 * Guard bảo vệ Portal Quản trị Chuỗi Khách sạn (/tenant/*)
 * Dành cho TenantAdmin (và SystemAdmin của Host xuống hỗ trợ kỹ thuật)
 */
export const tenantAdminGuard: CanActivateFn = (route, state): boolean | UrlTree => {
  const router = inject(Router);
  const oAuthStorage = inject(OAuthStorage);
  const configState = inject(ConfigStateService);
  const toastr = inject(BhToastrService);
  const localization = inject(LocalizationService);

  const tenantId = getUserTenantId(oAuthStorage, configState);
  const isTenantUser = !!tenantId;
  const roles = getUserRoles(oAuthStorage, configState);

  if (
    roles.includes('TenantAdmin') ||
    (isTenantUser && roles.includes('admin')) ||
    (!isTenantUser && (roles.includes('SystemAdmin') || roles.includes('admin')))
  ) {
    return true;
  }

  toastr.warning(
    localization.instant('Guard:TenantAccessDenied'),
    localization.instant('Common:AccessDenied')
  );
  return getFallbackUrlTree(router, roles, isTenantUser);
};

/**
 * Guard bảo vệ Portal Quản lý Cơ sở Khách sạn (/manager/*)
 * Dành cho HotelManager (và cấp trên: TenantAdmin, SystemAdmin)
 */
export const hotelManagerGuard: CanActivateFn = (route, state): boolean | UrlTree => {
  const router = inject(Router);
  const oAuthStorage = inject(OAuthStorage);
  const configState = inject(ConfigStateService);
  const toastr = inject(BhToastrService);
  const localization = inject(LocalizationService);

  const roles = getUserRoles(oAuthStorage, configState);

  if (roles.includes('HotelManager') || roles.includes('TenantAdmin') || roles.includes('SystemAdmin') || roles.includes('admin')) {
    return true;
  }

  toastr.warning(
    localization.instant('Guard:ManagerAccessDenied'),
    localization.instant('Common:AccessDenied')
  );
  return getFallbackUrlTree(router, roles);
};

/**
 * Guard bảo vệ Portal Tác nghiệp Quầy Lễ tân (/frontdesk/*)
 * Dành cho Receptionist (và các cấp quản lý khi kiểm tra/kiêm nhiệm)
 */
export const receptionistGuard: CanActivateFn = (route, state): boolean | UrlTree => {
  const router = inject(Router);
  const oAuthStorage = inject(OAuthStorage);
  const configState = inject(ConfigStateService);
  const toastr = inject(BhToastrService);
  const localization = inject(LocalizationService);

  const roles = getUserRoles(oAuthStorage, configState);

  if (roles.includes('Receptionist') || roles.includes('HotelManager') || roles.includes('TenantAdmin') || roles.includes('SystemAdmin') || roles.includes('admin')) {
    return true;
  }

  toastr.warning(
    localization.instant('Guard:ReceptionistAccessDenied'),
    localization.instant('Common:AccessDenied')
  );
  return getFallbackUrlTree(router, roles);
};
