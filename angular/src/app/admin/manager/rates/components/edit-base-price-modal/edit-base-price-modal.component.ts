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
import { LocalizationPipe } from '@abp/ng.core';
import { BhModalComponent } from '../../../../../shared/components/modal/bh-modal.component';
import { BhModalConfig } from '../../../../../shared/models/ui-models';
import { BhToastrService } from '../../../../../shared/components/toastr/bh-toastr.service';
import {
  ManagerRateService,
  RoomRatePlanItemDto,
} from '../../../../services/manager/manager-rate.service';

@Component({
  selector: 'bh-edit-base-price-modal',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, LocalizationPipe, BhModalComponent],
  templateUrl: './edit-base-price-modal.component.html',
  styleUrls: ['./edit-base-price-modal.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EditBasePriceModalComponent implements OnChanges {
  private readonly fb = inject(FormBuilder);
  private readonly rateService = inject(ManagerRateService);
  private readonly toastr = inject(BhToastrService);

  @Input() isOpen = false;
  @Input() hotelId = '';
  @Input() roomTypeId = '';
  @Input() ratePlan: RoomRatePlanItemDto | null = null;

  @Output() closed = new EventEmitter<void>();
  @Output() saved = new EventEmitter<RoomRatePlanItemDto>();

  readonly isSaving = signal(false);
  readonly errorKey = signal<string | null>(null);

  readonly modalConfig: BhModalConfig = {
    titleKey: 'Manager:Rates:EditBasePriceTitle',
    confirmTextKey: 'Common:Save',
    cancelTextKey: 'Common:Cancel',
    iconClass: 'fa-solid fa-pen-to-square',
    size: 'md',
    confirmVariant: 'primary',
  };

  readonly form = this.fb.group({
    basePrice: [0, [Validators.required, Validators.min(1), Validators.max(1000000000)]],
  });

  ngOnChanges(changes: SimpleChanges): void {
    if ((changes['isOpen'] || changes['ratePlan']) && this.isOpen && this.ratePlan) {
      this.errorKey.set(null);
      this.form.reset({ basePrice: this.ratePlan.basePrice });
    }
  }

  onCancel(): void {
    this.closed.emit();
  }

  onConfirm(): void {
    if (!this.ratePlan || !this.hotelId || !this.roomTypeId) return;
    this.form.markAllAsTouched();
    const val = Number(this.form.value.basePrice);
    if (this.form.invalid || !Number.isFinite(val) || val <= 0) {
      this.errorKey.set('Manager:Rates:ValidationPositivePrice');
      return;
    }

    this.isSaving.set(true);
    this.errorKey.set(null);

    this.rateService
      .updateRatePlanBasePrice({
        hotelId: this.hotelId,
        roomTypeId: this.roomTypeId,
        ratePlanId: this.ratePlan.ratePlanId,
        basePrice: val,
      })
      .subscribe({
        next: updated => {
          this.isSaving.set(false);
          this.toastr.success('Manager:Rates:BasePriceUpdatedSuccess');
          this.saved.emit(updated);
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
