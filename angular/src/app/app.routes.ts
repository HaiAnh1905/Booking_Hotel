import { authGuard, permissionGuard } from '@abp/ng.core';
import { Routes } from '@angular/router';
export const APP_ROUTES: Routes = [
  {
    path: '',
    pathMatch: 'full',
    loadComponent: () => import('./home/home.component').then(c => c.HomeComponent),
  },
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
