import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LocalizationPipe } from '@abp/ng.core';
import { BhModalComponent } from '../../../shared/components/modal/bh-modal.component';
import { BhModalConfig } from '../../../shared/models/ui-models';
import { TenantBookingSurveillanceDto } from '../../services/tenant-dashboard.service';

@Component({
  selector: 'bh-booking-detail-modal',
  standalone: true,
  imports: [CommonModule, LocalizationPipe, BhModalComponent],
  template: `
    <bh-modal
      [isOpen]="isOpen"
      [config]="modalConfig"
      (confirm)="onClose()"
      (cancel)="onClose()"
    >
      <div *ngIf="booking" class="booking-detail-content">
        <!-- Notice banner -->
        <div class="alert alert-info d-flex align-items-center mb-3 py-2">
          <i class="fa-solid fa-eye me-2 fa-lg text-info"></i>
          <span class="small">{{ 'TenantBookings:ReadOnlyNotice' | abpLocalization }}</span>
        </div>

        <div class="row g-3">
          <div class="col-md-6">
            <label class="form-label text-muted small">{{ 'TenantBookings:ColBookingCode' | abpLocalization }}</label>
            <div class="fw-bold font-monospace text-primary fs-5">{{ booking.bookingCode }}</div>
          </div>

          <div class="col-md-6">
            <label class="form-label text-muted small">{{ 'TenantBookings:ColHotel' | abpLocalization }}</label>
            <div class="fw-semibold text-dark">{{ booking.hotelName }} ({{ booking.hotelCode }})</div>
          </div>

          <div class="col-md-6">
            <label class="form-label text-muted small">{{ 'TenantBookings:ColCustomer' | abpLocalization }}</label>
            <div class="fw-semibold text-dark">{{ booking.customerName }}</div>
            <div class="small text-muted">{{ booking.customerPhone }}</div>
          </div>

          <div class="col-md-6">
            <label class="form-label text-muted small">{{ 'TenantBookings:ColDates' | abpLocalization }}</label>
            <div class="fw-semibold text-dark">
              {{ booking.checkInDate | date:'dd/MM/yyyy' }} — {{ booking.checkOutDate | date:'dd/MM/yyyy' }}
            </div>
          </div>

          <div class="col-md-6">
            <label class="form-label text-muted small">{{ 'TenantBookings:ColRoomInfo' | abpLocalization }}</label>
            <div class="fw-semibold text-dark">
              {{ booking.roomTypeName }}
              <span *ngIf="booking.roomNumber" class="badge bg-secondary ms-1">{{ 'TenantBookings:RoomNumber' | abpLocalization }} {{ booking.roomNumber }}</span>
            </div>
          </div>

          <div class="col-md-6">
            <label class="form-label text-muted small">{{ 'TenantBookings:ColTotalAmount' | abpLocalization }}</label>
            <div class="fw-bold text-success fs-5">
              {{ booking.totalAmount | currency:'VND':'symbol':'1.0-0':'vi-VN' }}
            </div>
          </div>

          <div class="col-md-6">
            <label class="form-label text-muted small">{{ 'TenantBookings:ColStatus' | abpLocalization }}</label>
            <div>
              <span class="badge bg-success-subtle text-success fs-6">{{ booking.status }}</span>
            </div>
          </div>

          <div class="col-md-6">
            <label class="form-label text-muted small">{{ 'TenantBookings:ColPaymentStatus' | abpLocalization }}</label>
            <div>
              <span class="badge bg-primary-subtle text-primary fs-6">{{ booking.paymentStatus }}</span>
            </div>
          </div>
        </div>
      </div>
    </bh-modal>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BookingDetailModalComponent {
  @Input() isOpen = false;
  @Input() booking: TenantBookingSurveillanceDto | null = null;
  @Output() close = new EventEmitter<void>();

  get modalConfig(): BhModalConfig {
    return {
      titleKey: 'TenantBookings:DetailTitle',
      confirmTextKey: 'Common:Close',
      confirmVariant: 'secondary',
      hideCancelButton: true,
      size: 'lg',
      iconClass: 'fa-solid fa-file-invoice',
    };
  }

  onClose(): void {
    this.close.emit();
  }
}
