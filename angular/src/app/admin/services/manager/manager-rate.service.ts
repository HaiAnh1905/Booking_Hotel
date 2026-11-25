import { Injectable, inject } from '@angular/core';
import { PagedResultDto, RestService } from '@abp/ng.core';
import { Observable } from 'rxjs';

export enum PriceAdjustmentType {
  FixedPrice = 1,
  Percentage = 2,
  RestrictionsOnly = 3,
}

export enum PriceConflictResolution {
  Cancel = 0,
  ReplaceExisting = 1,
  SkipExisting = 2,
}

export enum PricingAuditActionType {
  CreatePriceOverride = 1,
  UpdatePriceOverride = 2,
  DeletePriceOverride = 3,
  UpdateBasePrice = 4,
  ToggleRatePlanStatus = 5,
  UpdateRestrictions = 6,
}

export interface RoomTypePricingContextDto {
  hotelId: string;
  hotelName: string;
  hotelCode: string;
  roomTypeId: string;
  roomTypeName: string;
  referenceBasePrice: number;
  ratePlanCount: number;
  activeRatePlanCount: number;
  activeOverrideCount: number;
  ratePlans: RoomRatePlanItemDto[];
}

export interface RoomRatePlanItemDto {
  ratePlanId: string;
  code: string;
  name: string;
  ratePlanType: string;
  description: string;
  isRefundable: boolean;
  cancellationCutoffHours: number;
  includesBreakfast: boolean;
  defaultPriceAdjustmentPercent: number;
  basePrice: number;
  effectivePrice: number;
  isTodayOverridden: boolean;
  isActive: boolean;
  overrideCount: number;
}

export interface ToggleRoomRatePlanStatusDto {
  hotelId: string;
  roomTypeId: string;
  ratePlanId: string;
  isActive: boolean;
}

export interface UpdateRoomRatePlanBasePriceDto {
  hotelId: string;
  roomTypeId: string;
  ratePlanId: string;
  basePrice: number;
}

export interface MonthlyPricingCalendarDto {
  hotelId: string;
  roomTypeId: string;
  ratePlanId: string;
  ratePlanName: string;
  ratePlanCode: string;
  isRatePlanActive: boolean;
  basePrice: number;
  year: number;
  month: number;
  days: DailyPricingCellDto[];
}

export interface DailyPricingCellDto {
  applicableDate: string;
  isPastDate: boolean;
  isToday: boolean;
  basePrice: number;
  effectivePrice: number;
  isPriceOverridden: boolean;
  hasRestrictionsOverride: boolean;
  overrideId?: string | null;
  adjustmentType?: PriceAdjustmentType | null;
  adjustmentValue?: number | null;
  isClosedToArrival: boolean;
  isClosedToDeparture: boolean;
  isOpenForSale: boolean;
}

export interface QuickEditDailyPricingDto {
  hotelId: string;
  roomTypeId: string;
  ratePlanId: string;
  applicableDate: string;
  adjustmentType: PriceAdjustmentType;
  adjustmentValue?: number | null;
  isClosedToArrival: boolean;
  isClosedToDeparture: boolean;
  clearOverride?: boolean;
}

export interface BulkPriceAdjustmentPreviewInputDto {
  hotelId: string;
  roomTypeId: string;
  ratePlanIds: string[];
  startDate: string;
  endDate: string;
  daysOfWeek?: number[] | null;
  adjustmentType: PriceAdjustmentType;
  adjustmentValue?: number | null;
  isClosedToArrival: boolean;
  isClosedToDeparture: boolean;
}

export interface BulkPriceAdjustmentPreviewDto {
  hotelName: string;
  roomTypeName: string;
  startDate: string;
  endDate: string;
  daysOfWeek: number[];
  adjustmentType: PriceAdjustmentType;
  adjustmentValue?: number | null;
  isClosedToArrival: boolean;
  isClosedToDeparture: boolean;
  totalAffectedDaysAcrossPlans: number;
  hasAnyConflicts: boolean;
  ratePlanPreviews: RatePlanBulkPreviewItemDto[];
}

export interface RatePlanBulkPreviewItemDto {
  ratePlanId: string;
  ratePlanName: string;
  ratePlanCode: string;
  currentBasePrice: number;
  projectedEffectivePrice?: number | null;
  matchingDatesCount: number;
  conflictingDates: string[];
  newDatesCount: number;
}

export interface ApplyBulkPriceAdjustmentDto extends BulkPriceAdjustmentPreviewInputDto {
  conflictResolution: PriceConflictResolution;
}

export interface BulkPriceAdjustmentResultDto {
  appliedRatePlanCount: number;
  affectedDaysCount: number;
  replacedDaysCount: number;
  skippedDaysCount: number;
  createdOverrideIds: string[];
}

export interface RoomPriceOverrideDto {
  id: string;
  hotelId: string;
  roomTypeId: string;
  ratePlanId: string;
  startDate: string;
  endDate: string;
  adjustmentType: PriceAdjustmentType;
  adjustmentValue?: number | null;
  calculatedEffectivePrice?: number | null;
  isClosedToArrival: boolean;
  isClosedToDeparture: boolean;
  creationTime: string;
  creatorId?: string | null;
  creatorName: string;
  isPastOnly: boolean;
  isStraddlingToday: boolean;
}

export interface UpdateRoomPriceOverrideDto {
  hotelId: string;
  startDate: string;
  endDate: string;
  adjustmentType: PriceAdjustmentType;
  adjustmentValue?: number | null;
  isClosedToArrival: boolean;
  isClosedToDeparture: boolean;
  conflictResolution: PriceConflictResolution;
}

export interface GetPricingAuditLogsInputDto {
  hotelId: string;
  roomTypeId?: string | null;
  ratePlanId?: string | null;
  actionType?: PricingAuditActionType | null;
  fromDate?: string | null;
  toDate?: string | null;
  skipCount?: number;
  maxResultCount?: number;
}

export interface PricingAuditLogDto {
  id: string;
  hotelId: string;
  roomTypeId: string;
  roomTypeName: string;
  ratePlanId: string;
  ratePlanCode: string;
  ratePlanName: string;
  actionType: PricingAuditActionType;
  actorId: string;
  actorName: string;
  actionTime: string;
  beforeValueJson?: string | null;
  afterValueJson?: string | null;
  affectedStartDate?: string | null;
  affectedEndDate?: string | null;
  summaryNote?: string | null;
}

@Injectable({
  providedIn: 'root',
})
export class ManagerRateService {
  private readonly rest = inject(RestService);
  private readonly baseUrl = '/api/app/manager/rooms/pricing';

  getPricingContext(hotelId: string, roomTypeId: string): Observable<RoomTypePricingContextDto> {
    return this.rest.request<void, RoomTypePricingContextDto>({
      method: 'GET',
      url: `${this.baseUrl}/context`,
      params: { hotelId, roomTypeId },
    });
  }

  toggleRatePlanStatus(input: ToggleRoomRatePlanStatusDto): Observable<RoomRatePlanItemDto> {
    return this.rest.request<ToggleRoomRatePlanStatusDto, RoomRatePlanItemDto>({
      method: 'POST',
      url: `${this.baseUrl}/rate-plans/toggle`,
      body: input,
    });
  }

  updateRatePlanBasePrice(input: UpdateRoomRatePlanBasePriceDto): Observable<RoomRatePlanItemDto> {
    return this.rest.request<UpdateRoomRatePlanBasePriceDto, RoomRatePlanItemDto>({
      method: 'PUT',
      url: `${this.baseUrl}/rate-plans/base-price`,
      body: input,
    });
  }

  getMonthlyCalendar(
    hotelId: string,
    roomTypeId: string,
    ratePlanId: string,
    year: number,
    month: number
  ): Observable<MonthlyPricingCalendarDto> {
    return this.rest.request<void, MonthlyPricingCalendarDto>({
      method: 'GET',
      url: `${this.baseUrl}/calendar`,
      params: { hotelId, roomTypeId, ratePlanId, year, month },
    });
  }

  quickEditDailyPricing(input: QuickEditDailyPricingDto): Observable<DailyPricingCellDto> {
    return this.rest.request<QuickEditDailyPricingDto, DailyPricingCellDto>({
      method: 'PUT',
      url: `${this.baseUrl}/daily-quick-edit`,
      body: input,
    });
  }

  previewBulkAdjustment(input: BulkPriceAdjustmentPreviewInputDto): Observable<BulkPriceAdjustmentPreviewDto> {
    return this.rest.request<BulkPriceAdjustmentPreviewInputDto, BulkPriceAdjustmentPreviewDto>({
      method: 'POST',
      url: `${this.baseUrl}/bulk-preview`,
      body: input,
    });
  }

  applyBulkAdjustment(input: ApplyBulkPriceAdjustmentDto): Observable<BulkPriceAdjustmentResultDto> {
    return this.rest.request<ApplyBulkPriceAdjustmentDto, BulkPriceAdjustmentResultDto>({
      method: 'POST',
      url: `${this.baseUrl}/bulk-apply`,
      body: input,
    });
  }

  getPriceOverrides(hotelId: string, roomTypeId: string, ratePlanId: string): Observable<RoomPriceOverrideDto[]> {
    return this.rest.request<void, RoomPriceOverrideDto[]>({
      method: 'GET',
      url: `${this.baseUrl}/overrides`,
      params: { hotelId, roomTypeId, ratePlanId },
    });
  }

  updatePriceOverride(id: string, input: UpdateRoomPriceOverrideDto): Observable<RoomPriceOverrideDto> {
    return this.rest.request<UpdateRoomPriceOverrideDto, RoomPriceOverrideDto>({
      method: 'PUT',
      url: `${this.baseUrl}/overrides/${id}`,
      body: input,
    });
  }

  deletePriceOverride(id: string, hotelId: string): Observable<void> {
    return this.rest.request<void, void>({
      method: 'DELETE',
      url: `${this.baseUrl}/overrides/${id}`,
      params: { hotelId },
    });
  }

  getPricingAuditLogs(input: GetPricingAuditLogsInputDto): Observable<PagedResultDto<PricingAuditLogDto>> {
    const params: Record<string, string | number> = {
      hotelId: input.hotelId,
      skipCount: input.skipCount ?? 0,
      maxResultCount: input.maxResultCount ?? 20,
    };

    if (input.roomTypeId) params['roomTypeId'] = input.roomTypeId;
    if (input.ratePlanId) params['ratePlanId'] = input.ratePlanId;
    if (input.actionType !== null && input.actionType !== undefined) params['actionType'] = input.actionType;
    if (input.fromDate) params['fromDate'] = input.fromDate;
    if (input.toDate) params['toDate'] = input.toDate;

    return this.rest.request<void, PagedResultDto<PricingAuditLogDto>>({
      method: 'GET',
      url: `${this.baseUrl}/audit-logs`,
      params,
    });
  }
}
