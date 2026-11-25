import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { LocalizationPipe, LocalizationService } from '@abp/ng.core';
import { TenantService, TenantDto, TenantCreateDto, TenantUpdateDto } from '@abp/ng.tenant-management/proxy';
import { BhDataTableComponent } from '../../../shared/components/data-table/bh-data-table.component';
import { BhModalComponent } from '../../../shared/components/modal/bh-modal.component';
import { BhToastrService } from '../../../shared/components/toastr/bh-toastr.service';
import { BhDataTableColumn, BhModalConfig, BhPageChangeEvent, BhPaginationState } from '../../../shared/models/ui-models';
import { checkPasswordCriteria, PasswordCriteriaState, passwordComplexityValidator } from '../../../auth/validators/password-complexity.validator';

@Component({
  selector: 'bh-tenant-list',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    LocalizationPipe,
    BhDataTableComponent,
    BhModalComponent
  ],
  templateUrl: './tenant-list.component.html',
  styleUrls: ['./tenant-list.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TenantListComponent implements OnInit {
  private tenantService = inject(TenantService);
  private toastr = inject(BhToastrService);
  private cdr = inject(ChangeDetectorRef);
  private fb = inject(FormBuilder);
  private localization = inject(LocalizationService);

  // Danh sách dữ liệu & Phân trang
  tenants: TenantDto[] = [];
  isLoading = false;
  totalCount = 0;
  searchFilter = '';

  pagination: BhPaginationState = {
    currentPage: 1,
    pageSize: 10,
    totalCount: 0
  };

  columns: BhDataTableColumn<TenantDto>[] = [
    { field: 'name', headerKey: 'Tenant:TenantName', sortable: false },
    { field: 'id', headerKey: 'Common:Id', sortable: false },
    { field: 'database', headerKey: 'Tenant:ConnectionStrings', sortable: false },
    { field: 'actions', headerKey: 'Common:Actions', sortable: false, width: '160px', align: 'right' }
  ];

  // Create Modal State
  isCreateModalOpen = false;
  isCreating = false;
  createForm!: FormGroup;
  createModalConfig: BhModalConfig = {
    titleKey: 'Tenant:CreateModalTitle',
    confirmTextKey: 'Tenant:SaveNewTenant',
    cancelTextKey: 'Common:Cancel',
    confirmVariant: 'primary',
    size: 'lg',
    iconClass: 'fa-solid fa-building-circle-check'
  };

  // Edit Modal State
  isEditModalOpen = false;
  isEditing = false;
  editForm!: FormGroup;
  selectedTenant: TenantDto | null = null;
  editModalConfig: BhModalConfig = {
    titleKey: 'Tenant:EditModalTitle',
    confirmTextKey: 'Common:Save',
    cancelTextKey: 'Common:Cancel',
    confirmVariant: 'primary',
    size: 'md',
    iconClass: 'fa-solid fa-pen-to-square'
  };

  // Connection String Modal State
  isConnModalOpen = false;
  isSavingConn = false;
  useDedicatedDb = false;
  dedicatedConnStr = '';
  connModalConfig: BhModalConfig = {
    titleKey: 'Tenant:ConnModalTitle',
    confirmTextKey: 'Common:Save',
    cancelTextKey: 'Common:Cancel',
    confirmVariant: 'primary',
    size: 'lg',
    iconClass: 'fa-solid fa-database'
  };

  // Delete Modal State
  isDeleteModalOpen = false;
  isDeleting = false;
  deleteTargetTenant: TenantDto | null = null;
  deleteModalConfig: BhModalConfig = {
    titleKey: 'Tenant:DeleteModalTitle',
    confirmTextKey: 'Common:Delete',
    cancelTextKey: 'Common:Cancel',
    confirmVariant: 'danger',
    size: 'sm',
    iconClass: 'fa-solid fa-triangle-exclamation'
  };

  get passwordCriteria(): PasswordCriteriaState {
    return checkPasswordCriteria(this.createForm?.get('adminPassword')?.value || '');
  }

  ngOnInit(): void {
    this.initForms();
    this.loadTenants();
  }

  private initForms(): void {
    // Hiến pháp IX: Tuyệt đối không điền sẵn thông tin xác thực (Zero Credential Pre-filling)
    this.createForm = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(64)]],
      adminEmailAddress: ['', [Validators.required, Validators.email]],
      adminPassword: ['', [Validators.required, Validators.minLength(8), passwordComplexityValidator]]
    });

    this.editForm = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(64)]]
    });
  }

  loadTenants(): void {
    this.isLoading = true;
    this.cdr.markForCheck();

    const skipCount = (this.pagination.currentPage - 1) * this.pagination.pageSize;

    this.tenantService.getList({
      skipCount,
      maxResultCount: this.pagination.pageSize,
      filter: this.searchFilter.trim() || undefined
    }).subscribe({
      next: (res) => {
        this.tenants = res.items || [];
        this.totalCount = res.totalCount || 0;
        this.pagination.totalCount = this.totalCount;
        this.isLoading = false;
        this.cdr.markForCheck();
      },
      error: () => {
        this.isLoading = false;
        this.toastr.error(this.localization.instant('Tenant:LoadError'));
        this.cdr.markForCheck();
      }
    });
  }

  onSearch(): void {
    this.pagination.currentPage = 1;
    this.loadTenants();
  }

  onPageChange(event: BhPageChangeEvent): void {
    this.pagination.currentPage = event.page;
    this.pagination.pageSize = event.pageSize;
    this.loadTenants();
  }

  // --- CREATE TENANT ---
  openCreateModal(): void {
    this.createForm.reset({
      name: '',
      adminEmailAddress: '',
      adminPassword: ''
    });
    this.isCreateModalOpen = true;
    this.cdr.markForCheck();

    setTimeout(() => {
      const emailInput = document.getElementById('tenantAdminEmail') as HTMLInputElement;
      const passInput = document.getElementById('tenantAdminPassword') as HTMLInputElement;
      if (emailInput && !this.createForm.get('adminEmailAddress')?.dirty) {
        emailInput.value = '';
      }
      if (passInput && !this.createForm.get('adminPassword')?.dirty) {
        passInput.value = '';
      }
    }, 50);
  }

  cancelCreate(): void {
    this.isCreateModalOpen = false;
    this.cdr.markForCheck();
  }

  confirmCreate(): void {
    if (this.createForm.invalid || this.isCreating) {
      this.createForm.markAllAsTouched();
      return;
    }

    this.isCreating = true;
    this.cdr.markForCheck();

    const input: TenantCreateDto = {
      name: this.createForm.value.name.trim(),
      adminEmailAddress: this.createForm.value.adminEmailAddress.trim(),
      adminPassword: this.createForm.value.adminPassword,
      extraProperties: {}
    };

    this.tenantService.create(input).subscribe({
      next: () => {
        this.isCreating = false;
        this.isCreateModalOpen = false;
        this.toastr.success(this.localization.instant('Tenant:CreateSuccess'));
        this.loadTenants();
      },
      error: (err) => {
        this.isCreating = false;
        this.toastr.error(err?.error?.error?.message || this.localization.instant('Tenant:CreateError'));
        this.cdr.markForCheck();
      }
    });
  }

  // --- EDIT TENANT ---
  openEditModal(tenant: TenantDto): void {
    this.selectedTenant = tenant;
    this.editForm.patchValue({
      name: tenant.name || ''
    });
    this.isEditModalOpen = true;
    this.cdr.markForCheck();
  }

  cancelEdit(): void {
    this.isEditModalOpen = false;
    this.selectedTenant = null;
    this.cdr.markForCheck();
  }

  confirmEdit(): void {
    if (this.editForm.invalid || this.isEditing || !this.selectedTenant?.id) {
      this.editForm.markAllAsTouched();
      return;
    }

    this.isEditing = true;
    this.cdr.markForCheck();

    const input: TenantUpdateDto = {
      name: this.editForm.value.name.trim(),
      concurrencyStamp: this.selectedTenant.concurrencyStamp,
      extraProperties: {}
    };

    this.tenantService.update(this.selectedTenant.id, input).subscribe({
      next: () => {
        this.isEditing = false;
        this.isEditModalOpen = false;
        this.selectedTenant = null;
        this.toastr.success(this.localization.instant('Tenant:UpdateSuccess'));
        this.loadTenants();
      },
      error: (err) => {
        this.isEditing = false;
        this.toastr.error(err?.error?.error?.message || this.localization.instant('Tenant:UpdateError'));
        this.cdr.markForCheck();
      }
    });
  }

  // --- CONNECTION STRING ---
  openConnectionStringModal(tenant: TenantDto): void {
    if (!tenant?.id) return;
    this.selectedTenant = tenant;
    this.isConnModalOpen = true;
    this.useDedicatedDb = false;
    this.dedicatedConnStr = '';
    this.cdr.markForCheck();

    this.tenantService.getDefaultConnectionString(tenant.id).subscribe({
      next: (connStr) => {
        if (connStr && connStr.trim().length > 0) {
          this.useDedicatedDb = true;
          this.dedicatedConnStr = connStr;
        } else {
          this.useDedicatedDb = false;
          this.dedicatedConnStr = '';
        }
        this.cdr.markForCheck();
      },
      error: () => {
        this.useDedicatedDb = false;
        this.dedicatedConnStr = '';
        this.cdr.markForCheck();
      }
    });
  }

  cancelConnectionString(): void {
    this.isConnModalOpen = false;
    this.selectedTenant = null;
    this.cdr.markForCheck();
  }

  confirmConnectionString(): void {
    if (this.isSavingConn || !this.selectedTenant?.id) return;

    this.isSavingConn = true;
    this.cdr.markForCheck();

    if (this.useDedicatedDb && this.dedicatedConnStr.trim()) {
      this.tenantService.updateDefaultConnectionString(this.selectedTenant.id, this.dedicatedConnStr.trim()).subscribe({
        next: () => {
          this.isSavingConn = false;
          this.isConnModalOpen = false;
          this.selectedTenant = null;
          this.toastr.success(this.localization.instant('Tenant:ConnStringUpdateSuccess'));
          this.loadTenants();
        },
        error: (err) => {
          this.isSavingConn = false;
          this.toastr.error(err?.error?.error?.message || this.localization.instant('Tenant:UpdateError'));
          this.cdr.markForCheck();
        }
      });
    } else {
      this.tenantService.deleteDefaultConnectionString(this.selectedTenant.id).subscribe({
        next: () => {
          this.isSavingConn = false;
          this.isConnModalOpen = false;
          this.selectedTenant = null;
          this.toastr.success(this.localization.instant('Tenant:ConnStringDeleteSuccess'));
          this.loadTenants();
        },
        error: (err) => {
          this.isSavingConn = false;
          this.toastr.error(err?.error?.error?.message || this.localization.instant('Tenant:UpdateError'));
          this.cdr.markForCheck();
        }
      });
    }
  }

  // --- DELETE TENANT ---
  openDeleteModal(tenant: TenantDto): void {
    this.deleteTargetTenant = tenant;
    this.isDeleteModalOpen = true;
    this.cdr.markForCheck();
  }

  cancelDelete(): void {
    this.isDeleteModalOpen = false;
    this.deleteTargetTenant = null;
    this.cdr.markForCheck();
  }

  confirmDelete(): void {
    if (this.isDeleting || !this.deleteTargetTenant?.id) return;

    this.isDeleting = true;
    this.cdr.markForCheck();

    this.tenantService.delete(this.deleteTargetTenant.id).subscribe({
      next: () => {
        this.isDeleting = false;
        this.isDeleteModalOpen = false;
        this.deleteTargetTenant = null;
        this.toastr.success(this.localization.instant('Tenant:DeleteSuccess'));
        this.loadTenants();
      },
      error: (err) => {
        this.isDeleting = false;
        this.toastr.error(err?.error?.error?.message || this.localization.instant('Tenant:DeleteError'));
        this.cdr.markForCheck();
      }
    });
  }
}
