import { Injectable, inject } from '@angular/core';
import { RestService } from '@abp/ng.core';
import { Observable } from 'rxjs';

export enum ComplaintStatus {
  Open = 1,
  InInvestigation = 2,
  Resolved = 3,
  Escalated = 4
}

export interface ReviewDto {
  id: string;
  hotelId: string;
  bookingCode: string;
  guestFullName: string;
  cleanlinessScore: number;
  staffScore: number;
  amenitiesScore: number;
  locationScore: number;
  overallScore: number;
  comment: string;
  creationTime: string;
  officialReply?: string;
  repliedByName?: string;
  repliedAt?: string;
}

export interface ReplyReviewDto {
  hotelId: string;
  replyContent: string;
}

export interface ComplaintDto {
  id: string;
  hotelId: string;
  title: string;
  description: string;
  guestFullName: string;
  guestPhone: string;
  bookingCode?: string;
  priority: string;
  status: ComplaintStatus;
  creationTime: string;
  slaDeadline: string;
  hoursRemaining: number;
  isOverdue: boolean;
  escalatedAt?: string;
  resolutionNotes?: string;
  resolvedByName?: string;
  resolvedAt?: string;
}

export interface CreateComplaintDto {
  hotelId: string;
  title: string;
  description: string;
  priority?: string;
  guestId?: string;
  bookingId?: string;
}

export interface UpdateComplaintStatusDto {
  hotelId: string;
  targetStatus: ComplaintStatus;
  resolutionNotes?: string;
}

@Injectable({
  providedIn: 'root'
})
export class ManagerFeedbackService {
  private rest = inject(RestService);

  getReviews(hotelId: string, minScore?: number): Observable<ReviewDto[]> {
    return this.rest.request<void, ReviewDto[]>({
      method: 'GET',
      url: `/api/app/manager/feedback/reviews`,
      params: { hotelId, minScore }
    });
  }

  replyToReview(reviewId: string, input: ReplyReviewDto): Observable<ReviewDto> {
    return this.rest.request<ReplyReviewDto, ReviewDto>({
      method: 'POST',
      url: `/api/app/manager/feedback/reviews/${reviewId}/reply`,
      body: input
    });
  }

  getComplaints(hotelId: string, status?: ComplaintStatus): Observable<ComplaintDto[]> {
    return this.rest.request<void, ComplaintDto[]>({
      method: 'GET',
      url: `/api/app/manager/feedback/complaints`,
      params: { hotelId, status }
    });
  }

  createComplaint(input: CreateComplaintDto): Observable<ComplaintDto> {
    return this.rest.request<CreateComplaintDto, ComplaintDto>({
      method: 'POST',
      url: `/api/app/manager/feedback/complaints`,
      body: input
    });
  }

  updateComplaintStatus(complaintId: string, input: UpdateComplaintStatusDto): Observable<ComplaintDto> {
    return this.rest.request<UpdateComplaintStatusDto, ComplaintDto>({
      method: 'PUT',
      url: `/api/app/manager/feedback/complaints/${complaintId}/status`,
      body: input
    });
  }
}
