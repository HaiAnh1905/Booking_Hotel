import { ChangeDetectionStrategy, Component, computed, effect, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { LocalizationPipe, LocalizationService } from '@abp/ng.core';
import { BhDataTableComponent } from '../../../shared/components/data-table/bh-data-table.component';
import { BhStatusBadgeComponent } from '../../../shared/components/status-badge/bh-status-badge.component';
import { BhEmptyStateComponent } from '../../../shared/components/empty-state/bh-empty-state.component';
import { BhLoadingSpinnerComponent } from '../../../shared/components/loading-spinner/bh-loading-spinner.component';
import { BhToastrService } from '../../../shared/components/toastr/bh-toastr.service';
import { BhDataTableColumn, BhPageChangeEvent, BhPaginationState, StatusBadgeVariant } from '../../../shared/models/ui-models';
import { PropertyContextService } from '../../services/property-context.service';
import {
  BookingStatus,
  FrontdeskReservationListItemDto,
  FrontdeskRoomRackDto,
  FrontdeskRoomRackItemDto,
  FrontdeskRoomRackStatus,
  FrontdeskService,
  RoomAssignmentStatus,
} from '../../services/frontdesk/frontdesk.service';

@Component({
  selector: 'bh-room-rack',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    LocalizationPipe,
    BhDataTableComponent,
    BhStatusBadgeComponent,
    BhEmptyStateComponent,
    BhLoadingSpinnerComponent,
  ],
  templateUrl: './room-rack.component.html',
  styleUrls: ['./room-rack.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RoomRackComponent {
  readonly propertyService = inject(PropertyContextService);
  private frontdeskService = inject(FrontdeskService);
  private toastr = inject(BhToastrService);
  private localization = inject(LocalizationService);
  private router = inject(Router);

  readonly RackStatus = FrontdeskRoomRackStatus;
  readonly BookingStatus = BookingStatus;

  readonly viewMode = signal<'grid' | 'table'>('grid');
  readonly selectedFloor = signal<number | null>(null);
  readonly selectedStatus = signal<FrontdeskRoomRackStatus | null>(null);
  readonly searchQuery = signal<string>('');
  readonly isLoading = signal<boolean>(false);

  readonly currentPage = signal<number>(1);
  readonly pageSize = signal<number>(12);

  readonly rackData = signal<FrontdeskRoomRackDto | null>(null);
  readonly selectedRoom = signal<FrontdeskRoomRackItemDto | null>(null);
  readonly unassignedBookings = signal<FrontdeskReservationListItemDto[]>([]);
  readonly isLoadingUnassigned = signal<boolean>(false);
  readonly assigningBookingId = signal<string | null>(null);

  readonly tableColumns: BhDataTableColumn<FrontdeskRoomRackItemDto>[] = [
    { field: 'roomNumber', headerKey: 'Approvals:RoomNumber', sortable: true },
    { field: 'roomTypeName', headerKey: 'RoomRack:ColRoomType', sortable: true },
    { field: 'floor', headerKey: 'RoomRack:Floor', sortable: true },
    { field: 'rackStatus', headerKey: 'Common:Status', sortable: true },
    { field: 'guestName', headerKey: 'RoomRack:Guest' },
    { field: 'actions', headerKey: 'Common:Actions', align: 'right' },
  ];

  readonly floors = computed<number[]>(() => this.rackData()?.floors ?? []);
  readonly allRooms = computed<FrontdeskRoomRackItemDto[]>(() => this.rackData()?.rooms ?? []);

  readonly filteredRooms = computed(() => {
    let list = this.allRooms();
    const floor = this.selectedFloor();
    const status = this.selectedStatus();
    const q = this.searchQuery().trim().toLowerCase();

    if (floor !== null) {
      list = list.filter(r => r.floor === floor);
    }
    if (status !== null) {
      list = list.filter(r => r.rackStatus === status);
    }
    if (q) {
      list = list.filter(
        r =>
          r.roomNumber.toLowerCase().includes(q) ||
          r.roomTypeName.toLowerCase().includes(q) ||
          (r.currentBooking?.guestFullName?.toLowerCase().includes(q) ?? false) ||
          (r.currentBooking?.bookingCode?.toLowerCase().includes(q) ?? false)
      );
    }
    return list;
  });

  readonly paginatedRooms = computed(() => {
    const list = this.filteredRooms();
    const start = (this.currentPage() - 1) * this.pageSize();
    return list.slice(start, start + this.pageSize());
  });

  readonly pagination = computed<BhPaginationState>(() => ({
    currentPage: this.currentPage(),
    pageSize: this.pageSize(),
    totalCount: this.filteredRooms().length,
  }));

  readonly hasActiveFilters = computed(
    () =>
      this.selectedFloor() !== null ||
      this.selectedStatus() !== null ||
      this.searchQuery().trim().length > 0
  );

  readonly counts = computed(() => {
    const d = this.rackData();
    return {
      total: d?.totalRooms ?? 0,
      ready: d?.readyCount ?? 0,
      reserved: d?.reservedCount ?? 0,
      occupied: d?.occupiedCount ?? 0,
      notReady: d?.notReadyCount ?? 0,
      unavailable: d?.unavailableCount ?? 0,
    };
  });

  constructor() {
    effect(() => {
      const hotelId = this.propertyService.currentHotelId();
      if (hotelId) {
        this.loadRooms(hotelId);
      } else {
        this.rackData.set(null);
      }
    });
  }

  loadRooms(hotelId?: string | null): void {
    const targetHotelId = hotelId ?? this.propertyService.currentHotelId();
    if (!targetHotelId) {
      this.rackData.set(null);
      return;
    }

    this.isLoading.set(true);
    this.frontdeskService.getRoomRack({ hotelId: targetHotelId }).subscribe({
      next: res => {
        this.rackData.set(res);
        this.currentPage.set(1);
        this.isLoading.set(false);
        const cur = this.selectedRoom();
        if (cur) {
          this.selectedRoom.set(res.rooms.find(r => r.roomId === cur.roomId) ?? null);
        }
      },
      error: err => {
        this.rackData.set(null);
        this.isLoading.set(false);
        this.toastr.error(
          err?.error?.error?.message || this.localization.instant('RoomRack:LoadError'),
          this.localization.instant('Toastr:Error')
        );
      },
    });
  }

  toggleViewMode(mode: 'grid' | 'table'): void {
    this.viewMode.set(mode);
  }

  setFloorFilter(floor: number | null): void {
    this.selectedFloor.set(floor);
    this.currentPage.set(1);
  }

  setStatusFilter(status: FrontdeskRoomRackStatus | null): void {
    this.selectedStatus.set(this.selectedStatus() === status ? null : status);
    this.currentPage.set(1);
  }

  onSearchChange(query: string): void {
    this.searchQuery.set(query);
    this.currentPage.set(1);
  }

  resetFilters(): void {
    this.selectedFloor.set(null);
    this.selectedStatus.set(null);
    this.searchQuery.set('');
    this.currentPage.set(1);
  }

  onPageChange(event: BhPageChangeEvent): void {
    this.currentPage.set(event.page);
    this.pageSize.set(event.pageSize);
  }

  openRoomDetail(room: FrontdeskRoomRackItemDto): void {
    this.selectedRoom.set(room);
    this.unassignedBookings.set([]);

    const hotelId = this.propertyService.currentHotelId();
    if (hotelId && room.rackStatus === FrontdeskRoomRackStatus.Ready) {
      this.loadUnassignedBookingsForRoom(hotelId, room);
    }
  }

  private loadUnassignedBookingsForRoom(hotelId: string, room: FrontdeskRoomRackItemDto): void {
    this.isLoadingUnassigned.set(true);
    this.frontdeskService
      .getReservations({
        hotelId,
        roomAssignmentStatus: RoomAssignmentStatus.Unassigned,
        skipCount: 0,
        maxResultCount: 30,
      })
      .subscribe({
        next: res => {
          const matching = (res.items || []).filter(
            b =>
              (b.bookingStatus === BookingStatus.Confirmed || b.bookingStatus === BookingStatus.PendingConfirmation) &&
              (!b.roomTypeId || b.roomTypeId === room.roomTypeId)
          );
          this.unassignedBookings.set(matching);
          this.isLoadingUnassigned.set(false);
        },
        error: () => {
          this.unassignedBookings.set([]);
          this.isLoadingUnassigned.set(false);
        },
      });
  }

  assignBookingToSelectedRoom(booking: FrontdeskReservationListItemDto): void {
    const room = this.selectedRoom();
    const hotelId = this.propertyService.currentHotelId();
    if (!room || !hotelId || this.assigningBookingId()) return;

    this.assigningBookingId.set(booking.id);
    this.frontdeskService.assignRoom(booking.id, hotelId, room.roomId).subscribe({
      next: () => {
        this.assigningBookingId.set(null);
        this.toastr.success(this.localization.instant('Frontdesk:AssignRoomSuccess'));
        this.loadRooms(hotelId);
      },
      error: err => {
        this.assigningBookingId.set(null);
        this.toastr.error(
          err?.error?.error?.message || this.localization.instant('Frontdesk:LoadError'),
          this.localization.instant('Toastr:Error')
        );
      },
    });
  }

  closeRoomDetail(): void {
    this.selectedRoom.set(null);
    this.unassignedBookings.set([]);
  }

  goToReservationsList(): void {
    this.router.navigate(['/frontdesk/reservations']);
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

  goToReservation(bookingId: string): void {
    this.router.navigate(['/frontdesk/reservations'], {
      queryParams: { bookingId },
    });
  }

  getStatusCssClass(status: FrontdeskRoomRackStatus): string {
    switch (status) {
      case FrontdeskRoomRackStatus.Ready: return 'status-available';
      case FrontdeskRoomRackStatus.Reserved: return 'status-reserved';
      case FrontdeskRoomRackStatus.Occupied: return 'status-occupied';
      case FrontdeskRoomRackStatus.NotReady: return 'status-dirty';
      default: return 'status-unavailable';
    }
  }

  getBadgeVariant(status: FrontdeskRoomRackStatus): StatusBadgeVariant {
    switch (status) {
      case FrontdeskRoomRackStatus.Ready: return 'available';
      case FrontdeskRoomRackStatus.Reserved: return 'pending';
      case FrontdeskRoomRackStatus.Occupied: return 'occupied';
      case FrontdeskRoomRackStatus.NotReady: return 'dirty';
      default: return 'maintenance';
    }
  }

  getStatusText(status: FrontdeskRoomRackStatus): string {
    switch (status) {
      case FrontdeskRoomRackStatus.Ready: return this.localization.instant('Frontdesk:RackStatusReady');
      case FrontdeskRoomRackStatus.Reserved: return this.localization.instant('Frontdesk:RackStatusReserved');
      case FrontdeskRoomRackStatus.Occupied: return this.localization.instant('Frontdesk:RackStatusOccupied');
      case FrontdeskRoomRackStatus.NotReady: return this.localization.instant('Frontdesk:RackStatusNotReady');
      default: return this.localization.instant('Frontdesk:RackStatusUnavailable');
    }
  }
}
