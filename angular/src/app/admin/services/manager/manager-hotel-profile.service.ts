import { Injectable, inject } from '@angular/core';
import { RestService } from '@abp/ng.core';
import { Observable } from 'rxjs';

export interface ManagerHotelProfileDto {
  id: string;
  name: string;
  code: string;
  address: string;
  city: string;
  phone: string;
  email: string;
  starRating: number;
  roomCount: number;
  managerName: string;
  standardCheckInTime: string;
  standardCheckOutTime: string;
  description?: string;
  amenitiesJson?: string;
  houseRules?: string;
}

export interface UpdateManagerHotelProfileDto {
  hotelId: string;
  address: string;
  city: string;
  phone: string;
  email: string;
  standardCheckInTime: string;
  standardCheckOutTime: string;
  description?: string;
  amenitiesJson?: string;
  houseRules?: string;
}

@Injectable({
  providedIn: 'root'
})
export class ManagerHotelProfileService {
  private rest = inject(RestService);

  get(hotelId: string): Observable<ManagerHotelProfileDto> {
    return this.rest.request<void, ManagerHotelProfileDto>({
      method: 'GET',
      url: `/api/app/manager/hotel-profile`,
      params: { hotelId }
    });
  }

  update(input: UpdateManagerHotelProfileDto): Observable<ManagerHotelProfileDto> {
    return this.rest.request<UpdateManagerHotelProfileDto, ManagerHotelProfileDto>({
      method: 'PUT',
      url: `/api/app/manager/hotel-profile`,
      body: input
    });
  }
}
