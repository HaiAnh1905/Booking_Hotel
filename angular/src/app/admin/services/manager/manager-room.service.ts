import { Injectable, inject } from '@angular/core';
import { RestService } from '@abp/ng.core';
import { Observable } from 'rxjs';

export enum RoomOperationalStatus {
  Active = 1,
  Available = 1,
  UnderMaintenance = 2,
  Maintenance = 2,
  OutOfService = 3,
  Occupied = 4,
  Reserved = 5
}

export enum RoomOccupancyStatus {
  Vacant = 1,
  Reserved = 2,
  Occupied = 3,
  Blocked = 4
}

export enum RoomClosureType {
  Planned = 1,
  Emergency = 2
}

export enum RoomClosureStatus {
  Pending = 1,
  Active = 2,
  Resolved = 3,
  Cancelled = 4
}

export enum AffectedBookingResolutionType {
  RoomChange = 1,
  RoomUpgrade = 2,
  Refund = 3,
  OtherResolution = 4
}

export enum HousekeepingStatus {
  Clean = 1,
  Dirty = 2,
  Cleaning = 3,
  Inspected = 4
}

export interface RoomTypeDto {
  id: string;
  hotelId: string;
  name: string;
  description: string;
  standardOccupancy: number;
  maxOccupancy: number;
  basePrice: number;
  sizeSquareMeters: number;
  amenities: string[];
  totalRoomCount: number;
}

export interface CreateUpdateRoomTypeDto {
  hotelId: string;
  name: string;
  description?: string;
  standardOccupancy: number;
  maxOccupancy: number;
  basePrice: number;
  sizeSquareMeters: number;
  amenities?: string[];
}

export interface RoomDto {
  id: string;
  hotelId: string;
  roomTypeId: string;
  roomTypeName: string;
  roomNumber: string;
  floor: number;
  operationalStatus: RoomOperationalStatus;
  occupancyStatus: RoomOccupancyStatus;
  housekeepingStatus: HousekeepingStatus;
  isActive: boolean;
  building?: string;
  description?: string;
  isSellable: boolean;
  currentOccupantName?: string;
  maintenanceReason?: string;
  maintenanceEndDate?: string;
  outOfServiceReason?: string;
  outOfServiceEndDate?: string;
}

export interface CreateUpdateRoomDto {
  hotelId: string;
  roomTypeId: string;
  roomNumber: string;
  floor: number;
  building?: string;
  description?: string;
}

export interface DeactivateRoomDto {
  hotelId: string;
  reason?: string;
}

export interface ChangeOperationalStatusDto {
  hotelId: string;
  targetStatus: RoomOperationalStatus;
  reason?: string;
  startDate?: string;
  expectedEndDate?: string;
}

export interface UpdateHousekeepingStatusDto {
  hotelId: string;
  targetStatus: HousekeepingStatus;
}

export interface RoomDashboardOverviewDto {
  hotelId: string;
  totalRooms: number;
  occupiedRooms: number;
  vacantRooms: number;
  reservedRooms: number;
  unavailableRooms: number;
  occupancyRate: number;
  occupancyBreakdown: { [key: string]: number };
  operationalBreakdown: { [key: string]: number };
}

export interface CheckAffectedBookingsInputDto {
  hotelId: string;
  roomIds: string[];
  startDate: string;
  endDate: string;
}

export interface AffectedBookingDto {
  bookingId: string;
  bookingCode: string;
  customerName: string;
  customerPhone: string;
  customerEmail: string;
  checkInDate: string;
  checkOutDate: string;
  roomId: string;
  roomNumber: string;
  roomTypeId: string;
  roomTypeName: string;
  basePrice: number;
}

export interface CreatePlannedClosureDto {
  hotelId: string;
  roomIds: string[];
  startDate: string;
  endDate: string;
  reason: string;
}

export interface CreateEmergencyClosureDto {
  hotelId: string;
  roomIds: string[];
  reason: string;
  incidentDescription: string;
  startDate: string;
  estimatedRecoveryTime?: string;
}

export interface RoomClosureDto {
  id: string;
  hotelId: string;
  roomId: string;
  roomNumber: string;
  closureType: RoomClosureType;
  reason: string;
  incidentDescription?: string;
  startDate: string;
  endDate?: string;
  estimatedRecoveryTime?: string;
  status: RoomClosureStatus;
  recoveryConfirmedBy?: string;
  recoveryConfirmedTime?: string;
  creationTime: string;
  creatorName?: string;
  resolutions: AffectedBookingResolutionDto[];
}

export interface AffectedBookingResolutionDto {
  id: string;
  roomClosureId: string;
  bookingId: string;
  bookingCode: string;
  customerName: string;
  customerEmail: string;
  roomNumber: string;
  resolutionType?: AffectedBookingResolutionType;
  alternativeRoomId?: string;
  alternativeRoomNumber?: string;
  resolvedBy?: string;
  resolvedTime?: string;
  notes?: string;
  isCustomerNotified: boolean;
}

export interface ResolveAffectedBookingDto {
  hotelId: string;
  resolutionId: string;
  resolutionType: AffectedBookingResolutionType;
  alternativeRoomId?: string;
  notes?: string;
}

export interface ConfirmRoomRecoveryDto {
  hotelId: string;
  notes?: string;
}

export interface AlternativeRoomSuggestionDto {
  roomId: string;
  roomNumber: string;
  floor: number;
  roomTypeId: string;
  roomTypeName: string;
  basePrice: number;
  isUpgrade: boolean;
}

export interface RatePlanDto {
  ratePlanId: string;
  code: string;
  name: string;
  description: string;
  isRefundable: boolean;
  includesBreakfast: boolean;
  isActiveAtHotel: boolean;
  customBasePrice?: number;
  priceMultiplier: number;
}

export interface ToggleHotelRatePlanDto {
  hotelId: string;
  roomTypeId: string;
  ratePlanId: string;
  isActive: boolean;
  customBasePrice?: number;
  priceMultiplier: number;
}

export interface DailyRateOverrideDto {
  id: string;
  hotelId: string;
  roomTypeId: string;
  date: string;
  overridePrice: number;
  isClosedToArrival: boolean;
  isClosedToDeparture: boolean;
}

export interface BatchOverrideDailyRatesDto {
  hotelId: string;
  roomTypeId: string;
  fromDate: string;
  toDate: string;
  overridePrice: number;
  isClosedToArrival: boolean;
  isClosedToDeparture: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class ManagerRoomService {
  private rest = inject(RestService);

  getDashboardOverview(hotelId: string): Observable<RoomDashboardOverviewDto> {
    return this.rest.request<void, RoomDashboardOverviewDto>({
      method: 'GET',
      url: `/api/app/manager/rooms/dashboard-overview`,
      params: { hotelId }
    });
  }

  getRoomTypes(hotelId: string): Observable<RoomTypeDto[]> {
    return this.rest.request<void, RoomTypeDto[]>({
      method: 'GET',
      url: `/api/app/manager/rooms/room-types`,
      params: { hotelId }
    });
  }

  createRoomType(input: CreateUpdateRoomTypeDto): Observable<RoomTypeDto> {
    return this.rest.request<CreateUpdateRoomTypeDto, RoomTypeDto>({
      method: 'POST',
      url: `/api/app/manager/rooms/room-types`,
      body: input
    });
  }

  updateRoomType(id: string, input: CreateUpdateRoomTypeDto): Observable<RoomTypeDto> {
    return this.rest.request<CreateUpdateRoomTypeDto, RoomTypeDto>({
      method: 'PUT',
      url: `/api/app/manager/rooms/room-types/${id}`,
      body: input
    });
  }

  deleteRoomType(id: string, hotelId: string): Observable<void> {
    return this.rest.request<void, void>({
      method: 'DELETE',
      url: `/api/app/manager/rooms/room-types/${id}`,
      params: { hotelId }
    });
  }

  getPhysicalRooms(
    hotelId: string,
    floor?: number | null,
    roomTypeId?: string | null,
    occupancyStatus?: RoomOccupancyStatus | null,
    operationalStatus?: RoomOperationalStatus | null,
    search?: string | null
  ): Observable<RoomDto[]> {
    const params: any = { hotelId };
    if (floor !== null && floor !== undefined) params.floor = floor;
    if (roomTypeId) params.roomTypeId = roomTypeId;
    if (occupancyStatus !== null && occupancyStatus !== undefined) params.occupancyStatus = occupancyStatus;
    if (operationalStatus !== null && operationalStatus !== undefined) params.operationalStatus = operationalStatus;
    if (search) params.search = search;

    return this.rest.request<void, RoomDto[]>({
      method: 'GET',
      url: `/api/app/manager/rooms/physical-rooms`,
      params
    });
  }

  createPhysicalRoom(input: CreateUpdateRoomDto): Observable<RoomDto> {
    return this.rest.request<CreateUpdateRoomDto, RoomDto>({
      method: 'POST',
      url: `/api/app/manager/rooms/physical-rooms`,
      body: input
    });
  }

  updatePhysicalRoom(id: string, input: CreateUpdateRoomDto): Observable<RoomDto> {
    return this.rest.request<CreateUpdateRoomDto, RoomDto>({
      method: 'PUT',
      url: `/api/app/manager/rooms/physical-rooms/${id}`,
      body: input
    });
  }

  deactivateRoom(id: string, input: DeactivateRoomDto): Observable<void> {
    return this.rest.request<DeactivateRoomDto, void>({
      method: 'POST',
      url: `/api/app/manager/rooms/physical-rooms/${id}/deactivate`,
      body: input
    });
  }

  activateRoom(id: string, hotelId: string): Observable<void> {
    return this.rest.request<void, void>({
      method: 'POST',
      url: `/api/app/manager/rooms/physical-rooms/${id}/activate`,
      params: { hotelId }
    });
  }

  deletePhysicalRoom(id: string, hotelId: string): Observable<void> {
    return this.rest.request<void, void>({
      method: 'DELETE',
      url: `/api/app/manager/rooms/physical-rooms/${id}`,
      params: { hotelId }
    });
  }

  changeOperationalStatus(id: string, input: ChangeOperationalStatusDto): Observable<RoomDto> {
    return this.rest.request<ChangeOperationalStatusDto, RoomDto>({
      method: 'POST',
      url: `/api/app/manager/rooms/${id}/change-operational-status`,
      body: input
    });
  }

  updateHousekeepingStatus(id: string, input: UpdateHousekeepingStatusDto): Observable<RoomDto> {
    return this.rest.request<UpdateHousekeepingStatusDto, RoomDto>({
      method: 'POST',
      url: `/api/app/manager/rooms/${id}/update-housekeeping`,
      body: input
    });
  }

  checkAffectedBookings(input: CheckAffectedBookingsInputDto): Observable<AffectedBookingDto[]> {
    return this.rest.request<CheckAffectedBookingsInputDto, AffectedBookingDto[]>({
      method: 'POST',
      url: `/api/app/manager/rooms/closures/check-affected-bookings`,
      body: input
    });
  }

  createPlannedClosure(input: CreatePlannedClosureDto): Observable<RoomClosureDto[]> {
    return this.rest.request<CreatePlannedClosureDto, RoomClosureDto[]>({
      method: 'POST',
      url: `/api/app/manager/rooms/closures/planned`,
      body: input
    });
  }

  createEmergencyClosure(input: CreateEmergencyClosureDto): Observable<RoomClosureDto[]> {
    return this.rest.request<CreateEmergencyClosureDto, RoomClosureDto[]>({
      method: 'POST',
      url: `/api/app/manager/rooms/closures/emergency`,
      body: input
    });
  }

  getClosureHistory(
    hotelId: string,
    closureType?: RoomClosureType | null,
    fromDate?: string | null,
    toDate?: string | null
  ): Observable<RoomClosureDto[]> {
    const params: any = { hotelId };
    if (closureType !== null && closureType !== undefined) params.closureType = closureType;
    if (fromDate) params.fromDate = fromDate;
    if (toDate) params.toDate = toDate;

    return this.rest.request<void, RoomClosureDto[]>({
      method: 'GET',
      url: `/api/app/manager/rooms/closures/history`,
      params
    });
  }

  getAlternativeRoomSuggestions(
    hotelId: string,
    bookingId: string,
    resolutionType: AffectedBookingResolutionType
  ): Observable<AlternativeRoomSuggestionDto[]> {
    return this.rest.request<void, AlternativeRoomSuggestionDto[]>({
      method: 'GET',
      url: `/api/app/manager/rooms/closures/alternative-suggestions`,
      params: { hotelId, bookingId, resolutionType }
    });
  }

  resolveAffectedBooking(input: ResolveAffectedBookingDto): Observable<void> {
    return this.rest.request<ResolveAffectedBookingDto, void>({
      method: 'POST',
      url: `/api/app/manager/rooms/closures/resolve-booking`,
      body: input
    });
  }

  confirmRoomRecovery(closureId: string, input: ConfirmRoomRecoveryDto): Observable<void> {
    return this.rest.request<ConfirmRoomRecoveryDto, void>({
      method: 'POST',
      url: `/api/app/manager/rooms/closures/${closureId}/confirm-recovery`,
      body: input
    });
  }
}
