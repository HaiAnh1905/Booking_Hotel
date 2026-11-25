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
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { LocalizationPipe, LocalizationService } from '@abp/ng.core';
import { BhModalComponent } from '../../../../../shared/components/modal/bh-modal.component';
import { BhModalConfig } from '../../../../../shared/models/ui-models';
import { BhToastrService } from '../../../../../shared/components/toastr/bh-toastr.service';
import {
  BhSearchableSelectComponent,
  SelectOption,
} from '../../../../../shared/components/searchable-select/bh-searchable-select.component';
import {
  ManagerRateService,
  PriceAdjustmentType,
  PriceConflictResolution,
  RoomPriceOverrideDto,
} from '../../../../services/manager/manager-rate.service';

@Component({
  selector: 'bh-edit-override-modal',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, LocalizationPipe, BhModalComponent, BhSearchableSelectComponent],
  templateUrl: './edit-override-modal.component.html',
  styleUrls: ['./edit-override-modal.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EditOverrideModalComponent implements OnChanges {
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
  @Input() mode: 'view' | 'edit' = 'view';
  @Input() hotelId = '';
  @Input() overrideItem: RoomPriceOverrideDto | null = null;

  @Output() closed = new EventEmitter<void>();
  @Output() saved = new EventEmitter<void>();

  readonly PriceAdjustmentType = PriceAdjustmentType;
  readonly PriceConflictResolution = PriceConflictResolution;
  readonly todayIso = this.formatIsoDate(new Date());

  readonly isSaving = signal(false);
  readonly errorKey = signal<string | null>(null);
  readonly conflictDetected = signal(false);
  readonly conflictResolution = signal<PriceConflictResolution>(PriceConflictResolution.ReplaceExisting);

  readonly form = this.fb.group({
    startDate: ['', [Validators.required]],
    endDate: ['', [Validators.required]],
    adjustmentType: [PriceAdjustmentType.FixedPrice, [Validators.required]],
    adjustmentValue: [0 as number | null],
    isClosedToArrival: [false],
    isClosedToDeparture: [false],
  });

  get isEditMode(): boolean {
    return this.mode === 'edit' && !!this.overrideItem && !this.overrideItem.isPastOnly;
  }

  get modalConfig(): BhModalConfig {
    return {
      titleKey: this.isEditMode ? 'Manager:Rates:OverrideEditTitle' : 'Manager:Rates:OverrideViewTitle',
      confirmTextKey: this.isEditMode ? 'Common:Save' : 'Common:Close',
      cancelTextKey: 'Common:Cancel',
      hideCancelButton: !this.isEditMode,
      iconClass: this.isEditMode ? 'fa-solid fa-pen-to-square' : 'fa-solid fa-eye',
      size: 'md',
      confirmVariant: 'primary',
    };
  }

  ngOnChanges(changes: SimpleChanges): void {
    if ((changes['isOpen'] || changes['overrideItem'] || changes['mode']) && this.isOpen && this.overrideItem) {
      this.errorKey.set(null);
      this.conflictDetected.set(false);
      this.conflictResolution.set(PriceConflictResolution.ReplaceExisting);

      const item = this.overrideItem;
      const startIso = item.startDate.substring(0, 10);
      const endIso = item.endDate.substring(0, 10);
      const editableStart = item.isStraddlingToday ? this.todayIso : startIso;

      this.form.reset({
        startDate: this.isEditMode ? editableStart : startIso,
        endDate: endIso,
        adjustmentType: item.adjustmentType,
        adjustmentValue: item.adjustmentValue ?? null,
        isClosedToArrival: item.isClosedToArrival,
        isClosedToDeparture: item.isClosedToDeparture,
      });
    }
  }

  onCancel(): void {
    this.closed.emit();
  }

  onConfirm(): void {
    if (!this.isEditMode || !this.overrideItem || !this.hotelId) {
      this.closed.emit();
      return;
    }

    const val = this.form.value;
    if (!val.startDate || !val.endDate) {
      this.errorKey.set('Manager:Rates:ValidationRequiredDate');
      return;
    }
    if (val.startDate > val.endDate) {
      this.errorKey.set('Manager:Rates:ValidationDateOrder');
      return;
    }
    if (val.startDate < this.todayIso) {
      this.errorKey.set('Manager:Rates:ValidationPastDate');
      return;
    }

    const adjType = Number(val.adjustmentType) as PriceAdjustmentType;
    const rawNum = val.adjustmentValue !== null && val.adjustmentValue !== undefined && String(val.adjustmentValue) !== ''
      ? Number(val.adjustmentValue)
      : null;

    if (adjType === PriceAdjustmentType.FixedPrice && (rawNum === null || !Number.isFinite(rawNum) || rawNum <= 0)) {
      this.errorKey.set('Manager:Rates:ValidationPositivePrice');
      return;
    }
    if (adjType === PriceAdjustmentType.Percentage && (rawNum === null || !Number.isFinite(rawNum) || rawNum <= -100)) {
      this.errorKey.set('Manager:Rates:ValidationPercentResult');
      return;
    }

    if (this.conflictDetected() && this.conflictResolution() === PriceConflictResolution.Cancel) {
      this.closed.emit();
      return;
    }

    this.isSaving.set(true);
    this.errorKey.set(null);

    this.rateService
      .updatePriceOverride(this.overrideItem.id, {
        hotelId: this.hotelId,
        startDate: val.startDate,
        endDate: val.endDate,
        adjustmentType: adjType,
        adjustmentValue: adjType === PriceAdjustmentType.RestrictionsOnly ? null : rawNum,
        isClosedToArrival: !!val.isClosedToArrival,
        isClosedToDeparture: !!val.isClosedToDeparture,
        conflictResolution: this.conflictDetected()
          ? this.conflictResolution()
          : PriceConflictResolution.Cancel,
      })
      .subscribe({
        next: () => {
          this.isSaving.set(false);
          this.toastr.success('Manager:Rates:OverrideUpdateSuccess');
          this.saved.emit();
        },
        error: err => {
          this.isSaving.set(false);
          const code = err?.error?.error?.code;
          if (code === 'BookingHotel:PriceOverrideConflictDetected') {
            this.conflictDetected.set(true);
            this.errorKey.set(null);
            return;
          }
          this.errorKey.set(code || 'Manager:Rates:ErrorSaveFailed');
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
