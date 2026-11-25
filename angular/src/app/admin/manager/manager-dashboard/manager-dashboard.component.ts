import { ChangeDetectionStrategy, Component, OnInit, OnDestroy, inject, signal, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { LocalizationPipe } from '@abp/ng.core';
import { PropertyContextService } from '../../services/property-context.service';
import {
  ManagerDashboardService,
  ManagerDashboardOverviewDto,
  TodayArrivalDto,
  ManagerActionItemsDto,
} from '../../services/manager/manager-dashboard.service';
import { Subscription } from 'rxjs';

@Component({
  selector: 'bh-manager-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule, LocalizationPipe],
  templateUrl: './manager-dashboard.component.html',
  styleUrls: ['./manager-dashboard.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ManagerDashboardComponent implements OnInit, OnDestroy {
  readonly propertyService = inject(PropertyContextService);
  private dashboardService = inject(ManagerDashboardService);

  readonly kpis = signal<ManagerDashboardOverviewDto | null>(null);
  readonly todayArrivals = signal<TodayArrivalDto[]>([]);
  readonly actionItems = signal<ManagerActionItemsDto | null>(null);
  readonly isLoading = signal<boolean>(false);
  readonly error = signal<string | null>(null);

  private sub?: Subscription;

  constructor() {
    // Tự động tải lại dữ liệu khi HotelId thay đổi (Signal Effect)
    effect(() => {
      const hotelId = this.propertyService.currentHotelId();
      if (hotelId) {
        this.loadDashboardData(hotelId);
      }
    });
  }

  ngOnInit(): void {}

  ngOnDestroy(): void {
    this.sub?.unsubscribe();
  }

  loadDashboardData(hotelId: string): void {
    this.isLoading.set(true);
    this.error.set(null);

    this.dashboardService.getKpis(hotelId).subscribe({
      next: kpis => {
        this.kpis.set(kpis);
        this.isLoading.set(false);
      },
      error: err => {
        this.error.set(err.message || 'Error loading dashboard');
        this.isLoading.set(false);
      },
    });

    this.dashboardService.getTodayArrivals(hotelId).subscribe({
      next: arrivals => this.todayArrivals.set(arrivals),
      error: () => this.todayArrivals.set([]),
    });

    this.dashboardService.getActionItems(hotelId).subscribe({
      next: items => this.actionItems.set(items),
      error: () => this.actionItems.set(null),
    });
  }

  getPaymentBadgeClass(status: number): string {
    switch (status) {
      case 3: // Paid
        return 'badge-success';
      case 2: // PartiallyPaid
        return 'badge-warning';
      case 4: // Refunded
        return 'badge-info';
      default: // Unpaid
        return 'badge-danger';
    }
  }

  getPaymentStatusKey(status: number): string {
    switch (status) {
      case 3:
        return 'Manager:Booking:PaymentStatus_Paid';
      case 2:
        return 'Manager:Booking:PaymentStatus_PartiallyPaid';
      case 4:
        return 'Manager:Booking:PaymentStatus_Refunded';
      default:
        return 'Manager:Booking:PaymentStatus_Unpaid';
    }
  }
}
