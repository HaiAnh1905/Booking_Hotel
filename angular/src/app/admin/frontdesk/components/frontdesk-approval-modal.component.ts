import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LocalizationPipe, LocalizationService } from '@abp/ng.core';
import {
  ApprovalTicketType,
  OperationalApprovalService,
} from '../../services/operational-approval.service';
import { BhToastrService } from '../../../shared/components/toastr/bh-toastr.service';
import { BhModalComponent } from '../../../shared/components/modal/bh-modal.component';

@Component({
  selector: 'bh-frontdesk-approval-modal',
  standalone: true,
  imports: [CommonModule, FormsModule, LocalizationPipe, BhModalComponent],
  templateUrl: './frontdesk-approval-modal.component.html',
  styleUrls: ['./frontdesk-approval-modal.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FrontdeskApprovalModalComponent {
  @Input() isOpen = false;
  @Input() hotelId = '';
  @Input() roomNumber = '';
  @Output() close = new EventEmitter<void>();
  @Output() submitted = new EventEmitter<void>();

  private approvalService = inject(OperationalApprovalService);
  private toastr = inject(BhToastrService);
  private localization = inject(LocalizationService);

  readonly ticketType = signal<ApprovalTicketType>(ApprovalTicketType.DiscountOverride);
  readonly amount = signal<number | null>(null);
  readonly discountPercent = signal<number | null>(10);
  readonly reason = signal<string>('');
  readonly isSubmitting = signal<boolean>(false);

  readonly ticketTypes = [
    { value: ApprovalTicketType.DiscountOverride, labelKey: 'ApprovalTicketType:DiscountOverride' },
    { value: ApprovalTicketType.PenaltyWaiver, labelKey: 'ApprovalTicketType:PenaltyWaiver' },
    { value: ApprovalTicketType.EarlyCheckIn, labelKey: 'ApprovalTicketType:EarlyCheckIn' },
    { value: ApprovalTicketType.LateCheckOut, labelKey: 'ApprovalTicketType:LateCheckOut' },
    { value: ApprovalTicketType.RoomUpgrade, labelKey: 'ApprovalTicketType:RoomUpgrade' },
  ];

  onSubmit(): void {
    if (!this.reason().trim() || this.reason().trim().length < 5) {
      this.toastr.warning(this.localization.instant('Approvals:RejectionReasonRequired'));
      return;
    }

    this.isSubmitting.set(true);

    this.approvalService
      .create({
        hotelId: this.hotelId,
        ticketType: Number(this.ticketType()),
        roomNumber: this.roomNumber,
        amount: this.amount() || undefined,
        discountPercent: this.discountPercent() || undefined,
        reason: this.reason().trim(),
      })
      .subscribe({
        next: () => {
          this.isSubmitting.set(false);
          this.toastr.success(this.localization.instant('Approvals:CreateSuccess'));
          this.submitted.emit();
          this.onCancel();
        },
        error: (err) => {
          this.isSubmitting.set(false);
          this.toastr.error(err?.error?.error?.message || this.localization.instant('Common:NoData'));
        },
      });
  }

  onCancel(): void {
    this.reason.set('');
    this.amount.set(null);
    this.discountPercent.set(10);
    this.close.emit();
  }
}
