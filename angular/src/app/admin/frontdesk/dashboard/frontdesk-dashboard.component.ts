import { ChangeDetectionStrategy, Component, effect, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { LocalizationPipe, LocalizationService } from '@abp/ng.core';
import { BhEmptyStateComponent } from '../../../shared/components/empty-state/bh-empty-state.component';
import { BhLoadingSpinnerComponent } from '../../../shared/components/loading-spinner/bh-loading-spinner.component';
import { BhStatusBadgeComponent } from '../../../shared/components/status-badge/bh-status-badge.component';
import { BhToastrService } from '../../../shared/components/toastr/bh-toastr.service';
import { StatusBadgeVariant } from '../../../shared/models/ui-models';
import { PropertyContextService } from '../../services/property-context.service';
import {
  BookingStatus,
  FrontdeskDashboardDto,
  FrontdeskQuickBookingItemDto,
  FrontdeskService,
} from '../../services/frontdesk/frontdesk.service';

@Component({
  selector: 'bh-frontdesk-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    LocalizationPipe,
    BhEmptyStateComponent,
    BhLoadingSpinnerComponent,
    BhStatusBadgeComponent,
  ],
  templateUrl: './frontdesk-dashboard.component.html',
  styleUrls: ['./frontdesk-dashboard.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FrontdeskDashboardComponent {
  readonly propertyService = inject(PropertyContextService);
  private frontdeskService = inject(FrontdeskService);
  private toastr = inject(BhToastrService);
  private localization = inject(LocalizationService);
  private router = inject(Router);

  readonly isLoading = signal<boolean>(false);
  readonly overview = signal<FrontdeskDashboardDto | null>(null);
  readonly BookingStatus = BookingStatus;

  constructor() {
    effect(() => {
      const hotelId = this.propertyService.currentHotelId();
      if (hotelId) {
        this.loadOverview(hotelId);
      } else {
        this.overview.set(null);
      }
    });
  }

  loadOverview(hotelId?: string | null): void {
    const targetHotelId = hotelId ?? this.propertyService.currentHotelId();
    if (!targetHotelId) {
      this.overview.set(null);
      return;
    }

    this.isLoading.set(true);
    this.frontdeskService.getDashboardOverview(targetHotelId).subscribe({
      next: res => {
        this.overview.set(res);
        this.isLoading.set(false);
      },
      error: err => {
        this.overview.set(null);
        this.isLoading.set(false);
        this.toastr.error(
          err?.error?.error?.message || this.localization.instant('Frontdesk:LoadError'),
          this.localization.instant('Toastr:Error')
        );
      },
    });
  }

  goToReservations(status?: BookingStatus): void {
    this.router.navigate(['/frontdesk/reservations'], {
      queryParams: status ? { status } : {},
    });
  }

  goToCheckIn(bookingId?: string): void {
    this.router.navigate(['/frontdesk/check-in-out'], {
      queryParams: { tab: 'checkin', ...(bookingId ? { bookingId } : {}) },
    });
  }

  goToCheckOut(bookingId?: string): void {
    this.router.navigate(['/frontdesk/check-in-out'], {
      queryParams: { tab: 'checkout', ...(bookingId ? { bookingId } : {}) },
    });
  }

  openReservationDetail(item: FrontdeskQuickBookingItemDto): void {
    this.router.navigate(['/frontdesk/reservations'], {
      queryParams: { bookingId: item.bookingId },
    });
  }

  getStatusVariant(status: BookingStatus): StatusBadgeVariant {
    switch (status) {
      case BookingStatus.PendingConfirmation: return 'pending';
      case BookingStatus.Confirmed: return 'available';
      case BookingStatus.CheckedIn: return 'occupied';
      case BookingStatus.Completed: return 'active';
      default: return 'inactive';
    }
  }
}
