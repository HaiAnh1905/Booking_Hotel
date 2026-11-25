import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output, OnChanges, SimpleChanges, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { LocalizationPipe } from '@abp/ng.core';
import { ManagerBookingService, CreateDirectBookingDto, ManagerBookingDto } from '../../../services/manager/manager-booking.service';
import { RoomTypeDto } from '../../../services/manager/manager-room.service';
import {
  BhSearchableSelectComponent,
  SelectOption,
} from '../../../../shared/components/searchable-select/bh-searchable-select.component';

@Component({
  selector: 'bh-direct-booking-modal',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    LocalizationPipe,
    BhSearchableSelectComponent,
  ],
  template: `
    <div class="modal-backdrop" *ngIf="isOpen">
      <div class="modal-dialog">
        <div class="modal-content">
          <div class="modal-header">
            <h5 class="modal-title">
              <i class="fa-solid fa-plus-circle me-2 text-primary"></i>
              {{ 'Manager:Booking:CreateDirectTitle' | abpLocalization }}
            </h5>
            <button type="button" class="btn-close" (click)="close()"></button>
          </div>

          <form [formGroup]="form" (ngSubmit)="submit()">
            <div class="modal-body">
              <div *ngIf="errorMessage()" class="alert alert-danger mb-3">
                {{ errorMessage() }}
              </div>

              <!-- Thông tin khách / doanh nghiệp -->
              <div class="row g-2 mb-3">
                <div class="col-md-6">
                  <label class="form-label">
                    {{ 'Manager:Guest:Name' | abpLocalization }}
                    <span class="text-danger">*</span>
                  </label>
                  <input type="text" class="form-control" formControlName="guestFullName" placeholder="Họ và tên khách / Đại diện đoàn" />
                </div>
                <div class="col-md-6">
                  <label class="form-label">
                    {{ 'Manager:Guest:Phone' | abpLocalization }}
                    <span class="text-danger">*</span>
                  </label>
                  <input type="text" class="form-control" formControlName="guestPhone" placeholder="Số điện thoại liên hệ" />
                </div>
                <div class="col-md-6">
                  <label class="form-label">{{ 'Manager:Guest:Email' | abpLocalization }}</label>
                  <input type="email" class="form-control" formControlName="guestEmail" placeholder="Email nhận hóa đơn/xác nhận" />
                </div>
                <div class="col-md-6">
                  <label class="form-label">{{ 'Manager:Guest:CCCD' | abpLocalization }} / Passport</label>
                  <input type="text" class="form-control" formControlName="guestIdentityNumber" placeholder="Số giấy tờ tùy thân" />
                </div>
              </div>

              <!-- Chi tiết phòng & Lịch lưu trú -->
              <div class="row g-2 mb-3">
                <div class="col-md-6">
                  <label class="form-label">
                    {{ 'Manager:Room:Type' | abpLocalization }}
                    <span class="text-danger">*</span>
                  </label>
                  <bh-searchable-select
                    formControlName="roomTypeId"
                    [options]="roomTypeOptions"
                    [placeholder]="roomTypes.length === 0 ? ('Manager:Room:NoRoomTypesAvailable' | abpLocalization) : ('Manager:Room:SelectRoomType' | abpLocalization)"
                    [emptyText]="'Manager:Room:NoRoomTypesAvailable' | abpLocalization"
                    [clearable]="false"
                    [searchable]="roomTypes.length > 5"
                    (selectionChange)="onRoomTypeSelect()"
                  ></bh-searchable-select>
                </div>
                <div class="col-md-6">
                  <label class="form-label">
                    {{ 'Manager:Booking:NumberOfRooms' | abpLocalization }}
                    <span class="text-danger">*</span>
                  </label>
                  <input type="number" class="form-control" formControlName="numberOfRooms" min="1" max="50" />
                </div>
                <div class="col-md-6">
                  <label class="form-label">
                    {{ 'Manager:Booking:CheckInDate' | abpLocalization }}
                    <span class="text-danger">*</span>
                  </label>
                  <input type="date" class="form-control" formControlName="checkInDate" />
                </div>
                <div class="col-md-6">
                  <label class="form-label">
                    {{ 'Manager:Booking:CheckOutDate' | abpLocalization }}
                    <span class="text-danger">*</span>
                  </label>
                  <input type="date" class="form-control" formControlName="checkOutDate" />
                </div>
              </div>

              <!-- Giá thỏa thuận & Tiền cọc thu trước -->
              <div class="row g-2 mb-3">
                <div class="col-md-6">
                  <label class="form-label">
                    {{ 'Manager:Booking:AgreedRatePerNight' | abpLocalization }} (VNĐ)
                    <span class="text-danger">*</span>
                  </label>
                  <input type="number" class="form-control" formControlName="agreedRatePerNight" min="0" step="50000" />
                </div>
                <div class="col-md-6">
                  <label class="form-label">
                    {{ 'Manager:Booking:DepositAmountReceived' | abpLocalization }} (VNĐ)
                    <span class="text-danger">*</span>
                  </label>
                  <input type="number" class="form-control" formControlName="depositAmountReceived" min="0" step="50000" />
                </div>
              </div>

              <!-- Ghi chú hợp đồng & Yêu cầu đặc biệt -->
              <div class="mb-3">
                <label class="form-label">{{ 'Manager:Booking:ContractNotes' | abpLocalization }}</label>
                <textarea class="form-control" rows="2" formControlName="contractNotes" placeholder="Ghi chú xuất hóa đơn GTGT, thanh toán chuyển khoản doanh nghiệp..."></textarea>
              </div>

              <div class="mb-3">
                <label class="form-label">{{ 'Manager:Booking:SpecialRequests' | abpLocalization }}</label>
                <input type="text" class="form-control" formControlName="specialRequests" placeholder="Yêu cầu phòng liền kề, tầng cao, đồ uống chào đón..." />
              </div>
            </div>

            <div class="modal-footer">
              <button type="button" class="btn btn-outline-secondary" (click)="close()" [disabled]="isSubmitting()">
                {{ 'Common:Cancel' | abpLocalization }}
              </button>
              <button type="submit" class="btn btn-primary" [disabled]="form.invalid || isSubmitting()">
                <i *ngIf="isSubmitting()" class="fa-solid fa-spinner fa-spin me-1"></i>
                {{ 'Manager:Booking:CreateBookingButton' | abpLocalization }}
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
      max-width: 650px;
      margin: 20px;
    }
    .modal-content {
      background: #ffffff;
      border-radius: 12px;
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
      max-height: 75vh;
      overflow-y: auto;
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
export class DirectBookingModalComponent implements OnChanges {
  private fb = inject(FormBuilder);
  private bookingService = inject(ManagerBookingService);

  @Input() isOpen = false;
  @Input() hotelId = '';
  @Input() roomTypes: RoomTypeDto[] = [];

  @Output() closed = new EventEmitter<void>();
  @Output() created = new EventEmitter<ManagerBookingDto>();

  readonly isSubmitting = signal<boolean>(false);
  readonly errorMessage = signal<string | null>(null);

  get roomTypeOptions(): SelectOption[] {
    if (!this.roomTypes || this.roomTypes.length === 0) {
      return [
        {
          value: '',
          label: 'Chưa có hạng phòng nào',
          sublabel: 'Vui lòng tạo hạng phòng trước',
          icon: 'fa-solid fa-circle-exclamation text-warning',
          disabled: true,
        },
      ];
    }
    return this.roomTypes.map(t => ({
      value: t.id,
      label: t.name,
      sublabel: `Niêm yết: ${Number(t.basePrice).toLocaleString()} VNĐ`,
      icon: 'fa-solid fa-door-open',
    }));
  }

  form = this.fb.group({
    guestFullName: ['', [Validators.required, Validators.maxLength(128)]],
    guestPhone: ['', [Validators.required, Validators.maxLength(32)]],
    guestEmail: [''],
    guestIdentityNumber: [''],
    roomTypeId: ['', [Validators.required]],
    checkInDate: [new Date().toISOString().substring(0, 10), [Validators.required]],
    checkOutDate: [new Date(Date.now() + 86400000).toISOString().substring(0, 10), [Validators.required]],
    numberOfRooms: [1, [Validators.required, Validators.min(1), Validators.max(50)]],
    agreedRatePerNight: [1500000, [Validators.required, Validators.min(0)]],
    depositAmountReceived: [500000, [Validators.required, Validators.min(0)]],
    contractNotes: [''],
    specialRequests: [''],
  });

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['roomTypes'] && this.roomTypes.length > 0) {
      if (!this.form.get('roomTypeId')?.value) {
        this.form.patchValue({
          roomTypeId: this.roomTypes[0].id,
          agreedRatePerNight: this.roomTypes[0].basePrice,
        });
      }
    }
  }

  onRoomTypeSelect(): void {
    const selectedId = this.form.get('roomTypeId')?.value;
    const rt = this.roomTypes.find(t => t.id === selectedId);
    if (rt) {
      this.form.patchValue({ agreedRatePerNight: rt.basePrice });
    }
  }

  submit(): void {
    if (this.form.invalid || !this.hotelId) return;

    this.isSubmitting.set(true);
    this.errorMessage.set(null);

    const val = this.form.value;
    const input: CreateDirectBookingDto = {
      hotelId: this.hotelId,
      guestFullName: val.guestFullName!,
      guestPhone: val.guestPhone!,
      guestEmail: val.guestEmail || undefined,
      guestIdentityNumber: val.guestIdentityNumber || undefined,
      roomTypeId: val.roomTypeId!,
      checkInDate: new Date(val.checkInDate!).toISOString(),
      checkOutDate: new Date(val.checkOutDate!).toISOString(),
      numberOfRooms: Number(val.numberOfRooms),
      agreedRatePerNight: Number(val.agreedRatePerNight),
      depositAmountReceived: Number(val.depositAmountReceived),
      contractNotes: val.contractNotes || undefined,
      specialRequests: val.specialRequests || undefined,
    };

    this.bookingService.createDirectBooking(input).subscribe({
      next: res => {
        this.isSubmitting.set(false);
        this.created.emit(res);
        this.close();
      },
      error: err => {
        this.isSubmitting.set(false);
        this.errorMessage.set(err.error?.error?.message || err.message || 'Lỗi tạo đặt phòng trực tiếp');
      },
    });
  }

  close(): void {
    this.errorMessage.set(null);
    this.closed.emit();
  }
}
