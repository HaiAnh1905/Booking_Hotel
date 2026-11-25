import { Routes } from '@angular/router';
import { ReceptionistLayoutComponent } from './receptionist-layout/receptionist-layout.component';
import { receptionistGuard } from '../guards/role-guards';
import { firstLoginPasswordChangeGuard } from '../../auth/guards/first-login-password-change.guard';

export const FRONTDESK_ROUTES: Routes = [
  {
    path: '',
    component: ReceptionistLayoutComponent,
    canActivateChild: [receptionistGuard, firstLoginPasswordChangeGuard],
    children: [
      {
        path: '',
        pathMatch: 'full',
        redirectTo: 'dashboard',
      },
      {
        path: 'dashboard',
        loadComponent: () =>
          import('./dashboard/frontdesk-dashboard.component').then(c => c.FrontdeskDashboardComponent),
      },
      {
        path: 'room-rack',
        loadComponent: () =>
          import('./room-rack/room-rack.component').then(c => c.RoomRackComponent),
      },
      {
        path: 'reservations',
        loadComponent: () =>
          import('./reservations/frontdesk-reservations.component').then(c => c.FrontdeskReservationsComponent),
      },
      {
        path: 'guests',
        loadComponent: () =>
          import('./guests/frontdesk-guests.component').then(c => c.FrontdeskGuestsComponent),
      },
      {
        path: 'check-in-out',
        loadComponent: () =>
          import('./check-in-out/frontdesk-check-in-out.component').then(c => c.FrontdeskCheckInOutComponent),
      },
      // Alias tương thích cho các đường dẫn Check-in / Check-out
      {
        path: 'check-in',
        loadComponent: () =>
          import('./check-in-out/frontdesk-check-in-out.component').then(c => c.FrontdeskCheckInOutComponent),
        data: { initialTab: 'checkin' },
      },
      {
        path: 'check-out',
        loadComponent: () =>
          import('./check-in-out/frontdesk-check-in-out.component').then(c => c.FrontdeskCheckInOutComponent),
        data: { initialTab: 'checkout' },
      },
      { path: 'folios', redirectTo: 'dashboard', pathMatch: 'full' },
      { path: 'frontdesk/dashboard', redirectTo: 'dashboard', pathMatch: 'full' },
      { path: 'frontdesk/room-rack', redirectTo: 'room-rack', pathMatch: 'full' },
      { path: 'frontdesk/reservations', redirectTo: 'reservations', pathMatch: 'full' },
      { path: 'frontdesk/guests', redirectTo: 'guests', pathMatch: 'full' },
      { path: 'frontdesk/check-in-out', redirectTo: 'check-in-out', pathMatch: 'full' },
      { path: 'frontdesk/check-in', redirectTo: 'check-in', pathMatch: 'full' },
      { path: 'frontdesk/check-out', redirectTo: 'check-out', pathMatch: 'full' },
    ],
  },
];
