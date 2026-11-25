import { ChangeDetectionStrategy, Component, OnInit, inject, signal, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule, Router } from '@angular/router';
import { LocalizationPipe } from '@abp/ng.core';
import { PropertyContextService } from '../../../services/property-context.service';
import {
  ManagerFeedbackService,
  ComplaintDto,
  ComplaintStatus,
  CreateComplaintDto,
  UpdateComplaintStatusDto
} from '../../../services/manager/manager-feedback.service';

@Component({
  selector: 'bh-complaint-list',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, LocalizationPipe],
  templateUrl: './complaint-list.component.html',
  styleUrls: ['./complaint-list.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ComplaintListComponent implements OnInit {
  private feedbackService = inject(ManagerFeedbackService);
  readonly propertyService = inject(PropertyContextService);
  private router = inject(Router);

  ComplaintStatus = ComplaintStatus;

  complaints = signal<ComplaintDto[]>([]);
  isLoading = signal<boolean>(false);
  selectedStatusFilter = signal<ComplaintStatus | undefined>(undefined);

  // Update Status Modal
  isUpdateModalOpen = signal<boolean>(false);
  selectedComplaint = signal<ComplaintDto | null>(null);
  targetStatus = signal<ComplaintStatus>(ComplaintStatus.InInvestigation);
  resolutionNotes = signal<string>('');

  // Create Modal
  isCreateModalOpen = signal<boolean>(false);
  newTitle = signal<string>('');
  newDescription = signal<string>('');
  newPriority = signal<string>('Medium');
  newBookingCode = signal<string>('');

  isSubmitting = signal<boolean>(false);
  errorMessage = signal<string>('');

  constructor() {
    effect(() => {
      const hotelId = this.propertyService.currentHotelId();
      if (hotelId) {
        this.loadComplaints();
      }
    });
  }

  ngOnInit(): void {}

  loadComplaints(): void {
    const hotelId = this.propertyService.currentHotelId();
    if (!hotelId) return;

    this.isLoading.set(true);
    this.errorMessage.set('');

    this.feedbackService.getComplaints(hotelId, this.selectedStatusFilter()).subscribe({
      next: data => {
        this.complaints.set(data || []);
        this.isLoading.set(false);
      },
      error: err => {
        this.errorMessage.set(err.message || 'Error loading complaints');
        this.isLoading.set(false);
      }
    });
  }

  onFilterStatus(status?: ComplaintStatus): void {
    this.selectedStatusFilter.set(status);
    this.loadComplaints();
  }

  navigateToReviews(): void {
    this.router.navigate(['/manager/feedback'], {
      queryParams: { hotelId: this.propertyService.currentHotelId() }
    });
  }

  openUpdateModal(c: ComplaintDto): void {
    this.selectedComplaint.set(c);
    this.targetStatus.set(c.status === ComplaintStatus.Open ? ComplaintStatus.InInvestigation : ComplaintStatus.Resolved);
    this.resolutionNotes.set(c.resolutionNotes || '');
    this.errorMessage.set('');
    this.isUpdateModalOpen.set(true);
  }

  closeUpdateModal(): void {
    this.isUpdateModalOpen.set(false);
    this.selectedComplaint.set(null);
  }

  submitUpdateStatus(): void {
    const complaint = this.selectedComplaint();
    const hotelId = this.propertyService.currentHotelId();
    if (!complaint || !hotelId) return;

    this.isSubmitting.set(true);
    this.errorMessage.set('');

    const input: UpdateComplaintStatusDto = {
      hotelId,
      targetStatus: Number(this.targetStatus()),
      resolutionNotes: this.resolutionNotes().trim() || undefined
    };

    this.feedbackService.updateComplaintStatus(complaint.id, input).subscribe({
      next: () => {
        this.isSubmitting.set(false);
        this.closeUpdateModal();
        this.loadComplaints();
      },
      error: err => {
        this.isSubmitting.set(false);
        this.errorMessage.set(err.message || 'Error updating complaint status');
      }
    });
  }

  openCreateModal(): void {
    this.newTitle.set('');
    this.newDescription.set('');
    this.newPriority.set('Medium');
    this.newBookingCode.set('');
    this.errorMessage.set('');
    this.isCreateModalOpen.set(true);
  }

  closeCreateModal(): void {
    this.isCreateModalOpen.set(false);
  }

  submitCreate(): void {
    const hotelId = this.propertyService.currentHotelId();
    if (!hotelId) return;

    const title = this.newTitle().trim();
    const desc = this.newDescription().trim();

    if (!title || !desc) {
      this.errorMessage.set('Vui lòng điền đầy đủ các trường bắt buộc');
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set('');

    const input: CreateComplaintDto = {
      hotelId,
      title,
      description: desc,
      priority: this.newPriority()
    };

    this.feedbackService.createComplaint(input).subscribe({
      next: () => {
        this.isSubmitting.set(false);
        this.closeCreateModal();
        this.loadComplaints();
      },
      error: err => {
        this.isSubmitting.set(false);
        this.errorMessage.set(err.message || 'Error creating complaint');
      }
    });
  }

  getStatusBadgeClass(status: ComplaintStatus): string {
    switch (status) {
      case ComplaintStatus.Open:
        return 'bg-info text-dark';
      case ComplaintStatus.InInvestigation:
        return 'bg-warning text-dark';
      case ComplaintStatus.Resolved:
        return 'bg-success text-white';
      case ComplaintStatus.Escalated:
        return 'bg-danger text-white';
      default:
        return 'bg-secondary text-white';
    }
  }

  getStatusLabelKey(status: ComplaintStatus): string {
    switch (status) {
      case ComplaintStatus.Open:
        return 'Manager:Complaint:StatusOpen';
      case ComplaintStatus.InInvestigation:
        return 'Manager:Complaint:StatusInInvestigation';
      case ComplaintStatus.Resolved:
        return 'Manager:Complaint:StatusResolved';
      case ComplaintStatus.Escalated:
        return 'Manager:Complaint:StatusEscalated';
      default:
        return 'Common:Unknown';
    }
  }

  getPriorityBadgeClass(priority: string): string {
    switch (priority?.toLowerCase()) {
      case 'urgent':
        return 'bg-danger text-white';
      case 'high':
        return 'bg-warning text-dark';
      case 'low':
        return 'bg-secondary text-white';
      default:
        return 'bg-primary text-white';
    }
  }
}
