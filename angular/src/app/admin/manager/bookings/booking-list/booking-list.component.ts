import { ChangeDetectionStrategy, Component, OnInit, inject, signal, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LocalizationPipe } from '@abp/ng.core';
import { PropertyContextService } from '../../../services/property-context.service';
import {
  ManagerBookingService,
  ManagerBookingDto,
  BookingStatus,
  PaymentStatus,
  RoomAssignmentStatus,
  NoShowResultDto,
} from '../../../services/manager/manager-booking.service';
import { ManagerRoomService, RoomTypeDto } from '../../../services/manager/manager-room.service';
import { BookingDetailModalComponent } from '../booking-detail-modal/booking-detail-modal.component';
import { DirectBookingModalComponent } from '../direct-booking-modal/direct-booking-modal.component';
import { NoShowModalComponent } from '../no-show-modal/no-show-modal.component';

@Component({
  selector: 'bh-booking-list',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    LocalizationPipe,
    BookingDetailModalComponent,
    DirectBookingModalComponent,
    NoShowModalComponent,
  ],
  templateUrl: './booking-list.component.html',
  styleUrls: ['./booking-list.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BookingListComponent implements OnInit {
  readonly propertyService = inject(PropertyContextService);
  private bookingService = inject(ManagerBookingService);
  private roomService = inject(ManagerRoomService);

  readonly BookingStatus = BookingStatus;
  readonly PaymentStatus = PaymentStatus;
  readonly RoomAssignmentStatus = RoomAssignmentStatus;

  readonly bookings = signal<ManagerBookingDto[]>([]);
  readonly roomTypes = signal<RoomTypeDto[]>([]);
  readonly totalCount = signal<number>(0);
  readonly isLoading = signal<boolean>(false);

  // Filters
  searchTerm = signal<string>('');
  selectedBookingStatus = signal<BookingStatus | null>(null);
  selectedPaymentStatus = signal<PaymentStatus | null>(null);

  // Modals state
  isDetailModalOpen = signal<boolean>(false);
  selectedBookingForDetail = signal<ManagerBookingDto | null>(null);

  isDirectModalOpen = signal<boolean>(false);

  isNoShowModalOpen = signal<boolean>(false);
  selectedBookingForNoShow = signal<ManagerBookingDto | null>(null);

  constructor() {
    effect(() => {
      const hotelId = this.propertyService.currentHotelId();
      if (hotelId) {
        this.loadBookings(hotelId);
        this.loadRoomTypes(hotelId);
      }
    });
  }

  ngOnInit(): void {}

  loadRoomTypes(hotelId: string): void {
    this.roomService.getRoomTypes(hotelId).subscribe({
      next: types => this.roomTypes.set(types),
    });
  }

  loadBookings(hotelId: string): void {
    this.isLoading.set(true);

    this.bookingService.getList({
      hotelId,
      guestKeyword: this.searchTerm() || undefined,
      bookingStatus: this.selectedBookingStatus() || undefined,
      paymentStatus: this.selectedPaymentStatus() || undefined,
      maxResultCount: 50,
      skipCount: 0,
    }).subscribe({
      next: res => {
        this.bookings.set(res.items || []);
        this.totalCount.set(res.totalCount || 0);
        this.isLoading.set(false);
      },
      error: () => {
        this.bookings.set([]);
        this.totalCount.set(0);
        this.isLoading.set(false);
      },
    });
  }

  onFilterChange(): void {
    const hotelId = this.propertyService.currentHotelId();
    if (hotelId) {
      this.loadBookings(hotelId);
    }
  }

  openDetailModal(b: ManagerBookingDto): void {
    this.selectedBookingForDetail.set(b);
    this.isDetailModalOpen.set(true);
  }

  openDirectModal(): void {
    this.isDirectModalOpen.set(true);
  }

  openNoShowModal(b: ManagerBookingDto): void {
    this.selectedBookingForNoShow.set(b);
    this.isNoShowModalOpen.set(true);
  }

  onBookingUpdated(updated: ManagerBookingDto): void {
    this.bookings.update(list => list.map(b => (b.id === updated.id ? updated : b)));
  }

  onDirectBookingCreated(created: ManagerBookingDto): void {
    this.bookings.update(list => [created, ...list]);
    this.totalCount.update(c => c + 1);
  }

  onNoShowProcessed(res: NoShowResultDto): void {
    this.bookings.update(list =>
      list.map(b =>
        b.id === res.bookingId
          ? {
              ...b,
              bookingStatus: res.bookingStatus,
              paymentStatus: res.paymentStatus,
              penaltyAmount: res.penaltyAmountApplied,
              assignedRoomNumber: undefined,
              roomAssignmentStatus: RoomAssignmentStatus.Unassigned,
            }
          : b
      )
    );
  }

  quickConfirm(b: ManagerBookingDto): void {
    const hotelId = this.propertyService.currentHotelId();
    if (!hotelId) return;

    this.bookingService.confirm(b.id, hotelId).subscribe({
      next: updated => this.onBookingUpdated(updated),
    });
  }

  getBookingBadgeClass(status: BookingStatus): string {
    switch (status) {
      case BookingStatus.Confirmed: return 'badge-bk-confirmed';
      case BookingStatus.CheckedIn: return 'badge-bk-checkedin';
      case BookingStatus.Completed: return 'badge-bk-completed';
      case BookingStatus.PendingConfirmation: return 'badge-bk-pending';
      case BookingStatus.Rejected:
      case BookingStatus.Cancelled:
      case BookingStatus.NoShow: return 'badge-bk-danger';
      default: return '';
    }
  }

  getBookingStatusKey(status: BookingStatus): string {
    switch (status) {
      case BookingStatus.Confirmed: return 'Manager:Booking:Status_Confirmed';
      case BookingStatus.CheckedIn: return 'Manager:Booking:Status_CheckedIn';
      case BookingStatus.Completed: return 'Manager:Booking:Status_Completed';
      case BookingStatus.PendingConfirmation: return 'Manager:Booking:Status_Pending';
      case BookingStatus.Rejected: return 'Manager:Booking:Status_Rejected';
      case BookingStatus.Cancelled: return 'Manager:Booking:Status_Cancelled';
      case BookingStatus.NoShow: return 'Manager:Booking:Status_NoShow';
      default: return '';
    }
  }

  getPaymentBadgeClass(status: PaymentStatus): string {
    switch (status) {
      case PaymentStatus.Paid: return 'badge-pay-paid';
      case PaymentStatus.PartiallyPaid: return 'badge-pay-partially';
      case PaymentStatus.Refunded: return 'badge-pay-refunded';
      default: return 'badge-pay-unpaid';
    }
  }

  getPaymentStatusKey(status: PaymentStatus): string {
    switch (status) {
      case PaymentStatus.Paid: return 'Manager:Booking:PaymentStatus_Paid';
      case PaymentStatus.PartiallyPaid: return 'Manager:Booking:PaymentStatus_PartiallyPaid';
      case PaymentStatus.Refunded: return 'Manager:Booking:PaymentStatus_Refunded';
      default: return 'Manager:Booking:PaymentStatus_Unpaid';
    }
  }
}
