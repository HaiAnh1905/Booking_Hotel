import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LocalizationPipe, LocalizationService } from '@abp/ng.core';
import { BhToastrService } from '../../../shared/components/toastr/bh-toastr.service';
import { BhModalComponent } from '../../../shared/components/modal/bh-modal.component';
import { BhModalConfig } from '../../../shared/models/ui-models';
import { CreateTenantStaffDto, TenantStaffService, TransferBranchDto, UpdateTenantStaffDto, UserWithAssignmentDto } from '../../services/tenant-staff.service';
import { HotelDto, HotelService } from '../../services/hotel.service';
import { StaffModalComponent } from './staff-modal.component';
import { StaffTransferModalComponent } from './staff-transfer-modal.component';

@Component({
  selector: 'bh-staff-list',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    LocalizationPipe,
    BhModalComponent,
    StaffModalComponent,
    StaffTransferModalComponent,
  ],
  templateUrl: './staff-list.component.html',
  styleUrls: ['./staff-list.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StaffListComponent implements OnInit {
  private cdr = inject(ChangeDetectorRef);
  private toastr = inject(BhToastrService);
  private localization = inject(LocalizationService);
  private staffService = inject(TenantStaffService);
  private hotelService = inject(HotelService);

  users: UserWithAssignmentDto[] = [];
  hotels: HotelDto[] = [];
  isLoading = false;
  searchQuery = '';
  selectedRoleFilter = '';
  selectedHotelFilter = '';

  // Modals
  isStaffModalOpen = false;
  selectedUserForEdit: UserWithAssignmentDto | null = null;

  isTransferModalOpen = false;
  selectedUserForTransfer: UserWithAssignmentDto | null = null;

  isDeleteModalOpen = false;
  deletingUser: UserWithAssignmentDto | null = null;

  deleteModalConfig: BhModalConfig = {
    titleKey: 'Common:Delete',
    confirmTextKey: 'Common:Delete',
    cancelTextKey: 'Common:Cancel',
    confirmVariant: 'danger',
    size: 'sm',
    iconClass: 'fa-solid fa-triangle-exclamation',
  };

  ngOnInit(): void {
    this.loadHotels();
    this.loadStaff();
  }

  loadHotels(): void {
    this.hotelService.getList({ maxResultCount: 1000 }).subscribe({
      next: (res) => {
        this.hotels = res.items || [];
        this.cdr.markForCheck();
      },
    });
  }

  loadStaff(): void {
    this.isLoading = true;
    this.cdr.markForCheck();

    this.staffService.getList({ maxResultCount: 1000 }).subscribe({
      next: (res) => {
        this.users = res.items || [];
        this.isLoading = false;
        this.cdr.markForCheck();
      },
      error: () => {
        this.isLoading = false;
        this.cdr.markForCheck();
      },
    });
  }

  get filteredUsers(): UserWithAssignmentDto[] {
    let list = this.users;
    if (this.searchQuery.trim()) {
      const q = this.searchQuery.toLowerCase().trim();
      list = list.filter(
        (u) =>
          u.userName.toLowerCase().includes(q) ||
          u.email.toLowerCase().includes(q) ||
          (u.name && u.name.toLowerCase().includes(q)) ||
          (u.surname && u.surname.toLowerCase().includes(q)) ||
          (u.phoneNumber && u.phoneNumber.includes(q))
      );
    }
    if (this.selectedRoleFilter) {
      list = list.filter((u) => u.roleNames?.includes(this.selectedRoleFilter));
    }
    if (this.selectedHotelFilter) {
      list = list.filter((u) => u.assignedHotels?.some((a) => a.hotelId === this.selectedHotelFilter));
    }
    return list;
  }

  openCreateModal(): void {
    this.selectedUserForEdit = null;
    this.loadHotels();
    this.isStaffModalOpen = true;
  }

  openEditModal(user: UserWithAssignmentDto): void {
    this.selectedUserForEdit = user;
    this.loadHotels();
    this.isStaffModalOpen = true;
  }

  onSaveCreate(dto: CreateTenantStaffDto): void {
    this.staffService.createTenantStaff(dto).subscribe({
      next: () => {
        this.isStaffModalOpen = false;
        this.toastr.success(this.localization.instant('TenantStaff:CreateSuccess'));
        this.loadStaff();
        this.loadHotels();
      },
      error: (err) => {
        this.toastr.error(err?.error?.error?.message || this.localization.instant('Common:ErrorOccurred'));
      },
    });
  }

  onSaveUpdate(event: { id: string; dto: UpdateTenantStaffDto }): void {
    this.staffService.updateTenantStaff(event.id, event.dto).subscribe({
      next: () => {
        this.isStaffModalOpen = false;
        this.selectedUserForEdit = null;
        this.toastr.success(this.localization.instant('TenantStaff:UpdateSuccess'));
        this.loadStaff();
        this.loadHotels();
      },
      error: (err) => {
        this.toastr.error(err?.error?.error?.message || this.localization.instant('Common:ErrorOccurred'));
      },
    });
  }

  openTransferModal(user: UserWithAssignmentDto): void {
    this.selectedUserForTransfer = user;
    this.isTransferModalOpen = true;
  }

  onTransfer(dto: TransferBranchDto): void {
    if (!this.selectedUserForTransfer) return;
    this.staffService.transferBranch(this.selectedUserForTransfer.id, dto).subscribe({
      next: () => {
        this.isTransferModalOpen = false;
        this.selectedUserForTransfer = null;
        this.toastr.success(this.localization.instant('TenantStaff:TransferSuccess'));
        this.loadStaff();
        this.loadHotels();
      },
    });
  }

  unlockBruteForce(user: UserWithAssignmentDto): void {
    this.staffService.unlockBruteForce(user.id).subscribe({
      next: () => {
        this.toastr.success(this.localization.instant('TenantStaff:UnlockSuccess'));
        this.loadStaff();
      },
    });
  }

  toggleActive(user: UserWithAssignmentDto): void {
    this.staffService.toggleStatus(user.id).subscribe({
      next: () => {
        this.loadStaff();
      },
    });
  }

  openDeleteModal(user: UserWithAssignmentDto): void {
    this.deletingUser = user;
    this.isDeleteModalOpen = true;
  }

  confirmDelete(): void {
    if (!this.deletingUser) return;
    this.staffService.delete(this.deletingUser.id).subscribe({
      next: () => {
        this.isDeleteModalOpen = false;
        this.deletingUser = null;
        this.toastr.success(this.localization.instant('Common:DeleteSuccess'));
        this.loadStaff();
      },
    });
  }

  isDualRole(user: UserWithAssignmentDto): boolean {
    return (
      (user.roleNames?.includes('HotelManager') && user.roleNames?.includes('Receptionist')) ?? false
    );
  }
}
