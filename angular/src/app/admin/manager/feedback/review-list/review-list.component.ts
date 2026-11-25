import { ChangeDetectionStrategy, Component, OnInit, inject, signal, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule, Router } from '@angular/router';
import { LocalizationPipe } from '@abp/ng.core';
import { PropertyContextService } from '../../../services/property-context.service';
import { ManagerFeedbackService, ReviewDto, ReplyReviewDto } from '../../../services/manager/manager-feedback.service';

@Component({
  selector: 'bh-review-list',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, LocalizationPipe],
  templateUrl: './review-list.component.html',
  styleUrls: ['./review-list.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ReviewListComponent implements OnInit {
  private feedbackService = inject(ManagerFeedbackService);
  readonly propertyService = inject(PropertyContextService);
  private router = inject(Router);

  reviews = signal<ReviewDto[]>([]);
  isLoading = signal<boolean>(false);
  minScoreFilter = signal<number | undefined>(undefined);

  // Modal State
  isReplyModalOpen = signal<boolean>(false);
  selectedReview = signal<ReviewDto | null>(null);
  replyContent = signal<string>('');
  isSubmitting = signal<boolean>(false);
  errorMessage = signal<string>('');
  successMessage = signal<string>('');

  constructor() {
    effect(() => {
      const hotelId = this.propertyService.currentHotelId();
      if (hotelId) {
        this.loadReviews();
      }
    });
  }

  ngOnInit(): void {}

  loadReviews(): void {
    const hotelId = this.propertyService.currentHotelId();
    if (!hotelId) return;

    this.isLoading.set(true);
    this.errorMessage.set('');

    this.feedbackService.getReviews(hotelId, this.minScoreFilter()).subscribe({
      next: data => {
        this.reviews.set(data || []);
        this.isLoading.set(false);
      },
      error: err => {
        this.errorMessage.set(err.message || 'Error loading reviews');
        this.isLoading.set(false);
      }
    });
  }

  onFilterChange(score?: number): void {
    this.minScoreFilter.set(score);
    this.loadReviews();
  }

  openReplyModal(review: ReviewDto): void {
    this.selectedReview.set(review);
    this.replyContent.set(review.officialReply || '');
    this.isReplyModalOpen.set(true);
    this.errorMessage.set('');
    this.successMessage.set('');
  }

  closeReplyModal(): void {
    this.isReplyModalOpen.set(false);
    this.selectedReview.set(null);
    this.replyContent.set('');
  }

  submitReply(): void {
    const review = this.selectedReview();
    const hotelId = this.propertyService.currentHotelId();
    if (!review || !hotelId) return;

    const content = this.replyContent().trim();
    if (!content) {
      this.errorMessage.set('Vui lòng nhập nội dung phản hồi');
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set('');

    const input: ReplyReviewDto = {
      hotelId,
      replyContent: content
    };

    this.feedbackService.replyToReview(review.id, input).subscribe({
      next: updated => {
        this.isSubmitting.set(false);
        this.closeReplyModal();
        this.loadReviews();
      },
      error: err => {
        this.isSubmitting.set(false);
        this.errorMessage.set(err.message || 'Error submitting reply');
      }
    });
  }

  navigateToComplaints(): void {
    this.router.navigate(['/manager/complaints'], {
      queryParams: { hotelId: this.propertyService.currentHotelId() }
    });
  }

  getStarArray(score: number): number[] {
    return Array.from({ length: 5 }, (_, i) => i + 1);
  }
}
