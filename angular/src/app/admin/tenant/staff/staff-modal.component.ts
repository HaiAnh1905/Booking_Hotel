import { ChangeDetectionStrategy, Component, EventEmitter, Input, OnChanges, Output, SimpleChanges, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { LocalizationPipe } from '@abp/ng.core';
import { BhModalComponent } from '../../../shared/components/modal/bh-modal.component';
import { BhModalConfig } from '../../../shared/models/ui-models';
import { BhSearchableSelectComponent, SelectOption } from '../../../shared/components/searchable-select/bh-searchable-select.component';
import { CreateTenantStaffDto, UpdateTenantStaffDto, UserWithAssignmentDto } from '../../services/tenant-staff.service';
import { HotelDto } from '../../services/hotel.service';
import { PasswordCriteriaState, checkPasswordCriteria } from '../../../auth/validators/password-complexity.validator';

@Component({
  selector: 'bh-staff-modal',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, LocalizationPipe, BhModalComponent, BhSearchableSelectComponent],
  template: `
    <bh-modal
      [isOpen]="isOpen"
      [config]="modalConfig"
      (confirm)="onSubmit()"
      (cancel)="onCancel()"
    >
      <form [formGroup]="form" class="staff-form" (ngSubmit)="onSubmit()">
        <div class="row g-3">
          <!-- Account Details -->
          <div class="col-md-6">
            <label class="form-label">{{ 'Identity:UserName' | abpLocalization }} <span class="text-danger">*</span></label>
            <input
              type="text"
              class="form-control"
              [class.is-invalid]="form.get('userName')?.touched && form.get('userName')?.invalid"
              formControlName="userName"
              [placeholder]="'Identity:UserNamePlaceholder' | abpLocalization"
            />
            <div class="text-danger small mt-1" *ngIf="form.get('userName')?.touched && form.get('userName')?.invalid">
              <span *ngIf="form.get('userName')?.errors?.['required']">{{ 'Identity:UserNameRequired' | abpLocalization }}</span>
              <span *ngIf="form.get('userName')?.errors?.['minlength']">{{ 'Identity:UserNameMinLength' | abpLocalization }}</span>
            </div>
          </div>

          <div class="col-md-6">
            <label class="form-label">{{ 'TenantStaff:ColEmail' | abpLocalization }} <span class="text-danger">*</span></label>
            <input
              type="email"
              class="form-control"
              [class.is-invalid]="form.get('email')?.touched && form.get('email')?.invalid"
              formControlName="email"
              placeholder="user@bookinghotel.vn"
            />
            <div class="text-danger small mt-1" *ngIf="form.get('email')?.touched && form.get('email')?.invalid">
              <span *ngIf="form.get('email')?.errors?.['required']">{{ 'Common:FieldRequired' | abpLocalization }}</span>
              <span *ngIf="form.get('email')?.errors?.['email']">{{ 'Auth:EmailInvalid' | abpLocalization }}</span>
            </div>
          </div>

          <div class="col-md-6">
            <label class="form-label">{{ 'TenantStaff:ColFullName' | abpLocalization }} <span class="text-danger">*</span></label>
            <input
              type="text"
              class="form-control"
              formControlName="fullName"
              placeholder="vd: Nguyễn Văn An"
            />
            <div class="text-danger small mt-1" *ngIf="form.get('fullName')?.touched && form.get('fullName')?.invalid">
              {{ 'Common:FieldRequired' | abpLocalization }}
            </div>
          </div>

          <div class="col-md-6">
            <label class="form-label">{{ 'TenantStaff:ColPhone' | abpLocalization }}</label>
            <input type="text" class="form-control" formControlName="phoneNumber" [placeholder]="'TenantStaff:ColPhone' | abpLocalization" />
          </div>

          <div class="col-md-12">
            <label class="form-label">
              {{ 'Auth:Password' | abpLocalization }}
              <span class="text-danger" *ngIf="!user">*</span>
            </label>
            <input
              type="password"
              class="form-control"
              [class.is-invalid]="form.get('password')?.touched && form.get('password')?.invalid"
              formControlName="password"
              autocomplete="new-password"
              [placeholder]="user ? ('TenantStaff:PasswordLeaveBlank' | abpLocalization) : '••••••••'"
            />
            <div class="text-danger small mt-1" *ngIf="form.get('password')?.touched && form.get('password')?.invalid && (!form.get('password')?.value && !user)">
              {{ 'Common:FieldRequired' | abpLocalization }}
            </div>

            <!-- Password Criteria Checklist -->
            <div class="password-checklist" *ngIf="!user || form.get('password')?.value">
              <div class="checklist-item" [class.valid]="passwordCriteria.hasMinLength">
                <span class="check-icon">{{ passwordCriteria.hasMinLength ? '✓' : '•' }}</span>
                <span>{{ 'Auth:PasswordRuleMinLength' | abpLocalization }}</span>
              </div>
              <div class="checklist-item" [class.valid]="passwordCriteria.hasUpperCase">
                <span class="check-icon">{{ passwordCriteria.hasUpperCase ? '✓' : '•' }}</span>
                <span>{{ 'Auth:PasswordRuleUpperCase' | abpLocalization }}</span>
              </div>
              <div class="checklist-item" [class.valid]="passwordCriteria.hasLowerCase">
                <span class="check-icon">{{ passwordCriteria.hasLowerCase ? '✓' : '•' }}</span>
                <span>{{ 'Auth:PasswordRuleLowerCase' | abpLocalization }}</span>
              </div>
              <div class="checklist-item" [class.valid]="passwordCriteria.hasNumeric">
                <span class="check-icon">{{ passwordCriteria.hasNumeric ? '✓' : '•' }}</span>
                <span>{{ 'Auth:PasswordRuleDigit' | abpLocalization }}</span>
              </div>
              <div class="checklist-item" [class.valid]="passwordCriteria.hasSpecial">
                <span class="check-icon">{{ passwordCriteria.hasSpecial ? '✓' : '•' }}</span>
                <span>{{ 'Auth:PasswordRuleSpecial' | abpLocalization }}</span>
              </div>
            </div>
          </div>

          <!-- Roles selection -->
          <div class="col-md-12">
            <label class="form-label mb-2">{{ 'TenantStaff:ColRoles' | abpLocalization }} <span class="text-danger">*</span></label>
            <div class="row g-3">
              <div class="col-md-6">
                <label
                  class="role-card d-flex align-items-center p-3 rounded-3 cursor-pointer h-100"
                  [class.active-manager]="form.get('isHotelManager')?.value"
                  for="roleHotelManager"
                >
                  <input
                    class="form-check-input me-3 mt-0 flex-shrink-0"
                    type="checkbox"
                    id="roleHotelManager"
                    formControlName="isHotelManager"
                  />
                  <div class="role-info">
                    <div class="d-flex align-items-center gap-2">
                      <i class="fa-solid fa-user-tie text-primary fs-5"></i>
                      <span class="role-name fw-medium text-dark">{{ 'TenantStaff:RoleHotelManager' | abpLocalization }}</span>
                    </div>
                    <small class="text-muted d-block mt-1">{{ 'TenantStaff:RoleHotelManagerDesc' | abpLocalization }}</small>
                  </div>
                </label>
              </div>

              <div class="col-md-6">
                <label
                  class="role-card d-flex align-items-center p-3 rounded-3 cursor-pointer h-100"
                  [class.active-receptionist]="form.get('isReceptionist')?.value"
                  for="roleReceptionist"
                >
                  <input
                    class="form-check-input me-3 mt-0 flex-shrink-0"
                    type="checkbox"
                    id="roleReceptionist"
                    formControlName="isReceptionist"
                  />
                  <div class="role-info">
                    <div class="d-flex align-items-center gap-2">
                      <i class="fa-solid fa-bell-concierge text-success fs-5"></i>
                      <span class="role-name fw-medium text-dark">{{ 'TenantStaff:RoleReceptionist' | abpLocalization }}</span>
                    </div>
                    <small class="text-muted d-block mt-1">{{ 'TenantStaff:RoleReceptionistDesc' | abpLocalization }}</small>
                  </div>
                </label>
              </div>
            </div>

            <div class="text-danger small mt-2" *ngIf="noRoleSelected">
              {{ 'TenantStaff:ErrMustSelectAtLeastOneRole' | abpLocalization }}
            </div>
          </div>

          <!-- Manager Branches selection -->
          <div class="col-md-12" *ngIf="form.get('isHotelManager')?.value">
            <label class="form-label d-block mb-2">
              <i class="fa-solid fa-hotel text-primary me-1"></i>
              {{ 'TenantStaff:ManagerBranchesLabel' | abpLocalization }}:
            </label>
            <div class="branches-container p-3 rounded-3 border bg-white">
              <div class="row g-2">
                <div class="col-md-6" *ngFor="let h of hotels">
                  <label
                    class="branch-item p-2 rounded border cursor-pointer d-block"
                    [for]="'m_hotel_' + h.id"
                    [class.border-primary]="isManagerHotelChecked(h.id) && !hasOtherManager(h)"
                    [class.border-warning]="isManagerHotelChecked(h.id) && hasOtherManager(h)"
                    [class.bg-light-blue]="isManagerHotelChecked(h.id) && !hasOtherManager(h)"
                    [class.bg-light-warning]="isManagerHotelChecked(h.id) && hasOtherManager(h)"
                  >
                    <div class="d-flex align-items-center">
                      <input
                        class="form-check-input me-2 mt-0 flex-shrink-0 cursor-pointer"
                        type="checkbox"
                        [id]="'m_hotel_' + h.id"
                        [checked]="isManagerHotelChecked(h.id)"
                        (change)="toggleManagerHotel(h.id)"
                      />
                      <div class="branch-label small text-truncate flex-grow-1">
                        <div class="text-truncate">
                          <strong class="text-dark">{{ h.code }}</strong> - {{ h.name }}
                        </div>
                        <div *ngIf="h.managerName && !hasOtherManager(h)" class="text-muted text-truncate" style="font-size: 0.75rem;">
                          <i class="fa-solid fa-user-tie text-secondary me-1"></i>
                          <span>{{ 'TenantStaff:CurrentManager' | abpLocalization }}: </span>
                          <strong class="text-primary">{{ h.managerName }}</strong>
                        </div>
                      </div>
                    </div>

                    <!-- Cảnh báo khi tick chọn cơ sở đang có Quản lý khác -->
                    <div
                      *ngIf="isManagerHotelChecked(h.id) && hasOtherManager(h)"
                      class="mt-1 d-flex align-items-center text-warning-emphasis small gap-1 ms-4"
                      style="font-size: 0.75rem;"
                    >
                      <i class="fa-solid fa-triangle-exclamation text-warning flex-shrink-0"></i>
                      <span>Đang do <strong>{{ h.managerName }}</strong> quản lý. Sẽ chuyển quyền khi lưu.</span>
                    </div>

                    <!-- Hiển thị thông tin quản lý khi chưa tick chọn -->
                    <div
                      *ngIf="!isManagerHotelChecked(h.id) && hasOtherManager(h)"
                      class="text-muted text-truncate mt-1 ms-4"
                      style="font-size: 0.75rem;"
                    >
                      <i class="fa-solid fa-user-tie text-secondary me-1"></i>
                      <span>{{ 'TenantStaff:CurrentManager' | abpLocalization }}: </span>
                      <strong>{{ h.managerName }}</strong>
                    </div>
                  </label>
                </div>
              </div>
            </div>
          </div>

          <!-- Receptionist Primary Branch selection -->
          <div class="col-md-12" *ngIf="form.get('isReceptionist')?.value">
            <label class="form-label d-block mb-2">
              <i class="fa-solid fa-bell-concierge text-success me-1"></i>
              {{ 'TenantStaff:ReceptionistPrimaryLabel' | abpLocalization }}: <span class="text-danger">*</span>
            </label>
            <bh-searchable-select
              formControlName="primaryReceptionistHotelId"
              [options]="hotelOptions"
              [placeholder]="'TenantStaff:SelectHotel' | abpLocalization"
              [clearable]="false"
            ></bh-searchable-select>
          </div>
        </div>
      </form>
    </bh-modal>

    <!-- Modal xác nhận thay đổi Quản lý cơ sở khi bấm Lưu thay đổi -->
    <div class="confirm-manager-modal-wrapper" *ngIf="isConfirmChangeManagerOpen">
      <bh-modal
        [isOpen]="isConfirmChangeManagerOpen"
        [config]="confirmChangeManagerConfig"
        (confirm)="confirmChangeManagerAndSave()"
        (cancel)="isConfirmChangeManagerOpen = false"
      >
        <div class="text-center py-2">
          <div class="mb-3">
            <i class="fa-solid fa-triangle-exclamation text-warning" style="font-size: 2.8rem;"></i>
          </div>
          <h5 class="fw-bold mb-2 text-dark">
            {{ 'TenantStaff:ConfirmChangeManagerTitle' | abpLocalization }}
          </h5>

          <!-- 1 cơ sở -->
          <p class="text-muted mb-3" *ngIf="conflictingHotels.length === 1">
            Cơ sở <strong class="text-dark">{{ conflictingHotels[0].code }} - {{ conflictingHotels[0].name }}</strong> hiện đang do Quản lý <strong class="text-danger">{{ conflictingHotels[0].managerName || 'một nhân sự khác' }}</strong> phụ trách.
          </p>

          <!-- Nhiều cơ sở -->
          <div class="text-start mb-3" *ngIf="conflictingHotels.length > 1">
            <p class="text-muted mb-2 small">Các cơ sở sau hiện đang có Quản lý phụ trách:</p>
            <ul class="list-group list-group-flush border rounded-3 p-2 bg-light mb-2 small">
              <li class="list-group-item bg-transparent d-flex justify-content-between align-items-center py-1 px-2" *ngFor="let ch of conflictingHotels">
                <span class="fw-medium text-dark">{{ ch.code }} - {{ ch.name }}</span>
                <span class="badge bg-danger-subtle text-danger border border-danger-subtle">
                  <i class="fa-solid fa-user-tie me-1"></i>{{ ch.managerName }}
                </span>
              </li>
            </ul>
          </div>

          <div class="alert alert-warning text-start small mb-0">
            <div class="d-flex align-items-start">
              <i class="fa-solid fa-circle-exclamation text-warning me-2 mt-1 fs-6"></i>
              <div>
                <strong>Lưu ý:</strong> Mỗi cơ sở khách sạn chỉ được phép có <strong>duy nhất 1 Quản lý</strong> hoạt động. Khi bạn xác nhận, quyền quản lý tại cơ sở này sẽ được chuyển giao cho nhân sự này và Quản lý hiện tại sẽ bị hủy quyền phân công tại cơ sở.
              </div>
            </div>
          </div>
        </div>
      </bh-modal>
    </div>
  `,
  styles: [`
    .role-card {
      background: #ffffff;
      border: 1.5px solid #e2e8f0;
      transition: all 0.2s ease-in-out;
      cursor: pointer;
      user-select: none;
    }
    .role-card:hover {
      border-color: #cbd5e1;
      background-color: #f8fafc;
    }
    .role-card.active-manager {
      background: #eff6ff !important;
      border-color: #3b82f6 !important;
      box-shadow: 0 1px 3px rgba(59, 130, 246, 0.15);
    }
    .role-card.active-receptionist {
      background: #ecfdf5 !important;
      border-color: #10b981 !important;
      box-shadow: 0 1px 3px rgba(16, 185, 129, 0.15);
    }
    .cursor-pointer {
      cursor: pointer;
    }
    .branches-container {
      max-height: 180px;
      overflow-y: auto;
      border-color: #e2e8f0 !important;
    }
    .branch-item {
      background: #f8fafc;
      border: 1px solid #e2e8f0;
      cursor: pointer;
      transition: all 0.15s ease;
      margin-bottom: 0;
    }
    .branch-item:hover {
      background: #eff6ff;
      border-color: #93c5fd;
    }
    .password-checklist {
      margin-top: 0.6rem;
      padding: 0.75rem 1rem;
      background-color: #fafbfc;
      border: 1px solid #e2e8f0;
      border-radius: 8px;
      font-size: 0.85rem;
    }
    .checklist-item {
      display: flex;
      align-items: center;
      color: #64748b;
      margin-bottom: 0.35rem;
      transition: color 0.15s ease;
    }
    .checklist-item:last-child {
      margin-bottom: 0;
    }
    .checklist-item .check-icon {
      display: inline-block;
      width: 18px;
      font-weight: 700;
      margin-right: 0.5rem;
      text-align: center;
    }
    .checklist-item.valid {
      color: #16a34a;
      font-weight: 500;
    }
    .checklist-item.valid .check-icon {
      color: #16a34a;
    }
    .confirm-manager-modal-wrapper ::ng-deep .bh-modal-backdrop {
      z-index: 1070 !important;
    }
    .bg-light-blue {
      background-color: #eff6ff !important;
    }
    .bg-light-warning {
      background-color: #fffbeb !important;
    }
    .border-warning {
      border-color: #f59e0b !important;
    }
    .text-warning-emphasis {
      color: #b45309 !important;
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StaffModalComponent implements OnChanges {
  private fb = inject(FormBuilder);

  @Input() isOpen = false;
  @Input() user: UserWithAssignmentDto | null = null;
  @Input() hotels: HotelDto[] = [];

  get hotelOptions(): SelectOption[] {
    return this.hotels.map((h) => ({
      value: h.id,
      label: `${h.code} - ${h.name}`,
    }));
  }

  @Output() saveCreate = new EventEmitter<CreateTenantStaffDto>();
  @Output() saveUpdate = new EventEmitter<{ id: string; dto: UpdateTenantStaffDto }>();
  @Output() cancel = new EventEmitter<void>();

  selectedManagerHotelIds: string[] = [];

  // Modal xác nhận đổi quản lý khi bấm Lưu thay đổi
  isConfirmChangeManagerOpen = false;
  conflictingHotels: HotelDto[] = [];

  confirmChangeManagerConfig: BhModalConfig = {
    titleKey: 'TenantStaff:ConfirmChangeManagerTitle',
    confirmTextKey: 'TenantStaff:ConfirmChangeManagerBtn',
    cancelTextKey: 'Common:Cancel',
    confirmVariant: 'danger',
    size: 'md',
    iconClass: 'fa-solid fa-triangle-exclamation text-warning',
  };

  form: FormGroup = this.fb.group({
    userName: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(128)]],
    email: ['', [Validators.required, Validators.email]],
    fullName: ['', [Validators.required]],
    phoneNumber: [''],
    password: [''],
    isHotelManager: [false],
    isReceptionist: [false],
    primaryReceptionistHotelId: [''],
  });

  get modalConfig(): BhModalConfig {
    return {
      titleKey: this.user ? 'TenantStaff:EditStaffModalTitle' : 'TenantStaff:CreateStaffModalTitle',
      confirmTextKey: 'Common:Save',
      cancelTextKey: 'Common:Cancel',
      confirmVariant: 'primary',
      size: 'lg',
      iconClass: 'fa-solid fa-user-plus',
    };
  }

  get passwordCriteria(): PasswordCriteriaState {
    const val = this.form.get('password')?.value || '';
    return checkPasswordCriteria(val);
  }

  get isPasswordComplex(): boolean {
    const c = this.passwordCriteria;
    return c.hasMinLength && c.hasUpperCase && c.hasLowerCase && c.hasNumeric && c.hasSpecial;
  }

  get noRoleSelected(): boolean {
    return !this.form.get('isHotelManager')?.value && !this.form.get('isReceptionist')?.value;
  }

  isManagerHotelChecked(hotelId: string): boolean {
    if (!hotelId) return false;
    const lower = hotelId.toLowerCase();
    return this.selectedManagerHotelIds.some((id) => id?.toLowerCase() === lower);
  }

  isCurrentUserManager(h: HotelDto): boolean {
    if (!this.user) {
      return false;
    }
    if (this.user.id && h.managerUserId && h.managerUserId.toLowerCase() === this.user.id.toLowerCase()) {
      return true;
    }
    const currentFullName = `${this.user.surname || ''} ${this.user.name || ''}`.trim();
    if (h.managerName) {
      const mName = h.managerName.trim().toLowerCase();
      const cleanMName = mName.split('(')[0].trim();
      if (currentFullName && (mName === currentFullName.toLowerCase() || cleanMName === currentFullName.toLowerCase())) {
        return true;
      }
      if (this.user.name && (mName === this.user.name.trim().toLowerCase() || cleanMName === this.user.name.trim().toLowerCase())) {
        return true;
      }
      if (this.user.userName && (mName === this.user.userName.trim().toLowerCase() || cleanMName === this.user.userName.trim().toLowerCase())) {
        return true;
      }
    }
    return false;
  }

  hasOtherManager(h: HotelDto): boolean {
    const hasManager = !!(h.managerUserId || (h.managerName && h.managerName.trim().length > 0));
    if (!hasManager) {
      return false;
    }
    return !this.isCurrentUserManager(h);
  }

  toggleManagerHotel(hotelId: string): void {
    if (!hotelId) return;
    const lower = hotelId.toLowerCase();
    if (this.isManagerHotelChecked(hotelId)) {
      this.selectedManagerHotelIds = this.selectedManagerHotelIds.filter((id) => id?.toLowerCase() !== lower);
    } else {
      this.selectedManagerHotelIds = [...this.selectedManagerHotelIds, hotelId];
    }
  }

  confirmChangeManagerAndSave(): void {
    this.isConfirmChangeManagerOpen = false;
    this.executeSave();
  }

  onSubmit(): void {
    if (this.noRoleSelected) {
      return;
    }

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const val = this.form.getRawValue();
    if (val.password && !this.isPasswordComplex) {
      this.form.get('password')?.setErrors({ invalid: true });
      this.form.get('password')?.markAsTouched();
      return;
    }

    if (!this.user && (!val.password || !this.isPasswordComplex)) {
      this.form.get('password')?.setErrors({ invalid: true });
      this.form.get('password')?.markAsTouched();
      return;
    }

    // Khi người dùng bấm Lưu thay đổi:
    // Kiểm tra xem có khách sạn nào được chọn đang có Quản lý khác phụ trách không
    if (val.isHotelManager) {
      this.conflictingHotels = this.hotels.filter(
        (h) => this.selectedManagerHotelIds.includes(h.id) && this.hasOtherManager(h)
      );

      if (this.conflictingHotels.length > 0) {
        this.isConfirmChangeManagerOpen = true;
        return;
      }
    }

    this.executeSave();
  }

  private executeSave(): void {
    const val = this.form.getRawValue();
    const roles: string[] = [];
    if (val.isHotelManager) roles.push('HotelManager');
    if (val.isReceptionist) roles.push('Receptionist');

    const rawFullName = (val.fullName || '').trim();
    const parts = rawFullName.split(/\s+/).filter(Boolean);
    let surname = '';
    let name = rawFullName;
    if (parts.length > 1) {
      surname = parts[0];
      name = parts.slice(1).join(' ');
    }

    if (this.user) {
      this.saveUpdate.emit({
        id: this.user.id,
        dto: {
          email: val.email.trim(),
          name,
          surname,
          phoneNumber: val.phoneNumber?.trim() || '',
          password: val.password?.trim() || undefined,
          roles,
          managerHotelIds: this.selectedManagerHotelIds,
          primaryReceptionistHotelId: val.primaryReceptionistHotelId || undefined,
          concurrencyStamp: this.user.concurrencyStamp,
        },
      });
    } else {
      this.saveCreate.emit({
        userName: val.userName.trim(),
        email: val.email.trim(),
        name,
        surname,
        phoneNumber: val.phoneNumber?.trim() || '',
        password: val.password.trim(),
        roles,
        managerHotelIds: this.selectedManagerHotelIds,
        primaryReceptionistHotelId: val.primaryReceptionistHotelId || undefined,
      });
    }
  }

  ngOnChanges(changes: SimpleChanges): void {
    if ((changes['user'] || (changes['isOpen'] && this.isOpen)) && this.user) {
      this.initFormForUser();
    } else if (changes['hotels'] && this.user) {
      this.syncManagerHotels();
    } else if (changes['isOpen'] && this.isOpen && !this.user) {
      this.selectedManagerHotelIds = [];
      this.form.reset({
        userName: '',
        email: '',
        fullName: '',
        phoneNumber: '',
        password: '',
        isHotelManager: true,
        isReceptionist: false,
        primaryReceptionistHotelId: '',
      });
      this.form.get('userName')?.enable();
    }
  }

  private initFormForUser(): void {
    if (!this.user) return;

    const isManager = this.user.roleNames?.includes('HotelManager') || false;
    const isReceptionist = this.user.roleNames?.includes('Receptionist') || false;
    const activeAssigns = this.user.assignedHotels?.filter((a) => a.isActive) || [];
    const primaryAssign = isReceptionist
      ? (activeAssigns.find((a) => a.isPrimary) ||
         this.user.assignedHotels?.find((a) => a.isPrimary) ||
         activeAssigns[0] ||
         this.user.assignedHotels?.[0])
      : null;

    const managerHotelSet = new Set<string>();

    // 1. Thêm tất cả khách sạn mà người này đang là Quản lý
    this.hotels.forEach((h) => {
      if (this.isCurrentUserManager(h)) {
        managerHotelSet.add(h.id.toLowerCase());
      }
    });

    // 2. Thêm từ assignedHotels: Nếu nhân sự có vai trò Manager, mọi khách sạn đang được phân bổ (kể cả có cờ isPrimary)
    // và không do người khác quản lý thì đều phải được tick chọn
    if (isManager && this.user.assignedHotels) {
      this.user.assignedHotels.forEach((a) => {
        if (a.isActive) {
          const h = this.hotels.find((item) => item.id.toLowerCase() === a.hotelId.toLowerCase());
          if (h && this.isCurrentUserManager(h)) {
            managerHotelSet.add(h.id.toLowerCase());
          } else if (!h || !this.hasOtherManager(h)) {
            managerHotelSet.add(a.hotelId.toLowerCase());
          }
        }
      });
    }

    this.selectedManagerHotelIds = this.hotels
      .filter((h) => managerHotelSet.has(h.id.toLowerCase()))
      .map((h) => h.id);

    // Fallback nếu khách sạn trong assignedHotels chưa có trong this.hotels list
    managerHotelSet.forEach((lowerId) => {
      if (!this.selectedManagerHotelIds.some((id) => id.toLowerCase() === lowerId)) {
        const found = this.user?.assignedHotels?.find((a) => a.hotelId.toLowerCase() === lowerId);
        if (found) {
          this.selectedManagerHotelIds.push(found.hotelId);
        }
      }
    });

    const fullName = `${this.user.surname || ''} ${this.user.name || ''}`.trim() || this.user.name || '';

    this.form.patchValue({
      userName: this.user.userName,
      email: this.user.email,
      fullName: fullName,
      phoneNumber: this.user.phoneNumber || '',
      password: '',
      isHotelManager: isManager,
      isReceptionist: isReceptionist,
      primaryReceptionistHotelId: primaryAssign?.hotelId || '',
    });
    this.form.get('userName')?.disable();
  }

  private syncManagerHotels(): void {
    if (!this.user) return;
    const isManager = this.user.roleNames?.includes('HotelManager') || false;
    if (!isManager) return;

    const managerHotelSet = new Set<string>(this.selectedManagerHotelIds.map((id) => id.toLowerCase()));
    this.hotels.forEach((h) => {
      if (this.isCurrentUserManager(h)) {
        managerHotelSet.add(h.id.toLowerCase());
      }
    });
    this.selectedManagerHotelIds = this.hotels
      .filter((h) => managerHotelSet.has(h.id.toLowerCase()))
      .map((h) => h.id);
  }

  onCancel(): void {
    this.cancel.emit();
  }
}
