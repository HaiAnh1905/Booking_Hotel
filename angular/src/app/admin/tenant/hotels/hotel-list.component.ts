import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LocalizationPipe, LocalizationService } from '@abp/ng.core';
import { BhToastrService } from '../../../shared/components/toastr/bh-toastr.service';
import { BhModalComponent } from '../../../shared/components/modal/bh-modal.component';
import { BhModalConfig } from '../../../shared/models/ui-models';
import { BhSearchableSelectComponent, SelectOption } from '../../../shared/components/searchable-select/bh-searchable-select.component';
import { AssignHotelManagerDto, CreateUpdateHotelDto, HotelDto, HotelService, HotelStatus, SuspendHotelInputDto } from '../../services/hotel.service';
import { VietnamProvincesService } from '../../../shared/services/vietnam-provinces.service';
import { IdentityUserManagementService } from '../../../identity/services/identity-user-management.service';
import { HotelModalComponent } from './hotel-modal.component';
import { HotelSuspendModalComponent } from './hotel-suspend-modal.component';
import { HotelManagerModalComponent } from './hotel-manager-modal.component';

@Component({
  selector: 'bh-hotel-list',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    LocalizationPipe,
    BhModalComponent,
    BhSearchableSelectComponent,
    HotelModalComponent,
    HotelSuspendModalComponent,
    HotelManagerModalComponent,
  ],
  templateUrl: './hotel-list.component.html',
  styleUrls: ['./hotel-list.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HotelListComponent implements OnInit {
  private cdr = inject(ChangeDetectorRef);
  private toastr = inject(BhToastrService);
  private localization = inject(LocalizationService);
  private hotelService = inject(HotelService);
  private provincesService = inject(VietnamProvincesService);
  private userService = inject(IdentityUserManagementService);

  readonly HotelStatus = HotelStatus;

  branches: HotelDto[] = [];
  isLoading = false;
  searchQuery = '';
  selectedStatusFilter = '';

  // Modal states
  isCreateEditModalOpen = false;
  selectedHotelForEdit: HotelDto | null = null;
  hotelModalErrorMessage: string | null = null;
  isHotelSaving = false;

  isSuspendModalOpen = false;
  selectedHotelForSuspend: HotelDto | null = null;

  isAssignModalOpen = false;
  selectedHotelForAssign: HotelDto | null = null;

  isDeleteModalOpen = false;
  deletingHotel: HotelDto | null = null;

  provinceOptions: SelectOption[] = [];
  managerOptions: SelectOption[] = [];

  deleteModalConfig: BhModalConfig = {
    titleKey: 'TenantHotels:DeleteConfirm',
    confirmTextKey: 'Common:Delete',
    cancelTextKey: 'Common:Cancel',
    confirmVariant: 'danger',
    size: 'sm',
    iconClass: 'fa-solid fa-triangle-exclamation',
  };

  ngOnInit(): void {
    this.loadProvinces();
    this.loadManagers();
    this.loadHotels();
  }

  loadHotels(): void {
    this.isLoading = true;
    this.cdr.markForCheck();

    this.hotelService.getList({ maxResultCount: 100 }).subscribe({
      next: (res) => {
        this.branches = res.items || [];
        this.isLoading = false;
        this.cdr.markForCheck();
      },
      error: () => {
        this.isLoading = false;
        this.cdr.markForCheck();
      },
    });
  }

  private loadProvinces(): void {
    this.provincesService.getProvinces().subscribe({
      next: (data) => {
        this.provinceOptions = data.map((p) => ({
          value: p.name,
          label: p.name,
        }));
        this.cdr.markForCheck();
      },
    });
  }

  rawManagerUsers: any[] = [];

  private loadManagers(): void {
    this.userService.getList({ maxResultCount: 1000 }).subscribe({
      next: (res) => {
        const allUsers = res.items || [];
        const managers = allUsers.filter((u) => {
          const hasManagerRole =
            u.roles?.some((r: any) => r.name === 'HotelManager' || r.roleName === 'HotelManager') ||
            u.roleNames?.some((r: string) => r.toLowerCase() === 'hotelmanager');
          const isCurrentManager = this.branches?.some(
            (h) =>
              h.managerUserId === u.id ||
              (h.managerName &&
                (`${u.surname || ''} ${u.name || ''}`.trim().toLowerCase() === h.managerName.toLowerCase() ||
                  u.userName?.toLowerCase() === h.managerName.toLowerCase()))
          );
          return hasManagerRole || isCurrentManager;
        });

        this.rawManagerUsers = managers.length > 0 ? managers : allUsers;

        if (this.selectedHotelForAssign?.managerName && !this.selectedHotelForAssign.managerUserId) {
          const target = this.selectedHotelForAssign.managerName.trim().toLowerCase();
          const matched = this.rawManagerUsers.find((u) => {
            const fullName = `${u.surname || ''} ${u.name || ''}`.trim().toLowerCase();
            return fullName === target || u.userName?.toLowerCase() === target;
          });
          if (matched) {
            this.selectedHotelForAssign.managerUserId = matched.id;
          }
        }

        this.managerOptions = this.rawManagerUsers.map((u) => {
          const fullName = `${u.surname || ''} ${u.name || ''}`.trim();
          const label = fullName ? `${fullName} (${u.userName})` : u.userName;
          return {
            value: u.userName,
            label,
            icon: 'fa-solid fa-user-tie text-primary',
          };
        });
        this.cdr.markForCheck();
      },
      error: (err) => {
        console.error('Failed to load managers:', err);
      },
    });
  }

  get managerUserIdOptions(): SelectOption[] {
    const emptyOption: SelectOption = {
      value: '',
      label: this.localization.instant('TenantHotels:UnassignedManager') || '--- Để trống (Chưa có người quản lý) ---',
      icon: 'fa-regular fa-circle text-muted',
    };
    const options = this.rawManagerUsers.map((u) => {
      const fullName = `${u.surname || ''} ${u.name || ''}`.trim();
      const label = fullName ? `${fullName} (${u.userName})` : u.userName;
      return {
        value: u.id,
        label,
        icon: 'fa-solid fa-user-tie text-primary',
      };
    });

    if (this.selectedHotelForAssign?.managerUserId) {
      if (!options.some((o) => o.value === this.selectedHotelForAssign?.managerUserId)) {
        options.unshift({
          value: this.selectedHotelForAssign.managerUserId,
          label: this.selectedHotelForAssign.managerName || this.selectedHotelForAssign.managerUserId,
          icon: 'fa-solid fa-user-tie text-primary',
        });
      }
    } else if (this.selectedHotelForAssign?.managerName) {
      const target = this.selectedHotelForAssign.managerName.trim().toLowerCase();
      const matched = this.rawManagerUsers.find((u) => {
        const fullName = `${u.surname || ''} ${u.name || ''}`.trim().toLowerCase();
        return fullName === target || u.userName?.toLowerCase() === target;
      });
      if (matched) {
        this.selectedHotelForAssign.managerUserId = matched.id;
      } else {
        options.unshift({
          value: this.selectedHotelForAssign.managerName,
          label: this.selectedHotelForAssign.managerName,
          icon: 'fa-solid fa-user-tie text-primary',
        });
      }
    }

    return [emptyOption, ...options];
  }

  get managerNameOptions(): SelectOption[] {
    const emptyOption: SelectOption = {
      value: '',
      label: this.localization.instant('TenantHotels:UnassignedManager') || '--- Để trống (Chưa có người quản lý) ---',
      icon: 'fa-regular fa-circle text-muted',
    };
    return [emptyOption, ...this.managerOptions];
  }

  get statusFilterOptions(): SelectOption[] {
    return [
      { value: '', label: this.localization.instant('TenantHotels:FilterAllStatus') },
      { value: HotelStatus.Operating, label: this.localization.instant('TenantHotels:FilterOperating'), icon: 'fa-solid fa-circle-check text-success' },
      { value: HotelStatus.ScheduledSuspended, label: this.localization.instant('TenantHotels:StatusScheduledSuspended'), icon: 'fa-solid fa-calendar-xmark text-warning' },
      { value: HotelStatus.EmergencySuspended, label: this.localization.instant('TenantHotels:StatusEmergencySuspended'), icon: 'fa-solid fa-triangle-exclamation text-danger' },
      { value: HotelStatus.Closed, label: this.localization.instant('TenantHotels:FilterClosed'), icon: 'fa-solid fa-ban text-secondary' },
    ];
  }

  get filteredBranches(): HotelDto[] {
    let list = this.branches;
    if (this.searchQuery.trim()) {
      const q = this.searchQuery.toLowerCase().trim();
      list = list.filter(
        (h) =>
          h.name.toLowerCase().includes(q) ||
          h.code.toLowerCase().includes(q) ||
          h.address.toLowerCase().includes(q) ||
          (h.city && h.city.toLowerCase().includes(q))
      );
    }
    if (this.selectedStatusFilter) {
      const statusNum = Number(this.selectedStatusFilter);
      list = list.filter((h) => h.status === statusNum);
    }
    return list;
  }

  get totalBranches(): number {
    return this.branches.length;
  }

  get operatingBranches(): number {
    return this.branches.filter((h) => h.status === HotelStatus.Operating).length;
  }

  get suspendedBranches(): number {
    return this.branches.filter(
      (h) => h.status === HotelStatus.ScheduledSuspended || h.status === HotelStatus.EmergencySuspended
    ).length;
  }

  get totalRoomCapacity(): number {
    return this.branches.reduce((sum, h) => sum + (h.roomCount || 0), 0);
  }

  openCreateModal(): void {
    this.selectedHotelForEdit = null;
    this.hotelModalErrorMessage = null;
    this.isCreateEditModalOpen = true;
  }

  openEditModal(hotel: HotelDto): void {
    this.selectedHotelForEdit = hotel;
    this.hotelModalErrorMessage = null;
    this.isCreateEditModalOpen = true;
  }

  onSaveHotel(dto: CreateUpdateHotelDto): void {
    this.isHotelSaving = true;
    this.hotelModalErrorMessage = null;
    this.cdr.markForCheck();

    const request$ = this.selectedHotelForEdit
      ? this.hotelService.update(this.selectedHotelForEdit.id, dto)
      : this.hotelService.create(dto);

    request$.subscribe({
      next: () => {
        this.isHotelSaving = false;
        this.isCreateEditModalOpen = false;
        this.selectedHotelForEdit = null;
        this.hotelModalErrorMessage = null;
        this.toastr.success(this.localization.instant('TenantHotels:SaveSuccess'));
        this.loadHotels();
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.isHotelSaving = false;
        const msg = err.error?.error?.message || err.error?.message || err.message || this.localization.instant('Common:Error');
        this.hotelModalErrorMessage = msg;
        this.toastr.error(msg);
        this.cdr.markForCheck();
      }
    });
  }

  openSuspendModal(hotel: HotelDto): void {
    this.selectedHotelForSuspend = hotel;
    this.isSuspendModalOpen = true;
  }

  onSuspendHotel(dto: SuspendHotelInputDto): void {
    if (!this.selectedHotelForSuspend) return;
    this.hotelService.suspend(this.selectedHotelForSuspend.id, dto).subscribe({
      next: (res) => {
        this.isSuspendModalOpen = false;
        this.selectedHotelForSuspend = null;
        this.toastr.success(res.message || this.localization.instant('TenantHotels:SuspendSuccess'));
        this.loadHotels();
      },
    });
  }

  activateHotel(hotel: HotelDto): void {
    this.hotelService.activate(hotel.id).subscribe({
      next: () => {
        this.toastr.success(this.localization.instant('TenantHotels:ResumeSuccess'));
        this.loadHotels();
      },
    });
  }

  openAssignManagerModal(hotel: HotelDto): void {
    this.selectedHotelForAssign = hotel;
    this.loadManagers();
    this.isAssignModalOpen = true;
  }

  onAssignManager(dto: AssignHotelManagerDto): void {
    if (!this.selectedHotelForAssign) return;
    this.hotelService.assignManager(this.selectedHotelForAssign.id, dto).subscribe({
      next: () => {
        this.isAssignModalOpen = false;
        this.selectedHotelForAssign = null;
        this.toastr.success(this.localization.instant('TenantHotels:AssignManagerSuccess'));
        this.loadHotels();
      },
    });
  }

  openDeleteModal(hotel: HotelDto): void {
    this.deletingHotel = hotel;
    this.isDeleteModalOpen = true;
  }

  confirmDelete(): void {
    if (!this.deletingHotel) return;
    this.hotelService.delete(this.deletingHotel.id).subscribe({
      next: () => {
        this.isDeleteModalOpen = false;
        this.deletingHotel = null;
        this.toastr.success(this.localization.instant('TenantHotels:DeleteSuccess'));
        this.loadHotels();
      },
    });
  }

  getStatusBadgeClass(status: number): string {
    switch (status) {
      case HotelStatus.Operating:
        return 'badge-success';
      case HotelStatus.ScheduledSuspended:
      case HotelStatus.Maintenance:
        return 'badge-warning';
      case HotelStatus.EmergencySuspended:
        return 'badge-danger';
      case HotelStatus.Closed:
        return 'badge-secondary';
      default:
        return 'badge-secondary';
    }
  }

  getStatusKey(status: number): string {
    switch (status) {
      case HotelStatus.Operating:
        return 'TenantHotels:StatusOperating';
      case HotelStatus.ScheduledSuspended:
        return 'TenantHotels:StatusScheduledSuspended';
      case HotelStatus.EmergencySuspended:
        return 'TenantHotels:StatusEmergencySuspended';
      case HotelStatus.Maintenance:
        return 'TenantHotels:StatusMaintenance';
      case HotelStatus.Closed:
        return 'TenantHotels:StatusClosed';
      default:
        return 'TenantHotels:StatusOperating';
    }
  }
}
