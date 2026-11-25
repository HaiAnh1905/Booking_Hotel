import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LocalizationPipe, LocalizationService } from '@abp/ng.core';
import { OAuthStorage } from 'angular-oauth2-oidc';
import { BhDataTableComponent } from '../../shared/components/data-table/bh-data-table.component';
import { BhModalComponent } from '../../shared/components/modal/bh-modal.component';
import { BhStatusBadgeComponent } from '../../shared/components/status-badge/bh-status-badge.component';
import { BhToastrService } from '../../shared/components/toastr/bh-toastr.service';
import { BhDataTableColumn, BhModalConfig, BhPageChangeEvent, BhPaginationState } from '../../shared/models/ui-models';
import {
  IdentityUserManagementService,
  UserWithAssignmentDto,
  CreateUserWithAssignmentDto,
  UpdateUserWithAssignmentDto
} from '../services/identity-user-management.service';
import { HotelOption, UserModalComponent } from './user-modal.component';
import { HotelService } from '../../admin/services/hotel.service';

function parseJwt(token: string): any {
  try {
    const base64Url = token.split('.')[1];
    const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
    const jsonPayload = decodeURIComponent(
      atob(base64)
        .split('')
        .map(c => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2))
        .join('')
    );
    return JSON.parse(jsonPayload);
  } catch {
    return null;
  }
}

@Component({
  selector: 'bh-user-list',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    LocalizationPipe,
    BhDataTableComponent,
    BhModalComponent,
    UserModalComponent
  ],
  templateUrl: './user-list.component.html',
  styleUrls: ['./user-list.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class UserListComponent implements OnInit {
  private identityService = inject(IdentityUserManagementService);
  private hotelService = inject(HotelService);
  private toastr = inject(BhToastrService);
  private oAuthStorage = inject(OAuthStorage);
  private localization = inject(LocalizationService);
  private cdr = inject(ChangeDetectorRef);

  users: UserWithAssignmentDto[] = [];
  isLoading = false;
  filterText = '';
  selectedRole = '';
  selectedStatus = '';

  // Phân quyền theo vai trò
  currentUserRoles: string[] = [];
  isHotelManager = false;
  isTenantAdmin = false;
  isSystemAdmin = false;

  availableRoles: string[] = [];
  availableHotels: HotelOption[] = [];

  // Phân trang
  pagination: BhPaginationState = {
    currentPage: 1,
    pageSize: 10,
    totalCount: 0,
    sortField: 'creationTime',
    sortDirection: 'desc'
  };

  // Cấu hình cột bảng
  columns: BhDataTableColumn<UserWithAssignmentDto>[] = [
    { field: 'userName', headerKey: 'Identity:UserName', sortable: true, width: '180px' },
    { field: 'name', headerKey: 'Identity:FullName', width: '180px' },
    { field: 'email', headerKey: 'Identity:Email', sortable: true, width: '220px' },
    { field: 'roles', headerKey: 'Identity:Roles', width: '160px' },
    { field: 'assignedHotels', headerKey: 'Identity:AssignedHotels', width: '200px' },
    { field: 'status', headerKey: 'Identity:Status', align: 'center', width: '130px' },
    { field: 'actions', headerKey: 'Common:Actions', align: 'right', width: '180px' }
  ];

  // User Modal State
  isUserModalOpen = false;
  selectedUser: UserWithAssignmentDto | null = null;
  isSavingUser = false;

  // Unlock Modal State
  isUnlockModalOpen = false;
  unlockTargetUser: UserWithAssignmentDto | null = null;
  unlockReason = '';
  isUnlocking = false;
  unlockModalConfig: BhModalConfig = {
    titleKey: 'Identity:UnlockConfirmTitle',
    confirmTextKey: 'Identity:UnlockUser',
    cancelTextKey: 'Common:Cancel',
    confirmVariant: 'warning',
    size: 'md',
    iconClass: 'fa-solid fa-key'
  };

  // Delete Modal State
  isDeleteModalOpen = false;
  deleteTargetUser: UserWithAssignmentDto | null = null;
  isDeleting = false;
  deleteModalConfig: BhModalConfig = {
    titleKey: 'Identity:DeleteUser',
    confirmTextKey: 'Common:Delete',
    cancelTextKey: 'Common:Cancel',
    confirmVariant: 'danger',
    size: 'sm',
    iconClass: 'fa-solid fa-triangle-exclamation'
  };

  ngOnInit(): void {
    this.inspectCurrentUserRoles();
    this.loadAssignableRoles();
    this.loadAvailableHotels();
    this.loadUsers();
  }

  loadAvailableHotels(): void {
    // Host Admin (SystemAdmin hoặc không thuộc Tenant) không gán cơ sở chi nhánh
    if (this.isSystemAdmin || !this.currentUserRoles.includes('TenantAdmin')) {
      this.availableHotels = [];
      return;
    }

    this.hotelService.getList({ maxResultCount: 100 }).subscribe({
      next: (res) => {
        this.availableHotels = (res.items || []).map(h => ({
          id: h.id,
          name: h.name,
          code: h.code,
          managerName: h.managerName,
          managerUserId: h.managerUserId
        }));
        this.cdr.markForCheck();
      },
      error: () => {}
    });
  }

  private inspectCurrentUserRoles(): void {
    const storageToken = this.oAuthStorage.getItem('access_token');
    const localToken = typeof localStorage !== 'undefined' ? localStorage.getItem('access_token') : null;
    const token = storageToken || localToken;

    if (token) {
      const payload = parseJwt(token);
      const roleClaim = payload?.role || payload?.roles || [];
      this.currentUserRoles = Array.isArray(roleClaim) ? roleClaim : [roleClaim];

      this.isHotelManager = this.currentUserRoles.includes('HotelManager');
      this.isTenantAdmin = this.currentUserRoles.includes('TenantAdmin');
      this.isSystemAdmin = this.currentUserRoles.includes('SystemAdmin');
    }
  }

  loadAssignableRoles(): void {
    this.identityService.getAssignableRoles().subscribe({
      next: (roles) => {
        this.availableRoles = roles;
        this.cdr.markForCheck();
      },
      error: () => {}
    });
  }

  loadUsers(): void {
    this.isLoading = true;
    this.cdr.markForCheck();

    const skipCount = (this.pagination.currentPage - 1) * this.pagination.pageSize;

    let isActive: boolean | undefined = undefined;
    let isLockedOut: boolean | undefined = undefined;

    if (this.selectedStatus === 'active') {
      isActive = true;
      isLockedOut = false;
    } else if (this.selectedStatus === 'locked') {
      isLockedOut = true;
    } else if (this.selectedStatus === 'inactive') {
      isActive = false;
    }

    this.identityService.getList({
      filter: this.filterText || undefined,
      roleName: this.selectedRole || undefined,
      isActive,
      isLockedOut,
      skipCount,
      maxResultCount: this.pagination.pageSize
    }).subscribe({
      next: (res) => {
        this.users = res.items || [];
        this.pagination.totalCount = res.totalCount ?? 0;
        this.isLoading = false;
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.isLoading = false;
        this.toastr.error(err?.error?.error?.message || this.localization.instant('Identity:LoadUsersFailed'));
        this.cdr.markForCheck();
      }
    });
  }

  onSearch(): void {
    this.pagination.currentPage = 1;
    this.loadUsers();
  }

  onPageChange(event: BhPageChangeEvent): void {
    this.pagination.currentPage = event.page;
    this.pagination.pageSize = event.pageSize;
    this.loadUsers();
  }

  // Create & Edit Actions
  openCreateModal(): void {
    this.loadAvailableHotels();
    this.selectedUser = null;
    this.isUserModalOpen = true;
    this.cdr.markForCheck();
  }

  openEditModal(user: UserWithAssignmentDto): void {
    this.loadAvailableHotels();
    // Tải thông tin người dùng mới nhất từ server để đảm bảo ConcurrencyStamp luôn khớp với database
    this.identityService.get(user.id).subscribe({
      next: (freshUser) => {
        this.selectedUser = freshUser;
        this.isUserModalOpen = true;
        this.cdr.markForCheck();
      },
      error: () => {
        this.selectedUser = user;
        this.isUserModalOpen = true;
        this.cdr.markForCheck();
      }
    });
  }

  onUserModalCancel(): void {
    this.isUserModalOpen = false;
    this.selectedUser = null;
    this.cdr.markForCheck();
  }

  onUserModalSave(dto: CreateUserWithAssignmentDto | UpdateUserWithAssignmentDto): void {
    this.isSavingUser = true;
    this.cdr.markForCheck();

    if (this.selectedUser) {
      this.identityService.update(this.selectedUser.id, dto as UpdateUserWithAssignmentDto).subscribe({
        next: () => {
          this.isSavingUser = false;
          this.isUserModalOpen = false;
          this.selectedUser = null;
          this.toastr.success(this.localization.instant('Identity:UserUpdateSuccess'));
          this.loadUsers();
          this.loadAvailableHotels();
        },
        error: (err) => {
          this.isSavingUser = false;
          this.toastr.error(err?.error?.error?.message || this.localization.instant('Identity:UserUpdateFailed'));
          this.cdr.markForCheck();
        }
      });
    } else {
      this.identityService.create(dto as CreateUserWithAssignmentDto).subscribe({
        next: () => {
          this.isSavingUser = false;
          this.isUserModalOpen = false;
          this.toastr.success(this.localization.instant('Identity:UserCreateSuccess'));
          this.loadUsers();
          this.loadAvailableHotels();
        },
        error: (err) => {
          this.isSavingUser = false;
          this.toastr.error(err?.error?.error?.message || this.localization.instant('Identity:UserCreateFailed'));
          this.cdr.markForCheck();
        }
      });
    }
  }

  // Unlock Actions
  openUnlockModal(user: UserWithAssignmentDto): void {
    this.unlockTargetUser = user;
    this.unlockReason = '';
    this.isUnlockModalOpen = true;
    this.cdr.markForCheck();
  }

  confirmUnlock(): void {
    if (!this.unlockTargetUser || this.isUnlocking) return;

    this.isUnlocking = true;
    this.cdr.markForCheck();

    this.identityService.unlockUser(this.unlockTargetUser.id, { reason: this.unlockReason }).subscribe({
      next: (res) => {
        this.isUnlocking = false;
        this.isUnlockModalOpen = false;
        this.unlockTargetUser = null;
        this.toastr.success(this.localization.instant('Identity:UnlockSuccess'));
        this.loadUsers();
      },
      error: (err) => {
        this.isUnlocking = false;
        this.toastr.error(err?.error?.error?.message || this.localization.instant('Identity:UnlockFailed'));
        this.cdr.markForCheck();
      }
    });
  }

  cancelUnlock(): void {
    this.isUnlockModalOpen = false;
    this.unlockTargetUser = null;
    this.cdr.markForCheck();
  }

  isAdminUser(user: UserWithAssignmentDto): boolean {
    if (!user) return false;
    if (user.userName?.toLowerCase() === 'admin') return true;
    const roles = user.roleNames || [];
    return roles.some(r => ['admin', 'TenantAdmin', 'SystemAdmin'].includes(r));
  }

  // Delete Actions
  openDeleteModal(user: UserWithAssignmentDto): void {
    if (this.isAdminUser(user)) {
      this.toastr.warning(this.localization.instant('BookingHotel:CannotDeleteAdmin') || 'Không thể xóa tài khoản Quản trị viên hệ thống.');
      return;
    }
    this.deleteTargetUser = user;
    this.isDeleteModalOpen = true;
    this.cdr.markForCheck();
  }

  confirmDelete(): void {
    if (!this.deleteTargetUser || this.isDeleting) return;

    this.isDeleting = true;
    this.cdr.markForCheck();

    this.identityService.delete(this.deleteTargetUser.id).subscribe({
      next: () => {
        this.isDeleting = false;
        this.isDeleteModalOpen = false;
        this.deleteTargetUser = null;
        this.toastr.success(this.localization.instant('Identity:DeleteSuccess'));
        this.loadUsers();
      },
      error: (err) => {
        this.isDeleting = false;
        this.toastr.error(err?.error?.error?.message || this.localization.instant('Identity:DeleteFailed'));
        this.cdr.markForCheck();
      }
    });
  }

  cancelDelete(): void {
    this.isDeleteModalOpen = false;
    this.deleteTargetUser = null;
    this.cdr.markForCheck();
  }

  // Toggle Active/Inactive
  toggleUserStatus(user: UserWithAssignmentDto): void {
    if (this.isAdminUser(user)) {
      this.toastr.warning(this.localization.instant('BookingHotel:CannotDeactivateAdmin') || 'Không thể tạm ngừng tài khoản Quản trị viên hệ thống.');
      return;
    }
    this.identityService.toggleStatus(user.id).subscribe({
      next: () => {
        this.toastr.success(this.localization.instant('Identity:ToggleStatusSuccess'));
        this.loadUsers();
      },
      error: (err) => {
        this.toastr.error(err?.error?.error?.message || this.localization.instant('Identity:ToggleStatusFailed'));
      }
    });
  }
}
