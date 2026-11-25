import { Injectable, inject } from '@angular/core';
import { RestService } from '@abp/ng.core';
import { Observable } from 'rxjs';

export interface HotelPerformanceReportDto {
  hotelId: string;
  fromDate: string;
  toDate: string;
  totalPhysicalRooms: number;
  outOfServiceRoomNights: number;
  availableInventoryNights: number;
  occupiedRoomNights: number;
  occupancyRate: number;
  totalRoomRevenue: number;
  averageDailyRate: number;
  revenuePerAvailableRoom: number;
}

export interface RevenueBreakdownReportDto {
  roomRevenue: number;
  serviceRevenue: number;
  totalBilledRevenue: number;
  collectedRevenue: number;
  refundAmount: number;
  uncollectedBalance: number;
}

export interface CancellationLossReportDto {
  totalBookings: number;
  cancelledCount: number;
  cancellationRate: number;
  noShowCount: number;
  noShowRate: number;
  waivedNoShowPenaltiesCount: number;
  totalPenaltiesCollected: number;
  outOfServiceLossNights: number;
  estimatedRevenueLoss: number;
}

@Injectable({
  providedIn: 'root'
})
export class ManagerReportService {
  private rest = inject(RestService);

  getPerformance(hotelId: string, fromDate: string, toDate: string): Observable<HotelPerformanceReportDto> {
    return this.rest.request<void, HotelPerformanceReportDto>({
      method: 'GET',
      url: `/api/app/manager/reports/performance`,
      params: { hotelId, fromDate, toDate }
    });
  }

  getRevenueBreakdown(hotelId: string, fromDate: string, toDate: string): Observable<RevenueBreakdownReportDto> {
    return this.rest.request<void, RevenueBreakdownReportDto>({
      method: 'GET',
      url: `/api/app/manager/reports/revenue-breakdown`,
      params: { hotelId, fromDate, toDate }
    });
  }

  getCancellationLoss(hotelId: string, fromDate: string, toDate: string): Observable<CancellationLossReportDto> {
    return this.rest.request<void, CancellationLossReportDto>({
      method: 'GET',
      url: `/api/app/manager/reports/cancellations-loss`,
      params: { hotelId, fromDate, toDate }
    });
  }
}
