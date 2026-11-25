import { Injectable, inject } from '@angular/core';
import { RestService } from '@abp/ng.core';
import { Observable } from 'rxjs';

export interface ManagerDashboardOverviewDto {
  hotelId: string;
  hotelName: string;
  hotelCode: string;
  totalRooms: number;
  availableRooms: number;
  occupiedRooms: number;
  reservedRooms: number;
  maintenanceRooms: number;
  outOfServiceRooms: number;
  availableInventory: number;
  occupancyRate: number;
  todayArrivalsCount: number;
  todayDeparturesCount: number;
  actionItemsCount: number;
  todayRoomRevenue: number;
  todayCollectedRevenue: number;
  weekRoomRevenue: number;
  monthRoomRevenue: number;
}

export interface TodayArrivalDto {
  bookingId: string;
  bookingCode: string;
  guestFullName: string;
  guestPhone: string;
  roomTypeName: string;
  assignedRoomNumber?: string;
  paymentStatus: number;
  estimatedArrivalTime: string;
  specialRequests?: string;
}

export interface ManagerActionItemsDto {
  pendingConfirmationBookings: number;
  unassignedArrivalsToday: number;
  overdueHousekeepingRooms: number;
  openComplaintsNearSla: number;
}

@Injectable({
  providedIn: 'root'
})
export class ManagerDashboardService {
  private rest = inject(RestService);

  getKpis(hotelId: string): Observable<ManagerDashboardOverviewDto> {
    return this.rest.request<void, ManagerDashboardOverviewDto>({
      method: 'GET',
      url: `/api/app/manager/dashboard/kpis`,
      params: { hotelId }
    });
  }

  getTodayArrivals(hotelId: string, maxResultCount = 10): Observable<TodayArrivalDto[]> {
    return this.rest.request<void, TodayArrivalDto[]>({
      method: 'GET',
      url: `/api/app/manager/dashboard/today-arrivals`,
      params: { hotelId, maxResultCount }
    });
  }

  getActionItems(hotelId: string): Observable<ManagerActionItemsDto> {
    return this.rest.request<void, ManagerActionItemsDto>({
      method: 'GET',
      url: `/api/app/manager/dashboard/action-items`,
      params: { hotelId }
    });
  }
}
