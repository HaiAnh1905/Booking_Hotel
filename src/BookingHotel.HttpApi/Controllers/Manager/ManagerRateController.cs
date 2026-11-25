using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BookingHotel.Controllers;
using BookingHotel.Manager.Rooms;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace BookingHotel.HttpApi.Controllers.Manager;

[RemoteService]
[Route("api/app/manager/rooms/pricing")]
public class ManagerRateController : BookingHotelController, IManagerRateAppService
{
    private readonly IManagerRateAppService _rateAppService;

    public ManagerRateController(IManagerRateAppService rateAppService)
    {
        _rateAppService = rateAppService;
    }

    [HttpGet("context")]
    public Task<RoomTypePricingContextDto> GetPricingContextAsync(
        [FromQuery] Guid hotelId,
        [FromQuery] Guid roomTypeId)
    {
        return _rateAppService.GetPricingContextAsync(hotelId, roomTypeId);
    }

    [HttpPost("rate-plans/toggle")]
    public Task<RoomRatePlanItemDto> ToggleRatePlanStatusAsync([FromBody] ToggleRoomRatePlanStatusDto input)
    {
        return _rateAppService.ToggleRatePlanStatusAsync(input);
    }

    [HttpPut("rate-plans/base-price")]
    public Task<RoomRatePlanItemDto> UpdateRatePlanBasePriceAsync([FromBody] UpdateRoomRatePlanBasePriceDto input)
    {
        return _rateAppService.UpdateRatePlanBasePriceAsync(input);
    }

    [HttpGet("calendar")]
    public Task<MonthlyPricingCalendarDto> GetMonthlyCalendarAsync(
        [FromQuery] Guid hotelId,
        [FromQuery] Guid roomTypeId,
        [FromQuery] Guid ratePlanId,
        [FromQuery] int year,
        [FromQuery] int month)
    {
        return _rateAppService.GetMonthlyCalendarAsync(hotelId, roomTypeId, ratePlanId, year, month);
    }

    [HttpPut("daily-quick-edit")]
    public Task<DailyPricingCellDto> QuickEditDailyPricingAsync([FromBody] QuickEditDailyPricingDto input)
    {
        return _rateAppService.QuickEditDailyPricingAsync(input);
    }

    [HttpPost("bulk-preview")]
    public Task<BulkPriceAdjustmentPreviewDto> PreviewBulkAdjustmentAsync([FromBody] BulkPriceAdjustmentPreviewInputDto input)
    {
        return _rateAppService.PreviewBulkAdjustmentAsync(input);
    }

    [HttpPost("bulk-apply")]
    public Task<BulkPriceAdjustmentResultDto> ApplyBulkAdjustmentAsync([FromBody] ApplyBulkPriceAdjustmentDto input)
    {
        return _rateAppService.ApplyBulkAdjustmentAsync(input);
    }

    [HttpGet("overrides")]
    public Task<List<RoomPriceOverrideDto>> GetPriceOverridesAsync(
        [FromQuery] Guid hotelId,
        [FromQuery] Guid roomTypeId,
        [FromQuery] Guid ratePlanId)
    {
        return _rateAppService.GetPriceOverridesAsync(hotelId, roomTypeId, ratePlanId);
    }

    [HttpPut("overrides/{id}")]
    public Task<RoomPriceOverrideDto> UpdatePriceOverrideAsync(
        Guid id,
        [FromBody] UpdateRoomPriceOverrideDto input)
    {
        return _rateAppService.UpdatePriceOverrideAsync(id, input);
    }

    [HttpDelete("overrides/{id}")]
    public Task DeletePriceOverrideAsync(Guid id, [FromQuery] Guid hotelId)
    {
        return _rateAppService.DeletePriceOverrideAsync(id, hotelId);
    }

    [HttpGet("audit-logs")]
    public Task<PagedResultDto<PricingAuditLogDto>> GetPricingAuditLogsAsync([FromQuery] GetPricingAuditLogsInputDto input)
    {
        return _rateAppService.GetPricingAuditLogsAsync(input);
    }
}
