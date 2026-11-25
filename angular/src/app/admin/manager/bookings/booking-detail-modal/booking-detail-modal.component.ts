import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LocalizationPipe } from '@abp/ng.core';
import { ManagerBookingService, ManagerBookingDto, BookingStatus, PaymentStatus } from '../../../services/manager/manager-booking.service';

@Component({
  selector: 'bh-booking-detail-modal',
  standalone: true,
  imports: [CommonModule, FormsModule, LocalizationPipe],
  template: `
    <div class="modal-backdrop" *ngIf="isOpen && booking">
      <div class="modal-dialog">
        <div class="modal-content">
          <div class="modal-header">
            <div class="header-left">
              <span class="badge bg-primary me-2 font-monospace">{{ booking.bookingCode }}</span>
              <h5 class="modal-title d-inline">{{ 'Manager:Booking:DetailTitle' | abpLocalization }}</h5>
            </div>
            <button type="button" class="btn-close" (click)="close()"></button>
          </div>

          <div class="modal-body">
            <!-- 3 Trạng thái độc lập -->
            <div class="status-summary-row p-3 bg-light rounded-3 border mb-3">
              <div class="row text-center">
                <div class="col-4">
                  <span class="d-block small text-muted mb-1">{{ 'Manager:Booking:Status' | abpLocalization }}</span>
                  <span class="badge" [ngClass]="getBookingBadgeClass(booking.bookingStatus)">
                    {{ getBookingStatusKey(booking.bookingStatus) | abpLocalization }}
                  </span>
                </div>
                <div class="col-4">
                  <span class="d-block small text-muted mb-1">{{ 'Manager:Booking:PaymentStatus' | abpLocalization }}</span>
                  <span class="badge" [ngClass]="getPaymentBadgeClass(booking.paymentStatus)">
                    {{ getPaymentStatusKey(booking.paymentStatus) | abpLocalization }}
                  </span>
                </div>
                <div class="col-4">
                  <span class="d-block small text-muted mb-1">{{ 'Manager:Booking:RoomAssignment' | abpLocalization }}</span>
                  <span class="badge bg-secondary">
                    {{ (booking.assignedRoomNumber ? ('Phòng ' + booking.assignedRoomNumber) : ('Manager:Booking:RoomAssignment_Unassigned' | abpLocalization)) }}
                  </span>
                </div>
              </div>
            </div>

            <!-- Thông tin khách hàng & CCCD che masked -->
            <div class="detail-section mb-3">
              <h6 class="section-title"><i class="fa-solid fa-user me-2 text-primary"></i> {{ 'Manager:Guest:Info' | abpLocalization }}</h6>
              <div class="row g-2">
                <div class="col-6">
                  <span class="lbl">{{ 'Manager:Guest:Name' | abpLocalization }}:</span>
                  <span class="val">{{ booking.guestFullName }}</span>
                </div>
                <div class="col-6">
                  <span class="lbl">{{ 'Manager:Guest:Phone' | abpLocalization }}:</span>
                  <span class="val">{{ booking.guestPhone }}</span>
                </div>
                <div class="col-6">
                  <span class="lbl">{{ 'Manager:Guest:Email' | abpLocalization }}:</span>
                  <span class="val">{{ booking.guestEmail || '-' }}</span>
                </div>
                <div class="col-6">
                  <span class="lbl">{{ 'Manager:Guest:CCCD' | abpLocalization }}:</span>
                  <span class="val font-monospace">{{ booking.guestIdentityNumberMasked || '***-***-****' }}</span>
                </div>
              </div>
            </div>

            <!-- Chi tiết phòng & Lịch lưu trú -->
            <div class="detail-section mb-3">
              <h6 class="section-title"><i class="fa-solid fa-bed me-2 text-primary"></i> {{ 'Manager:Booking:RoomAndDates' | abpLocalization }}</h6>
              <div class="row g-2">
                <div class="col-6">
                  <span class="lbl">{{ 'Manager:Room:Type' | abpLocalization }}:</span>
                  <span class="val">{{ booking.roomTypeName }}</span>
                </div>
                <div class="col-6">
                  <span class="lbl">{{ 'Manager:Booking:DirectTag' | abpLocalization }}:</span>
                  <span class="val">
                    <span *ngIf="booking.isDirectBooking" class="badge bg-success">{{ 'Manager:Booking:DirectBooking' | abpLocalization }}</span>
                    <span *ngIf="!booking.isDirectBooking" class="badge bg-light text-dark border">{{ 'Manager:Booking:OnlineBooking' | abpLocalization }}</span>
                  </span>
                </div>
                <div class="col-6">
                  <span class="lbl">{{ 'Manager:Booking:CheckInDate' | abpLocalization }}:</span>
                  <span class="val">{{ booking.checkInDate | date:'dd/MM/yyyy' }}</span>
                </div>
                <div class="col-6">
                  <span class="lbl">{{ 'Manager:Booking:CheckOutDate' | abpLocalization }}:</span>
                  <span class="val">{{ booking.checkOutDate | date:'dd/MM/yyyy' }}</span>
                </div>
              </div>
            </div>

            <!-- Dòng tiền & Thanh toán -->
            <div class="detail-section mb-3">
              <h6 class="section-title"><i class="fa-solid fa-receipt me-2 text-primary"></i> {{ 'Manager:Booking:Financials' | abpLocalization }}</h6>
              <div class="row g-2">
                <div class="col-6">
                  <span class="lbl">{{ 'Manager:Booking:TotalAmount' | abpLocalization }}:</span>
                  <span class="val fw-bold text-primary">{{ booking.totalAmount | number:'1.0-0' }} VNĐ</span>
                </div>
                <div class="col-6">
                  <span class="lbl">{{ 'Manager:Booking:DepositAmount' | abpLocalization }}:</span>
                  <span class="val fw-bold text-success">{{ booking.depositAmount | number:'1.0-0' }} VNĐ</span>
                </div>
                <div class="col-12" *ngIf="booking.penaltyAmount > 0">
                  <span class="lbl text-danger">{{ 'Manager:Booking:PenaltyAmount' | abpLocalization }}:</span>
                  <span class="val text-danger fw-bold">{{ booking.penaltyAmount | number:'1.0-0' }} VNĐ</span>
                </div>
                <div class="col-12" *ngIf="booking.waivePenaltyReason">
                  <span class="lbl text-muted">{{ 'Manager:Booking:WaivePenaltyReason' | abpLocalization }}:</span>
                  <span class="val fst-italic">{{ booking.waivePenaltyReason }}</span>
                </div>
              </div>
            </div>

            <!-- Ghi chú hợp đồng & Yêu cầu đặc biệt -->
            <div class="detail-section" *ngIf="booking.contractNotes || booking.specialRequests || booking.rejectionReason">
              <h6 class="section-title"><i class="fa-solid fa-note-sticky me-2 text-primary"></i> {{ 'Common:Notes' | abpLocalization }}</h6>
              <p *ngIf="booking.contractNotes" class="mb-1 small"><strong>Hợp đồng:</strong> {{ booking.contractNotes }}</p>
              <p *ngIf="booking.specialRequests" class="mb-1 small"><strong>Yêu cầu:</strong> {{ booking.specialRequests }}</p>
              <p *ngIf="booking.rejectionReason" class="mb-1 small text-danger"><strong>Lý do từ chối:</strong> {{ booking.rejectionReason }}</p>
            </div>

            <!-- Từ chối Box (nếu đang bấm Từ chối) -->
            <div *ngIf="isRejecting()" class="reject-box mt-3 p-3 bg-danger-subtle rounded-3 border border-danger">
              <label class="form-label small fw-bold text-danger">Lý do từ chối booking <span class="text-danger">*</span></label>
              <textarea class="form-control form-control-sm mb-2" rows="2" [(ngModel)]="rejectReason"></textarea>
              <div class="d-flex justify-content-end gap-2">
                <button type="button" class="btn btn-sm btn-outline-secondary" (click)="isRejecting.set(false)">Hủy</button>
                <button type="button" class="btn btn-sm btn-danger" [disabled]="!rejectReason" (click)="confirmReject()">Xác Nhận Từ Chối</button>
              </div>
            </div>
          </div>

          <div class="modal-footer">
            <!-- Nút duyệt/từ chối nếu PendingConfirmation -->
            <ng-container *ngIf="booking.bookingStatus === BookingStatus.PendingConfirmation && !isRejecting()">
              <button type="button" class="btn btn-outline-danger" (click)="isRejecting.set(true)">
                <i class="fa-solid fa-xmark me-1"></i> {{ 'Manager:Booking:Reject' | abpLocalization }}
              </button>
              <button type="button" class="btn btn-success" (click)="confirmBooking()">
                <i class="fa-solid fa-check me-1"></i> {{ 'Manager:Booking:Confirm' | abpLocalization }}
              </button>
            </ng-container>

            <!-- Nút No-Show nếu Confirmed -->
            <button
              type="button"
              class="btn btn-warning"
              *ngIf="booking.bookingStatus === BookingStatus.Confirmed"
              (click)="triggerNoShow()">
              <i class="fa-solid fa-user-xmark me-1"></i> {{ 'Manager:Booking:MarkNoShow' | abpLocalization }}
            </button>

            <button type="button" class="btn btn-outline-secondary" (click)="close()">
              {{ 'Common:Close' | abpLocalization }}
            </button>
          </div>
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
      max-width: 620px;
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
    .detail-section {
      border-bottom: 1px solid #f1f5f9;
      padding-bottom: 12px;
      .section-title {
        font-size: 0.875rem;
        font-weight: 700;
        color: #0f172a;
        margin-bottom: 8px;
      }
      .lbl {
        font-size: 0.8125rem;
        color: #64748b;
        display: block;
      }
      .val {
        font-size: 0.875rem;
        color: #0f172a;
        font-weight: 500;
      }
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BookingDetailModalComponent {
  private bookingService = inject(ManagerBookingService);

  readonly BookingStatus = BookingStatus;
  readonly PaymentStatus = PaymentStatus;

  @Input() isOpen = false;
  @Input() booking: ManagerBookingDto | null = null;
  @Input() hotelId = '';

  @Output() closed = new EventEmitter<void>();
  @Output() statusUpdated = new EventEmitter<ManagerBookingDto>();
  @Output() noShowTriggered = new EventEmitter<ManagerBookingDto>();

  isRejecting = signal<boolean>(false);
  rejectReason = '';

  confirmBooking(): void {
    if (!this.booking || !this.hotelId) return;

    this.bookingService.confirm(this.booking.id, this.hotelId).subscribe({
      next: updated => {
        this.statusUpdated.emit(updated);
        this.close();
      },
      error: err => alert(err.error?.error?.message || 'Lỗi xác nhận booking'),
    });
  }

  confirmReject(): void {
    if (!this.booking || !this.hotelId || !this.rejectReason) return;

    this.bookingService.reject(this.booking.id, {
      hotelId: this.hotelId,
      reason: this.rejectReason,
    }).subscribe({
      next: updated => {
        this.isRejecting.set(false);
        this.statusUpdated.emit(updated);
        this.close();
      },
      error: err => alert(err.error?.error?.message || 'Lỗi từ chối booking'),
    });
  }

  triggerNoShow(): void {
    if (!this.booking) return;
    this.noShowTriggered.emit(this.booking);
    this.close();
  }

  close(): void {
    this.isRejecting.set(false);
    this.rejectReason = '';
    this.closed.emit();
  }

  getBookingBadgeClass(status: BookingStatus): string {
    switch (status) {
      case BookingStatus.Confirmed: return 'bg-primary';
      case BookingStatus.CheckedIn: return 'bg-success';
      case BookingStatus.Completed: return 'bg-info text-dark';
      case BookingStatus.PendingConfirmation: return 'bg-warning text-dark';
      case BookingStatus.Rejected:
      case BookingStatus.Cancelled:
      case BookingStatus.NoShow: return 'bg-danger';
      default: return 'bg-secondary';
    }
  }

  getBookingStatusKey(status: BookingStatus): string {
    switch (status) {
      case BookingStatus.Confirmed: return 'Manager:Booking:Status_Confirmed';
      case BookingStatus.CheckedIn: return 'Manager:Booking:Status_CheckedIn';
      case BookingStatus.Completed: return 'Manager:Booking:Status_Completed';
      case BookingStatus.PendingConfirmation: return 'Manager:Booking:Status_Pending';
      case BookingStatus.Rejected: return 'Manager:Booking:Status_Rejected';
      case BookingStatus.Cancelled: return 'Manager:Booking:Status_Cancelled';
      case BookingStatus.NoShow: return 'Manager:Booking:Status_NoShow';
      default: return '';
    }
  }

  getPaymentBadgeClass(status: PaymentStatus): string {
    switch (status) {
      case PaymentStatus.Paid: return 'bg-success';
      case PaymentStatus.PartiallyPaid: return 'bg-warning text-dark';
      case PaymentStatus.Refunded: return 'bg-info text-dark';
      default: return 'bg-danger';
    }
  }

  getPaymentStatusKey(status: PaymentStatus): string {
    switch (status) {
      case PaymentStatus.Paid: return 'Manager:Booking:PaymentStatus_Paid';
      case PaymentStatus.PartiallyPaid: return 'Manager:Booking:PaymentStatus_PartiallyPaid';
      case PaymentStatus.Refunded: return 'Manager:Booking:PaymentStatus_Refunded';
      default: return 'Manager:Booking:PaymentStatus_Unpaid';
    }
  }
}
