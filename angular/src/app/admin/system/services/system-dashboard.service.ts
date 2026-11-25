import { Injectable, inject } from '@angular/core';
import { RestService } from '@abp/ng.core';
import { Observable } from 'rxjs';

export interface SystemActivityLogDto {
  title: string;
  description: string;
  executionTime: string;
  level: 'success' | 'info' | 'warning' | 'danger';
  userName?: string | null;
  httpMethod?: string | null;
  url?: string | null;
  statusCode?: number | null;
  timeFormatted: string;
}

export interface SystemServerHealthDto {
  openIddictStatus: string;
  databaseStatus: string;
  databaseLatencyMs: number;
  memoryUsageMb: number;
  serverPlatform: string;
  uptimeFormatted: string;
}

export interface SystemDashboardOverviewDto {
  tenantCount: number;
  newTenantsThisMonth: number;
  operatingBranchCount: number;
  activeHostUserCount: number;
  totalHostUserCount: number;
  twoFactorHostUserCount: number;
  securityAlertCount: number;
  lockedOutUserCount: number;
  recentActivities: SystemActivityLogDto[];
  serverHealth: SystemServerHealthDto;
  sqlLatencyMs?: number;
  redisHitRatePercent?: number;
}

@Injectable({
  providedIn: 'root'
})
export class SystemDashboardService {
  private restService = inject(RestService);

  getOverview(): Observable<SystemDashboardOverviewDto> {
    return this.restService.request<void, SystemDashboardOverviewDto>({
      method: 'GET',
      url: '/api/app/system-dashboard/overview'
    });
  }
}

