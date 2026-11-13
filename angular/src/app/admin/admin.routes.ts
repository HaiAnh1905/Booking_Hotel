import { Routes } from '@angular/router';
import { ManagementLayoutComponent } from '../layout/management/management-layout.component';

export const ADMIN_ROUTES: Routes = [
  {
    path: '',
    component: ManagementLayoutComponent,
    data: {
      layout: 'empty',
    },
    children: [
      {
        path: '',
        pathMatch: 'full',
        redirectTo: 'ui-showcase',
      },
      {
        path: 'ui-showcase',
        loadComponent: () => import('./ui-showcase/ui-showcase.component').then(c => c.UiShowcaseComponent),
      },
      {
        path: 'room-rack-preview',
        loadComponent: () => import('./room-rack-preview/room-rack-preview.component').then(c => c.RoomRackPreviewComponent),
      },
    ],
  },
];
