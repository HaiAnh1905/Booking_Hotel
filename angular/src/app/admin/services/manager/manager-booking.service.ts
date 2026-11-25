import { Injectable, inject } from '@angular/core';
import { RestService, PagedResultDto } from '@abp/ng.core';
import { Observable } from 'rxjs';

export enum BookingStatus {
  PendingConfirmation = 1,
  Confirmed = 2,
  CheckedIn = 3,
  Completed = 4,
  Rejected = 5,
  Cancelled = 6,
  NoShow = 7
}

export enum PaymentStatus {
  Unpaid = 1,
  PartiallyPaid = 2,
  Paid = 3,
  Refunded = 4
}

export enum RoomAssignmentStatus {
  Unassigned = 1,
  Assigned = 2
}

export interface ManagerBookingDto {
  id: string;
  hotelId: string;
  bookingCode: string;
  guestFullName: string;
  guestPhone: string;
  guestEmail: string;
  guestIdentityNumberMasked: string;
  roomTypeName: string;
  assignedRoomNumber?: string;
  checkInDate: string;
  checkOutDate: string;
  totalAmount: number;
  depositAmount: number;
  bookingStatus: BookingStatus;
  paymentStatus: PaymentStatus;
  roomAssignmentStatus: RoomAssignmentStatus;
  isDirectBooking: boolean;
  contractNotes?: string;
  specialRequests?: string;
  rejectionReason?: string;
  waivePenaltyReason?: string;
  penaltyAmount: number;
  creationTime: string;
}

export interface GetManagerBookingsInputDto {
  hotelId: string;
  bookingCode?: string;
  guestKeyword?: string;
  bookingStatus?: BookingStatus;
  paymentStatus?: PaymentStatus;
  fromDate?: string;
  toDate?: string;
  skipCount?: number;
  maxResultCount?: number;
}

export interface CreateDirectBookingDto {
  hotelId: string;
  guestFullName: string;
  guestPhone: string;
  guestEmail?: string;
  guestIdentityNumber?: string;
  roomTypeId: string;
  checkInDate: string;
  checkOutDate: string;
  numberOfRooms: number;
  agreedRatePerNight: number;
  depositAmountReceived: number;
  contractNotes?: string;
  specialRequests?: string;
}

export interface RejectBookingDto {
  hotelId: string;
  reason: string;
}

export interface MarkNoShowDto {
  hotelId: string;
  waivePenalty: boolean;
  penaltyAmountOverride?: number;
  waivePenaltyReason?: string;
}

export interface NoShowResultDto {
  bookingId: string;
  bookingStatus: BookingStatus;
  paymentStatus: PaymentStatus;
  penaltyAmountApplied: number;
  refundAmountInitiated: number;
  releasedRoomNumbers: string[];
  auditMessage?: string;
}

@Injectable({
  providedIn: 'root'
})
export class ManagerBookingService {
  private rest = inject(RestService);

  getList(input: GetManagerBookingsInputDto): Observable<PagedResultDto<ManagerBookingDto>> {
    return this.rest.request<GetManagerBookingsInputDto, PagedResultDto<ManagerBookingDto>>({
      method: 'GET',
      url: `/api/app/manager/bookings`,
      params: input
    });
  }

  get(id: string, hotelId: string): Observable<ManagerBookingDto> {
    return this.rest.request<void, ManagerBookingDto>({
      method: 'GET',
      url: `/api/app/manager/bookings/${id}`,
      params: { hotelId }
    });
  }

  createDirectBooking(input: CreateDirectBookingDto): Observable<ManagerBookingDto> {
    return this.rest.request<CreateDirectBookingDto, ManagerBookingDto>({
      method: 'POST',
      url: `/api/app/manager/bookings/direct-booking`,
      body: input
    });
  }

  confirm(id: string, hotelId: string): Observable<ManagerBookingDto> {
    return this.rest.request<void, ManagerBookingDto>({
      method: 'POST',
      url: `/api/app/manager/bookings/${id}/confirm`,
      params: { hotelId }
    });
  }

  reject(id: string, input: RejectBookingDto): Observable<ManagerBookingDto> {
    return this.rest.request<RejectBookingDto, ManagerBookingDto>({
      method: 'POST',
      url: `/api/app/manager/bookings/${id}/reject`,
      body: input
    });
  }

  markNoShow(id: string, input: MarkNoShowDto): Observable<NoShowResultDto> {
    return this.rest.request<MarkNoShowDto, NoShowResultDto>({
      method: 'POST',
      url: `/api/app/manager/bookings/${id}/mark-no-show`,
      body: input
    });
  }
}
