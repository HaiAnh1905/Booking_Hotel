import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  inject,
  signal,
  effect,
  computed,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LocalizationPipe, LocalizationService } from '@abp/ng.core';
import { PropertyContextService } from '../../../services/property-context.service';
import {
  ManagerRoomService,
  RoomDto,
  RoomTypeDto,
  RoomOperationalStatus,
  RoomOccupancyStatus,
  HousekeepingStatus,
  RoomClosureType,
  RoomClosureStatus,
  AffectedBookingResolutionType,
  RoomDashboardOverviewDto,
  AffectedBookingDto,
  RoomClosureDto,
  AlternativeRoomSuggestionDto,
} from '../../../services/manager/manager-room.service';
import { RoomStatusModalComponent } from '../room-modal/room-status-modal.component';
import {
  RoomTypeModalComponent,
  AVAILABLE_ROOM_AMENITIES,
} from '../room-type-modal/room-type-modal.component';
import {
  BhSearchableSelectComponent,
  SelectOption,
} from '../../../../shared/components/searchable-select/bh-searchable-select.component';
import { BhConfirmService } from '../../../../shared/components/confirm-modal/bh-confirm.service';

export interface FloorGroup {
  floor: number;
  rooms: RoomDto[];
}

@Component({
  selector: 'bh-room-list',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    LocalizationPipe,
    RoomStatusModalComponent,
    RoomTypeModalComponent,
    BhSearchableSelectComponent,
  ],
  templateUrl: './room-list.component.html',
  styleUrls: ['./room-list.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RoomListComponent implements OnInit {
  readonly propertyService = inject(PropertyContextService);
  private roomService = inject(ManagerRoomService);
  private localizationService = inject(LocalizationService);
  private confirmService = inject(BhConfirmService);

  getAmenityLabel(am: string): string {
    const matched = AVAILABLE_ROOM_AMENITIES.find(item => item.id === am || item.legacyVi === am);
    if (matched) {
      return this.localizationService.instant(matched.key);
    }
    return am;
  }

  readonly RoomOperationalStatus = RoomOperationalStatus;
  readonly RoomOccupancyStatus = RoomOccupancyStatus;
  readonly HousekeepingStatus = HousekeepingStatus;
  readonly RoomClosureType = RoomClosureType;
  readonly RoomClosureStatus = RoomClosureStatus;
  readonly AffectedBookingResolutionType = AffectedBookingResolutionType;

  activeTab = signal<'rooms' | 'types' | 'closures'>('rooms');
  roomViewMode = signal<'table' | 'cards'>('table');

  // Dữ liệu buồng phòng
  readonly rooms = signal<RoomDto[]>([]);
  readonly roomTypes = signal<RoomTypeDto[]>([]);
  readonly dashboard = signal<RoomDashboardOverviewDto | null>(null);
  readonly closures = signal<RoomClosureDto[]>([]);
  readonly isLoading = signal<boolean>(false);

  // Bộ lọc
  selectedFloor = signal<number | null>(null);
  selectedRoomTypeFilter = signal<string | null>(null);
  selectedOccupancyStatus = signal<RoomOccupancyStatus | null>(null);
  selectedOperationalStatus = signal<RoomOperationalStatus | null>(null);
  searchTerm = signal<string>('');

  readonly hasActiveFilters = computed<boolean>(() => {
    return (
      this.selectedFloor() !== null ||
      !!this.selectedRoomTypeFilter() ||
      this.selectedOccupancyStatus() !== null ||
      this.selectedOperationalStatus() !== null ||
      this.searchTerm().trim().length > 0
    );
  });

  resetFilters(): void {
    this.selectedFloor.set(null);
    this.selectedRoomTypeFilter.set(null);
    this.selectedOccupancyStatus.set(null);
    this.selectedOperationalStatus.set(null);
    this.searchTerm.set('');
  }

  get floorFilterOptions(): SelectOption[] {
    const floorLabel = this.localizationService.instant('RoomManagement:Floor');
    return [
      { value: null, label: this.localizationService.instant('RoomManagement:AllFloors') },
      ...this.floors.map(f => ({ value: f, label: `${floorLabel} ${f}` })),
    ];
  }

  get roomTypeFilterOptions(): SelectOption[] {
    return [
      { value: null, label: this.localizationService.instant('RoomManagement:AllRoomTypes') },
      ...this.roomTypes().map(t => ({ value: t.id, label: t.name })),
    ];
  }

  get occupancyFilterOptions(): SelectOption[] {
    return [
      { value: null, label: this.localizationService.instant('Common:All') },
      { value: RoomOccupancyStatus.Vacant, label: this.localizationService.instant('RoomManagement:Occupancy:Vacant') },
      { value: RoomOccupancyStatus.Reserved, label: this.localizationService.instant('RoomManagement:Occupancy:Reserved') },
      { value: RoomOccupancyStatus.Occupied, label: this.localizationService.instant('RoomManagement:Occupancy:Occupied') },
      { value: RoomOccupancyStatus.Blocked, label: this.localizationService.instant('RoomManagement:Occupancy:Blocked') },
    ];
  }

  get operationalFilterOptions(): SelectOption[] {
    return [
      { value: null, label: this.localizationService.instant('Common:All') },
      { value: RoomOperationalStatus.Active, label: this.localizationService.instant('RoomManagement:Operational:Active') },
      { value: RoomOperationalStatus.UnderMaintenance, label: this.localizationService.instant('RoomManagement:Operational:UnderMaintenance') },
      { value: RoomOperationalStatus.OutOfService, label: this.localizationService.instant('RoomManagement:Operational:OutOfService') },
    ];
  }

  // Modals state
  isStatusModalOpen = signal<boolean>(false);
  selectedRoomForStatus = signal<RoomDto | null>(null);

  isTypeModalOpen = signal<boolean>(false);
  selectedRoomType = signal<RoomTypeDto | null>(null);

  // Quick Add / Edit Room
  isAddRoomModalOpen = signal<boolean>(false);
  isSavingRoom = signal<boolean>(false);
  editingRoomId = signal<string | null>(null);
  newRoomNumber = '';
  newRoomFloor = 1;
  newRoomTypeId = '';
  newRoomBuilding = '';
  newRoomDescription = '';

  // Planned Closure Modal
  isPlannedClosureModalOpen = signal<boolean>(false);
  isSavingPlannedClosure = signal<boolean>(false);
  plannedRoomIds = signal<string[]>([]);
  plannedStartDate = '';
  plannedEndDate = '';
  plannedReason = '';
  affectedBookingsPreview = signal<AffectedBookingDto[]>([]);
  isCheckingAffected = signal<boolean>(false);

  // Emergency Closure Modal
  isEmergencyClosureModalOpen = signal<boolean>(false);
  isSavingEmergencyClosure = signal<boolean>(false);
  emergencyRoomIds = signal<string[]>([]);
  emergencyReason = '';
  emergencyDescription = '';
  emergencyStartDate = '';
  emergencyEstimatedRecovery = '';

  // Resolution Modal
  isResolutionModalOpen = signal<boolean>(false);
  activeClosureForResolution = signal<RoomClosureDto | null>(null);
  selectedBookingForResolution = signal<any | null>(null);
  resolutionType = signal<AffectedBookingResolutionType>(AffectedBookingResolutionType.RoomChange);
  alternativeRooms = signal<AlternativeRoomSuggestionDto[]>([]);
  selectedAlternativeRoomId = signal<string | null>(null);
  resolutionNotes = '';
  isSavingResolution = signal<boolean>(false);

  // Recovery Modal
  isRecoveryModalOpen = signal<boolean>(false);
  closureForRecovery = signal<RoomClosureDto | null>(null);
  recoveryNotes = '';
  isSavingRecovery = signal<boolean>(false);

  readonly roomTypeOptions = computed<SelectOption[]>(() => {
    const types = this.roomTypes();
    if (!types || types.length === 0) {
      return [
        {
          value: '',
          label: 'Chưa có hạng phòng nào',
          sublabel: 'Vui lòng tạo hạng phòng trước',
          icon: 'fa-solid fa-circle-exclamation text-warning',
          disabled: true,
        },
      ];
    }
    return types.map(t => ({
      value: t.id,
      label: t.name,
      sublabel: t.basePrice ? `${Number(t.basePrice).toLocaleString()} VNĐ` : undefined,
      icon: 'fa-solid fa-door-open',
    }));
  });

  readonly floorGroups = computed<FloorGroup[]>(() => {
    const filtered = this.filteredRooms;
    const map = new Map<number, RoomDto[]>();
    for (const r of filtered) {
      if (!map.has(r.floor)) {
        map.set(r.floor, []);
      }
      map.get(r.floor)!.push(r);
    }
    const result: FloorGroup[] = [];
    const sortedFloors = Array.from(map.keys()).sort((a, b) => a - b);
    for (const floor of sortedFloors) {
      result.push({
        floor,
        rooms: map.get(floor)!.sort((a, b) => a.roomNumber.localeCompare(b.roomNumber)),
      });
    }
    return result;
  });

  constructor() {
    effect(() => {
      const hotelId = this.propertyService.currentHotelId();
      if (hotelId) {
        this.loadData(hotelId);
      }
    });
  }

  ngOnInit(): void {}

  loadData(hotelId: string): void {
    this.isLoading.set(true);

    this.roomService.getDashboardOverview(hotelId).subscribe({
      next: data => this.dashboard.set(data),
      error: () => this.dashboard.set(null),
    });

    this.roomService.getRoomTypes(hotelId).subscribe({
      next: types => this.roomTypes.set(types),
      error: () => this.roomTypes.set([]),
    });

    this.roomService.getPhysicalRooms(hotelId).subscribe({
      next: rooms => {
        this.rooms.set(rooms);
        this.isLoading.set(false);
      },
      error: () => {
        this.rooms.set([]);
        this.isLoading.set(false);
      },
    });

    this.loadClosures(hotelId);
  }

  loadClosures(hotelId: string): void {
    this.roomService.getClosureHistory(hotelId).subscribe({
      next: data => this.closures.set(data),
      error: () => this.closures.set([]),
    });
  }

  get floors(): number[] {
    const list = this.rooms().map(r => r.floor);
    return Array.from(new Set(list)).sort((a, b) => a - b);
  }

  get filteredRooms(): RoomDto[] {
    const keyword = this.searchTerm().trim().toLowerCase();
    return this.rooms().filter(r => {
      const matchFloor = this.selectedFloor() === null || r.floor === this.selectedFloor();
      const matchType =
        !this.selectedRoomTypeFilter() || r.roomTypeId === this.selectedRoomTypeFilter();
      const matchOccupancy =
        this.selectedOccupancyStatus() === null ||
        r.occupancyStatus === this.selectedOccupancyStatus();
      const matchOperational =
        this.selectedOperationalStatus() === null ||
        r.operationalStatus === this.selectedOperationalStatus();
      const matchSearch =
        !keyword ||
        r.roomNumber.toLowerCase().includes(keyword) ||
        (r.building && r.building.toLowerCase().includes(keyword));

      return matchFloor && matchType && matchOccupancy && matchOperational && matchSearch;
    });
  }

  // Quản lý Modal đổi trạng thái vận hành
  openStatusModal(room: RoomDto): void {
    this.selectedRoomForStatus.set(room);
    this.isStatusModalOpen.set(true);
  }

  onRoomStatusChanged(updated: RoomDto): void {
    this.rooms.update(list => list.map(r => (r.id === updated.id ? updated : r)));
    const hotelId = this.propertyService.currentHotelId();
    if (hotelId) this.loadData(hotelId);
  }

  // Chuyển nhanh trạng thái vệ sinh
  quickUpdateHousekeeping(room: RoomDto, target: HousekeepingStatus): void {
    const hotelId = this.propertyService.currentHotelId();
    if (!hotelId) return;

    this.roomService
      .updateHousekeepingStatus(room.id, {
        hotelId,
        targetStatus: target,
      })
      .subscribe({
        next: updated => {
          this.rooms.update(list => list.map(r => (r.id === updated.id ? updated : r)));
        },
      });
  }

  // Deactivate Room
  async toggleDeactivate(room: RoomDto): Promise<void> {
    const hotelId = this.propertyService.currentHotelId();
    if (!hotelId) return;

    if (room.isActive) {
      const confirmed = await this.confirmService.confirm({
        title: 'Ngừng kinh doanh phòng',
        message: `Xác nhận ngừng kinh doanh phòng "${room.roomNumber}"? Phòng sẽ bị ẩn khỏi các cổng đặt phòng mới.`,
        confirmText: 'Ngừng kinh doanh',
        cancelText: 'Hủy',
        type: 'warning',
      });
      if (!confirmed) {
        return;
      }
      this.roomService.deactivateRoom(room.id, { hotelId }).subscribe({
        next: () => {
          this.rooms.update(list =>
            list.map(r => (r.id === room.id ? { ...r, isActive: false } : r)),
          );
        },
        error: err => {
          alert(
            err.error?.error?.message ||
              'Không thể ngừng kinh doanh phòng đang có khách hoặc có booking chưa xử lý!',
          );
        },
      });
    } else {
      const confirmed = await this.confirmService.confirm({
        title: 'Mở lại kinh doanh phòng',
        message: `Xác nhận mở lại kinh doanh cho phòng "${room.roomNumber}"?`,
        confirmText: 'Mở lại',
        cancelText: 'Hủy',
        type: 'primary',
      });
      if (!confirmed) {
        return;
      }

      this.roomService.activateRoom(room.id, hotelId).subscribe({
        next: () => {
          this.rooms.update(list =>
            list.map(r => (r.id === room.id ? { ...r, isActive: true } : r)),
          );
        },
        error: err => {
          alert(err.error?.error?.message || 'Không thể mở lại kinh doanh cho phòng này!');
        },
      });
    }
  }

  // Quản lý Loại phòng
  openTypeModal(type?: RoomTypeDto): void {
    this.selectedRoomType.set(type || null);
    this.isTypeModalOpen.set(true);
  }

  onRoomTypeSaved(saved: RoomTypeDto): void {
    const hotelId = this.propertyService.currentHotelId();
    if (hotelId) {
      this.loadData(hotelId);
    }
  }

  async deleteRoomType(type: RoomTypeDto): Promise<void> {
    const hotelId = this.propertyService.currentHotelId();
    if (!hotelId) return;

    const confirmed = await this.confirmService.confirm({
      title: 'Xóa loại phòng',
      message: `Xác nhận xóa loại phòng "${type.name}"?`,
      confirmText: 'Xóa',
      cancelText: 'Hủy',
      type: 'danger',
    });
    if (!confirmed) return;

    this.roomService.deleteRoomType(type.id, hotelId).subscribe({
      next: () => {
        this.roomTypes.update(list => list.filter(t => t.id !== type.id));
      },
      error: err => {
        alert(err.error?.error?.message || 'Không thể xóa loại phòng đã có phòng thực tế gán');
      },
    });
  }

  // Quick Add / Edit Room
  openAddRoomModal(): void {
    this.editingRoomId.set(null);
    if (this.roomTypes().length > 0) {
      this.newRoomTypeId = this.roomTypes()[0].id;
    } else {
      this.newRoomTypeId = '';
    }
    this.newRoomNumber = '';
    this.newRoomFloor = 1;
    this.newRoomBuilding = '';
    this.newRoomDescription = '';
    this.isAddRoomModalOpen.set(true);
  }

  openEditRoomModal(room: RoomDto): void {
    this.editingRoomId.set(room.id);
    this.newRoomNumber = room.roomNumber;
    this.newRoomFloor = room.floor;
    this.newRoomTypeId = room.roomTypeId;
    this.newRoomBuilding = room.building || '';
    this.newRoomDescription = room.description || '';
    this.isAddRoomModalOpen.set(true);
  }

  closeRoomModal(): void {
    this.isAddRoomModalOpen.set(false);
    this.editingRoomId.set(null);
  }

  saveRoom(): void {
    const hotelId = this.propertyService.currentHotelId();
    if (!hotelId || !this.newRoomNumber?.trim() || !this.newRoomTypeId) return;

    this.isSavingRoom.set(true);
    const roomId = this.editingRoomId();

    if (roomId) {
      this.roomService
        .updatePhysicalRoom(roomId, {
          hotelId,
          roomNumber: this.newRoomNumber.trim(),
          floor: Number(this.newRoomFloor),
          roomTypeId: this.newRoomTypeId,
          building: this.newRoomBuilding.trim() || undefined,
          description: this.newRoomDescription.trim() || undefined,
        })
        .subscribe({
          next: updated => {
            this.rooms.update(list => list.map(r => (r.id === updated.id ? updated : r)));
            this.isSavingRoom.set(false);
            this.closeRoomModal();
          },
          error: err => {
            this.isSavingRoom.set(false);
            alert(err.error?.error?.message || 'Không thể cập nhật thông tin phòng');
          },
        });
    } else {
      this.roomService
        .createPhysicalRoom({
          hotelId,
          roomNumber: this.newRoomNumber.trim(),
          floor: Number(this.newRoomFloor),
          roomTypeId: this.newRoomTypeId,
          building: this.newRoomBuilding.trim() || undefined,
          description: this.newRoomDescription.trim() || undefined,
        })
        .subscribe({
          next: created => {
            this.rooms.update(list => [...list, created]);
            this.isSavingRoom.set(false);
            this.closeRoomModal();
          },
          error: err => {
            this.isSavingRoom.set(false);
            alert(err.error?.error?.message || 'Không thể tạo phòng');
          },
        });
    }
  }

  async deleteRoom(room: RoomDto): Promise<void> {
    const hotelId = this.propertyService.currentHotelId();
    if (!hotelId) return;

    const confirmed = await this.confirmService.confirm({
      title: 'Xóa phòng',
      message: `Xác nhận xóa phòng "${room.roomNumber}" (Tầng ${room.floor})?`,
      confirmText: 'Xóa phòng',
      cancelText: 'Hủy',
      type: 'danger',
    });
    if (!confirmed) {
      return;
    }

    this.roomService.deletePhysicalRoom(room.id, hotelId).subscribe({
      next: () => {
        this.rooms.update(list => list.filter(r => r.id !== room.id));
      },
      error: err => {
        alert(
          err.error?.error?.message ||
            'Không thể xóa phòng đã từng phát sinh dữ liệu đặt phòng. Vui lòng chuyển sang Ngừng kinh doanh!',
        );
      },
    });
  }

  // Planned Closure
  openPlannedClosureModal(): void {
    const today = new Date();
    const tomorrow = new Date(today);
    tomorrow.setDate(tomorrow.getDate() + 1);
    const dayAfter = new Date(today);
    dayAfter.setDate(dayAfter.getDate() + 3);

    this.plannedStartDate = tomorrow.toISOString().split('T')[0];
    this.plannedEndDate = dayAfter.toISOString().split('T')[0];
    this.plannedReason = '';
    this.plannedRoomIds.set([]);
    this.affectedBookingsPreview.set([]);
    this.isPlannedClosureModalOpen.set(true);
  }

  closePlannedClosureModal(): void {
    this.isPlannedClosureModalOpen.set(false);
  }

  togglePlannedRoom(roomId: string): void {
    this.plannedRoomIds.update(ids =>
      ids.includes(roomId) ? ids.filter(id => id !== roomId) : [...ids, roomId],
    );
    this.onPlannedDatesOrRoomsChange();
  }

  onPlannedDatesOrRoomsChange(): void {
    const hotelId = this.propertyService.currentHotelId();
    const roomIds = this.plannedRoomIds();
    if (!hotelId || roomIds.length === 0 || !this.plannedStartDate || !this.plannedEndDate) {
      this.affectedBookingsPreview.set([]);
      return;
    }

    this.isCheckingAffected.set(true);
    this.roomService
      .checkAffectedBookings({
        hotelId,
        roomIds,
        startDate: `${this.plannedStartDate}T00:00:00`,
        endDate: `${this.plannedEndDate}T23:59:59`,
      })
      .subscribe({
        next: res => {
          this.affectedBookingsPreview.set(res);
          this.isCheckingAffected.set(false);
        },
        error: () => {
          this.affectedBookingsPreview.set([]);
          this.isCheckingAffected.set(false);
        },
      });
  }

  savePlannedClosure(): void {
    const hotelId = this.propertyService.currentHotelId();
    const roomIds = this.plannedRoomIds();
    if (
      !hotelId ||
      roomIds.length === 0 ||
      !this.plannedReason.trim() ||
      !this.plannedStartDate ||
      !this.plannedEndDate
    )
      return;

    this.isSavingPlannedClosure.set(true);
    this.roomService
      .createPlannedClosure({
        hotelId,
        roomIds,
        reason: this.plannedReason.trim(),
        startDate: `${this.plannedStartDate}T00:00:00`,
        endDate: `${this.plannedEndDate}T23:59:59`,
      })
      .subscribe({
        next: () => {
          this.isSavingPlannedClosure.set(false);
          this.closePlannedClosureModal();
          this.loadData(hotelId);
        },
        error: err => {
          this.isSavingPlannedClosure.set(false);
          alert(err.error?.error?.message || 'Không thể tạo lịch đóng phòng.');
        },
      });
  }

  // Emergency Closure
  openEmergencyClosureModal(): void {
    const today = new Date();
    this.emergencyStartDate = today.toISOString().split('T')[0];
    this.emergencyEstimatedRecovery = '';
    this.emergencyReason = '';
    this.emergencyDescription = '';
    this.emergencyRoomIds.set([]);
    this.isEmergencyClosureModalOpen.set(true);
  }

  closeEmergencyClosureModal(): void {
    this.isEmergencyClosureModalOpen.set(false);
  }

  toggleEmergencyRoom(roomId: string): void {
    this.emergencyRoomIds.update(ids =>
      ids.includes(roomId) ? ids.filter(id => id !== roomId) : [...ids, roomId],
    );
  }

  async saveEmergencyClosure(): Promise<void> {
    const hotelId = this.propertyService.currentHotelId();
    const roomIds = this.emergencyRoomIds();
    if (
      !hotelId ||
      roomIds.length === 0 ||
      !this.emergencyReason.trim() ||
      !this.emergencyDescription.trim()
    ) {
      alert('Vui lòng chọn phòng, nhập lý do sự cố và mô tả chi tiết!');
      return;
    }

    const confirmed = await this.confirmService.confirm({
      title: 'Cảnh báo khóa phòng khẩn cấp',
      message:
        'Khóa phòng khẩn cấp sẽ đổi phòng sang Out of Service và tự động gửi email cảnh báo cho toàn bộ khách hàng có booking bị ảnh hưởng. Xác nhận tiếp tục?',
      confirmText: 'Xác nhận khóa',
      cancelText: 'Hủy',
      type: 'danger',
      iconClass: 'fa-solid fa-triangle-exclamation',
    });
    if (!confirmed) {
      return;
    }

    this.isSavingEmergencyClosure.set(true);
    this.roomService
      .createEmergencyClosure({
        hotelId,
        roomIds,
        reason: this.emergencyReason.trim(),
        incidentDescription: this.emergencyDescription.trim(),
        startDate: `${this.emergencyStartDate}T00:00:00`,
        estimatedRecoveryTime: this.emergencyEstimatedRecovery
          ? `${this.emergencyEstimatedRecovery}T23:59:59`
          : undefined,
      })
      .subscribe({
        next: () => {
          this.isSavingEmergencyClosure.set(false);
          this.closeEmergencyClosureModal();
          this.loadData(hotelId);
        },
        error: err => {
          this.isSavingEmergencyClosure.set(false);
          alert(err.error?.error?.message || 'Không thể kích hoạt đóng phòng khẩn cấp.');
        },
      });
  }

  // Resolution
  openResolutionModal(closure: RoomClosureDto): void {
    this.activeClosureForResolution.set(closure);
    this.selectedBookingForResolution.set(closure.resolutions[0] || null);
    this.resolutionType.set(AffectedBookingResolutionType.RoomChange);
    this.resolutionNotes = '';
    this.selectedAlternativeRoomId.set(null);
    this.isResolutionModalOpen.set(true);

    if (closure.resolutions.length > 0) {
      this.loadAlternativeRooms(closure.resolutions[0]);
    }
  }

  closeResolutionModal(): void {
    this.isResolutionModalOpen.set(false);
    this.activeClosureForResolution.set(null);
    this.selectedBookingForResolution.set(null);
  }

  selectBookingForResolution(res: any): void {
    this.selectedBookingForResolution.set(res);
    this.resolutionNotes = res.notes || '';
    this.loadAlternativeRooms(res);
  }

  loadAlternativeRooms(res: any): void {
    const hotelId = this.propertyService.currentHotelId();
    if (!hotelId || !res) return;

    this.roomService
      .getAlternativeRoomSuggestions(hotelId, res.bookingId, this.resolutionType())
      .subscribe({
        next: list => {
          this.alternativeRooms.set(list);
          if (list.length > 0) {
            this.selectedAlternativeRoomId.set(list[0].roomId);
          } else {
            this.selectedAlternativeRoomId.set(null);
          }
        },
        error: () => this.alternativeRooms.set([]),
      });
  }

  onResolutionTypeChange(type: AffectedBookingResolutionType): void {
    this.resolutionType.set(type);
    const booking = this.selectedBookingForResolution();
    if (booking) {
      this.loadAlternativeRooms(booking);
    }
  }

  saveResolution(): void {
    const hotelId = this.propertyService.currentHotelId();
    const booking = this.selectedBookingForResolution();
    if (!hotelId || !booking) return;

    this.isSavingResolution.set(true);
    this.roomService
      .resolveAffectedBooking({
        hotelId,
        resolutionId: booking.id,
        resolutionType: this.resolutionType(),
        alternativeRoomId: this.selectedAlternativeRoomId() || undefined,
        notes: this.resolutionNotes.trim() || undefined,
      })
      .subscribe({
        next: () => {
          this.isSavingResolution.set(false);
          this.loadClosures(hotelId);
          this.closeResolutionModal();
        },
        error: err => {
          this.isSavingResolution.set(false);
          alert(err.error?.error?.message || 'Không thể lưu giải pháp xử lý.');
        },
      });
  }

  // Recovery
  openRecoveryModal(closure: RoomClosureDto): void {
    this.closureForRecovery.set(closure);
    this.recoveryNotes = '';
    this.isRecoveryModalOpen.set(true);
  }

  closeRecoveryModal(): void {
    this.isRecoveryModalOpen.set(false);
    this.closureForRecovery.set(null);
  }

  confirmRecovery(): void {
    const hotelId = this.propertyService.currentHotelId();
    const closure = this.closureForRecovery();
    if (!hotelId || !closure) return;

    this.isSavingRecovery.set(true);
    this.roomService
      .confirmRoomRecovery(closure.id, {
        hotelId,
        notes: this.recoveryNotes.trim() || undefined,
      })
      .subscribe({
        next: () => {
          this.isSavingRecovery.set(false);
          this.closeRecoveryModal();
          this.loadData(hotelId);
        },
        error: err => {
          this.isSavingRecovery.set(false);
          alert(err.error?.error?.message || 'Không thể khôi phục phòng.');
        },
      });
  }

  // Badge Helpers
  getRoomStatusThemeClass(room: RoomDto): string {
    if (!room.isActive) {
      return 'room-status-inactive';
    }
    if (
      room.operationalStatus === RoomOperationalStatus.UnderMaintenance ||
      room.operationalStatus === RoomOperationalStatus.OutOfService ||
      room.occupancyStatus === RoomOccupancyStatus.Blocked
    ) {
      return 'room-status-maintenance';
    }
    if (room.occupancyStatus === RoomOccupancyStatus.Occupied) {
      return 'room-status-occupied';
    }
    if (room.occupancyStatus === RoomOccupancyStatus.Reserved) {
      return 'room-status-reserved';
    }
    if (room.housekeepingStatus === HousekeepingStatus.Cleaning) {
      return 'room-status-cleaning';
    }
    if (room.housekeepingStatus === HousekeepingStatus.Dirty) {
      return 'room-status-dirty';
    }
    return 'room-status-available';
  }

  getOperationalBadgeClass(status: RoomOperationalStatus): string {
    switch (status) {
      case RoomOperationalStatus.Active:
        return 'badge-op-available';
      case RoomOperationalStatus.UnderMaintenance:
        return 'badge-op-maintenance';
      case RoomOperationalStatus.OutOfService:
        return 'badge-op-outofservice';
      default:
        return 'badge-op-available';
    }
  }

  getOccupancyBadgeClass(status: RoomOccupancyStatus): string {
    switch (status) {
      case RoomOccupancyStatus.Vacant:
        return 'badge-occ-vacant';
      case RoomOccupancyStatus.Reserved:
        return 'badge-occ-reserved';
      case RoomOccupancyStatus.Occupied:
        return 'badge-occ-occupied';
      case RoomOccupancyStatus.Blocked:
        return 'badge-occ-blocked';
      default:
        return 'badge-occ-vacant';
    }
  }

  getHousekeepingBadgeClass(status: HousekeepingStatus): string {
    switch (status) {
      case HousekeepingStatus.Clean:
        return 'badge-hk-clean';
      case HousekeepingStatus.Dirty:
        return 'badge-hk-dirty';
      case HousekeepingStatus.Cleaning:
        return 'badge-hk-cleaning';
      case HousekeepingStatus.Inspected:
        return 'badge-hk-inspected';
      default:
        return '';
    }
  }

  getHousekeepingStatusKey(status: HousekeepingStatus): string {
    switch (status) {
      case HousekeepingStatus.Clean:
        return 'Manager:Housekeeping:Clean';
      case HousekeepingStatus.Dirty:
        return 'Manager:Housekeeping:Dirty';
      case HousekeepingStatus.Cleaning:
        return 'Manager:Housekeeping:Cleaning';
      case HousekeepingStatus.Inspected:
        return 'Manager:Housekeeping:Inspected';
      default:
        return '';
    }
  }
}
