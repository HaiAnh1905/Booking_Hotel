import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { LocalizationPipe } from '@abp/ng.core';
import { TenantDashboardOverviewDto, TenantDashboardService } from '../../services/tenant-dashboard.service';

@Component({
  selector: 'bh-tenant-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule, LocalizationPipe],
  templateUrl: './tenant-dashboard.component.html',
  styleUrls: ['./tenant-dashboard.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TenantDashboardComponent implements OnInit {
  private dashboardService = inject(TenantDashboardService);
  private cdr = inject(ChangeDetectorRef);

  isLoading = true;
  startDate: string = '';
  endDate: string = '';

  overview: TenantDashboardOverviewDto = {
    chainName: '',
    totalHotelCount: 0,
    operatingBranchCount: 0,
    suspendedBranchCount: 0,
    totalStaffCount: 0,
    totalBookingCount: 0,
    monthlyCashRevenue: 0,
    averageOccupancyPercent: 0,
    averageRevPar: 0,
    averageAdr: 0,
    branches: [],
  };

  ngOnInit(): void {
    const now = new Date();
    const firstDay = new Date(now.getFullYear(), now.getMonth(), 1);
    this.startDate = firstDay.toISOString().split('T')[0];
    this.endDate = now.toISOString().split('T')[0];
    this.loadOverview();
  }

  loadOverview(): void {
    this.isLoading = true;
    this.cdr.markForCheck();

    this.dashboardService
      .getOverview({
        startDate: this.startDate ? new Date(this.startDate).toISOString() : undefined,
        endDate: this.endDate ? new Date(this.endDate).toISOString() : undefined,
      })
      .subscribe({
        next: (data) => {
          this.overview = data;
          this.isLoading = false;
          this.cdr.markForCheck();
        },
        error: () => {
          this.isLoading = false;
          this.cdr.markForCheck();
        },
      });
  }

  onDateFilterChange(): void {
    this.loadOverview();
  }

  get formatRevenue(): string {
    return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(
      this.overview.monthlyCashRevenue || 0
    );
  }

  get formatRevPar(): string {
    return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(
      this.overview.averageRevPar || 0
    );
  }

  get formatAdr(): string {
    return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(
      this.overview.averageAdr || 0
    );
  }

  get formatOccupancy(): string {
    return (this.overview.averageOccupancyPercent || 0).toFixed(1) + '%';
  }

  get sortedBranches() {
    return [...(this.overview.branches || [])].sort((a, b) => b.revPar - a.revPar);
  }
}
