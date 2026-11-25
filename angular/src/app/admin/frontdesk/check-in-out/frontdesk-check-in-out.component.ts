import { ChangeDetectionStrategy, Component, OnInit, computed, effect, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { LocalizationPipe, LocalizationService } from '@abp/ng.core';
import { BhEmptyStateComponent } from '../../../shared/components/empty-state/bh-empty-state.component';
import { BhLoadingSpinnerComponent } from '../../../shared/components/loading-spinner/bh-loading-spinner.component';
import { BhModalComponent } from '../../../shared/components/modal/bh-modal.component';
import { BhSearchableSelectComponent, SelectOption } from '../../../shared/components/searchable-select/bh-searchable-select.component';
import { BhToastrService } from '../../../shared/components/toastr/bh-toastr.service';
import { BhModalConfig } from '../../../shared/models/ui-models';
import { PropertyContextService } from '../../services/property-context.service';
import {
  BookingStatus,
  FrontdeskAvailableRoomDto,
  FrontdeskReservationListItemDto,
  FrontdeskService,
} from '../../services/frontdesk/frontdesk.service';

@Component({
  selector: 'bh-frontdesk-check-in-out',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    LocalizationPipe,
    BhEmptyStateComponent,
    BhLoadingSpinnerComponent,
    BhModalComponent,
    BhSearchableSelectComponent,
  ],
  templateUrl: './frontdesk-check-in-out.component.html',
  styleUrls: ['./frontdesk-check-in-out.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FrontdeskCheckInOutComponent implements OnInit {
  readonly propertyService = inject(PropertyContextService);
  private frontdeskService = inject(FrontdeskService);
  private toastr = inject(BhToastrService);
  private localization = inject(LocalizationService);
  private route = inject(ActivatedRoute);

  readonly activeTab = signal<'checkin' | 'checkout'>('checkin');
  readonly searchQuery = signal<string>('');
  readonly isLoading = signal<boolean>(false);
  readonly isBusy = signal<boolean>(false);

  readonly checkInList = signal<FrontdeskReservationListItemDto[]>([]);
  readonly checkOutList = signal<FrontdeskReservationListItemDto[]>([]);

  // Check-in Modal State
  readonly isCheckInModalOpen = signal<boolean>(false);
  readonly activeBooking = signal<FrontdeskReservationListItemDto | null>(null);
  readonly availableRooms = signal<FrontdeskAvailableRoomDto[]>([]);
  readonly checkInRoomId = signal<string>('');
  readonly checkInGuestName = signal<string>('');
  readonly checkInGuestPhone = signal<string>('');
  readonly checkInGuestEmail = signal<string>('');
  readonly checkInIdentityNumber = signal<string>('');
  readonly checkInNotes = signal<string>('');

  // Check-out Modal State
  readonly isCheckOutModalOpen = signal<boolean>(false);
  readonly checkOutNotes = signal<string>('');

  private pendingBookingId: string | null = null;

  readonly roomOptions = computed<SelectOption[]>(() =>
    this.availableRooms().map(r => ({
      value: r.roomId,
      label: `${r.roomNumber} (${r.roomTypeName})`,
      sublabel: this.localization.instant('RoomRack:FloorX', String(r.floor)),
    }))
  );

  readonly checkInModalConfig: BhModalConfig = {
    titleKey: 'Frontdesk:CheckInModalTitle',
    confirmTextKey: 'Frontdesk:CheckInButton',
    cancelTextKey: 'Common:Cancel',
    confirmVariant: 'primary',
    size: 'md',
    iconClass: 'fa-solid fa-user-check',
  };

  readonly checkOutModalConfig: BhModalConfig = {
    titleKey: 'Frontdesk:CheckOutModalTitle',
    confirmTextKey: 'Frontdesk:CheckOutButton',
    cancelTextKey: 'Common:Cancel',
    confirmVariant: 'danger',
    size: 'md',
    iconClass: 'fa-solid fa-door-open',
  };

  constructor() {
    effect(() => {
      const hotelId = this.propertyService.currentHotelId();
      if (hotelId) {
        this.loadData(hotelId);
      } else {
        this.checkInList.set([]);
        this.checkOutList.set([]);
      }
    });
  }

  ngOnInit(): void {
    const initialTab = this.route.snapshot.data['initialTab'];
    if (initialTab === 'checkin' || initialTab === 'checkout') {
      this.activeTab.set(initialTab);
    }

    this.route.queryParams.subscribe(params => {
      if (params['tab'] === 'checkin' || params['tab'] === 'checkout') {
        this.activeTab.set(params['tab']);
      }
      if (params['bookingId']) {
        this.pendingBookingId = params['bookingId'];
        this.tryOpenPendingBooking();
      }
    });
  }

  switchTab(tab: 'checkin' | 'checkout'): void {
    this.activeTab.set(tab);
    this.loadData();
  }

  loadData(hotelId?: string | null): void {
    const targetHotelId = hotelId ?? this.propertyService.currentHotelId();
    if (!targetHotelId) return;

    this.isLoading.set(true);
    const status = this.activeTab() === 'checkin' ? BookingStatus.Confirmed : BookingStatus.CheckedIn;

    this.frontdeskService
      .getReservations({
        hotelId: targetHotelId,
        keyword: this.searchQuery(),
        bookingStatus: status,
        skipCount: 0,
        maxResultCount: 50,
      })
      .subscribe({
        next: res => {
          if (this.activeTab() === 'checkin') {
            this.checkInList.set(res.items || []);
          } else {
            this.checkOutList.set(res.items || []);
          }
          this.isLoading.set(false);
          this.tryOpenPendingBooking();
        },
        error: err => {
          this.isLoading.set(false);
          this.toastr.error(err?.error?.error?.message || this.localization.instant('Frontdesk:LoadError'));
        },
      });
  }

  private tryOpenPendingBooking(): void {
    if (!this.pendingBookingId) return;
    const list = this.activeTab() === 'checkin' ? this.checkInList() : this.checkOutList();
    const match = list.find(b => b.id === this.pendingBookingId);
    if (match) {
      this.pendingBookingId = null;
      if (this.activeTab() === 'checkin') {
        this.openCheckInModal(match);
      } else {
        this.openCheckOutModal(match);
      }
    }
  }

  isDateDifferentFromToday(dateStr: string): boolean {
    const d = new Date(dateStr);
    const now = new Date();
    return (
      d.getFullYear() !== now.getFullYear() ||
      d.getMonth() !== now.getMonth() ||
      d.getDate() !== now.getDate()
    );
  }

  openCheckInModal(item: FrontdeskReservationListItemDto): void {
    const hotelId = this.propertyService.currentHotelId();
    if (!hotelId) return;

    this.activeBooking.set(item);
    this.checkInGuestName.set(item.guestFullName);
    this.checkInGuestPhone.set(item.guestPhone);
    this.checkInGuestEmail.set(item.guestEmail || '');
    this.checkInIdentityNumber.set(item.guestIdentityNumberMasked || '');
    this.checkInNotes.set('');
    this.checkInRoomId.set(item.assignedRoomIsReady && item.assignedRoomId ? item.assignedRoomId : '');
    this.availableRooms.set([]);
    this.isCheckInModalOpen.set(true);

    this.frontdeskService.getAvailableRooms(item.id, hotelId).subscribe({
      next: rooms => {
        this.availableRooms.set(rooms || []);
        if (!this.checkInRoomId() && rooms.length > 0) {
          this.checkInRoomId.set(rooms[0].roomId);
        }
      },
      error: err => {
        this.toastr.error(err?.error?.error?.message || this.localization.instant('Frontdesk:LoadError'));
      },
    });
  }

  submitCheckIn(): void {
    const booking = this.activeBooking();
    const hotelId = this.propertyService.currentHotelId();
    if (!booking || !hotelId) return;

    if (!this.checkInRoomId()) {
      this.toastr.warning(this.localization.instant('Frontdesk:NoAvailableRooms'));
      return;
    }
    if (this.checkInIdentityNumber().trim().length < 6) {
      this.toastr.warning(this.localization.instant('BookingHotel:FrontdeskIdentityRequired'));
      return;
    }

    this.isBusy.set(true);
    this.frontdeskService
      .checkIn(booking.id, {
        hotelId,
        roomId: this.checkInRoomId(),
        guestFullName: this.checkInGuestName().trim(),
        guestPhone: this.checkInGuestPhone().trim(),
        guestEmail: this.checkInGuestEmail().trim() || null,
        identityNumber: this.checkInIdentityNumber().trim(),
        frontdeskNotes: this.checkInNotes().trim() || null,
      })
      .subscribe({
        next: () => {
          this.isBusy.set(false);
          this.isCheckInModalOpen.set(false);
          this.toastr.success(
            this.localization.instant('Frontdesk:CheckInSuccess', booking.bookingCode)
          );
          this.loadData(hotelId);
        },
        error: err => {
          this.isBusy.set(false);
          this.toastr.error(err?.error?.error?.message || this.localization.instant('Frontdesk:LoadError'));
        },
      });
  }

  openCheckOutModal(item: FrontdeskReservationListItemDto): void {
    this.activeBooking.set(item);
    this.checkOutNotes.set('');
    this.isCheckOutModalOpen.set(true);
  }

  submitCheckOut(): void {
    const booking = this.activeBooking();
    const hotelId = this.propertyService.currentHotelId();
    if (!booking || !hotelId) return;

    this.isBusy.set(true);
    this.frontdeskService
      .checkOut(booking.id, {
        hotelId,
        frontdeskNotes: this.checkOutNotes().trim() || null,
      })
      .subscribe({
        next: () => {
          this.isBusy.set(false);
          this.isCheckOutModalOpen.set(false);
          this.toastr.success(
            this.localization.instant('Frontdesk:CheckOutSuccess', booking.bookingCode)
          );
          this.loadData(hotelId);
        },
        error: err => {
          this.isBusy.set(false);
          this.toastr.error(err?.error?.error?.message || this.localization.instant('Frontdesk:LoadError'));
        },
      });
  }
}
