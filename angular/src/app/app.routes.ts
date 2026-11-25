import { Routes } from '@angular/router';
import { CustomerLayoutComponent } from './layout/customer/customer-layout.component';
import { roleBasedRedirectGuard } from './admin/guards/role-based-redirect.guard';
import { firstLoginPasswordChangeGuard } from './auth/guards/first-login-password-change.guard';

export const APP_ROUTES: Routes = [
  // 1. Phân hệ Khách hàng & Trang công cộng
  {
    path: '',
    component: CustomerLayoutComponent,
    data: {
      layout: 'empty',
    },
    children: [
      {
        path: '',
        pathMatch: 'full',
        loadComponent: () => import('./home/home.component').then(c => c.HomeComponent),
      },
    ],
  },

  // 2. Phân hệ Xác thực & Định danh
  {
    path: 'auth',
    loadChildren: () => import('./auth/auth.routes').then(m => m.AUTH_ROUTES),
    data: {
      layout: 'empty',
    },
  },
  { path: 'account/login', redirectTo: 'auth/login', pathMatch: 'full' },
  { path: 'account/register', redirectTo: 'auth/register', pathMatch: 'full' },
  { path: 'account/forgot-password', redirectTo: 'auth/forgot-password', pathMatch: 'full' },
  { path: 'account/reset-password', redirectTo: 'auth/reset-password', pathMatch: 'full' },

  // 3. Phân hệ Quản trị Nền tảng Host (Chỉ dành riêng cho Admin System)
  {
    path: 'admin',
    loadChildren: () => import('./admin/system/system.routes').then(m => m.SYSTEM_ROUTES),
  },

  // 4. Phân hệ Quản trị Chuỗi / Doanh nghiệp (Dành cho Admin Tenant)
  {
    path: 'tenant',
    loadChildren: () => import('./admin/tenant/tenant.routes').then(m => m.TENANT_ROUTES),
  },

  // 5. Phân hệ Điều hành Cơ sở Khách sạn (Dành cho Hotel Manager)
  {
    path: 'manager',
    loadChildren: () => import('./admin/manager/manager.routes').then(m => m.MANAGER_ROUTES),
  },

  // 6. Phân hệ Tác nghiệp Quầy Lễ tân (Dành cho Receptionist)
  {
    path: 'frontdesk',
    loadChildren: () => import('./admin/frontdesk/frontdesk.routes').then(m => m.FRONTDESK_ROUTES),
  },

  // Tuyến đường tiện ích & tương thích ngược từ các giai đoạn trước
  {
    path: 'ui-showcase',
    canActivate: [firstLoginPasswordChangeGuard],
    loadComponent: () => import('./admin/ui-showcase/ui-showcase.component').then(c => c.UiShowcaseComponent),
  },
  {
    path: 'room-rack-preview',
    canActivate: [firstLoginPasswordChangeGuard],
    loadComponent: () => import('./admin/room-rack-preview/room-rack-preview.component').then(c => c.RoomRackPreviewComponent),
  },
  // Chuyển hướng tương thích ngược (Redirect legacy paths)
  { path: 'admin/system', redirectTo: 'admin', pathMatch: 'full' },
  { path: 'admin/tenant', redirectTo: 'tenant', pathMatch: 'full' },
  { path: 'admin/manager', redirectTo: 'manager', pathMatch: 'full' },
  { path: 'admin/frontdesk', redirectTo: 'frontdesk', pathMatch: 'full' },

  // Module mặc định ABP (fallback)
  {
    path: 'identity',
    loadChildren: () => import('@abp/ng.identity').then(c => c.createRoutes()),
  },
  {
    path: 'tenant-management',
    loadChildren: () => import('@abp/ng.tenant-management').then(c => c.createRoutes()),
  },
  {
    path: 'setting-management',
    loadChildren: () => import('@abp/ng.setting-management').then(c => c.createRoutes()),
  },
];
