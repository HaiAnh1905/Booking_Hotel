import { Injectable, inject } from '@angular/core';
import { RestService, PagedResultDto } from '@abp/ng.core';
import { Observable } from 'rxjs';
import { BookingStatus } from './manager-booking.service';

export interface ManagerGuestDto {
  guestId: string;
  fullName: string;
  phoneNumber: string;
  email: string;
  identityNumberMasked: string;
  totalStaysAtThisHotel: number;
  totalSpentAtThisHotel: number;
  lastStayDate?: string;
  hasInternalNotes: boolean;
}

export interface GuestStayHistoryDto {
  bookingId: string;
  bookingCode: string;
  roomTypeName: string;
  roomNumber?: string;
  checkInDate: string;
  checkOutDate: string;
  totalAmount: number;
  bookingStatus: BookingStatus;
}

export interface InternalGuestNoteDto {
  id: string;
  hotelId: string;
  guestId: string;
  category: string;
  noteText: string;
  isAlert: boolean;
  creatorName?: string;
  creationTime: string;
  lastModifierName?: string;
  lastModificationTime?: string;
}

export interface CreateInternalGuestNoteDto {
  hotelId: string;
  guestId: string;
  category: string;
  noteText: string;
  isAlert: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class ManagerGuestService {
  private rest = inject(RestService);

  getList(hotelId: string, keyword?: string, skipCount = 0, maxResultCount = 20): Observable<PagedResultDto<ManagerGuestDto>> {
    return this.rest.request<void, PagedResultDto<ManagerGuestDto>>({
      method: 'GET',
      url: `/api/app/manager/guests`,
      params: { hotelId, keyword, skipCount, maxResultCount }
    });
  }

  getStayHistory(guestId: string, hotelId: string): Observable<GuestStayHistoryDto[]> {
    return this.rest.request<void, GuestStayHistoryDto[]>({
      method: 'GET',
      url: `/api/app/manager/guests/${guestId}/history`,
      params: { hotelId }
    });
  }

  getNotes(guestId: string, hotelId: string): Observable<InternalGuestNoteDto[]> {
    return this.rest.request<void, InternalGuestNoteDto[]>({
      method: 'GET',
      url: `/api/app/manager/guests/${guestId}/notes`,
      params: { hotelId }
    });
  }

  createNote(input: CreateInternalGuestNoteDto): Observable<InternalGuestNoteDto> {
    return this.rest.request<CreateInternalGuestNoteDto, InternalGuestNoteDto>({
      method: 'POST',
      url: `/api/app/manager/guests/${input.guestId}/notes`,
      body: input
    });
  }

  deleteNote(id: string, hotelId: string): Observable<void> {
    return this.rest.request<void, void>({
      method: 'DELETE',
      url: `/api/app/manager/guests/notes/${id}`,
      params: { hotelId }
    });
  }
}
