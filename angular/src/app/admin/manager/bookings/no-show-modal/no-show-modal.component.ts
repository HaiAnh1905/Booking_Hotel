import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { LocalizationPipe } from '@abp/ng.core';
import { ManagerBookingService, ManagerBookingDto, NoShowResultDto } from '../../../services/manager/manager-booking.service';

@Component({
  selector: 'bh-no-show-modal',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, LocalizationPipe],
  template: `
    <div class="modal-backdrop" *ngIf="isOpen">
      <div class="modal-dialog">
        <div class="modal-content">
          <div class="modal-header">
            <h5 class="modal-title text-danger">
              <i class="fa-solid fa-user-xmark me-2"></i>
              {{ 'Manager:Booking:MarkNoShowTitle' | abpLocalization }} — {{ booking?.bookingCode }}
            </h5>
            <button type="button" class="btn-close" (click)="close()"></button>
          </div>

          <form [formGroup]="form" (ngSubmit)="submit()">
            <div class="modal-body">
              <div *ngIf="errorMessage()" class="alert alert-danger mb-3">
                {{ errorMessage() }}
              </div>

              <!-- Tóm tắt đơn đặt phòng -->
              <div class="booking-summary-box mb-3 p-3 bg-light rounded-3 border">
                <div class="d-flex justify-content-between mb-1">
                  <span class="text-muted">{{ 'Manager:Guest:Name' | abpLocalization }}:</span>
                  <span class="fw-bold">{{ booking?.guestFullName }}</span>
                </div>
                <div class="d-flex justify-content-between mb-1">
                  <span class="text-muted">{{ 'Manager:Booking:DepositAmount' | abpLocalization }}:</span>
                  <span class="fw-bold text-success">{{ (booking?.depositAmount || 0) | number:'1.0-0' }} VNĐ</span>
                </div>
                <div class="d-flex justify-content-between">
                  <span class="text-muted">{{ 'Manager:Booking:AssignedRoom' | abpLocalization }}:</span>
                  <span class="fw-bold text-primary">{{ booking?.assignedRoomNumber || ('Manager:Booking:RoomAssignment_Unassigned' | abpLocalization) }}</span>
                </div>
              </div>

              <!-- Miễn / Giảm phạt Checkbox -->
              <div class="form-check form-switch mb-3">
                <input
                  type="checkbox"
                  class="form-check-input"
                  id="waiveSwitch"
                  formControlName="waivePenalty"
                  (change)="onWaiveToggle()" />
                <label class="form-check-label fw-bold" for="waiveSwitch">
                  {{ 'Manager:Booking:WaivePenalty' | abpLocalization }}
                </label>
              </div>

              <!-- Mức phạt áp dụng -->
              <div class="mb-3">
                <label class="form-label">
                  {{ 'Manager:Booking:PenaltyAmount' | abpLocalization }} (VNĐ)
                  <span class="text-danger">*</span>
                </label>
                <input type="number" class="form-control" formControlName="penaltyAmountOverride" min="0" />
                <span class="form-text small text-muted">
                  {{ 'Manager:Booking:PenaltyHint' | abpLocalization }}
                </span>
              </div>

              <!-- Lý do miễn / giảm phạt (Bắt buộc nếu waivePenalty = true) -->
              <div class="mb-3" *ngIf="form.get('waivePenalty')?.value">
                <label class="form-label">
                  {{ 'Manager:Booking:WaivePenaltyReason' | abpLocalization }}
                  <span class="text-danger">*</span>
                </label>
                <textarea
                  class="form-control"
                  rows="3"
                  formControlName="waivePenaltyReason"
                  placeholder="Ghi rõ lý do ngoại lệ (vd: bão lũ, chuyến bay bị hủy...) để lưu vết AuditLog">
                </textarea>
                <div *ngIf="form.get('waivePenaltyReason')?.touched && form.get('waivePenaltyReason')?.errors?.['required']" class="text-danger small mt-1">
                  {{ 'Manager:Booking:WaivePenaltyReasonRequired' | abpLocalization }}
                </div>
              </div>
            </div>

            <div class="modal-footer">
              <button type="button" class="btn btn-outline-secondary" (click)="close()" [disabled]="isSubmitting()">
                {{ 'Common:Cancel' | abpLocalization }}
              </button>
              <button type="submit" class="btn btn-danger" [disabled]="form.invalid || isSubmitting()">
                <i *ngIf="isSubmitting()" class="fa-solid fa-spinner fa-spin me-1"></i>
                {{ 'Manager:Booking:ConfirmNoShow' | abpLocalization }}
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .modal-backdrop {
      position: fixed;
      top: 0; left: 0; right: 0; bottom: 0;
      background: rgba(15, 23, 42, 0.6);
      display: flex;
      align-items: center;
      justify-content: center;
      z-index: 1050;
    }
    .modal-dialog {
      width: 100%;
      max-width: 500px;
      margin: 20px;
    }
    .modal-content {
      background: #ffffff;
      border-radius: 12px;
      box-shadow: 0 20px 25px -5px rgba(0, 0, 0, 0.1);
      overflow: hidden;
      border: 1px solid #e2e8f0;
    }
    .modal-header {
      padding: 16px 20px;
      border-bottom: 1px solid #f1f5f9;
      display: flex;
      justify-content: space-between;
      align-items: center;
    }
    .modal-body {
      padding: 20px;
    }
    .modal-footer {
      padding: 14px 20px;
      border-top: 1px solid #f1f5f9;
      display: flex;
      justify-content: flex-end;
      gap: 10px;
      background: #f8fafc;
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NoShowModalComponent {
  private fb = inject(FormBuilder);
  private bookingService = inject(ManagerBookingService);

  @Input() isOpen = false;
  @Input() booking: ManagerBookingDto | null = null;
  @Input() hotelId = '';

  @Output() closed = new EventEmitter<void>();
  @Output() processed = new EventEmitter<NoShowResultDto>();

  readonly isSubmitting = signal<boolean>(false);
  readonly errorMessage = signal<string | null>(null);

  form = this.fb.group({
    waivePenalty: [false],
    penaltyAmountOverride: [0, [Validators.required, Validators.min(0)]],
    waivePenaltyReason: [''],
  });

  onWaiveToggle(): void {
    const waive = !!this.form.get('waivePenalty')?.value;
    if (waive) {
      this.form.get('waivePenaltyReason')?.setValidators([Validators.required]);
      this.form.patchValue({ penaltyAmountOverride: 0 });
    } else {
      this.form.get('waivePenaltyReason')?.clearValidators();
      this.form.patchValue({ penaltyAmountOverride: this.booking?.depositAmount || 0 });
    }
    this.form.get('waivePenaltyReason')?.updateValueAndValidity();
  }

  submit(): void {
    if (this.form.invalid || !this.booking || !this.hotelId) return;

    this.isSubmitting.set(true);
    this.errorMessage.set(null);

    const val = this.form.value;
    this.bookingService.markNoShow(this.booking.id, {
      hotelId: this.hotelId,
      waivePenalty: !!val.waivePenalty,
      penaltyAmountOverride: Number(val.penaltyAmountOverride),
      waivePenaltyReason: val.waivePenaltyReason || undefined,
    }).subscribe({
      next: res => {
        this.isSubmitting.set(false);
        this.processed.emit(res);
        this.close();
      },
      error: err => {
        this.isSubmitting.set(false);
        this.errorMessage.set(err.error?.error?.message || err.message || 'Lỗi xử lý No-Show');
      },
    });
  }

  close(): void {
    this.errorMessage.set(null);
    this.closed.emit();
  }
}
