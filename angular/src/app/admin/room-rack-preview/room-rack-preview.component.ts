import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LocalizationPipe } from '@abp/ng.core';
import { BhStatusBadgeComponent } from '../../shared/components/status-badge/bh-status-badge.component';
import { BhToastrService } from '../../shared/components/toastr/bh-toastr.service';
import { RoomRackCardItem, RoomRackStatus } from '../../shared/models/ui-models';

@Component({
  selector: 'bh-room-rack-preview',
  standalone: true,
  imports: [CommonModule, LocalizationPipe, BhStatusBadgeComponent],
  templateUrl: './room-rack-preview.component.html',
  styleUrls: ['./room-rack-preview.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RoomRackPreviewComponent {
  private toastr = inject(BhToastrService);

  selectedStatus = signal<RoomRackStatus | 'All'>('All');
  selectedFloor = signal<number | 0>(0);

  // 12 phòng mẫu đại diện cho 3 tầng và 4 trạng thái
  rooms = signal<RoomRackCardItem[]>([
    {
      roomId: 'r-101',
      roomNumber: '101',
      roomTypeNameKey: 'Deluxe Queen',
      floor: 1,
      status: 'Available',
      pricePerNight: 1200000,
      capacity: 2,
    },
    {
      roomId: 'r-102',
      roomNumber: '102',
      roomTypeNameKey: 'Deluxe Twin',
      floor: 1,
      status: 'Occupied',
      pricePerNight: 1200000,
      guestName: 'Nguyễn Văn An',
      checkOutDate: '2026-11-15',
      capacity: 2,
    },
    {
      roomId: 'r-103',
      roomNumber: '103',
      roomTypeNameKey: 'Superior Single',
      floor: 1,
      status: 'Dirty',
      pricePerNight: 950000,
      capacity: 1,
    },
    {
      roomId: 'r-104',
      roomNumber: '104',
      roomTypeNameKey: 'Standard Double',
      floor: 1,
      status: 'Maintenance',
      pricePerNight: 1000000,
      capacity: 2,
    },
    {
      roomId: 'r-201',
      roomNumber: '201',
      roomTypeNameKey: 'Executive Suite',
      floor: 2,
      status: 'Available',
      pricePerNight: 2500000,
      capacity: 3,
    },
    {
      roomId: 'r-202',
      roomNumber: '202',
      roomTypeNameKey: 'Executive Suite',
      floor: 2,
      status: 'Occupied',
      pricePerNight: 2500000,
      guestName: 'Johnathan Miller',
      checkOutDate: '2026-11-16',
      capacity: 3,
    },
    {
      roomId: 'r-203',
      roomNumber: '203',
      roomTypeNameKey: 'Deluxe Twin',
      floor: 2,
      status: 'Available',
      pricePerNight: 1350000,
      capacity: 2,
    },
    {
      roomId: 'r-204',
      roomNumber: '204',
      roomTypeNameKey: 'Deluxe King',
      floor: 2,
      status: 'Dirty',
      pricePerNight: 1450000,
      capacity: 2,
    },
    {
      roomId: 'r-301',
      roomNumber: '301',
      roomTypeNameKey: 'Presidential Suite',
      floor: 3,
      status: 'Available',
      pricePerNight: 5200000,
      capacity: 4,
    },
    {
      roomId: 'r-302',
      roomNumber: '302',
      roomTypeNameKey: 'Presidential Suite',
      floor: 3,
      status: 'Occupied',
      pricePerNight: 5200000,
      guestName: 'Trần Thị Mai',
      checkOutDate: '2026-11-18',
      capacity: 4,
    },
    {
      roomId: 'r-303',
      roomNumber: '303',
      roomTypeNameKey: 'Family Suite',
      floor: 3,
      status: 'Available',
      pricePerNight: 3100000,
      capacity: 5,
    },
    {
      roomId: 'r-304',
      roomNumber: '304',
      roomTypeNameKey: 'Family Suite',
      floor: 3,
      status: 'Dirty',
      pricePerNight: 3100000,
      capacity: 5,
    },
  ]);

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

  setStatusFilter(status: RoomRackStatus | 'All') {
    this.selectedStatus.set(status);
  }

  setFloorFilter(floor: number) {
    this.selectedFloor.set(floor);
  }

  quickCheckIn(room: RoomRackCardItem) {
    this.toastr.success(
      `Phòng ${room.roomNumber} đã hoàn tất thủ tục nhận phòng.`,
      'RoomRack:QuickCheckIn'
    );
    this.rooms.update(list =>
      list.map(r =>
        r.roomId === room.roomId
          ? { ...r, status: 'Occupied', guestName: 'Khách Check-in Nhanh', checkOutDate: '2026-11-14' }
          : r
      )
    );
  }

  toggleHousekeeping(room: RoomRackCardItem) {
    const newStatus: RoomRackStatus = room.status === 'Dirty' ? 'Available' : 'Dirty';
    this.rooms.update(list =>
      list.map(r => (r.roomId === room.roomId ? { ...r, status: newStatus } : r))
    );
    this.toastr.info(`Phòng ${room.roomNumber}: Cập nhật trạng thái thành công.`);
  }
}
