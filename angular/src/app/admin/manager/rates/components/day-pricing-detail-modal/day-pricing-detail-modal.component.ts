import {
  ChangeDetectionStrategy,
  Component,
  EventEmitter,
  Input,
  OnChanges,
  Output,
  SimpleChanges,
  inject,
  signal,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { LocalizationPipe, LocalizationService } from '@abp/ng.core';
import { BhModalComponent } from '../../../../../shared/components/modal/bh-modal.component';
import { BhModalConfig } from '../../../../../shared/models/ui-models';
import { BhToastrService } from '../../../../../shared/components/toastr/bh-toastr.service';
import {
  BhSearchableSelectComponent,
  SelectOption,
} from '../../../../../shared/components/searchable-select/bh-searchable-select.component';
import {
  DailyPricingCellDto,
  ManagerRateService,
  PriceAdjustmentType,
} from '../../../../services/manager/manager-rate.service';

@Component({
  selector: 'bh-day-pricing-detail-modal',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, LocalizationPipe, BhModalComponent, BhSearchableSelectComponent],
  templateUrl: './day-pricing-detail-modal.component.html',
  styleUrls: ['./day-pricing-detail-modal.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DayPricingDetailModalComponent implements OnChanges {
  private readonly fb = inject(FormBuilder);
  private readonly rateService = inject(ManagerRateService);
  private readonly toastr = inject(BhToastrService);
  private readonly localization = inject(LocalizationService);

  get adjustmentModeOptions(): SelectOption[] {
    return [
      {
        value: PriceAdjustmentType.FixedPrice,
        label: this.localization.instant('Manager:Rates:ModeFixedPrice'),
        icon: 'fa-solid fa-tag',
      },
      {
        value: PriceAdjustmentType.Percentage,
        label: this.localization.instant('Manager:Rates:ModePercentage'),
        icon: 'fa-solid fa-percent',
      },
      {
        value: PriceAdjustmentType.RestrictionsOnly,
        label: this.localization.instant('Manager:Rates:ModeRestrictionsOnly'),
        icon: 'fa-solid fa-ban',
      },
    ];
  }

  @Input() isOpen = false;
  @Input() hotelId = '';
  @Input() roomTypeId = '';
  @Input() ratePlanId = '';
  @Input() ratePlanName = '';
  @Input() dayCell: DailyPricingCellDto | null = null;

  @Output() closed = new EventEmitter<void>();
  @Output() saved = new EventEmitter<void>();

  readonly PriceAdjustmentType = PriceAdjustmentType;
  readonly isSaving = signal(false);
  readonly errorKey = signal<string | null>(null);

  readonly form = this.fb.group({
    adjustmentType: [PriceAdjustmentType.FixedPrice],
    adjustmentValue: [0 as number | null],
    isClosedToArrival: [false],
    isClosedToDeparture: [false],
  });

  get canEdit(): boolean {
    return !!this.dayCell && !this.dayCell.isPastDate && this.dayCell.isOpenForSale;
  }

  get modalConfig(): BhModalConfig {
    return {
      titleKey: 'Manager:Rates:DayDetailTitle',
      confirmTextKey: this.canEdit ? 'Common:Save' : 'Common:Close',
      cancelTextKey: 'Common:Cancel',
      hideCancelButton: !this.canEdit,
      iconClass: 'fa-solid fa-calendar-day',
      size: 'md',
      confirmVariant: 'primary',
    };
  }

  ngOnChanges(changes: SimpleChanges): void {
    if ((changes['isOpen'] || changes['dayCell']) && this.isOpen && this.dayCell) {
      this.errorKey.set(null);
      const cell = this.dayCell;
      const defaultType = cell.adjustmentType ?? PriceAdjustmentType.FixedPrice;
      const defaultValue =
        defaultType === PriceAdjustmentType.RestrictionsOnly
          ? null
          : cell.adjustmentValue ?? cell.effectivePrice;

      this.form.reset({
        adjustmentType: defaultType,
        adjustmentValue: defaultValue,
        isClosedToArrival: cell.isClosedToArrival,
        isClosedToDeparture: cell.isClosedToDeparture,
      });
    }
  }

  onCancel(): void {
    this.closed.emit();
  }

  onConfirm(): void {
    if (!this.canEdit || !this.dayCell) {
      this.closed.emit();
      return;
    }

    const mode = Number(this.form.value.adjustmentType) as PriceAdjustmentType;
    const rawVal = this.form.value.adjustmentValue;
    const numericVal = rawVal !== null && rawVal !== undefined && String(rawVal) !== '' ? Number(rawVal) : null;

    if (mode === PriceAdjustmentType.FixedPrice) {
      if (numericVal === null || !Number.isFinite(numericVal) || numericVal <= 0) {
        this.errorKey.set('Manager:Rates:ValidationPositivePrice');
        return;
      }
    } else if (mode === PriceAdjustmentType.Percentage) {
      if (numericVal === null || !Number.isFinite(numericVal) || numericVal <= -100) {
        this.errorKey.set('Manager:Rates:ValidationPercentResult');
        return;
      }
    }

    this.isSaving.set(true);
    this.errorKey.set(null);

    this.rateService
      .quickEditDailyPricing({
        hotelId: this.hotelId,
        roomTypeId: this.roomTypeId,
        ratePlanId: this.ratePlanId,
        applicableDate: this.dayCell.applicableDate.substring(0, 10),
        adjustmentType: mode,
        adjustmentValue: mode === PriceAdjustmentType.RestrictionsOnly ? null : numericVal,
        isClosedToArrival: !!this.form.value.isClosedToArrival,
        isClosedToDeparture: !!this.form.value.isClosedToDeparture,
        clearOverride: false,
      })
      .subscribe({
        next: () => {
          this.isSaving.set(false);
          this.toastr.success('Manager:Rates:QuickEditSaveSuccess');
          this.saved.emit();
        },
        error: err => {
          this.isSaving.set(false);
          const code = err?.error?.error?.code;
          this.errorKey.set(code || 'Manager:Rates:ErrorSaveFailed');
          this.toastr.error(code || 'Manager:Rates:ErrorSaveFailed');
        },
      });
  }

  onClearOverride(): void {
    if (!this.canEdit || !this.dayCell) return;
    this.isSaving.set(true);
    this.errorKey.set(null);

    this.rateService
      .quickEditDailyPricing({
        hotelId: this.hotelId,
        roomTypeId: this.roomTypeId,
        ratePlanId: this.ratePlanId,
        applicableDate: this.dayCell.applicableDate.substring(0, 10),
        adjustmentType: PriceAdjustmentType.RestrictionsOnly,
        adjustmentValue: null,
        isClosedToArrival: false,
        isClosedToDeparture: false,
        clearOverride: true,
      })
      .subscribe({
        next: () => {
          this.isSaving.set(false);
          this.toastr.success('Manager:Rates:ClearOverrideSuccess');
          this.saved.emit();
        },
        error: err => {
          this.isSaving.set(false);
          const code = err?.error?.error?.code;
          this.errorKey.set(code || 'Manager:Rates:ErrorSaveFailed');
          this.toastr.error(code || 'Manager:Rates:ErrorSaveFailed');
        },
      });
  }
}
