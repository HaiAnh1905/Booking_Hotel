import { Routes } from '@angular/router';
import { SYSTEM_ROUTES } from './system/system.routes';

/**
 * ADMIN_ROUTES:
 * Tương thích ngược: Phân hệ /admin được dành riêng hoàn toàn cho System Admin (Host Admin).
 */
export const ADMIN_ROUTES: Routes = SYSTEM_ROUTES;
