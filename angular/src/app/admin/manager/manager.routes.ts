import { Routes } from '@angular/router';
import { HotelManagerLayoutComponent } from './hotel-manager-layout/hotel-manager-layout.component';
import { hotelManagerGuard } from '../guards/role-guards';
import { firstLoginPasswordChangeGuard } from '../../auth/guards/first-login-password-change.guard';

export const MANAGER_ROUTES: Routes = [
  {
    path: '',
    component: HotelManagerLayoutComponent,
    canActivateChild: [hotelManagerGuard, firstLoginPasswordChangeGuard],
    children: [
      {
        path: '',
        pathMatch: 'full',
        redirectTo: 'dashboard',
      },
      {
        path: 'dashboard',
        loadComponent: () =>
          import('./manager-dashboard/manager-dashboard.component').then(c => c.ManagerDashboardComponent),
      },
      {
        path: 'hotel-profile',
        loadComponent: () =>
          import('./hotel-profile/hotel-profile.component').then(c => c.HotelProfileComponent),
      },
      {
        path: 'rooms',
        loadComponent: () =>
          import('./rooms/room-list/room-list.component').then(c => c.RoomListComponent),
      },
      {
        path: 'rates',
        loadComponent: () =>
          import('./rates/rates-management.component').then(c => c.RatesManagementComponent),
      },
      {
        path: 'bookings',
        loadComponent: () =>
          import('./bookings/booking-list/booking-list.component').then(c => c.BookingListComponent),
      },
      {
        path: 'guests',
        loadComponent: () =>
          import('./guests/guest-list/guest-list.component').then(c => c.GuestListComponent),
      },
      {
        path: 'reports',
        loadComponent: () =>
          import('./reports/manager-reports.component').then(c => c.ManagerReportsComponent),
      },
      {
        path: 'feedback',
        loadComponent: () =>
          import('./feedback/review-list/review-list.component').then(c => c.ReviewListComponent),
      },
      {
        path: 'complaints',
        loadComponent: () =>
          import('./feedback/complaint-list/complaint-list.component').then(c => c.ComplaintListComponent),
      },
      {
        path: 'approvals',
        loadComponent: () =>
          import('./manager-approvals/manager-approvals.component').then(c => c.ManagerApprovalsComponent),
      },
      // Hỗ trợ alias tương thích
      { path: 'manager/dashboard', redirectTo: 'dashboard', pathMatch: 'full' },
      { path: 'manager/hotel-profile', redirectTo: 'hotel-profile', pathMatch: 'full' },
      { path: 'manager/rooms', redirectTo: 'rooms', pathMatch: 'full' },
      { path: 'manager/rates', redirectTo: 'rates', pathMatch: 'full' },
      { path: 'manager/bookings', redirectTo: 'bookings', pathMatch: 'full' },
      { path: 'manager/guests', redirectTo: 'guests', pathMatch: 'full' },
      { path: 'manager/reports', redirectTo: 'reports', pathMatch: 'full' },
      { path: 'manager/feedback', redirectTo: 'feedback', pathMatch: 'full' },
      { path: 'manager/complaints', redirectTo: 'complaints', pathMatch: 'full' },
      { path: 'manager/approvals', redirectTo: 'approvals', pathMatch: 'full' },
    ],
  },
];
