import { Injectable, inject } from '@angular/core';
import { RestService, PagedResultDto } from '@abp/ng.core';
import { Observable, Subject } from 'rxjs';
import { tap } from 'rxjs/operators';

export enum ApprovalTicketType {
  DiscountOverride = 1,
  PenaltyWaiver = 2,
  EarlyCheckIn = 3,
  LateCheckOut = 4,
  RoomUpgrade = 5,
}

export enum ApprovalTicketStatus {
  Pending = 0,
  Approved = 1,
  Rejected = 2,
  Cancelled = 3,
}

export interface OperationalApprovalTicketDto {
  id: string;
  tenantId?: string | null;
  hotelId: string;
  hotelName: string;
  ticketType: ApprovalTicketType;
  ticketTypeName: string;
  status: ApprovalTicketStatus;
  statusName: string;
  requestedByStaffId: string;
  requestedByStaffName: string;
  bookingId?: string | null;
  folioId?: string | null;
  roomNumber?: string | null;
  amount?: number | null;
  discountPercent?: number | null;
  reason: string;
  reviewedByManagerId?: string | null;
  reviewedByManagerName?: string | null;
  reviewedAt?: string | null;
  rejectionReason?: string | null;
  creationTime: string;
}

export interface CreateApprovalTicketDto {
  hotelId: string;
  ticketType: ApprovalTicketType;
  bookingId?: string | null;
  folioId?: string | null;
  roomNumber?: string | null;
  amount?: number | null;
  discountPercent?: number | null;
  reason: string;
}

export interface GetApprovalTicketListDto {
  hotelId?: string | null;
  status?: ApprovalTicketStatus | null;
  ticketType?: ApprovalTicketType | null;
  skipCount?: number;
  maxResultCount?: number;
}

export interface ReviewApprovalTicketDto {
  rejectionReason: string;
}

@Injectable({
  providedIn: 'root',
})
export class OperationalApprovalService {
  private rest = inject(RestService);
  private readonly apiPath = '/api/app/operational-approval';
  readonly ticketChanged$ = new Subject<void>();

  getList(input: GetApprovalTicketListDto = {}): Observable<PagedResultDto<OperationalApprovalTicketDto>> {
    return this.rest.request<void, PagedResultDto<OperationalApprovalTicketDto>>({
      method: 'GET',
      url: this.apiPath,
      params: {
        hotelId: input.hotelId || undefined,
        status: input.status !== undefined && input.status !== null ? input.status : undefined,
        ticketType: input.ticketType !== undefined && input.ticketType !== null ? input.ticketType : undefined,
        skipCount: input.skipCount || 0,
        maxResultCount: input.maxResultCount || 10,
      },
    });
  }

  get(id: string): Observable<OperationalApprovalTicketDto> {
    return this.rest.request<void, OperationalApprovalTicketDto>({
      method: 'GET',
      url: `${this.apiPath}/${id}`,
    });
  }

  create(input: CreateApprovalTicketDto): Observable<OperationalApprovalTicketDto> {
    return this.rest
      .request<CreateApprovalTicketDto, OperationalApprovalTicketDto>({
        method: 'POST',
        url: this.apiPath,
        body: input,
      })
      .pipe(tap(() => this.ticketChanged$.next()));
  }

  approve(id: string): Observable<OperationalApprovalTicketDto> {
    return this.rest
      .request<void, OperationalApprovalTicketDto>({
        method: 'POST',
        url: `${this.apiPath}/${id}/approve`,
      })
      .pipe(tap(() => this.ticketChanged$.next()));
  }

  reject(id: string, input: ReviewApprovalTicketDto): Observable<OperationalApprovalTicketDto> {
    return this.rest
      .request<ReviewApprovalTicketDto, OperationalApprovalTicketDto>({
        method: 'POST',
        url: `${this.apiPath}/${id}/reject`,
        body: input,
      })
      .pipe(tap(() => this.ticketChanged$.next()));
  }

  cancel(id: string): Observable<OperationalApprovalTicketDto> {
    return this.rest
      .request<void, OperationalApprovalTicketDto>({
        method: 'POST',
        url: `${this.apiPath}/${id}/cancel`,
      })
      .pipe(tap(() => this.ticketChanged$.next()));
  }
}

