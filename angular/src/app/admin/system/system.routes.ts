import { Routes } from '@angular/router';
import { SystemAdminLayoutComponent } from './system-admin-layout/system-admin-layout.component';
import { systemAdminGuard } from '../guards/role-guards';
import { roleBasedRedirectGuard } from '../guards/role-based-redirect.guard';
import { firstLoginPasswordChangeGuard } from '../../auth/guards/first-login-password-change.guard';

export const SYSTEM_ROUTES: Routes = [
  {
    path: '',
    canActivate: [roleBasedRedirectGuard],
    component: SystemAdminLayoutComponent,
    canActivateChild: [systemAdminGuard, firstLoginPasswordChangeGuard],
    children: [
      {
        path: '',
        pathMatch: 'full',
        redirectTo: 'dashboard',
      },
      {
        path: 'dashboard',
        loadComponent: () =>
          import('./system-dashboard/system-dashboard.component').then(c => c.SystemDashboardComponent),
      },
      {
        path: 'tenants',
        loadComponent: () =>
          import('./tenant-management/tenant-list.component').then(c => c.TenantListComponent),
      },
      {
        path: 'users',
        loadComponent: () =>
          import('../../identity/users/user-list.component').then(c => c.UserListComponent),
      },
      {
        path: 'roles',
        loadComponent: () =>
          import('../../identity/roles/role-list.component').then(c => c.RoleListComponent),
      },
      // Hỗ trợ alias tương thích
      { path: 'system/dashboard', redirectTo: 'dashboard', pathMatch: 'full' },
      { path: 'system/tenants', redirectTo: 'tenants', pathMatch: 'full' },
      { path: 'system/users', redirectTo: 'users', pathMatch: 'full' },
      { path: 'system/roles', redirectTo: 'roles', pathMatch: 'full' },
    ],
  },
];
