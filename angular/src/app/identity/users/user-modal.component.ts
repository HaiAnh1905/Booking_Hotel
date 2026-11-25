import {
  Component,
  EventEmitter,
  Input,
  OnChanges,
  Output,
  SimpleChanges,
  inject,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { LocalizationPipe, LocalizationService } from '@abp/ng.core';
import { BhModalComponent } from '../../shared/components/modal/bh-modal.component';
import { BhModalConfig } from '../../shared/models/ui-models';
import { BhToastrService } from '../../shared/components/toastr/bh-toastr.service';
import {
  UserWithAssignmentDto,
  CreateUserWithAssignmentDto,
  UpdateUserWithAssignmentDto,
} from '../services/identity-user-management.service';
import {
  passwordComplexityValidator,
  checkPasswordCriteria,
  PasswordCriteriaState,
} from '../../auth/validators/password-complexity.validator';

export interface HotelOption {
  id: string;
  name: string;
  code: string;
  managerName?: string;
  managerUserId?: string;
}

function normalizeText(text: string | null | undefined): string {
  if (!text) return '';
  return text.normalize('NFC').replace(/\s+/g, ' ').trim().toLowerCase();
}

@Component({
  selector: 'bh-user-modal',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, LocalizationPipe, BhModalComponent],
  templateUrl: './user-modal.component.html',
  styleUrls: ['./user-modal.component.scss'],
})
export class UserModalComponent implements OnChanges {
  private fb = inject(FormBuilder);
  private toastr = inject(BhToastrService);
  private localization = inject(LocalizationService);

  @Input() isOpen = false;
  @Input() user: UserWithAssignmentDto | null = null;
  @Input() availableRoles: string[] = [];
  @Input() availableHotels: HotelOption[] = [];
  @Input() busy = false;

  @Output() cancel = new EventEmitter<void>();
  @Output() save = new EventEmitter<CreateUserWithAssignmentDto | UpdateUserWithAssignmentDto>();

  form!: FormGroup;

  isConfirmChangeManagerOpen = false;
  conflictingHotels: HotelOption[] = [];

  confirmChangeManagerConfig: BhModalConfig = {
    titleKey: 'TenantStaff:ConfirmChangeManagerTitle',
    confirmTextKey: 'TenantStaff:ConfirmChangeManagerBtn',
    cancelTextKey: 'Common:Cancel',
    confirmVariant: 'danger',
    size: 'md',
    iconClass: 'fa-solid fa-triangle-exclamation text-warning',
  };

  modalConfig: BhModalConfig = {
    titleKey: 'Identity:CreateUser',
    confirmTextKey: 'Common:Save',
    cancelTextKey: 'Common:Cancel',
    confirmVariant: 'primary',
    size: 'lg',
    iconClass: 'fa-solid fa-user-plus',
  };

  get isEditMode(): boolean {
    return !!this.user;
  }

  get isReceptionistSelected(): boolean {
    const roles: string[] = this.form?.get('roleNames')?.value || [];
    return roles.includes('Receptionist');
  }

  get isHotelManagerSelected(): boolean {
    const roles: string[] = this.form?.get('roleNames')?.value || [];
    return roles.includes('HotelManager');
  }

  get isDualManagerAndReceptionist(): boolean {
    return this.isHotelManagerSelected && this.isReceptionistSelected;
  }

  get isReceptionistOnly(): boolean {
    return this.isReceptionistSelected && !this.isHotelManagerSelected;
  }

  get passwordCriteria(): PasswordCriteriaState {
    return checkPasswordCriteria(this.form?.get('password')?.value || '');
  }

  ngOnChanges(changes: SimpleChanges): void {
    if ((changes['isOpen'] && this.isOpen) || (changes['user'] && this.isOpen)) {
      this.initForm();
    }
  }

  private initForm(): void {
    if (this.isEditMode && this.user) {
      this.modalConfig = {
        titleKey: 'Identity:EditUser',
        confirmTextKey: 'Common:Save',
        cancelTextKey: 'Common:Cancel',
        confirmVariant: 'primary',
        size: 'lg',
        iconClass: 'fa-solid fa-user-pen',
      };

      const hotelIds = this.user.assignedHotels?.map(h => h.hotelId) || [];
      const primary =
        this.user.assignedHotels?.find(h => h.isPrimary)?.hotelId || hotelIds[0] || null;

      this.form = this.fb.group({
        userName: [
          { value: this.user.userName, disabled: this.user.userName === 'admin' },
          [Validators.required, Validators.minLength(2), Validators.maxLength(128)],
        ],
        email: [this.user.email, [Validators.required, Validators.email]],
        name: [this.user.name || ''],
        surname: [this.user.surname || ''],
        phoneNumber: [this.user.phoneNumber || ''],
        password: [''],
        roleNames: [this.user.roleNames || [], [Validators.required]],
        hotelIds: [hotelIds],
        primaryHotelId: [primary],
        isActive: [this.user.isActive],
      });
    } else {
      this.modalConfig = {
        titleKey: 'Identity:CreateUser',
        confirmTextKey: 'Common:Save',
        cancelTextKey: 'Common:Cancel',
        confirmVariant: 'primary',
        size: 'lg',
        iconClass: 'fa-solid fa-user-plus',
      };

      this.form = this.fb.group({
        userName: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(128)]],
        email: ['', [Validators.required, Validators.email]],
        name: [''],
        surname: [''],
        phoneNumber: [''],
        password: ['', [Validators.required, Validators.minLength(8), passwordComplexityValidator]],
        roleNames: [[], [Validators.required]],
        hotelIds: [[]],
        primaryHotelId: [null],
        isActive: [true],
      });
    }
  }

  onRoleChange(role: string, event: Event): void {
    const isChecked = (event.target as HTMLInputElement).checked;
    let currentRoles: string[] = [...(this.form.get('roleNames')?.value || [])];

    if (isChecked) {
      if (!currentRoles.includes(role)) {
        currentRoles.push(role);
      }
    } else {
      currentRoles = currentRoles.filter(r => r !== role);
    }

    this.form.patchValue({ roleNames: currentRoles });

    const isRecepOnly =
      currentRoles.includes('Receptionist') && !currentRoles.includes('HotelManager');
    const isManager = currentRoles.includes('HotelManager');

    // Chỉ khi tài khoản thuần túy là Lễ tân (không kiêm nhiệm Quản lý), mới giới hạn hotelIds thành tối đa 1 cơ sở
    if (isRecepOnly) {
      const currentHotels: string[] = this.form.get('hotelIds')?.value || [];
      if (currentHotels.length > 1) {
        const singleHotel = [currentHotels[0]];
        this.form.patchValue({
          hotelIds: singleHotel,
          primaryHotelId: singleHotel[0],
        });
      } else if (currentHotels.length === 0 && this.availableHotels.length === 1) {
        this.form.patchValue({
          hotelIds: [this.availableHotels[0].id],
          primaryHotelId: this.availableHotels[0].id,
        });
      }
    } else if (isManager) {
      // Nếu là Quản lý (kể cả kiêm nhiệm Lễ tân), tự động gán khách sạn duy nhất nếu hệ thống có đúng 1 cơ sở
      const currentHotels: string[] = this.form.get('hotelIds')?.value || [];
      if (currentHotels.length === 0 && this.availableHotels.length === 1) {
        this.form.patchValue({
          hotelIds: [this.availableHotels[0].id],
          primaryHotelId: this.availableHotels[0].id,
        });
      }
    }
  }

  isRoleChecked(role: string): boolean {
    const roles: string[] = this.form?.get('roleNames')?.value || [];
    return roles.includes(role);
  }

  onSingleHotelSelect(hotelId: string): void {
    this.form.patchValue({
      hotelIds: [hotelId],
      primaryHotelId: hotelId,
    });
  }

  isCurrentUserManager(h: HotelOption): boolean {
    if (!this.user || !h) {
      return false;
    }
    if (
      this.user.id &&
      h.managerUserId &&
      h.managerUserId.toLowerCase() === this.user.id.toLowerCase()
    ) {
      return true;
    }

    const isAssigned = this.user.assignedHotels?.some(
      a => a.isActive && a.hotelId?.toLowerCase() === h.id?.toLowerCase(),
    );

    const hMgr = normalizeText(h.managerName?.split('(')[0]);

    if (isAssigned && !hMgr) {
      return true;
    }

    if (!hMgr) {
      return false;
    }

    const currentFullName = `${this.user.surname || ''} ${this.user.name || ''}`;
    const invertedFullName = `${this.user.name || ''} ${this.user.surname || ''}`;
    const formFullName = `${this.form?.get('surname')?.value || ''} ${this.form?.get('name')?.value || ''}`;

    const candidates = [
      normalizeText(currentFullName),
      normalizeText(invertedFullName),
      normalizeText(this.user.name),
      normalizeText(this.user.surname),
      normalizeText(this.user.userName),
      normalizeText(formFullName),
    ].filter(Boolean);

    if (candidates.some(c => c === hMgr || hMgr.includes(c) || c.includes(hMgr))) {
      return true;
    }

    if (isAssigned) {
      return true;
    }

    return false;
  }

  hasOtherManager(h: HotelOption): boolean {
    const hasManager = !!(h.managerUserId || (h.managerName && h.managerName.trim().length > 0));
    if (!hasManager) {
      return false;
    }
    return !this.isCurrentUserManager(h);
  }

  onMultiHotelToggle(hotelId: string, event: Event): void {
    const isChecked = (event.target as HTMLInputElement).checked;
    const lower = hotelId.toLowerCase();
    let currentHotels: string[] = [...(this.form.get('hotelIds')?.value || [])];

    if (isChecked) {
      if (!currentHotels.some(id => id?.toLowerCase() === lower)) {
        currentHotels.push(hotelId);
      }
    } else {
      currentHotels = currentHotels.filter(id => id?.toLowerCase() !== lower);
    }

    let primary = this.form.get('primaryHotelId')?.value;
    if (!currentHotels.some(id => id?.toLowerCase() === primary?.toLowerCase())) {
      primary = currentHotels.length > 0 ? currentHotels[0] : null;
    }

    this.form.patchValue({
      hotelIds: currentHotels,
      primaryHotelId: primary,
    });
  }

  confirmChangeManagerAndSave(): void {
    this.isConfirmChangeManagerOpen = false;
    this.executeSave();
  }

  setPrimaryHotel(hotelId: string, event: Event): void {
    event.stopPropagation();
    this.form.patchValue({ primaryHotelId: hotelId });
  }

  isHotelChecked(hotelId: string): boolean {
    if (!hotelId) return false;
    const lower = hotelId.toLowerCase();
    const hotels: string[] = this.form?.get('hotelIds')?.value || [];
    return hotels.some(id => id?.toLowerCase() === lower);
  }

  onConfirm(): void {
    if (this.form.invalid || this.busy) {
      this.form.markAllAsTouched();
      return;
    }

    const val = this.form.getRawValue();

    const isRecepOnly =
      val.roleNames.includes('Receptionist') && !val.roleNames.includes('HotelManager');
    const isManager = val.roleNames.includes('HotelManager');
    const isRecep = val.roleNames.includes('Receptionist');

    if (isRecepOnly) {
      if (!this.availableHotels || this.availableHotels.length === 0) {
        this.toastr.warning(this.localization.instant('Identity:NoHotelsAvailableNotice'));
        return;
      }
      if (!val.hotelIds || val.hotelIds.length !== 1) {
        this.toastr.warning(this.localization.instant('Identity:ReceptionistSingleHotelLimit'));
        return;
      }
    } else if (isManager || isRecep) {
      if (!this.availableHotels || this.availableHotels.length === 0) {
        this.toastr.warning(this.localization.instant('Identity:NoHotelsAvailableNotice'));
        return;
      }
      if (!val.hotelIds || val.hotelIds.length === 0) {
        this.toastr.warning(this.localization.instant('Identity:AtLeastOneHotelRequired'));
        return;
      }
    }

    // Kiểm tra xung đột quản lý khi bấm Lưu
    if (isManager) {
      const selectedHotelIds: string[] = val.hotelIds || [];
      this.conflictingHotels = this.availableHotels.filter(
        h => selectedHotelIds.includes(h.id) && this.hasOtherManager(h),
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

    if (this.isEditMode && this.user) {
      const updateDto: UpdateUserWithAssignmentDto = {
        userName: val.userName,
        name: val.name,
        surname: val.surname,
        email: val.email,
        phoneNumber: val.phoneNumber,
        roleNames: val.roleNames,
        hotelIds: val.hotelIds || [],
        primaryHotelId: val.primaryHotelId,
        concurrencyStamp: this.user.concurrencyStamp,
        isActive: val.isActive,
      };
      this.save.emit(updateDto);
    } else {
      const createDto: CreateUserWithAssignmentDto = {
        userName: val.userName,
        name: val.name,
        surname: val.surname,
        email: val.email,
        phoneNumber: val.phoneNumber,
        password: val.password,
        roleNames: val.roleNames,
        hotelIds: val.hotelIds || [],
        primaryHotelId: val.primaryHotelId,
      };
      this.save.emit(createDto);
    }
  }

  onCancel(): void {
    this.cancel.emit();
  }
}
