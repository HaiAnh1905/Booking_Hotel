import { ChangeDetectionStrategy, Component, OnInit, computed, effect, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { LocalizationPipe, LocalizationService } from '@abp/ng.core';
import { BhDataTableComponent } from '../../../shared/components/data-table/bh-data-table.component';
import { BhEmptyStateComponent } from '../../../shared/components/empty-state/bh-empty-state.component';
import { BhLoadingSpinnerComponent } from '../../../shared/components/loading-spinner/bh-loading-spinner.component';
import { BhModalComponent } from '../../../shared/components/modal/bh-modal.component';
import { BhSearchableSelectComponent, SelectOption } from '../../../shared/components/searchable-select/bh-searchable-select.component';
import { BhStatusBadgeComponent } from '../../../shared/components/status-badge/bh-status-badge.component';
import { BhToastrService } from '../../../shared/components/toastr/bh-toastr.service';
import { BhDataTableColumn, BhModalConfig, BhPageChangeEvent, BhPaginationState, StatusBadgeVariant } from '../../../shared/models/ui-models';
import { PropertyContextService } from '../../services/property-context.service';
import {
  BookingStatus,
  FrontdeskAvailableRoomDto,
  FrontdeskReservationDetailDto,
  FrontdeskReservationListItemDto,
  FrontdeskService,
  RoomAssignmentStatus,
} from '../../services/frontdesk/frontdesk.service';

@Component({
  selector: 'bh-frontdesk-reservations',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    LocalizationPipe,
    BhDataTableComponent,
    BhEmptyStateComponent,
    BhLoadingSpinnerComponent,
    BhModalComponent,
    BhSearchableSelectComponent,
    BhStatusBadgeComponent,
  ],
  templateUrl: './frontdesk-reservations.component.html',
  styleUrls: ['./frontdesk-reservations.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FrontdeskReservationsComponent implements OnInit {
  readonly propertyService = inject(PropertyContextService);
  private frontdeskService = inject(FrontdeskService);
  private toastr = inject(BhToastrService);
  private localization = inject(LocalizationService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  readonly BookingStatus = BookingStatus;
  readonly RoomAssignmentStatus = RoomAssignmentStatus;

  readonly isLoading = signal<boolean>(false);
  readonly isBusy = signal<boolean>(false);
  readonly items = signal<FrontdeskReservationListItemDto[]>([]);
  readonly totalCount = signal<number>(0);

  // Filters
  readonly keyword = signal<string>('');
  readonly selectedStatus = signal<BookingStatus | null>(null);
  readonly selectedAssignmentStatus = signal<RoomAssignmentStatus | null>(null);
  readonly fromDate = signal<string>('');
  readonly toDate = signal<string>('');
  readonly currentPage = signal<number>(1);
  readonly pageSize = signal<number>(15);

  // Detail Drawer
  readonly selectedDetail = signal<FrontdeskReservationDetailDto | null>(null);
  readonly isLoadingDetail = signal<boolean>(false);

  // Modals
  readonly isAssignModalOpen = signal<boolean>(false);
  readonly availableRooms = signal<FrontdeskAvailableRoomDto[]>([]);
  readonly selectedRoomId = signal<string>('');

  readonly isEditModalOpen = signal<boolean>(false);
  readonly editGuestFullName = signal<string>('');
  readonly editGuestPhone = signal<string>('');
  readonly editGuestEmail = signal<string>('');
  readonly editSpecialRequests = signal<string>('');
  readonly editFrontdeskNotes = signal<string>('');

  readonly isCancelModalOpen = signal<boolean>(false);
  readonly cancelReason = signal<string>('');

  readonly isConfirmModalOpen = signal<boolean>(false);
  private pendingDeepLinkBookingId: string | null = null;

  readonly tableColumns: BhDataTableColumn<FrontdeskReservationListItemDto>[] = [
    { field: 'bookingCode', headerKey: 'Frontdesk:ColBookingCode' },
    { field: 'guest', headerKey: 'Frontdesk:ColGuest' },
    { field: 'room', headerKey: 'Frontdesk:ColRoom' },
    { field: 'dates', headerKey: 'Frontdesk:ColDates' },
    { field: 'status', headerKey: 'Frontdesk:ColStatus' },
    { field: 'payment', headerKey: 'Frontdesk:ColPayment', align: 'right' },
    { field: 'actions', headerKey: 'Common:Actions', align: 'right' },
  ];

  readonly statusOptions = computed<SelectOption[]>(() => [
    { value: null, label: this.localization.instant('Frontdesk:AllStatuses') },
    { value: BookingStatus.PendingConfirmation, label: this.localization.instant('Frontdesk:BookingStatus_1') },
    { value: BookingStatus.Confirmed, label: this.localization.instant('Frontdesk:BookingStatus_2') },
    { value: BookingStatus.CheckedIn, label: this.localization.instant('Frontdesk:BookingStatus_3') },
    { value: BookingStatus.Completed, label: this.localization.instant('Frontdesk:BookingStatus_4') },
    { value: BookingStatus.Cancelled, label: this.localization.instant('Frontdesk:BookingStatus_6') },
  ]);

  readonly assignmentOptions = computed<SelectOption[]>(() => [
    { value: null, label: this.localization.instant('Frontdesk:AllAssignmentStatuses') },
    { value: RoomAssignmentStatus.Assigned, label: this.localization.instant('Frontdesk:Assigned') },
    { value: RoomAssignmentStatus.Unassigned, label: this.localization.instant('Frontdesk:Unassigned') },
  ]);

  readonly availableRoomOptions = computed<SelectOption[]>(() =>
    this.availableRooms().map(r => ({
      value: r.roomId,
      label: `${r.roomNumber} (${r.roomTypeName})`,
      sublabel: this.localization.instant('RoomRack:FloorX', String(r.floor)),
    }))
  );

  readonly pagination = computed<BhPaginationState>(() => ({
    currentPage: this.currentPage(),
    pageSize: this.pageSize(),
    totalCount: this.totalCount(),
  }));

  readonly assignModalConfig: BhModalConfig = {
    titleKey: 'Frontdesk:AssignRoomModalTitle',
    confirmTextKey: 'Common:Save',
    cancelTextKey: 'Common:Cancel',
    confirmVariant: 'primary',
    size: 'md',
    iconClass: 'fa-solid fa-key',
  };

  readonly editModalConfig: BhModalConfig = {
    titleKey: 'Frontdesk:EditBookingModalTitle',
    confirmTextKey: 'Common:Save',
    cancelTextKey: 'Common:Cancel',
    confirmVariant: 'primary',
    size: 'md',
    iconClass: 'fa-solid fa-pen-to-square',
  };

  readonly cancelModalConfig: BhModalConfig = {
    titleKey: 'Frontdesk:CancelBookingModalTitle',
    confirmTextKey: 'Frontdesk:ActionCancel',
    cancelTextKey: 'Common:Cancel',
    confirmVariant: 'danger',
    size: 'md',
    iconClass: 'fa-solid fa-ban',
  };

  readonly confirmModalConfig: BhModalConfig = {
    titleKey: 'Frontdesk:ActionConfirm',
    confirmTextKey: 'Frontdesk:ActionConfirm',
    cancelTextKey: 'Common:Cancel',
    confirmVariant: 'primary',
    size: 'sm',
    iconClass: 'fa-solid fa-circle-check',
  };

  constructor() {
    effect(() => {
      const hotelId = this.propertyService.currentHotelId();
      if (hotelId) {
        this.loadList(hotelId);
        if (this.pendingDeepLinkBookingId) {
          const targetId = this.pendingDeepLinkBookingId;
          this.pendingDeepLinkBookingId = null;
          this.openDetail(targetId);
        }
      } else {
        this.items.set([]);
        this.totalCount.set(0);
        this.selectedDetail.set(null);
      }
    });
  }

  ngOnInit(): void {
    this.route.queryParams.subscribe(params => {
      if (params['status']) {
        this.selectedStatus.set(Number(params['status']) as BookingStatus);
      }
      if (params['bookingId']) {
        const hotelId = this.propertyService.currentHotelId();
        if (hotelId) {
          this.openDetail(params['bookingId']);
        } else {
          this.pendingDeepLinkBookingId = params['bookingId'];
        }
      }
    });
  }

  loadList(hotelId?: string | null): void {
    const targetHotelId = hotelId ?? this.propertyService.currentHotelId();
    if (!targetHotelId) return;

    this.isLoading.set(true);
    this.frontdeskService
      .getReservations({
        hotelId: targetHotelId,
        keyword: this.keyword(),
        bookingStatus: this.selectedStatus(),
        roomAssignmentStatus: this.selectedAssignmentStatus(),
        fromDate: this.fromDate() || null,
        toDate: this.toDate() || null,
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
          this.toastr.error(
            err?.error?.error?.message || this.localization.instant('Frontdesk:LoadError'),
            this.localization.instant('Toastr:Error')
          );
        },
      });
  }

  applyFilters(): void {
    this.currentPage.set(1);
    this.loadList();
  }

  resetFilters(): void {
    this.keyword.set('');
    this.selectedStatus.set(null);
    this.selectedAssignmentStatus.set(null);
    this.fromDate.set('');
    this.toDate.set('');
    this.currentPage.set(1);
    this.loadList();
  }

  onPageChange(event: BhPageChangeEvent): void {
    this.currentPage.set(event.page);
    this.pageSize.set(event.pageSize);
    this.loadList();
  }

  openDetail(bookingId: string): void {
    const hotelId = this.propertyService.currentHotelId();
    if (!hotelId) return;

    this.isLoadingDetail.set(true);
    this.frontdeskService.getReservation(bookingId, hotelId).subscribe({
      next: detail => {
        this.selectedDetail.set(detail);
        this.isLoadingDetail.set(false);
      },
      error: err => {
        this.isLoadingDetail.set(false);
        this.toastr.error(
          err?.error?.error?.message || this.localization.instant('Frontdesk:LoadError'),
          this.localization.instant('Toastr:Error')
        );
      },
    });
  }

  closeDetail(): void {
    this.selectedDetail.set(null);
  }

  // Confirm
  openConfirmModal(): void {
    this.isConfirmModalOpen.set(true);
  }

  submitConfirm(): void {
    const detail = this.selectedDetail();
    const hotelId = this.propertyService.currentHotelId();
    if (!detail || !hotelId) return;

    this.isBusy.set(true);
    this.frontdeskService.confirmReservation(detail.id, hotelId).subscribe({
      next: updated => {
        this.isBusy.set(false);
        this.isConfirmModalOpen.set(false);
        this.selectedDetail.set(updated);
        this.toastr.success(this.localization.instant('Frontdesk:ConfirmSuccess'));
        this.loadList(hotelId);
      },
      error: err => {
        this.isBusy.set(false);
        this.toastr.error(err?.error?.error?.message || this.localization.instant('Frontdesk:LoadError'));
      },
    });
  }

  // Assign Room
  openAssignRoomModal(): void {
    const detail = this.selectedDetail();
    const hotelId = this.propertyService.currentHotelId();
    if (!detail || !hotelId) return;

    this.selectedRoomId.set(detail.assignedRoomId || '');
    this.availableRooms.set([]);
    this.isAssignModalOpen.set(true);

    this.frontdeskService.getAvailableRooms(detail.id, hotelId).subscribe({
      next: rooms => {
        this.availableRooms.set(rooms || []);
        if (!this.selectedRoomId() && rooms.length > 0) {
          this.selectedRoomId.set(rooms[0].roomId);
        }
      },
      error: err => {
        this.toastr.error(err?.error?.error?.message || this.localization.instant('Frontdesk:LoadError'));
      },
    });
  }

  submitAssignRoom(): void {
    const detail = this.selectedDetail();
    const hotelId = this.propertyService.currentHotelId();
    const roomId = this.selectedRoomId();
    if (!detail || !hotelId || !roomId) return;

    this.isBusy.set(true);
    this.frontdeskService.assignRoom(detail.id, hotelId, roomId).subscribe({
      next: updated => {
        this.isBusy.set(false);
        this.isAssignModalOpen.set(false);
        this.selectedDetail.set(updated);
        this.toastr.success(this.localization.instant('Frontdesk:AssignRoomSuccess'));
        this.loadList(hotelId);
      },
      error: err => {
        this.isBusy.set(false);
        this.toastr.error(err?.error?.error?.message || this.localization.instant('Frontdesk:LoadError'));
      },
    });
  }

  // Edit operational info
  openEditModal(): void {
    const detail = this.selectedDetail();
    if (!detail) return;

    this.editGuestFullName.set(detail.guestFullName);
    this.editGuestPhone.set(detail.guestPhone);
    this.editGuestEmail.set(detail.guestEmail || '');
    this.editSpecialRequests.set(detail.specialRequests || '');
    this.editFrontdeskNotes.set(detail.frontdeskNotes || '');
    this.isEditModalOpen.set(true);
  }

  submitEdit(): void {
    const detail = this.selectedDetail();
    const hotelId = this.propertyService.currentHotelId();
    if (!detail || !hotelId) return;

    if (!this.editGuestFullName().trim() || !this.editGuestPhone().trim()) {
      return;
    }

    this.isBusy.set(true);
    this.frontdeskService
      .updateReservation(detail.id, {
        hotelId,
        guestFullName: this.editGuestFullName().trim(),
        guestPhone: this.editGuestPhone().trim(),
        guestEmail: this.editGuestEmail().trim() || null,
        specialRequests: this.editSpecialRequests().trim() || null,
        frontdeskNotes: this.editFrontdeskNotes().trim() || null,
      })
      .subscribe({
        next: updated => {
          this.isBusy.set(false);
          this.isEditModalOpen.set(false);
          this.selectedDetail.set(updated);
          this.toastr.success(this.localization.instant('Frontdesk:UpdateBookingSuccess'));
          this.loadList(hotelId);
        },
        error: err => {
          this.isBusy.set(false);
          this.toastr.error(err?.error?.error?.message || this.localization.instant('Frontdesk:LoadError'));
        },
      });
  }

  // Cancel
  openCancelModal(): void {
    this.cancelReason.set('');
    this.isCancelModalOpen.set(true);
  }

  submitCancel(): void {
    const detail = this.selectedDetail();
    const hotelId = this.propertyService.currentHotelId();
    const reason = this.cancelReason().trim();
    if (!detail || !hotelId) return;

    if (reason.length < 5) {
      this.toastr.warning(this.localization.instant('BookingHotel:FrontdeskCancelReasonRequired'));
      return;
    }

    this.isBusy.set(true);
    this.frontdeskService.cancelReservation(detail.id, hotelId, reason).subscribe({
      next: updated => {
        this.isBusy.set(false);
        this.isCancelModalOpen.set(false);
        this.selectedDetail.set(updated);
        this.toastr.success(this.localization.instant('Frontdesk:CancelBookingSuccess'));
        this.loadList(hotelId);
      },
      error: err => {
        this.isBusy.set(false);
        this.toastr.error(err?.error?.error?.message || this.localization.instant('Frontdesk:LoadError'));
      },
    });
  }

  goToCheckIn(bookingId: string): void {
    this.router.navigate(['/frontdesk/check-in-out'], {
      queryParams: { tab: 'checkin', bookingId },
    });
  }

  goToCheckOut(bookingId: string): void {
    this.router.navigate(['/frontdesk/check-in-out'], {
      queryParams: { tab: 'checkout', bookingId },
    });
  }

  getStatusVariant(status: BookingStatus): StatusBadgeVariant {
    switch (status) {
      case BookingStatus.PendingConfirmation: return 'pending';
      case BookingStatus.Confirmed: return 'available';
      case BookingStatus.CheckedIn: return 'occupied';
      case BookingStatus.Completed: return 'active';
      case BookingStatus.Cancelled:
      case BookingStatus.Rejected:
      case BookingStatus.NoShow:
        return 'locked';
      default:
        return 'inactive';
    }
  }
}
