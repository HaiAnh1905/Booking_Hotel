import { Injectable, inject } from '@angular/core';
import { RestService, PagedResultDto } from '@abp/ng.core';
import { Observable } from 'rxjs';

export enum HotelStatus {
  Operating = 1,
  Maintenance = 2,
  Closed = 3,
  ScheduledSuspended = 4,
  EmergencySuspended = 5
}

export enum SuspensionType {
  Scheduled = 1,
  Emergency = 2
}

export interface HotelDto {
  id: string;
  tenantId?: string;
  name: string;
  code: string;
  address: string;
  city: string;
  phone: string;
  email: string;
  starRating: number;
  roomCount: number;
  managerName?: string;
  managerUserId?: string;
  status: HotelStatus;
  standardCheckInTime?: string;
  standardCheckOutTime?: string;
  bookingCutoffDays?: number;
  creationTime?: string;
}

export interface CreateUpdateHotelDto {
  name: string;
  code: string;
  address: string;
  city: string;
  phone: string;
  email: string;
  starRating: number;
  roomCount: number;
  managerName?: string;
  managerUserId?: string;
  status: HotelStatus;
  standardCheckInTime?: string;
  standardCheckOutTime?: string;
  bookingCutoffDays?: number;
}

export interface SuspendHotelInputDto {
  suspensionType: SuspensionType;
  startDate: string;
  endDate?: string;
  reason: string;
}

export interface HotelSuspensionResultDto {
  hotelId: string;
  status: HotelStatus;
  affectedBookingsCount: number;
  message: string;
}

export interface AssignHotelManagerDto {
  managerUserId: string;
}

export interface GetHotelsInput {
  filter?: string;
  status?: HotelStatus;
  skipCount?: number;
  maxResultCount?: number;
}

@Injectable({
  providedIn: 'root'
})
export class HotelService {
  private restService = inject(RestService);

  getList(input?: GetHotelsInput): Observable<PagedResultDto<HotelDto>> {
    return this.restService.request<GetHotelsInput, PagedResultDto<HotelDto>>({
      method: 'GET',
      url: '/api/app/hotels',
      params: input
    }, { skipHandleError: true });
  }

  get(id: string): Observable<HotelDto> {
    return this.restService.request<void, HotelDto>({
      method: 'GET',
      url: `/api/app/hotels/${id}`
    });
  }

  checkNameExists(name: string, excludeId?: string): Observable<boolean> {
    return this.restService.request<{ name: string; excludeId?: string }, boolean>({
      method: 'GET',
      url: '/api/app/hotels/check-name',
      params: { name, excludeId }
    }, { skipHandleError: true });
  }

  create(input: CreateUpdateHotelDto): Observable<HotelDto> {
    return this.restService.request<CreateUpdateHotelDto, HotelDto>({
      method: 'POST',
      url: '/api/app/hotels',
      body: input
    }, { skipHandleError: true });
  }

  update(id: string, input: CreateUpdateHotelDto): Observable<HotelDto> {
    return this.restService.request<CreateUpdateHotelDto, HotelDto>({
      method: 'PUT',
      url: `/api/app/hotels/${id}`,
      body: input
    }, { skipHandleError: true });
  }

  delete(id: string): Observable<void> {
    return this.restService.request<void, void>({
      method: 'DELETE',
      url: `/api/app/hotels/${id}`
    });
  }

  suspend(id: string, input: SuspendHotelInputDto): Observable<HotelSuspensionResultDto> {
    return this.restService.request<SuspendHotelInputDto, HotelSuspensionResultDto>({
      method: 'POST',
      url: `/api/app/hotels/${id}/suspend`,
      body: input
    });
  }

  activate(id: string): Observable<HotelDto> {
    return this.restService.request<void, HotelDto>({
      method: 'POST',
      url: `/api/app/hotels/${id}/activate`
    });
  }

  assignManager(id: string, input: AssignHotelManagerDto): Observable<HotelDto> {
    return this.restService.request<AssignHotelManagerDto, HotelDto>({
      method: 'POST',
      url: `/api/app/hotels/${id}/assign-manager`,
      body: input
    });
  }
}
