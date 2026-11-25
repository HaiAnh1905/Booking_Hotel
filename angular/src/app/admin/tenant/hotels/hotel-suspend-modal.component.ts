import { ChangeDetectionStrategy, Component, EventEmitter, Input, OnChanges, Output, SimpleChanges, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { LocalizationPipe } from '@abp/ng.core';
import { BhModalComponent } from '../../../shared/components/modal/bh-modal.component';
import { BhModalConfig } from '../../../shared/models/ui-models';
import { HotelDto, SuspendHotelInputDto, SuspensionType } from '../../services/hotel.service';

@Component({
  selector: 'bh-hotel-suspend-modal',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, LocalizationPipe, BhModalComponent],
  template: `
    <bh-modal
      [isOpen]="isOpen"
      [config]="modalConfig"
      (confirm)="onSubmit()"
      (cancel)="onCancel()"
    >
      <form [formGroup]="form" class="suspend-form" (ngSubmit)="onSubmit()">
        <!-- Type Selection -->
        <div class="mb-3">
          <label class="form-label fw-bold">{{ 'TenantHotels:SuspensionType' | abpLocalization }}</label>
          <div class="d-flex flex-column gap-2">
            <div class="form-check p-3 border rounded" [class.border-warning]="isScheduled" [class.bg-warning-subtle]="isScheduled">
              <input
                class="form-check-input"
                type="radio"
                id="typeScheduled"
                [value]="SuspensionType.Scheduled"
                formControlName="suspensionType"
              />
              <label class="form-check-label fw-semibold" for="typeScheduled">
                <i class="fa-solid fa-calendar-days text-warning me-1"></i>
                {{ 'TenantHotels:SuspensionTypeScheduled' | abpLocalization }}
              </label>
              <div class="text-muted small mt-1">
                {{ 'TenantHotels:ScheduledPolicyNotice' | abpLocalization }}
              </div>
            </div>

            <div class="form-check p-3 border rounded" [class.border-danger]="isEmergency" [class.bg-danger-subtle]="isEmergency">
              <input
                class="form-check-input"
                type="radio"
                id="typeEmergency"
                [value]="SuspensionType.Emergency"
                formControlName="suspensionType"
              />
              <label class="form-check-label fw-semibold text-danger" for="typeEmergency">
                <i class="fa-solid fa-triangle-exclamation text-danger me-1"></i>
                {{ 'TenantHotels:SuspensionTypeEmergency' | abpLocalization }}
              </label>
              <div class="text-muted small mt-1">
                {{ 'TenantHotels:EmergencyPolicyNotice' | abpLocalization }}
              </div>
            </div>
          </div>
        </div>

        <!-- Dates -->
        <div class="row g-3 mb-3">
          <div class="col-md-6">
            <label class="form-label">{{ 'TenantHotels:SuspensionStartDate' | abpLocalization }} <span class="text-danger">*</span></label>
            <input
              type="date"
              class="form-control"
              formControlName="startDate"
              [min]="minStartDate"
            />
            <div class="text-danger small mt-1" *ngIf="form.get('startDate')?.touched && form.get('startDate')?.invalid">
              <span *ngIf="isScheduled">{{ 'TenantHotels:ErrScheduledAdvance30Days' | abpLocalization }}</span>
              <span *ngIf="!isScheduled">{{ 'Common:FieldRequired' | abpLocalization }}</span>
            </div>
          </div>

          <div class="col-md-6">
            <label class="form-label">{{ 'TenantHotels:SuspensionEndDate' | abpLocalization }}</label>
            <input
              type="date"
              class="form-control"
              formControlName="endDate"
              [min]="form.get('startDate')?.value || minStartDate"
            />
          </div>
        </div>

        <!-- Reason -->
        <div class="mb-3">
          <label class="form-label" [class.required]="isEmergency">
            {{ 'TenantHotels:SuspensionReason' | abpLocalization }}
            <span class="text-danger" *ngIf="isEmergency">*</span>
          </label>
          <textarea
            class="form-control"
            rows="3"
            formControlName="reason"
            [placeholder]="'TenantHotels:SuspensionReasonPlaceholder' | abpLocalization"
          ></textarea>
          <div class="text-danger small mt-1" *ngIf="form.get('reason')?.touched && form.get('reason')?.invalid">
            {{ 'TenantHotels:ErrEmergencyReasonMin20Chars' | abpLocalization }}
          </div>
        </div>
      </form>
    </bh-modal>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HotelSuspendModalComponent implements OnChanges {
  private fb = inject(FormBuilder);

  @Input() isOpen = false;
  @Input() hotel: HotelDto | null = null;

  @Output() suspend = new EventEmitter<SuspendHotelInputDto>();
  @Output() cancel = new EventEmitter<void>();

  readonly SuspensionType = SuspensionType;

  form: FormGroup = this.fb.group({
    suspensionType: [SuspensionType.Scheduled, [Validators.required]],
    startDate: ['', [Validators.required]],
    endDate: [''],
    reason: [''],
  });

  get isScheduled(): boolean {
    return this.form.get('suspensionType')?.value === SuspensionType.Scheduled;
  }

  get isEmergency(): boolean {
    return this.form.get('suspensionType')?.value === SuspensionType.Emergency;
  }

  get minStartDate(): string {
    const d = new Date();
    if (this.isScheduled) {
      d.setDate(d.getDate() + 30);
    }
    return d.toISOString().split('T')[0];
  }

  get modalConfig(): BhModalConfig {
    return {
      titleKey: 'TenantHotels:SuspendModalTitle',
      confirmTextKey: 'TenantHotels:SuspendHotel',
      cancelTextKey: 'Common:Cancel',
      confirmVariant: this.isEmergency ? 'danger' : 'warning',
      size: 'lg',
      iconClass: 'fa-solid fa-ban',
    };
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['isOpen'] && this.isOpen) {
      const scheduledMin = new Date();
      scheduledMin.setDate(scheduledMin.getDate() + 30);
      const defaultStart = scheduledMin.toISOString().split('T')[0];

      this.form.reset({
        suspensionType: SuspensionType.Scheduled,
        startDate: defaultStart,
        endDate: '',
        reason: '',
      });
      this.updateValidators();
    }
  }

  private updateValidators(): void {
    const reasonControl = this.form.get('reason');
    if (this.isEmergency) {
      reasonControl?.setValidators([Validators.required, Validators.minLength(20)]);
    } else {
      reasonControl?.clearValidators();
    }
    reasonControl?.updateValueAndValidity();
  }

  onSubmit(): void {
    this.updateValidators();
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const val = this.form.value;
    const sType = Number(val.suspensionType) as SuspensionType;

    // Additional check for 30 days advance on scheduled
    if (sType === SuspensionType.Scheduled) {
      const start = new Date(val.startDate);
      const todayPlus30 = new Date();
      todayPlus30.setDate(todayPlus30.getDate() + 29); // margin check
      if (start < todayPlus30) {
        this.form.get('startDate')?.setErrors({ min30Days: true });
        return;
      }
    }

    this.suspend.emit({
      suspensionType: sType,
      startDate: new Date(val.startDate).toISOString(),
      endDate: val.endDate ? new Date(val.endDate).toISOString() : undefined,
      reason: val.reason?.trim() || '',
    });
  }

  onCancel(): void {
    this.cancel.emit();
  }
}
