import { ChangeDetectionStrategy, Component, EventEmitter, Input, OnChanges, Output, SimpleChanges, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { LocalizationPipe, LocalizationService } from '@abp/ng.core';
import { BhModalComponent } from '../../../shared/components/modal/bh-modal.component';
import { BhModalConfig } from '../../../shared/models/ui-models';
import { BhSearchableSelectComponent, SelectOption } from '../../../shared/components/searchable-select/bh-searchable-select.component';
import { CreateUpdateHotelDto, HotelDto, HotelService, HotelStatus } from '../../services/hotel.service';

@Component({
  selector: 'bh-hotel-modal',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, LocalizationPipe, BhModalComponent, BhSearchableSelectComponent],
  template: `
    <bh-modal
      [isOpen]="isOpen"
      [config]="modalConfig"
      (confirm)="onSubmit()"
      (cancel)="onCancel()"
    >
      <form [formGroup]="form" class="hotel-form" (ngSubmit)="onSubmit()">
        <div *ngIf="errorMessage" class="alert alert-danger d-flex align-items-center mb-3">
          <i class="fa-solid fa-triangle-exclamation me-2"></i>
          <div>{{ errorMessage }}</div>
        </div>

        <div class="row g-3">
          <div class="col-md-8">
            <label class="form-label">{{ 'TenantHotels:FieldName' | abpLocalization }} <span class="text-danger">*</span></label>
            <input
              type="text"
              class="form-control"
              formControlName="name"
              [class.is-invalid]="(form.get('name')?.touched && form.get('name')?.invalid) || !!nameExistsError"
              [placeholder]="'TenantHotels:FieldName' | abpLocalization"
              (blur)="onNameBlur()"
              (input)="onNameInput()"
            />
            <div class="text-danger small mt-1" *ngIf="nameExistsError">
              {{ nameExistsError }}
            </div>
            <div class="text-danger small mt-1" *ngIf="!nameExistsError && form.get('name')?.touched && form.get('name')?.invalid">
              {{ 'Common:FieldRequired' | abpLocalization }}
            </div>
          </div>

          <div class="col-md-4">
            <label class="form-label">{{ 'TenantHotels:FieldCode' | abpLocalization }} <span class="text-danger">*</span></label>
            <input
              type="text"
              class="form-control"
              formControlName="code"
              [placeholder]="'TenantHotels:PlaceholderCode' | abpLocalization"
            />
            <div class="text-danger small mt-1" *ngIf="form.get('code')?.touched && form.get('code')?.invalid">
              {{ 'TenantHotels:CodeValidation' | abpLocalization }}
            </div>
          </div>

          <div class="col-md-8">
            <label class="form-label">{{ 'TenantHotels:FieldAddress' | abpLocalization }} <span class="text-danger">*</span></label>
            <input
              type="text"
              class="form-control"
              formControlName="address"
              [placeholder]="'TenantHotels:FieldAddress' | abpLocalization"
            />
            <div class="text-danger small mt-1" *ngIf="form.get('address')?.touched && form.get('address')?.invalid">
              {{ 'Common:FieldRequired' | abpLocalization }}
            </div>
          </div>

          <div class="col-md-4">
            <label class="form-label">{{ 'TenantHotels:FieldCity' | abpLocalization }} <span class="text-danger">*</span></label>
            <bh-searchable-select
              formControlName="city"
              [options]="provinceOptions"
              [placeholder]="'TenantHotels:SelectCity' | abpLocalization"
              [searchPlaceholder]="'TenantHotels:SearchProvincePlaceholder' | abpLocalization"
              [emptyText]="'TenantHotels:NoMatchingResults' | abpLocalization"
              [clearable]="false"
            ></bh-searchable-select>
            <div class="text-danger small mt-1" *ngIf="form.get('city')?.touched && form.get('city')?.invalid">
              {{ 'Common:FieldRequired' | abpLocalization }}
            </div>
          </div>

          <div class="col-md-6">
            <label class="form-label">{{ 'TenantHotels:FieldPhone' | abpLocalization }} <span class="text-danger">*</span></label>
            <input
              type="text"
              class="form-control"
              formControlName="phone"
              [placeholder]="'TenantHotels:PlaceholderPhone' | abpLocalization"
            />
            <div class="text-danger small mt-1" *ngIf="form.get('phone')?.touched && form.get('phone')?.invalid">
              {{ 'Common:FieldRequired' | abpLocalization }}
            </div>
          </div>

          <div class="col-md-6">
            <label class="form-label">{{ 'TenantHotels:FieldEmail' | abpLocalization }} <span class="text-danger">*</span></label>
            <input
              type="email"
              class="form-control"
              formControlName="email"
              placeholder="branch@bookinghotel.vn"
            />
            <div class="text-danger small mt-1" *ngIf="form.get('email')?.touched && form.get('email')?.invalid">
              {{ 'Common:FieldRequired' | abpLocalization }}
            </div>
          </div>

          <div class="col-md-4">
            <label class="form-label">{{ 'TenantHotels:FieldRating' | abpLocalization }} <span class="text-danger">*</span></label>
            <bh-searchable-select
              formControlName="starRating"
              [options]="starRatingOptions"
              [searchable]="false"
              [clearable]="false"
              [placeholder]="'TenantHotels:FieldRating' | abpLocalization"
            ></bh-searchable-select>
          </div>

          <div class="col-md-4">
            <label class="form-label">{{ 'TenantHotels:FieldCapacity' | abpLocalization }} <span class="text-danger">*</span></label>
            <input type="number" class="form-control" formControlName="roomCount" min="1" max="5000" />
            <div class="text-danger small mt-1" *ngIf="form.get('roomCount')?.touched && form.get('roomCount')?.invalid">
              {{ 'Common:FieldRequired' | abpLocalization }}
            </div>
          </div>

          <div class="col-md-4">
            <label class="form-label">{{ 'TenantHotels:ColStatus' | abpLocalization }} <span class="text-danger">*</span></label>
            <bh-searchable-select
              formControlName="status"
              [options]="statusOptions"
              [searchable]="false"
              [clearable]="false"
              [placeholder]="'TenantHotels:ColStatus' | abpLocalization"
            ></bh-searchable-select>
          </div>

          <div class="col-md-4">
            <label class="form-label">{{ 'TenantHotels:StandardCheckInTime' | abpLocalization }} <span class="text-danger">*</span></label>
            <input type="time" class="form-control" formControlName="standardCheckInTime" />
          </div>

          <div class="col-md-4">
            <label class="form-label">{{ 'TenantHotels:StandardCheckOutTime' | abpLocalization }} <span class="text-danger">*</span></label>
            <input type="time" class="form-control" formControlName="standardCheckOutTime" />
          </div>

          <div class="col-md-4">
            <label class="form-label">{{ 'TenantHotels:BookingCutoffDays' | abpLocalization }} <span class="text-danger">*</span></label>
            <input type="number" class="form-control" formControlName="bookingCutoffDays" min="1" max="90" />
          </div>

          <div class="col-md-12">
            <label class="form-label">{{ 'TenantHotels:FieldManager' | abpLocalization }}</label>
            <bh-searchable-select
              formControlName="managerUserId"
              [options]="managerOptions"
              [placeholder]="'TenantHotels:SelectManager' | abpLocalization"
              [searchPlaceholder]="'TenantHotels:SearchManagerPlaceholder' | abpLocalization"
              [emptyText]="'TenantHotels:NoMatchingResults' | abpLocalization"
              [clearable]="true"
              [clearOptionLabel]="'TenantHotels:UnassignedManager' | abpLocalization"
            ></bh-searchable-select>
          </div>
        </div>
      </form>
    </bh-modal>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HotelModalComponent implements OnChanges {
  private fb = inject(FormBuilder);
  private localization = inject(LocalizationService);
  private hotelService = inject(HotelService);
  private cdr = inject(ChangeDetectorRef);

  @Input() isOpen = false;
  @Input() hotel: HotelDto | null = null;
  @Input() provinceOptions: SelectOption[] = [];
  @Input() managerOptions: SelectOption[] = [];
  @Input() errorMessage: string | null = null;
  @Input() isSubmitting = false;

  @Output() save = new EventEmitter<CreateUpdateHotelDto>();
  @Output() cancel = new EventEmitter<void>();

  readonly HotelStatus = HotelStatus;
  nameExistsError: string | null = null;

  get starRatingOptions(): SelectOption[] {
    const starsText = this.localization.instant('TenantHotels:Stars');
    return [
      { value: 5, label: `5 ${starsText}`, icon: 'fa-solid fa-star text-warning' },
      { value: 4, label: `4 ${starsText}`, icon: 'fa-solid fa-star text-warning' },
      { value: 3, label: `3 ${starsText}`, icon: 'fa-solid fa-star text-warning' },
      { value: 2, label: `2 ${starsText}`, icon: 'fa-solid fa-star text-warning' },
      { value: 1, label: `1 ${starsText}`, icon: 'fa-solid fa-star text-warning' },
    ];
  }

  get statusOptions(): SelectOption[] {
    return [
      {
        value: HotelStatus.Operating,
        label: this.localization.instant('TenantHotels:StatusOperating'),
        icon: 'fa-solid fa-circle-check text-success',
      },
      {
        value: HotelStatus.ScheduledSuspended,
        label: this.localization.instant('TenantHotels:StatusScheduledSuspended'),
        icon: 'fa-solid fa-calendar-xmark text-warning',
      },
      {
        value: HotelStatus.EmergencySuspended,
        label: this.localization.instant('TenantHotels:StatusEmergencySuspended'),
        icon: 'fa-solid fa-triangle-exclamation text-danger',
      },
      {
        value: HotelStatus.Maintenance,
        label: this.localization.instant('TenantHotels:StatusMaintenance'),
        icon: 'fa-solid fa-wrench text-info',
      },
      {
        value: HotelStatus.Closed,
        label: this.localization.instant('TenantHotels:StatusClosed'),
        icon: 'fa-solid fa-ban text-secondary',
      },
    ];
  }

  form: FormGroup = this.fb.group({
    name: ['', [Validators.required, Validators.maxLength(256)]],
    code: ['', [Validators.required, Validators.pattern('^[A-Z0-9\\-_]{3,20}$')]],
    address: ['', [Validators.required, Validators.maxLength(512)]],
    city: ['', [Validators.required]],
    phone: ['', [Validators.required]],
    email: ['', [Validators.required, Validators.email]],
    starRating: [5, [Validators.required, Validators.min(1), Validators.max(5)]],
    roomCount: [50, [Validators.required, Validators.min(1)]],
    status: [HotelStatus.Operating, [Validators.required]],
    standardCheckInTime: ['14:00', [Validators.required]],
    standardCheckOutTime: ['12:00', [Validators.required]],
    bookingCutoffDays: [14, [Validators.required, Validators.min(1)]],
    managerUserId: [''],
  });

  get modalConfig(): BhModalConfig {
    return {
      titleKey: this.hotel ? 'TenantHotels:EditModalTitle' : 'TenantHotels:CreateModalTitle',
      confirmTextKey: 'Common:Save',
      cancelTextKey: 'Common:Cancel',
      confirmVariant: 'primary',
      size: 'lg',
      iconClass: 'fa-solid fa-hotel',
    };
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['isOpen'] && this.isOpen) {
      this.nameExistsError = null;
      if (this.form.get('name')?.hasError('nameExists')) {
        const errors = { ...this.form.get('name')?.errors };
        delete errors['nameExists'];
        this.form.get('name')?.setErrors(Object.keys(errors).length ? errors : null);
      }
    }

    if (changes['errorMessage'] && this.errorMessage) {
      if (this.errorMessage.includes(this.form.get('name')?.value?.trim())) {
        this.nameExistsError = this.errorMessage;
        this.form.get('name')?.setErrors({ ...(this.form.get('name')?.errors || {}), nameExists: true });
      }
    }

    if (changes['hotel'] && this.hotel) {
      this.nameExistsError = null;
      let selectedManagerId = this.hotel.managerUserId || '';
      if (!selectedManagerId && this.hotel.managerName) {
        const found = this.managerOptions.find(o =>
          (o.label && o.label.includes(this.hotel!.managerName!)) ||
          o.value === this.hotel!.managerName
        );
        if (found && found.value) {
          selectedManagerId = found.value as string;
        }
      }

      this.form.patchValue({
        name: this.hotel.name,
        code: this.hotel.code,
        address: this.hotel.address,
        city: this.hotel.city,
        phone: this.hotel.phone,
        email: this.hotel.email,
        starRating: this.hotel.starRating,
        roomCount: this.hotel.roomCount,
        status: this.hotel.status,
        standardCheckInTime: this.hotel.standardCheckInTime?.slice(0, 5) || '14:00',
        standardCheckOutTime: this.hotel.standardCheckOutTime?.slice(0, 5) || '12:00',
        bookingCutoffDays: this.hotel.bookingCutoffDays || 14,
        managerUserId: selectedManagerId,
      });
    } else if (changes['isOpen'] && this.isOpen && !this.hotel) {
      this.nameExistsError = null;
      this.form.reset({
        name: '',
        code: '',
        address: '',
        city: '',
        phone: '',
        email: '',
        starRating: 5,
        roomCount: 50,
        status: HotelStatus.Operating,
        standardCheckInTime: '14:00',
        standardCheckOutTime: '12:00',
        bookingCutoffDays: 14,
        managerUserId: '',
      });
    }
  }

  onNameInput(): void {
    if (this.nameExistsError) {
      this.nameExistsError = null;
      if (this.form.get('name')?.hasError('nameExists')) {
        const errors = { ...this.form.get('name')?.errors };
        delete errors['nameExists'];
        this.form.get('name')?.setErrors(Object.keys(errors).length ? errors : null);
      }
      this.cdr.markForCheck();
    }
  }

  onNameBlur(): void {
    const raw = this.form.get('name')?.value;
    const name = raw ? raw.trim() : '';
    if (!name) return;

    if (this.hotel && this.hotel.name && this.hotel.name.trim().toLowerCase() === name.toLowerCase()) {
      this.nameExistsError = null;
      this.cdr.markForCheck();
      return;
    }

    this.hotelService.checkNameExists(name, this.hotel?.id).subscribe({
      next: (exists) => {
        if (exists) {
          const msg = this.localization.instant('TenantHotels:ErrNameAlreadyExists', name) ||
            `Tên khách sạn '${name}' đã tồn tại trong chuỗi. Vui lòng chọn tên khác.`;
          this.nameExistsError = msg;
          this.form.get('name')?.setErrors({ ...(this.form.get('name')?.errors || {}), nameExists: true });
        } else {
          this.nameExistsError = null;
          if (this.form.get('name')?.hasError('nameExists')) {
            const errors = { ...this.form.get('name')?.errors };
            delete errors['nameExists'];
            this.form.get('name')?.setErrors(Object.keys(errors).length ? errors : null);
          }
        }
        this.cdr.markForCheck();
      },
      error: () => {}
    });
  }

  onSubmit(): void {
    if (this.nameExistsError) {
      this.form.get('name')?.markAsTouched();
      return;
    }
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const val = this.form.value;
    const selectedOption = this.managerOptions.find(o => o.value === val.managerUserId);
    const resolvedManagerName = selectedOption && selectedOption.value
      ? (selectedOption.label.split(' (')[0] || selectedOption.label).trim()
      : '';

    this.save.emit({
      name: val.name.trim(),
      code: val.code.trim().toUpperCase(),
      address: val.address.trim(),
      city: val.city?.trim() || '',
      phone: val.phone.trim(),
      email: val.email.trim(),
      starRating: Number(val.starRating),
      roomCount: Number(val.roomCount),
      status: Number(val.status),
      standardCheckInTime: val.standardCheckInTime ? `${val.standardCheckInTime}:00` : undefined,
      standardCheckOutTime: val.standardCheckOutTime ? `${val.standardCheckOutTime}:00` : undefined,
      bookingCutoffDays: Number(val.bookingCutoffDays) || 14,
      managerUserId: val.managerUserId || null,
      managerName: resolvedManagerName,
    });
  }

  onCancel(): void {
    this.cancel.emit();
  }
}
