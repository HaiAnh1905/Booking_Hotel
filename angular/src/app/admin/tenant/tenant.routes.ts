import { Routes } from '@angular/router';
import { TenantAdminLayoutComponent } from './tenant-admin-layout/tenant-admin-layout.component';
import { systemAdminGuard, tenantAdminGuard } from '../guards/role-guards';
import { firstLoginPasswordChangeGuard } from '../../auth/guards/first-login-password-change.guard';

export const TENANT_ROUTES: Routes = [
  {
    path: '',
    component: TenantAdminLayoutComponent,
    canActivateChild: [tenantAdminGuard, firstLoginPasswordChangeGuard],
    children: [
      {
        path: '',
        pathMatch: 'full',
        redirectTo: 'dashboard',
      },
      {
        path: 'dashboard',
        loadComponent: () =>
          import('./tenant-dashboard/tenant-dashboard.component').then(c => c.TenantDashboardComponent),
      },
      {
        path: 'hotels',
        loadComponent: () =>
          import('./hotels/hotel-list.component').then(c => c.HotelListComponent),
      },
      {
        path: 'staff',
        loadComponent: () =>
          import('./staff/staff-list.component').then(c => c.StaffListComponent),
      },
      {
        path: 'roles',
        canActivate: [systemAdminGuard],
        loadComponent: () =>
          import('../../identity/roles/role-list.component').then(c => c.RoleListComponent),
      },
      {
        path: 'bookings',
        loadComponent: () =>
          import('./bookings/booking-surveillance.component').then(c => c.BookingSurveillanceComponent),
      },
      {
        path: 'reports',
        loadComponent: () =>
          import('./reports/tenant-reports.component').then(c => c.TenantReportsComponent),
      },
      {
        path: 'rate-plans',
        loadComponent: () =>
          import('./rates/tenant-rate-plans.component').then(c => c.TenantRatePlansComponent),
      },
      {
        path: 'organization',
        loadComponent: () =>
          import('./settings/organization-settings.component').then(c => c.OrganizationSettingsComponent),
      },
      // Hỗ trợ alias tương thích
      { path: 'tenant/dashboard', redirectTo: 'dashboard', pathMatch: 'full' },
      { path: 'tenant/hotels', redirectTo: 'hotels', pathMatch: 'full' },
      { path: 'tenant/staff', redirectTo: 'staff', pathMatch: 'full' },
      { path: 'tenant/roles', redirectTo: 'roles', pathMatch: 'full' },
      { path: 'tenant/bookings', redirectTo: 'bookings', pathMatch: 'full' },
      { path: 'tenant/reports', redirectTo: 'reports', pathMatch: 'full' },
      { path: 'tenant/rate-plans', redirectTo: 'rate-plans', pathMatch: 'full' },
      { path: 'tenant/organization', redirectTo: 'organization', pathMatch: 'full' },
    ],
  },
];
