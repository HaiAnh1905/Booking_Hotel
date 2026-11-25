using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BookingHotel.Controllers;
using BookingHotel.Manager.Rooms;
using BookingHotel.Rooms;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace BookingHotel.HttpApi.Controllers.Manager;

[RemoteService]
[Route("api/app/manager/rooms")]
public class ManagerRoomController : BookingHotelController, IManagerRoomAppService
{
    private readonly IManagerRoomAppService _roomAppService;

    public ManagerRoomController(IManagerRoomAppService roomAppService)
    {
        _roomAppService = roomAppService;
    }

    [HttpGet("room-types")]
    public Task<List<RoomTypeDto>> GetRoomTypesAsync([FromQuery] Guid hotelId)
    {
        return _roomAppService.GetRoomTypesAsync(hotelId);
    }

    [HttpPost("room-types")]
    public Task<RoomTypeDto> CreateRoomTypeAsync([FromBody] CreateUpdateRoomTypeDto input)
    {
        return _roomAppService.CreateRoomTypeAsync(input);
    }

    [HttpPut("room-types/{id}")]
    public Task<RoomTypeDto> UpdateRoomTypeAsync(Guid id, [FromBody] CreateUpdateRoomTypeDto input)
    {
        return _roomAppService.UpdateRoomTypeAsync(id, input);
    }

    [HttpDelete("room-types/{id}")]
    public Task DeleteRoomTypeAsync(Guid id, [FromQuery] Guid hotelId)
    {
        return _roomAppService.DeleteRoomTypeAsync(id, hotelId);
    }

    [HttpGet("dashboard-overview")]
    public Task<RoomDashboardOverviewDto> GetDashboardOverviewAsync([FromQuery] Guid hotelId)
    {
        return _roomAppService.GetDashboardOverviewAsync(hotelId);
    }

    [HttpGet("physical-rooms")]
    public Task<List<RoomDto>> GetPhysicalRoomsAsync(
        [FromQuery] Guid hotelId,
        [FromQuery] int? floor = null,
        [FromQuery] Guid? roomTypeId = null,
        [FromQuery] RoomOccupancyStatus? occupancyStatus = null,
        [FromQuery] RoomOperationalStatus? operationalStatus = null,
        [FromQuery] string? search = null)
    {
        return _roomAppService.GetPhysicalRoomsAsync(hotelId, floor, roomTypeId, occupancyStatus, operationalStatus, search);
    }

    [HttpPost("physical-rooms")]
    public Task<RoomDto> CreatePhysicalRoomAsync([FromBody] CreateUpdateRoomDto input)
    {
        return _roomAppService.CreatePhysicalRoomAsync(input);
    }

    [HttpPut("physical-rooms/{id}")]
    public Task<RoomDto> UpdatePhysicalRoomAsync(Guid id, [FromBody] CreateUpdateRoomDto input)
    {
        return _roomAppService.UpdatePhysicalRoomAsync(id, input);
    }

    [HttpPost("physical-rooms/{id}/deactivate")]
    public Task DeactivateRoomAsync(Guid id, [FromBody] DeactivateRoomDto input)
    {
        return _roomAppService.DeactivateRoomAsync(id, input);
    }

    [HttpPost("physical-rooms/{id}/activate")]
    public Task ActivateRoomAsync(Guid id, [FromQuery] Guid hotelId)
    {
        return _roomAppService.ActivateRoomAsync(id, hotelId);
    }

    [HttpDelete("physical-rooms/{id}")]
    public Task DeletePhysicalRoomAsync(Guid id, [FromQuery] Guid hotelId)
    {
        return _roomAppService.DeletePhysicalRoomAsync(id, hotelId);
    }

    [HttpPost("{id}/change-operational-status")]
    public Task<RoomDto> ChangeOperationalStatusAsync(Guid id, [FromBody] ChangeOperationalStatusDto input)
    {
        return _roomAppService.ChangeOperationalStatusAsync(id, input);
    }

    [HttpPost("{id}/update-housekeeping")]
    public Task<RoomDto> UpdateHousekeepingStatusAsync(Guid id, [FromBody] UpdateHousekeepingStatusDto input)
    {
        return _roomAppService.UpdateHousekeepingStatusAsync(id, input);
    }

    [HttpPost("closures/check-affected-bookings")]
    public Task<List<AffectedBookingDto>> CheckAffectedBookingsAsync([FromBody] CheckAffectedBookingsInputDto input)
    {
        return _roomAppService.CheckAffectedBookingsAsync(input);
    }

    [HttpPost("closures/planned")]
    public Task<List<RoomClosureDto>> CreatePlannedClosureAsync([FromBody] CreatePlannedClosureDto input)
    {
        return _roomAppService.CreatePlannedClosureAsync(input);
    }

    [HttpPost("closures/emergency")]
    public Task<List<RoomClosureDto>> CreateEmergencyClosureAsync([FromBody] CreateEmergencyClosureDto input)
    {
        return _roomAppService.CreateEmergencyClosureAsync(input);
    }

    [HttpGet("closures/history")]
    public Task<List<RoomClosureDto>> GetClosureHistoryAsync(
        [FromQuery] Guid hotelId,
        [FromQuery] RoomClosureType? closureType = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        return _roomAppService.GetClosureHistoryAsync(hotelId, closureType, fromDate, toDate);
    }

    [HttpGet("closures/alternative-suggestions")]
    public Task<List<AlternativeRoomSuggestionDto>> GetAlternativeRoomSuggestionsAsync(
        [FromQuery] Guid hotelId,
        [FromQuery] Guid bookingId,
        [FromQuery] AffectedBookingResolutionType resolutionType)
    {
        return _roomAppService.GetAlternativeRoomSuggestionsAsync(hotelId, bookingId, resolutionType);
    }

    [HttpPost("closures/resolve-booking")]
    public Task ResolveAffectedBookingAsync([FromBody] ResolveAffectedBookingDto input)
    {
        return _roomAppService.ResolveAffectedBookingAsync(input);
    }

    [HttpPost("closures/{closureId}/confirm-recovery")]
    public Task ConfirmRoomRecoveryAsync(Guid closureId, [FromBody] ConfirmRoomRecoveryDto input)
    {
        return _roomAppService.ConfirmRoomRecoveryAsync(closureId, input);
    }
}
