import { ChangeDetectionStrategy, Component, computed, effect, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { LocalizationPipe, LocalizationService } from '@abp/ng.core';
import { BhDataTableComponent } from '../../../shared/components/data-table/bh-data-table.component';
import { BhEmptyStateComponent } from '../../../shared/components/empty-state/bh-empty-state.component';
import { BhLoadingSpinnerComponent } from '../../../shared/components/loading-spinner/bh-loading-spinner.component';
import { BhModalComponent } from '../../../shared/components/modal/bh-modal.component';
import { BhStatusBadgeComponent } from '../../../shared/components/status-badge/bh-status-badge.component';
import { BhToastrService } from '../../../shared/components/toastr/bh-toastr.service';
import { BhDataTableColumn, BhModalConfig, BhPageChangeEvent, BhPaginationState, StatusBadgeVariant } from '../../../shared/models/ui-models';
import { PropertyContextService } from '../../services/property-context.service';
import {
  BookingStatus,
  FrontdeskGuestDetailDto,
  FrontdeskGuestListItemDto,
  FrontdeskService,
} from '../../services/frontdesk/frontdesk.service';

@Component({
  selector: 'bh-frontdesk-guests',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    LocalizationPipe,
    BhDataTableComponent,
    BhEmptyStateComponent,
    BhLoadingSpinnerComponent,
    BhModalComponent,
    BhStatusBadgeComponent,
  ],
  templateUrl: './frontdesk-guests.component.html',
  styleUrls: ['./frontdesk-guests.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FrontdeskGuestsComponent {
  readonly propertyService = inject(PropertyContextService);
  private frontdeskService = inject(FrontdeskService);
  private toastr = inject(BhToastrService);
  private localization = inject(LocalizationService);
  private router = inject(Router);

  readonly isLoading = signal<boolean>(false);
  readonly isLoadingDetail = signal<boolean>(false);
  readonly isBusy = signal<boolean>(false);

  readonly keyword = signal<string>('');
  readonly items = signal<FrontdeskGuestListItemDto[]>([]);
  readonly totalCount = signal<number>(0);
  readonly currentPage = signal<number>(1);
  readonly pageSize = signal<number>(15);

  readonly selectedGuest = signal<FrontdeskGuestDetailDto | null>(null);

  // Edit Guest Modal
  readonly isEditModalOpen = signal<boolean>(false);
  readonly editFullName = signal<string>('');
  readonly editPhone = signal<string>('');
  readonly editEmail = signal<string>('');
  readonly editIdentityNumber = signal<string>('');

  readonly tableColumns: BhDataTableColumn<FrontdeskGuestListItemDto>[] = [
    { field: 'fullName', headerKey: 'Frontdesk:ColFullName' },
    { field: 'phoneNumber', headerKey: 'Frontdesk:ColPhone' },
    { field: 'email', headerKey: 'Frontdesk:ColEmail' },
    { field: 'identityNumberMasked', headerKey: 'Frontdesk:ColIdentity' },
    { field: 'totalBookingsCount', headerKey: 'Frontdesk:ColBookingsCount', align: 'center' },
    { field: 'lastCheckInDate', headerKey: 'Frontdesk:ColLastCheckIn' },
    { field: 'actions', headerKey: 'Common:Actions', align: 'right' },
  ];

  readonly pagination = computed<BhPaginationState>(() => ({
    currentPage: this.currentPage(),
    pageSize: this.pageSize(),
    totalCount: this.totalCount(),
  }));

  readonly editModalConfig: BhModalConfig = {
    titleKey: 'Frontdesk:EditGuestModalTitle',
    confirmTextKey: 'Common:Save',
    cancelTextKey: 'Common:Cancel',
    confirmVariant: 'primary',
    size: 'md',
    iconClass: 'fa-solid fa-user-pen',
  };

  constructor() {
    effect(() => {
      const hotelId = this.propertyService.currentHotelId();
      if (hotelId) {
        this.loadGuests(hotelId);
      } else {
        this.items.set([]);
        this.totalCount.set(0);
        this.selectedGuest.set(null);
      }
    });
  }

  loadGuests(hotelId?: string | null): void {
    const targetHotelId = hotelId ?? this.propertyService.currentHotelId();
    if (!targetHotelId) return;

    this.isLoading.set(true);
    this.frontdeskService
      .getGuests({
        hotelId: targetHotelId,
        keyword: this.keyword(),
        skipCount: (this.currentPage() - 1) * this.pageSize(),
        maxResultCount: this.pageSize(),
      })
      .subscribe({
        next: res => {
          this.items.set(res.items || []);
          this.totalCount.set(res.totalCount || 0);
          this.isLoading.set(false);
        },
        error: err => {
          this.items.set([]);
          this.totalCount.set(0);
          this.isLoading.set(false);
          this.toastr.error(err?.error?.error?.message || this.localization.instant('Frontdesk:LoadError'));
        },
      });
  }

  onSearch(): void {
    this.currentPage.set(1);
    this.loadGuests();
  }

  onPageChange(event: BhPageChangeEvent): void {
    this.currentPage.set(event.page);
    this.pageSize.set(event.pageSize);
    this.loadGuests();
  }

  openDetail(guestKey: string): void {
    const hotelId = this.propertyService.currentHotelId();
    if (!hotelId) return;

    this.isLoadingDetail.set(true);
    this.frontdeskService.getGuestDetail(hotelId, guestKey).subscribe({
      next: detail => {
        this.selectedGuest.set(detail);
        this.isLoadingDetail.set(false);
      },
      error: err => {
        this.isLoadingDetail.set(false);
        this.toastr.error(err?.error?.error?.message || this.localization.instant('Frontdesk:LoadError'));
      },
    });
  }

  closeDetail(): void {
    this.selectedGuest.set(null);
  }

  openEditModal(): void {
    const g = this.selectedGuest();
    if (!g) return;

    this.editFullName.set(g.fullName);
    this.editPhone.set(g.phoneNumber);
    this.editEmail.set(g.email || '');
    this.editIdentityNumber.set(g.identityNumberMasked || '');
    this.isEditModalOpen.set(true);
  }

  submitEdit(): void {
    const g = this.selectedGuest();
    const hotelId = this.propertyService.currentHotelId();
    if (!g || !hotelId) return;

    if (!this.editFullName().trim() || !this.editPhone().trim()) {
      return;
    }

    this.isBusy.set(true);
    this.frontdeskService
      .updateGuest(g.guestKey, {
        hotelId,
        fullName: this.editFullName().trim(),
        phoneNumber: this.editPhone().trim(),
        email: this.editEmail().trim() || null,
        identityNumber: this.editIdentityNumber().trim() || null,
      })
      .subscribe({
        next: updated => {
          this.isBusy.set(false);
          this.isEditModalOpen.set(false);
          this.selectedGuest.set(updated);
          this.toastr.success(this.localization.instant('Frontdesk:UpdateGuestSuccess'));
          this.loadGuests(hotelId);
        },
        error: err => {
          this.isBusy.set(false);
          this.toastr.error(err?.error?.error?.message || this.localization.instant('Frontdesk:LoadError'));
        },
      });
  }

  goToReservation(bookingId: string): void {
    this.router.navigate(['/frontdesk/reservations'], {
      queryParams: { bookingId },
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
