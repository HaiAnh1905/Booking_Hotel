import { ChangeDetectionStrategy, Component, effect, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LocalizationPipe, LocalizationService } from '@abp/ng.core';
import {
  ApprovalTicketStatus,
  ApprovalTicketType,
  OperationalApprovalService,
  OperationalApprovalTicketDto,
} from '../../services/operational-approval.service';
import { PropertyContextService } from '../../services/property-context.service';
import { BhToastrService } from '../../../shared/components/toastr/bh-toastr.service';
import { BhModalComponent } from '../../../shared/components/modal/bh-modal.component';
import { BhConfirmService } from '../../../shared/components/confirm-modal/bh-confirm.service';

@Component({
  selector: 'bh-manager-approvals',
  standalone: true,
  imports: [CommonModule, FormsModule, LocalizationPipe, BhModalComponent],
  templateUrl: './manager-approvals.component.html',
  styleUrls: ['./manager-approvals.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ManagerApprovalsComponent {
  private approvalService = inject(OperationalApprovalService);
  readonly propertyService = inject(PropertyContextService);
  private toastr = inject(BhToastrService);
  private localization = inject(LocalizationService);
  private confirmService = inject(BhConfirmService);

  readonly tickets = signal<OperationalApprovalTicketDto[]>([]);
  readonly isLoading = signal<boolean>(false);
  readonly selectedStatusFilter = signal<number | null>(null);

  // Modal từ chối
  readonly isRejectModalOpen = signal<boolean>(false);
  readonly selectedTicket = signal<OperationalApprovalTicketDto | null>(null);
  rejectReason = '';

  constructor() {
    // Phản ứng khi cơ sở khách sạn đang chọn thay đổi
    effect(() => {
      const hotelId = this.propertyService.currentHotelId();
      if (hotelId) {
        this.loadTickets();
      }
    });
  }

  loadTickets(): void {
    const hotelId = this.propertyService.currentHotelId();
    this.isLoading.set(true);

    this.approvalService
      .getList({
        hotelId: hotelId || undefined,
        status: this.selectedStatusFilter() !== null ? (this.selectedStatusFilter() as ApprovalTicketStatus) : undefined,
        maxResultCount: 50,
      })
      .subscribe({
        next: res => {
          this.tickets.set(res.items || []);
          this.isLoading.set(false);
        },
        error: () => {
          this.tickets.set([]);
          this.isLoading.set(false);
        },
      });
  }

  filterByStatus(status: number | null): void {
    this.selectedStatusFilter.set(status);
    this.loadTickets();
  }

  async onApprove(ticket: OperationalApprovalTicketDto): Promise<void> {
    const confirmed = await this.confirmService.confirm({
      title: this.localization.instant('Common:Confirm'),
      message: this.localization.instant('Approvals:ApproveConfirm'),
      confirmText: this.localization.instant('Common:Confirm'),
      cancelText: this.localization.instant('Common:Cancel'),
      type: 'primary',
    });
    if (!confirmed) return;

    this.approvalService.approve(ticket.id).subscribe({
      next: () => {
        this.toastr.success(
          this.localization.instant('Approvals:ApproveSuccess'),
          this.localization.instant('Approvals:ApprovedTitle')
        );
        this.loadTickets();
      },
      error: err => {
        this.toastr.error(
          err?.error?.error?.message || err?.message || this.localization.instant('Approvals:ApproveFailed'),
          this.localization.instant('Common:Error')
        );
      },
    });
  }

  openRejectModal(ticket: OperationalApprovalTicketDto): void {
    this.selectedTicket.set(ticket);
    this.rejectReason = '';
    this.isRejectModalOpen.set(true);
  }

  closeRejectModal(): void {
    this.isRejectModalOpen.set(false);
    this.selectedTicket.set(null);
  }

  confirmReject(): void {
    const ticket = this.selectedTicket();
    if (!ticket) return;

    if (!this.rejectReason.trim() || this.rejectReason.trim().length < 5) {
      this.toastr.warning(
        this.localization.instant('Approvals:RejectReasonRequired'),
        this.localization.instant('Approvals:ReasonRequiredTitle')
      );
      return;
    }

    this.approvalService
      .reject(ticket.id, { rejectionReason: this.rejectReason.trim() })
      .subscribe({
        next: () => {
          this.toastr.info(
            this.localization.instant('Approvals:RejectSuccess'),
            this.localization.instant('Approvals:RejectedTitle')
          );
          this.closeRejectModal();
          this.loadTickets();
        },
        error: err => {
          this.toastr.error(
            err?.error?.error?.message || err?.message || this.localization.instant('Approvals:RejectFailed'),
            this.localization.instant('Common:Error')
          );
        },
      });
  }

  getTypeLabel(type: ApprovalTicketType): string {
    switch (type) {
      case ApprovalTicketType.DiscountOverride:
        return this.localization.instant('ApprovalTicketType:DiscountOverride');
      case ApprovalTicketType.PenaltyWaiver:
        return this.localization.instant('ApprovalTicketType:PenaltyWaiver');
      case ApprovalTicketType.EarlyCheckIn:
        return this.localization.instant('ApprovalTicketType:EarlyCheckIn');
      case ApprovalTicketType.LateCheckOut:
        return this.localization.instant('ApprovalTicketType:LateCheckOut');
      case ApprovalTicketType.RoomUpgrade:
        return this.localization.instant('ApprovalTicketType:RoomUpgrade');
      default:
        return this.localization.instant('ApprovalTicketType:Other');
    }
  }
}

