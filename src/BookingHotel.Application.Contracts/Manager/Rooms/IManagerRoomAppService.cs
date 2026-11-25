using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BookingHotel.Rooms;
using Volo.Abp.Application.Services;

namespace BookingHotel.Manager.Rooms;

public interface IManagerRoomAppService : IApplicationService
{
    Task<List<RoomTypeDto>> GetRoomTypesAsync(Guid hotelId);
    Task<RoomTypeDto> CreateRoomTypeAsync(CreateUpdateRoomTypeDto input);
    Task<RoomTypeDto> UpdateRoomTypeAsync(Guid id, CreateUpdateRoomTypeDto input);
    Task DeleteRoomTypeAsync(Guid id, Guid hotelId);

    Task<RoomDashboardOverviewDto> GetDashboardOverviewAsync(Guid hotelId);

    Task<List<RoomDto>> GetPhysicalRoomsAsync(
        Guid hotelId,
        int? floor = null,
        Guid? roomTypeId = null,
        RoomOccupancyStatus? occupancyStatus = null,
        RoomOperationalStatus? operationalStatus = null,
        string? search = null);
    Task<RoomDto> CreatePhysicalRoomAsync(CreateUpdateRoomDto input);
    Task<RoomDto> UpdatePhysicalRoomAsync(Guid id, CreateUpdateRoomDto input);
    Task DeactivateRoomAsync(Guid id, DeactivateRoomDto input);
    Task ActivateRoomAsync(Guid id, Guid hotelId);
    Task DeletePhysicalRoomAsync(Guid id, Guid hotelId);

    Task<RoomDto> ChangeOperationalStatusAsync(Guid id, ChangeOperationalStatusDto input);
    Task<RoomDto> UpdateHousekeepingStatusAsync(Guid id, UpdateHousekeepingStatusDto input);

    Task<List<AffectedBookingDto>> CheckAffectedBookingsAsync(CheckAffectedBookingsInputDto input);
    Task<List<RoomClosureDto>> CreatePlannedClosureAsync(CreatePlannedClosureDto input);
    Task<List<RoomClosureDto>> CreateEmergencyClosureAsync(CreateEmergencyClosureDto input);
    Task<List<RoomClosureDto>> GetClosureHistoryAsync(Guid hotelId, RoomClosureType? closureType = null, DateTime? fromDate = null, DateTime? toDate = null);
    Task<List<AlternativeRoomSuggestionDto>> GetAlternativeRoomSuggestionsAsync(Guid hotelId, Guid bookingId, AffectedBookingResolutionType resolutionType);
    Task ResolveAffectedBookingAsync(ResolveAffectedBookingDto input);
    Task ConfirmRoomRecoveryAsync(Guid closureId, ConfirmRoomRecoveryDto input);
}

public interface IManagerRateAppService : IApplicationService
{
    Task<RoomTypePricingContextDto> GetPricingContextAsync(Guid hotelId, Guid roomTypeId);
    Task<RoomRatePlanItemDto> ToggleRatePlanStatusAsync(ToggleRoomRatePlanStatusDto input);
    Task<RoomRatePlanItemDto> UpdateRatePlanBasePriceAsync(UpdateRoomRatePlanBasePriceDto input);
    Task<MonthlyPricingCalendarDto> GetMonthlyCalendarAsync(Guid hotelId, Guid roomTypeId, Guid ratePlanId, int year, int month);
    Task<DailyPricingCellDto> QuickEditDailyPricingAsync(QuickEditDailyPricingDto input);
    Task<BulkPriceAdjustmentPreviewDto> PreviewBulkAdjustmentAsync(BulkPriceAdjustmentPreviewInputDto input);
    Task<BulkPriceAdjustmentResultDto> ApplyBulkAdjustmentAsync(ApplyBulkPriceAdjustmentDto input);
    Task<List<RoomPriceOverrideDto>> GetPriceOverridesAsync(Guid hotelId, Guid roomTypeId, Guid ratePlanId);
    Task<RoomPriceOverrideDto> UpdatePriceOverrideAsync(Guid id, UpdateRoomPriceOverrideDto input);
    Task DeletePriceOverrideAsync(Guid id, Guid hotelId);
    Task<Volo.Abp.Application.Dtos.PagedResultDto<PricingAuditLogDto>> GetPricingAuditLogsAsync(GetPricingAuditLogsInputDto input);
}
