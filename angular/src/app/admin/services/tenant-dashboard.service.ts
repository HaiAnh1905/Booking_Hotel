import { Injectable, inject } from '@angular/core';
import { RestService, PagedResultDto } from '@abp/ng.core';
import { Observable } from 'rxjs';

export interface TenantBranchMetricDto {
  hotelId: string;
  name: string;
  code: string;
  rooms: number;
  occupancyPercent: number;
  monthlyRevenue: number;
  adr: number;
  revPar: number;
  rank: number;
}

export interface TenantDashboardOverviewDto {
  chainName: string;
  totalHotelCount: number;
  operatingBranchCount: number;
  suspendedBranchCount: number;
  totalStaffCount: number;
  totalBookingCount: number;
  monthlyCashRevenue: number;
  averageOccupancyPercent: number;
  averageRevPar: number;
  averageAdr: number;
  branches: TenantBranchMetricDto[];
}

export interface GetTenantDashboardInput {
  startDate?: string;
  endDate?: string;
}

export interface TenantBookingSurveillanceDto {
  id: string;
  bookingId: string;
  bookingCode: string;
  hotelId: string;
  hotelName: string;
  hotelCode: string;
  customerName: string;
  customerPhone: string;
  checkInDate: string;
  checkOutDate: string;
  roomTypeName: string;
  roomNumber?: string;
  status: string;
  paymentStatus: string;
  totalAmount: number;
  creationTime: string;
}

export interface GetBookingSurveillanceInput {
  hotelId?: string;
  status?: string;
  paymentStatus?: string;
  fromDate?: string;
  toDate?: string;
  filter?: string;
  skipCount?: number;
  maxResultCount?: number;
}

@Injectable({
  providedIn: 'root'
})
export class TenantDashboardService {
  private restService = inject(RestService);

  getOverview(input?: GetTenantDashboardInput): Observable<TenantDashboardOverviewDto> {
    return this.restService.request<GetTenantDashboardInput, TenantDashboardOverviewDto>({
      method: 'GET',
      url: '/api/app/tenant-dashboard/overview',
      params: input
    });
  }

  getSurveillanceBookings(input?: GetBookingSurveillanceInput): Observable<PagedResultDto<TenantBookingSurveillanceDto>> {
    return this.restService.request<GetBookingSurveillanceInput, PagedResultDto<TenantBookingSurveillanceDto>>({
      method: 'GET',
      url: '/api/app/tenant-dashboard/surveillance-bookings',
      params: input
    });
  }

  getSurveillanceBookingDetail(bookingId: string): Observable<TenantBookingSurveillanceDto> {
    return this.restService.request<void, TenantBookingSurveillanceDto>({
      method: 'GET',
      url: `/api/app/tenant-dashboard/surveillance-bookings/${bookingId}`
    });
  }
}
