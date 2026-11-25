import { ChangeDetectionStrategy, Component, computed, effect, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { forkJoin } from 'rxjs';
import { LocalizationPipe, LocalizationService } from '@abp/ng.core';
import { BhStatusBadgeComponent } from '../../shared/components/status-badge/bh-status-badge.component';
import { BhEmptyStateComponent } from '../../shared/components/empty-state/bh-empty-state.component';
import { BhLoadingSpinnerComponent } from '../../shared/components/loading-spinner/bh-loading-spinner.component';
import { BhToastrService } from '../../shared/components/toastr/bh-toastr.service';
import { RoomRackCardItem, RoomRackStatus } from '../../shared/models/ui-models';
import { PropertyContextService } from '../services/property-context.service';
import {
  HousekeepingStatus,
  ManagerRoomService,
  RoomDto,
  RoomOccupancyStatus,
  RoomOperationalStatus,
  RoomTypeDto,
} from '../services/manager/manager-room.service';

@Component({
  selector: 'bh-room-rack-preview',
  standalone: true,
  imports: [CommonModule, LocalizationPipe, BhStatusBadgeComponent, BhEmptyStateComponent, BhLoadingSpinnerComponent],
  templateUrl: './room-rack-preview.component.html',
  styleUrls: ['./room-rack-preview.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RoomRackPreviewComponent {
  private propertyService = inject(PropertyContextService);
  private roomService = inject(ManagerRoomService);
  private toastr = inject(BhToastrService);
  private localization = inject(LocalizationService);

  selectedStatus = signal<RoomRackStatus | 'All'>('All');
  selectedFloor = signal<number | 0>(0);
  isLoading = signal<boolean>(false);
  rooms = signal<RoomRackCardItem[]>([]);

  floors = computed<number[]>(() =>
    Array.from(new Set(this.rooms().map(r => r.floor))).sort((a, b) => a - b)
  );

  // Bộ lọc danh sách phòng
  filteredRooms = computed(() => {
    const status = this.selectedStatus();
    const floor = this.selectedFloor();

    return this.rooms().filter(r => {
      const matchStatus = status === 'All' || r.status === status;
      const matchFloor = floor === 0 || r.floor === floor;
      return matchStatus && matchFloor;
    });
  });

  // Số lượng thống kê theo trạng thái
  countAvailable = computed(() => this.rooms().filter(r => r.status === 'Available').length);
  countDirty = computed(() => this.rooms().filter(r => r.status === 'Dirty').length);
  countOccupied = computed(() => this.rooms().filter(r => r.status === 'Occupied').length);
  countMaintenance = computed(() => this.rooms().filter(r => r.status === 'Maintenance').length);

  constructor() {
    effect(() => {
      const hotelId = this.propertyService.currentHotelId();
      if (hotelId) {
        this.loadRooms(hotelId);
      } else {
        this.rooms.set([]);
      }
    });
  }

  private loadRooms(hotelId: string): void {
    this.isLoading.set(true);
    forkJoin({
      rooms: this.roomService.getPhysicalRooms(hotelId),
      roomTypes: this.roomService.getRoomTypes(hotelId),
    }).subscribe({
      next: ({ rooms, roomTypes }) => {
        const typeMap = new Map<string, RoomTypeDto>((roomTypes || []).map(rt => [rt.id, rt]));
        const mapped: RoomRackCardItem[] = (rooms || []).map(r => this.mapRoom(r, typeMap.get(r.roomTypeId)));
        this.rooms.set(mapped);
        this.isLoading.set(false);
      },
      error: () => {
        this.rooms.set([]);
        this.isLoading.set(false);
      },
    });
  }

  private mapRoom(room: RoomDto, roomType?: RoomTypeDto): RoomRackCardItem {
    let status: RoomRackStatus = 'Available';
    if (
      !room.isActive ||
      room.operationalStatus === RoomOperationalStatus.UnderMaintenance ||
      room.operationalStatus === RoomOperationalStatus.OutOfService ||
      room.occupancyStatus === RoomOccupancyStatus.Blocked
    ) {
      status = 'Maintenance';
    } else if (room.occupancyStatus === RoomOccupancyStatus.Occupied) {
      status = 'Occupied';
    } else if (
      room.housekeepingStatus === HousekeepingStatus.Dirty ||
      room.housekeepingStatus === HousekeepingStatus.Cleaning
    ) {
      status = 'Dirty';
    }

    return {
      roomId: room.id,
      roomNumber: room.roomNumber,
      roomTypeNameKey: room.roomTypeName || roomType?.name || '',
      floor: room.floor,
      status,
      pricePerNight: roomType?.basePrice ?? 0,
      guestName: room.currentOccupantName || undefined,
      capacity: roomType?.maxOccupancy ?? 0,
    };
  }

  setStatusFilter(status: RoomRackStatus | 'All') {
    this.selectedStatus.set(status);
  }

  setFloorFilter(floor: number) {
    this.selectedFloor.set(floor);
  }

  toggleHousekeeping(room: RoomRackCardItem) {
    const hotelId = this.propertyService.currentHotelId();
    if (!hotelId) return;

    const targetStatus =
      room.status === 'Dirty' ? HousekeepingStatus.Clean : HousekeepingStatus.Dirty;
    this.roomService
      .updateHousekeepingStatus(room.roomId, { hotelId, targetStatus })
      .subscribe({
        next: () => {
          this.toastr.success(
            this.localization.instant('RoomRack:QuickCleanSuccess', room.roomNumber),
            this.localization.instant('RoomRack:QuickCleanTitle')
          );
          this.loadRooms(hotelId);
        },
        error: () => {
          this.toastr.error(this.localization.instant('RoomRack:QuickCleanError'));
        },
      });
  }
}
