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
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { LocalizationPipe, LocalizationService } from '@abp/ng.core';
import { BhModalComponent } from '../../../../../shared/components/modal/bh-modal.component';
import { BhModalConfig } from '../../../../../shared/models/ui-models';
import { BhToastrService } from '../../../../../shared/components/toastr/bh-toastr.service';
import {
  BhSearchableSelectComponent,
  SelectOption,
} from '../../../../../shared/components/searchable-select/bh-searchable-select.component';
import {
  BulkPriceAdjustmentPreviewDto,
  ManagerRateService,
  PriceAdjustmentType,
  PriceConflictResolution,
  RoomRatePlanItemDto,
} from '../../../../services/manager/manager-rate.service';

interface DayOption {
  value: number;
  labelKey: string;
}

@Component({
  selector: 'bh-bulk-price-adjustment',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, LocalizationPipe, BhModalComponent, BhSearchableSelectComponent],
  templateUrl: './bulk-price-adjustment.component.html',
  styleUrls: ['./bulk-price-adjustment.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BulkPriceAdjustmentComponent implements OnChanges {
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

  @Input() hotelId = '';
  @Input() roomTypeId = '';
  @Input() ratePlans: RoomRatePlanItemDto[] = [];
  @Input() defaultRatePlanId = '';

  @Output() applied = new EventEmitter<void>();

  readonly PriceAdjustmentType = PriceAdjustmentType;
  readonly PriceConflictResolution = PriceConflictResolution;

  readonly todayIso = this.formatIsoDate(new Date());
  readonly selectedRatePlanIds = signal<string[]>([]);
  readonly selectedDaysOfWeek = signal<number[]>([1, 2, 3, 4, 5, 6, 0]);
  readonly validationErrorKey = signal<string | null>(null);
  readonly isPreviewing = signal(false);
  readonly isApplying = signal(false);
  readonly previewOpen = signal(false);
  readonly previewData = signal<BulkPriceAdjustmentPreviewDto | null>(null);
  readonly selectedConflictResolution = signal<PriceConflictResolution>(PriceConflictResolution.ReplaceExisting);

  readonly dayOptions: DayOption[] = [
    { value: 1, labelKey: 'Manager:Rates:DayMon' },
    { value: 2, labelKey: 'Manager:Rates:DayTue' },
    { value: 3, labelKey: 'Manager:Rates:DayWed' },
    { value: 4, labelKey: 'Manager:Rates:DayThu' },
    { value: 5, labelKey: 'Manager:Rates:DayFri' },
    { value: 6, labelKey: 'Manager:Rates:DaySat' },
    { value: 0, labelKey: 'Manager:Rates:DaySun' },
  ];

  readonly previewModalConfig: BhModalConfig = {
    titleKey: 'Manager:Rates:PreviewModalTitle',
    confirmTextKey: 'Manager:Rates:ConfirmApplyBulk',
    cancelTextKey: 'Common:Cancel',
    iconClass: 'fa-solid fa-magnifying-glass-chart',
    size: 'lg',
    confirmVariant: 'primary',
  };

  readonly form = this.fb.group({
    startDate: [this.todayIso, [Validators.required]],
    endDate: [this.formatIsoDate(new Date(Date.now() + 86400000 * 6)), [Validators.required]],
    adjustmentType: [PriceAdjustmentType.FixedPrice, [Validators.required]],
    adjustmentValue: [1000000 as number | null],
    isClosedToArrival: [false],
    isClosedToDeparture: [false],
  });

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['defaultRatePlanId'] || changes['ratePlans']) {
      const activeIds = new Set(this.ratePlans.filter(p => p.isActive).map(p => p.ratePlanId));
      const kept = this.selectedRatePlanIds().filter(id => activeIds.has(id));
      if (kept.length === 0) {
        if (this.defaultRatePlanId && activeIds.has(this.defaultRatePlanId)) {
          this.selectedRatePlanIds.set([this.defaultRatePlanId]);
        } else {
          const firstActive = this.ratePlans.find(p => p.isActive);
          this.selectedRatePlanIds.set(firstActive ? [firstActive.ratePlanId] : []);
        }
      } else {
        this.selectedRatePlanIds.set(kept);
      }
    }
  }

  toggleRatePlanSelection(plan: RoomRatePlanItemDto): void {
    if (!plan.isActive) return;
    this.selectedRatePlanIds.update(current =>
      current.includes(plan.ratePlanId)
        ? current.filter(id => id !== plan.ratePlanId)
        : [...current, plan.ratePlanId]
    );
  }

  selectAllActivePlans(): void {
    this.selectedRatePlanIds.set(this.ratePlans.filter(p => p.isActive).map(p => p.ratePlanId));
  }

  toggleDayOfWeek(day: number): void {
    this.selectedDaysOfWeek.update(days =>
      days.includes(day) ? days.filter(d => d !== day) : [...days, day]
    );
  }

  selectAllDays(): void {
    this.selectedDaysOfWeek.set([1, 2, 3, 4, 5, 6, 0]);
  }

  selectWeekends(): void {
    this.selectedDaysOfWeek.set([5, 6, 0]);
  }

  onRequestPreview(): void {
    this.validationErrorKey.set(null);
    if (!this.hotelId || !this.roomTypeId) return;

    const val = this.form.value;
    if (!val.startDate || !val.endDate) {
      this.validationErrorKey.set('Manager:Rates:ValidationRequiredDate');
      return;
    }

    if (val.startDate > val.endDate) {
      this.validationErrorKey.set('Manager:Rates:ValidationDateOrder');
      return;
    }

    if (val.startDate < this.todayIso) {
      this.validationErrorKey.set('Manager:Rates:ValidationPastDate');
      return;
    }

    if (this.selectedRatePlanIds().length === 0) {
      this.validationErrorKey.set('Manager:Rates:ValidationSelectRatePlan');
      return;
    }

    if (this.selectedDaysOfWeek().length === 0) {
      this.validationErrorKey.set('Manager:Rates:ValidationSelectDayOfWeek');
      return;
    }

    const mode = Number(val.adjustmentType) as PriceAdjustmentType;
    const rawNum = val.adjustmentValue !== null && val.adjustmentValue !== undefined && String(val.adjustmentValue) !== ''
      ? Number(val.adjustmentValue)
      : null;

    if (mode === PriceAdjustmentType.FixedPrice && (rawNum === null || !Number.isFinite(rawNum) || rawNum <= 0)) {
      this.validationErrorKey.set('Manager:Rates:ValidationPositivePrice');
      return;
    }

    if (mode === PriceAdjustmentType.Percentage && (rawNum === null || !Number.isFinite(rawNum) || rawNum <= -100)) {
      this.validationErrorKey.set('Manager:Rates:ValidationPercentResult');
      return;
    }

    this.isPreviewing.set(true);

    this.rateService
      .previewBulkAdjustment({
        hotelId: this.hotelId,
        roomTypeId: this.roomTypeId,
        ratePlanIds: this.selectedRatePlanIds(),
        startDate: val.startDate,
        endDate: val.endDate,
        daysOfWeek: this.selectedDaysOfWeek(),
        adjustmentType: mode,
        adjustmentValue: mode === PriceAdjustmentType.RestrictionsOnly ? null : rawNum,
        isClosedToArrival: !!val.isClosedToArrival,
        isClosedToDeparture: !!val.isClosedToDeparture,
      })
      .subscribe({
        next: preview => {
          this.isPreviewing.set(false);
          this.previewData.set(preview);
          this.selectedConflictResolution.set(
            preview.hasAnyConflicts
              ? PriceConflictResolution.ReplaceExisting
              : PriceConflictResolution.ReplaceExisting
          );
          this.previewOpen.set(true);
        },
        error: err => {
          this.isPreviewing.set(false);
          const code = err?.error?.error?.code;
          this.validationErrorKey.set(code || 'Manager:Rates:ErrorSaveFailed');
          this.toastr.error(code || 'Manager:Rates:ErrorSaveFailed');
        },
      });
  }

  onCancelPreview(): void {
    this.previewOpen.set(false);
  }

  onConfirmApply(): void {
    const preview = this.previewData();
    if (!preview) return;

    if (preview.hasAnyConflicts && this.selectedConflictResolution() === PriceConflictResolution.Cancel) {
      this.previewOpen.set(false);
      return;
    }

    const val = this.form.value;
    const mode = Number(val.adjustmentType) as PriceAdjustmentType;
    const rawNum = val.adjustmentValue !== null && val.adjustmentValue !== undefined && String(val.adjustmentValue) !== ''
      ? Number(val.adjustmentValue)
      : null;

    this.isApplying.set(true);

    this.rateService
      .applyBulkAdjustment({
        hotelId: this.hotelId,
        roomTypeId: this.roomTypeId,
        ratePlanIds: this.selectedRatePlanIds(),
        startDate: val.startDate!,
        endDate: val.endDate!,
        daysOfWeek: this.selectedDaysOfWeek(),
        adjustmentType: mode,
        adjustmentValue: mode === PriceAdjustmentType.RestrictionsOnly ? null : rawNum,
        isClosedToArrival: !!val.isClosedToArrival,
        isClosedToDeparture: !!val.isClosedToDeparture,
        conflictResolution: preview.hasAnyConflicts
          ? this.selectedConflictResolution()
          : PriceConflictResolution.ReplaceExisting,
      })
      .subscribe({
        next: () => {
          this.isApplying.set(false);
          this.previewOpen.set(false);
          this.toastr.success('Manager:Rates:BulkApplySuccess');
          this.applied.emit();
        },
        error: err => {
          this.isApplying.set(false);
          const code = err?.error?.error?.code;
          this.toastr.error(code || 'Manager:Rates:ErrorSaveFailed');
        },
      });
  }

  private formatIsoDate(date: Date): string {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }
}
