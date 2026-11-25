import { Injectable, inject } from '@angular/core';
import { PagedResultDto, RestService } from '@abp/ng.core';
import { Observable } from 'rxjs';

export enum BookingStatus {
  PendingConfirmation = 1,
  Confirmed = 2,
  CheckedIn = 3,
  Completed = 4,
  Rejected = 5,
  Cancelled = 6,
  NoShow = 7,
}

export enum PaymentStatus {
  Unpaid = 1,
  PartiallyPaid = 2,
  Paid = 3,
  Refunded = 4,
}

export enum RoomAssignmentStatus {
  Unassigned = 1,
  Assigned = 2,
}

export enum FrontdeskRoomRackStatus {
  Ready = 1,
  Reserved = 2,
  Occupied = 3,
  NotReady = 4,
  Unavailable = 5,
}

export interface FrontdeskQuickBookingItemDto {
  bookingId: string;
  bookingCode: string;
  guestFullName: string;
  guestPhone: string;
  roomTypeName: string;
  assignedRoomId?: string | null;
  assignedRoomNumber?: string | null;
  checkInDate: string;
  checkOutDate: string;
  bookingStatus: BookingStatus;
  roomAssignmentStatus: RoomAssignmentStatus;
  isIdentityVerified: boolean;
}

export interface FrontdeskDashboardDto {
  hotelId: string;
  hotelName: string;
  hotelCode: string;
  businessDate: string;
  totalActiveBookings: number;
  pendingConfirmationCount: number;
  expectedArrivalsTodayCount: number;
  inHouseGuestsCount: number;
  expectedDeparturesTodayCount: number;
  todayArrivals: FrontdeskQuickBookingItemDto[];
  todayDepartures: FrontdeskQuickBookingItemDto[];
  pendingBookings: FrontdeskQuickBookingItemDto[];
}

export interface FrontdeskRoomBookingSummaryDto {
  bookingId: string;
  bookingCode: string;
  guestFullName: string;
  guestPhone: string;
  checkInDate: string;
  checkOutDate: string;
  bookingStatus: BookingStatus;
  paymentStatus: PaymentStatus;
  totalAmount: number;
  depositAmount: number;
  guestCount: number;
  isIdentityVerified: boolean;
}

export interface FrontdeskRoomRackItemDto {
  roomId: string;
  roomNumber: string;
  floor: number;
  building?: string | null;
  roomTypeId: string;
  roomTypeName: string;
  capacity: number;
  maxOccupancy: number;
  sizeSquareMeters: number;
  bedType?: string | null;
  description?: string | null;
  rackStatus: FrontdeskRoomRackStatus;
  occupancyStatus: number;
  operationalStatus: number;
  isReadyForCheckIn: boolean;
  unavailableReason?: string | null;
  currentBooking?: FrontdeskRoomBookingSummaryDto | null;
}

export interface FrontdeskRoomRackDto {
  hotelId: string;
  totalRooms: number;
  readyCount: number;
  reservedCount: number;
  occupiedCount: number;
  notReadyCount: number;
  unavailableCount: number;
  floors: number[];
  rooms: FrontdeskRoomRackItemDto[];
}

export interface GetFrontdeskRoomRackInput {
  hotelId: string;
  floor?: number | null;
  roomTypeId?: string | null;
  rackStatus?: FrontdeskRoomRackStatus | null;
  search?: string | null;
}

export interface GetFrontdeskReservationsInput {
  hotelId: string;
  keyword?: string | null;
  bookingStatus?: BookingStatus | null;
  roomAssignmentStatus?: RoomAssignmentStatus | null;
  fromDate?: string | null;
  toDate?: string | null;
  arrivalTodayOnly?: boolean | null;
  departureTodayOnly?: boolean | null;
  skipCount?: number;
  maxResultCount?: number;
}

export interface FrontdeskReservationListItemDto {
  id: string;
  hotelId: string;
  bookingCode: string;
  guestFullName: string;
  guestPhone: string;
  guestEmail: string;
  guestIdentityNumberMasked: string;
  isIdentityVerified: boolean;
  roomTypeId?: string | null;
  roomTypeName: string;
  assignedRoomId?: string | null;
  assignedRoomNumber?: string | null;
  assignedRoomFloor?: number | null;
  assignedRoomIsReady: boolean;
  checkInDate: string;
  checkOutDate: string;
  actualCheckInTime?: string | null;
  actualCheckOutTime?: string | null;
  bookingStatus: BookingStatus;
  paymentStatus: PaymentStatus;
  roomAssignmentStatus: RoomAssignmentStatus;
  totalAmount: number;
  depositAmount: number;
  creationTime: string;
}

export interface FrontdeskReservationDetailDto extends FrontdeskReservationListItemDto {
  guestId?: string | null;
  guestKey: string;
  specialRequests?: string | null;
  frontdeskNotes?: string | null;
  cancellationReason?: string | null;
  rejectionReason?: string | null;
  lastModificationTime?: string | null;
}

export interface FrontdeskAvailableRoomDto {
  roomId: string;
  roomNumber: string;
  floor: number;
  building?: string | null;
  roomTypeId: string;
  roomTypeName: string;
}

export interface UpdateFrontdeskReservationDto {
  hotelId: string;
  guestFullName: string;
  guestPhone: string;
  guestEmail?: string | null;
  specialRequests?: string | null;
  frontdeskNotes?: string | null;
}

export interface FrontdeskCheckInDto {
  hotelId: string;
  roomId: string;
  guestFullName: string;
  guestPhone: string;
  guestEmail?: string | null;
  identityNumber: string;
  frontdeskNotes?: string | null;
}

export interface FrontdeskCheckOutDto {
  hotelId: string;
  frontdeskNotes?: string | null;
}

export interface GetFrontdeskGuestsInput {
  hotelId: string;
  keyword?: string | null;
  skipCount?: number;
  maxResultCount?: number;
}

export interface FrontdeskGuestListItemDto {
  guestKey: string;
  guestId?: string | null;
  fullName: string;
  phoneNumber: string;
  email: string;
  identityNumberMasked: string;
  isIdentityVerified: boolean;
  totalBookingsCount: number;
  activeBookingsCount: number;
  lastCheckInDate?: string | null;
}

export interface FrontdeskGuestBookingItemDto {
  bookingId: string;
  bookingCode: string;
  roomTypeName: string;
  assignedRoomNumber?: string | null;
  checkInDate: string;
  checkOutDate: string;
  actualCheckInTime?: string | null;
  actualCheckOutTime?: string | null;
  bookingStatus: BookingStatus;
  paymentStatus: PaymentStatus;
  totalAmount: number;
}

export interface FrontdeskGuestDetailDto extends FrontdeskGuestListItemDto {
  bookings: FrontdeskGuestBookingItemDto[];
}

export interface UpdateFrontdeskGuestDto {
  hotelId: string;
  fullName: string;
  phoneNumber: string;
  email?: string | null;
  identityNumber?: string | null;
}

@Injectable({ providedIn: 'root' })
export class FrontdeskService {
  private rest = inject(RestService);

  getDashboardOverview(hotelId: string): Observable<FrontdeskDashboardDto> {
    return this.rest.request<void, FrontdeskDashboardDto>({
      method: 'GET',
      url: '/api/app/frontdesk/dashboard/overview',
      params: { hotelId },
    });
  }

  getRoomRack(input: GetFrontdeskRoomRackInput): Observable<FrontdeskRoomRackDto> {
    const params: Record<string, any> = { hotelId: input.hotelId };
    if (input.floor !== null && input.floor !== undefined) params['floor'] = input.floor;
    if (input.roomTypeId) params['roomTypeId'] = input.roomTypeId;
    if (input.rackStatus !== null && input.rackStatus !== undefined) params['rackStatus'] = input.rackStatus;
    if (input.search?.trim()) params['search'] = input.search.trim();

    return this.rest.request<void, FrontdeskRoomRackDto>({
      method: 'GET',
      url: '/api/app/frontdesk/room-rack',
      params,
    });
  }

  getReservations(input: GetFrontdeskReservationsInput): Observable<PagedResultDto<FrontdeskReservationListItemDto>> {
    const params: Record<string, any> = {
      hotelId: input.hotelId,
      skipCount: input.skipCount ?? 0,
      maxResultCount: input.maxResultCount ?? 20,
    };
    if (input.keyword?.trim()) params['keyword'] = input.keyword.trim();
    if (input.bookingStatus !== null && input.bookingStatus !== undefined) params['bookingStatus'] = input.bookingStatus;
    if (input.roomAssignmentStatus !== null && input.roomAssignmentStatus !== undefined) {
      params['roomAssignmentStatus'] = input.roomAssignmentStatus;
    }
    if (input.fromDate) params['fromDate'] = input.fromDate;
    if (input.toDate) params['toDate'] = input.toDate;
    if (input.arrivalTodayOnly) params['arrivalTodayOnly'] = true;
    if (input.departureTodayOnly) params['departureTodayOnly'] = true;

    return this.rest.request<void, PagedResultDto<FrontdeskReservationListItemDto>>({
      method: 'GET',
      url: '/api/app/frontdesk/reservations',
      params,
    });
  }

  getReservation(id: string, hotelId: string): Observable<FrontdeskReservationDetailDto> {
    return this.rest.request<void, FrontdeskReservationDetailDto>({
      method: 'GET',
      url: `/api/app/frontdesk/reservations/${id}`,
      params: { hotelId },
    });
  }

  getAvailableRooms(id: string, hotelId: string): Observable<FrontdeskAvailableRoomDto[]> {
    return this.rest.request<void, FrontdeskAvailableRoomDto[]>({
      method: 'GET',
      url: `/api/app/frontdesk/reservations/${id}/available-rooms`,
      params: { hotelId },
    });
  }

  confirmReservation(id: string, hotelId: string): Observable<FrontdeskReservationDetailDto> {
    return this.rest.request<{ hotelId: string }, FrontdeskReservationDetailDto>({
      method: 'POST',
      url: `/api/app/frontdesk/reservations/${id}/confirm`,
      body: { hotelId },
    });
  }

  assignRoom(id: string, hotelId: string, roomId: string): Observable<FrontdeskReservationDetailDto> {
    return this.rest.request<{ hotelId: string; roomId: string }, FrontdeskReservationDetailDto>({
      method: 'POST',
      url: `/api/app/frontdesk/reservations/${id}/assign-room`,
      body: { hotelId, roomId },
    });
  }

  updateReservation(id: string, input: UpdateFrontdeskReservationDto): Observable<FrontdeskReservationDetailDto> {
    return this.rest.request<UpdateFrontdeskReservationDto, FrontdeskReservationDetailDto>({
      method: 'PUT',
      url: `/api/app/frontdesk/reservations/${id}`,
      body: input,
    });
  }

  cancelReservation(id: string, hotelId: string, reason: string): Observable<FrontdeskReservationDetailDto> {
    return this.rest.request<{ hotelId: string; reason: string }, FrontdeskReservationDetailDto>({
      method: 'POST',
      url: `/api/app/frontdesk/reservations/${id}/cancel`,
      body: { hotelId, reason },
    });
  }

  checkIn(id: string, input: FrontdeskCheckInDto): Observable<FrontdeskReservationDetailDto> {
    return this.rest.request<FrontdeskCheckInDto, FrontdeskReservationDetailDto>({
      method: 'POST',
      url: `/api/app/frontdesk/reservations/${id}/check-in`,
      body: input,
    });
  }

  checkOut(id: string, input: FrontdeskCheckOutDto): Observable<FrontdeskReservationDetailDto> {
    return this.rest.request<FrontdeskCheckOutDto, FrontdeskReservationDetailDto>({
      method: 'POST',
      url: `/api/app/frontdesk/reservations/${id}/check-out`,
      body: input,
    });
  }

  getGuests(input: GetFrontdeskGuestsInput): Observable<PagedResultDto<FrontdeskGuestListItemDto>> {
    const params: Record<string, any> = {
      hotelId: input.hotelId,
      skipCount: input.skipCount ?? 0,
      maxResultCount: input.maxResultCount ?? 20,
    };
    if (input.keyword?.trim()) params['keyword'] = input.keyword.trim();

    return this.rest.request<void, PagedResultDto<FrontdeskGuestListItemDto>>({
      method: 'GET',
      url: '/api/app/frontdesk/guests',
      params,
    });
  }

  getGuestDetail(hotelId: string, guestKey: string): Observable<FrontdeskGuestDetailDto> {
    return this.rest.request<void, FrontdeskGuestDetailDto>({
      method: 'GET',
      url: '/api/app/frontdesk/guests/detail',
      params: { hotelId, guestKey },
    });
  }

  updateGuest(guestKey: string, input: UpdateFrontdeskGuestDto): Observable<FrontdeskGuestDetailDto> {
    return this.rest.request<UpdateFrontdeskGuestDto, FrontdeskGuestDetailDto>({
      method: 'PUT',
      url: '/api/app/frontdesk/guests/detail',
      params: { guestKey },
      body: input,
    });
  }
}
