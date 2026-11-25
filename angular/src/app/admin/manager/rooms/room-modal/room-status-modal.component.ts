import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { LocalizationPipe } from '@abp/ng.core';
import { ManagerRoomService, RoomDto, RoomOperationalStatus } from '../../../services/manager/manager-room.service';

@Component({
  selector: 'bh-room-status-modal',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, LocalizationPipe],
  template: `
    <div class="modal-backdrop" *ngIf="isOpen">
      <div class="modal-dialog">
        <div class="modal-content">
          <div class="modal-header">
            <h5 class="modal-title">
              <i class="fa-solid fa-wrench me-2 text-warning"></i>
              {{ 'Manager:Room:ChangeStatusTitle' | abpLocalization }} — {{ room?.roomNumber }}
            </h5>
            <button type="button" class="btn-close" (click)="close()"></button>
          </div>

          <form [formGroup]="form" (ngSubmit)="submit()">
            <div class="modal-body">
              <!-- Cảnh báo lỗi từ Backend nếu có xung đột -->
              <div *ngIf="errorMessage()" class="alert alert-danger d-flex align-items-center mb-3">
                <i class="fa-solid fa-triangle-exclamation me-2"></i>
                <span>{{ errorMessage() }}</span>
              </div>

              <!-- Trạng thái mục tiêu -->
              <div class="mb-3">
                <label class="form-label">
                  {{ 'Manager:Room:TargetOperationalStatus' | abpLocalization }}
                  <span class="text-danger">*</span>
                </label>
                <select class="form-select" formControlName="targetStatus" (change)="onStatusChange()">
                  <option [value]="RoomOperationalStatus.Available">{{ 'Manager:Room:Status_Available' | abpLocalization }}</option>
                  <option [value]="RoomOperationalStatus.Maintenance">{{ 'Manager:Room:Status_Maintenance' | abpLocalization }}</option>
                  <option [value]="RoomOperationalStatus.OutOfService">{{ 'Manager:Room:Status_OutOfService' | abpLocalization }}</option>
                </select>
              </div>

              <!-- Trường lý do & thời gian nếu bảo trì hoặc ngừng kinh doanh -->
              <ng-container *ngIf="isRestrictedStatus()">
                <div class="mb-3">
                  <label class="form-label">
                    {{ 'Manager:Room:Reason' | abpLocalization }}
                    <span class="text-danger">*</span>
                  </label>
                  <textarea
                    class="form-control"
                    rows="3"
                    formControlName="reason"
                    [placeholder]="'Manager:Room:ReasonPlaceholder' | abpLocalization">
                  </textarea>
                  <div *ngIf="form.get('reason')?.touched && form.get('reason')?.errors?.['required']" class="text-danger small mt-1">
                    {{ 'Manager:Room:ReasonRequired' | abpLocalization }}
                  </div>
                </div>

                <div class="row">
                  <div class="col-md-6 mb-3">
                    <label class="form-label">
                      {{ 'Manager:Room:StartDate' | abpLocalization }}
                      <span class="text-danger">*</span>
                    </label>
                    <input type="date" class="form-control" formControlName="startDate" />
                  </div>
                  <div class="col-md-6 mb-3">
                    <label class="form-label">
                      {{ 'Manager:Room:ExpectedEndDate' | abpLocalization }}
                      <span class="text-danger">*</span>
                    </label>
                    <input type="date" class="form-control" formControlName="expectedEndDate" />
                  </div>
                </div>
              </ng-container>
            </div>

            <div class="modal-footer">
              <button type="button" class="btn btn-outline-secondary" (click)="close()" [disabled]="isSubmitting()">
                {{ 'Common:Cancel' | abpLocalization }}
              </button>
              <button type="submit" class="btn btn-primary" [disabled]="form.invalid || isSubmitting()">
                <i *ngIf="isSubmitting()" class="fa-solid fa-spinner fa-spin me-1"></i>
                {{ 'Common:Save' | abpLocalization }}
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
      max-width: 520px;
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
export class RoomStatusModalComponent {
  private fb = inject(FormBuilder);
  private roomService = inject(ManagerRoomService);

  readonly RoomOperationalStatus = RoomOperationalStatus;

  @Input() isOpen = false;
  @Input() room: RoomDto | null = null;
  @Input() hotelId = '';

  @Output() closed = new EventEmitter<void>();
  @Output() statusChanged = new EventEmitter<RoomDto>();

  readonly isSubmitting = signal<boolean>(false);
  readonly errorMessage = signal<string | null>(null);

  form = this.fb.group({
    targetStatus: [RoomOperationalStatus.Available, [Validators.required]],
    reason: [''],
    startDate: [new Date().toISOString().substring(0, 10)],
    expectedEndDate: [new Date(Date.now() + 86400000 * 2).toISOString().substring(0, 10)],
  });

  isRestrictedStatus(): boolean {
    const status = Number(this.form.get('targetStatus')?.value);
    return status === RoomOperationalStatus.Maintenance || status === RoomOperationalStatus.OutOfService;
  }

  onStatusChange(): void {
    if (this.isRestrictedStatus()) {
      this.form.get('reason')?.setValidators([Validators.required]);
      this.form.get('startDate')?.setValidators([Validators.required]);
      this.form.get('expectedEndDate')?.setValidators([Validators.required]);
    } else {
      this.form.get('reason')?.clearValidators();
      this.form.get('startDate')?.clearValidators();
      this.form.get('expectedEndDate')?.clearValidators();
    }
    this.form.get('reason')?.updateValueAndValidity();
    this.form.get('startDate')?.updateValueAndValidity();
    this.form.get('expectedEndDate')?.updateValueAndValidity();
  }

  submit(): void {
    if (this.form.invalid || !this.room || !this.hotelId) return;

    this.isSubmitting.set(true);
    this.errorMessage.set(null);

    const val = this.form.value;
    const targetStatus = Number(val.targetStatus);

    this.roomService.changeOperationalStatus(this.room.id, {
      hotelId: this.hotelId,
      targetStatus: targetStatus,
      reason: val.reason || undefined,
      startDate: val.startDate ? new Date(val.startDate).toISOString() : undefined,
      expectedEndDate: val.expectedEndDate ? new Date(val.expectedEndDate).toISOString() : undefined,
    }).subscribe({
      next: updated => {
        this.isSubmitting.set(false);
        this.statusChanged.emit(updated);
        this.close();
      },
      error: err => {
        this.isSubmitting.set(false);
        const msg = err.error?.error?.message || err.message || 'Error changing status';
        this.errorMessage.set(msg);
      },
    });
  }

  close(): void {
    this.errorMessage.set(null);
    this.closed.emit();
  }
}
