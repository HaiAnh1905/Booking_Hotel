import { ChangeDetectionStrategy, Component, EventEmitter, Input, OnChanges, Output, SimpleChanges, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { LocalizationPipe } from '@abp/ng.core';
import { BhModalComponent } from '../../../shared/components/modal/bh-modal.component';
import { BhModalConfig } from '../../../shared/models/ui-models';
import { BhSearchableSelectComponent, SelectOption } from '../../../shared/components/searchable-select/bh-searchable-select.component';
import { TransferBranchDto, UserWithAssignmentDto } from '../../services/tenant-staff.service';
import { HotelDto } from '../../services/hotel.service';

@Component({
  selector: 'bh-staff-transfer-modal',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, LocalizationPipe, BhModalComponent, BhSearchableSelectComponent],
  template: `
    <bh-modal
      [isOpen]="isOpen"
      [config]="modalConfig"
      (confirm)="onSubmit()"
      (cancel)="onCancel()"
    >
      <form [formGroup]="form" class="transfer-form" (ngSubmit)="onSubmit()">
        <p class="text-muted mb-3" *ngIf="user">
          {{ 'TenantStaff:TransferModalTitle' | abpLocalization }}:
          <strong class="text-primary">{{ user.surname }} {{ user.name }} ({{ user.userName }})</strong>
        </p>

        <div class="row g-3">
          <div class="col-md-6">
            <label class="form-label">{{ 'TenantStaff:CurrentHotel' | abpLocalization }} <span class="text-danger">*</span></label>
            <bh-searchable-select
              formControlName="fromHotelId"
              [options]="fromHotelOptions"
              [placeholder]="'TenantStaff:SelectHotel' | abpLocalization"
              [clearable]="false"
            ></bh-searchable-select>
            <div class="text-danger small mt-1" *ngIf="form.get('fromHotelId')?.touched && form.get('fromHotelId')?.invalid">
              {{ 'Common:FieldRequired' | abpLocalization }}
            </div>
          </div>

          <div class="col-md-6">
            <label class="form-label">{{ 'TenantStaff:TargetHotel' | abpLocalization }} <span class="text-danger">*</span></label>
            <bh-searchable-select
              formControlName="toHotelId"
              [options]="toHotelOptions"
              [placeholder]="'TenantStaff:SelectHotel' | abpLocalization"
              [clearable]="false"
            ></bh-searchable-select>
            <div class="text-danger small mt-1" *ngIf="form.get('toHotelId')?.touched && form.get('toHotelId')?.invalid">
              {{ 'Common:FieldRequired' | abpLocalization }}
            </div>
          </div>

          <div class="col-md-12">
            <label class="form-label">{{ 'TenantStaff:ColRoles' | abpLocalization }} <span class="text-danger">*</span></label>
            <bh-searchable-select
              formControlName="roleName"
              [options]="roleOptions"
              [searchable]="false"
              [clearable]="false"
            ></bh-searchable-select>
          </div>

          <div class="col-md-12">
            <label class="form-label">{{ 'TenantStaff:TransferNote' | abpLocalization }}</label>
            <textarea
              class="form-control"
              rows="3"
              formControlName="note"
              [placeholder]="'TenantStaff:TransferNotePlaceholder' | abpLocalization"
            ></textarea>
          </div>
        </div>
      </form>
    </bh-modal>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StaffTransferModalComponent implements OnChanges {
  private fb = inject(FormBuilder);

  @Input() isOpen = false;
  @Input() user: UserWithAssignmentDto | null = null;
  @Input() hotels: HotelDto[] = [];

  @Output() transfer = new EventEmitter<TransferBranchDto>();
  @Output() cancel = new EventEmitter<void>();

  form: FormGroup = this.fb.group({
    fromHotelId: ['', [Validators.required]],
    toHotelId: ['', [Validators.required]],
    roleName: ['Receptionist', [Validators.required]],
    note: [''],
  });

  get modalConfig(): BhModalConfig {
    return {
      titleKey: 'TenantStaff:TransferModalTitle',
      confirmTextKey: 'TenantStaff:TransferBranch',
      cancelTextKey: 'Common:Cancel',
      confirmVariant: 'primary',
      size: 'lg',
      iconClass: 'fa-solid fa-arrows-split-up-and-left',
    };
  }

  get availableDestinationHotels(): HotelDto[] {
    const fromId = this.form.get('fromHotelId')?.value;
    return this.hotels.filter((h) => h.id !== fromId);
  }

  get fromHotelOptions(): SelectOption[] {
    return (this.user?.assignedHotels || []).map((a) => ({
      value: a.hotelId,
      label: `${a.hotelCode} - ${a.hotelName}`,
    }));
  }

  get toHotelOptions(): SelectOption[] {
    return this.availableDestinationHotels.map((h) => ({
      value: h.id,
      label: `${h.code} - ${h.name}`,
    }));
  }

  get roleOptions(): SelectOption[] {
    return (this.user?.roleNames || ['Receptionist']).map((r) => ({
      value: r,
      label: r,
    }));
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['user'] && this.user) {
      const firstAssign = this.user.assignedHotels?.find((a) => a.isActive);
      const defaultRole = this.user.roleNames?.[0] || 'Receptionist';

      this.form.reset({
        fromHotelId: firstAssign?.hotelId || '',
        toHotelId: '',
        roleName: defaultRole,
        note: '',
      });
    }
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const val = this.form.value;
    this.transfer.emit({
      fromHotelId: val.fromHotelId,
      toHotelId: val.toHotelId,
      roleName: val.roleName,
      note: val.note?.trim() || undefined,
    });
  }

  onCancel(): void {
    this.cancel.emit();
  }
}
