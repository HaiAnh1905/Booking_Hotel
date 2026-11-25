import { Injectable, inject } from '@angular/core';
import { RestService } from '@abp/ng.core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface RevenueReportItemDto {
  date: string;
  periodLabel: string;
  roomRevenue: number;
  serviceRevenue: number;
  refundAmount: number;
  netRevenue: number;
  completedBookingsCount: number;
}

export interface RevenueReportDto {
  totalNetRevenue: number;
  totalRoomRevenue: number;
  totalServiceRevenue: number;
  totalRefundAmount: number;
  totalBookingsCount: number;
  items: RevenueReportItemDto[];
}

export interface HotelPerformanceItemDto {
  hotelId: string;
  hotelName: string;
  hotelCode: string;
  roomCount: number;
  roomRevenue: number;
  availableRoomNights: number;
  soldRoomNights: number;
  occupancyPercent: number;
  adr: number;
  revPar: number;
  rank: number;
}

export interface HotelPerformanceReportDto {
  startDate: string;
  endDate: string;
  chainAverageOccupancy: number;
  chainAverageRevPar: number;
  chainAverageAdr: number;
  items: HotelPerformanceItemDto[];
}

export interface GetTenantReportInput {
  startDate?: string;
  endDate?: string;
  hotelId?: string;
  groupBy?: string;
}

@Injectable({
  providedIn: 'root'
})
export class TenantReportService {
  private restService = inject(RestService);
  private http = inject(HttpClient);

  getRevenueReport(input?: GetTenantReportInput): Observable<RevenueReportDto> {
    return this.restService.request<GetTenantReportInput, RevenueReportDto>({
      method: 'GET',
      url: '/api/app/tenant-reports/revenue',
      params: input
    });
  }

  getHotelPerformanceReport(input?: GetTenantReportInput): Observable<HotelPerformanceReportDto> {
    return this.restService.request<GetTenantReportInput, HotelPerformanceReportDto>({
      method: 'GET',
      url: '/api/app/tenant-reports/hotel-performance',
      params: input
    });
  }

  exportReport(reportType: string, format: string, input?: GetTenantReportInput): Observable<Blob> {
    return this.http.get('/api/app/tenant-reports/export', {
      params: {
        reportType,
        format,
        ...(input?.startDate ? { startDate: input.startDate } : {}),
        ...(input?.endDate ? { endDate: input.endDate } : {}),
        ...(input?.hotelId ? { hotelId: input.hotelId } : {}),
        ...(input?.groupBy ? { groupBy: input.groupBy } : {})
      },
      responseType: 'blob'
    });
  }
}
