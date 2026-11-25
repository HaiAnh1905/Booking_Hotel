import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { LocalizationPipe, LocalizationService } from '@abp/ng.core';
import { SystemDashboardService, SystemDashboardOverviewDto } from '../services/system-dashboard.service';

interface DashboardMetric {
  title: string;
  value: string | number;
  trend: string;
  trendType: 'positive' | 'negative' | 'neutral';
  icon: string;
  color: string;
}

interface ActivityEvent {
  level: string;
  title: string;
  time: string;
  desc: string;
}

@Component({
  selector: 'bh-system-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule, LocalizationPipe],
  templateUrl: './system-dashboard.component.html',
  styleUrls: ['./system-dashboard.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SystemDashboardComponent implements OnInit {
  private dashboardService = inject(SystemDashboardService);
  private cdr = inject(ChangeDetectorRef);
  private localization = inject(LocalizationService);

  isLoading = true;
  isRefreshing = false;
  overview: SystemDashboardOverviewDto | null = null;
  metrics: DashboardMetric[] = [];
  systemAlerts: ActivityEvent[] = [];

  ngOnInit(): void {
    this.loadOverview();
  }

  loadOverview(isManualRefresh = false): void {
    if (isManualRefresh) {
      this.isRefreshing = true;
    } else {
      this.isLoading = true;
    }
    this.cdr.markForCheck();

    this.dashboardService.getOverview().subscribe({
      next: (res) => {
        this.overview = res;
        this.buildDisplayData(res);
        this.isLoading = false;
        this.isRefreshing = false;
        this.cdr.markForCheck();
      },
      error: () => {
        this.isLoading = false;
        this.isRefreshing = false;
        this.cdr.markForCheck();
      }
    });
  }

  private buildDisplayData(res: SystemDashboardOverviewDto): void {
    this.metrics = [
      {
        title: this.localization.instant('SystemDashboard:MetricTotalTenants'),
        value: res.tenantCount,
        trend: res.newTenantsThisMonth > 0
          ? this.localization.instant('SystemDashboard:TrendTenantsThisMonth', '' + res.newTenantsThisMonth)
          : this.localization.instant('SystemDashboard:TrendTotalTenantsInDb'),
        trendType: 'positive',
        icon: 'fa-solid fa-building-user',
        color: '#7c3aed',
      },
      {
        title: this.localization.instant('SystemDashboard:MetricOperatingBranches'),
        value: res.operatingBranchCount,
        trend: res.operatingBranchCount > 0
          ? this.localization.instant('SystemDashboard:TrendStaffAllocated')
          : this.localization.instant('SystemDashboard:TrendNoBranchAssigned'),
        trendType: 'neutral',
        icon: 'fa-solid fa-hotel',
        color: '#0284c7',
      },
      {
        title: this.localization.instant('SystemDashboard:MetricActiveHostUsers'),
        value: res.activeHostUserCount,
        trend: res.twoFactorHostUserCount > 0
          ? this.localization.instant('SystemDashboard:TrendTwoFactorCount', '' + res.twoFactorHostUserCount)
          : this.localization.instant('SystemDashboard:TrendActivatedCount', '' + res.activeHostUserCount, '' + res.totalHostUserCount),
        trendType: 'positive',
        icon: 'fa-solid fa-user-shield',
        color: '#059669',
      },
      {
        title: this.localization.instant('SystemDashboard:MetricSecurityAlerts'),
        value: res.securityAlertCount,
        trend: res.securityAlertCount === 0
          ? this.localization.instant('SystemDashboard:TrendNoAttack')
          : this.localization.instant('SystemDashboard:TrendLockedAccounts', '' + res.lockedOutUserCount),
        trendType: res.securityAlertCount === 0 ? 'positive' : 'negative',
        icon: 'fa-solid fa-shield-virus',
        color: res.securityAlertCount === 0 ? '#10b981' : '#ef4444',
      },
    ];

    this.systemAlerts = (res.recentActivities || []).map(a => ({
      level: a.level,
      title: a.title,
      time: a.timeFormatted,
      desc: a.description
    }));
  }
}
