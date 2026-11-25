using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BookingHotel.Controllers;
using BookingHotel.Frontdesk;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace BookingHotel.HttpApi.Controllers.Frontdesk;

[RemoteService(Name = "Frontdesk")]
[Area("app")]
[Route("api/app/frontdesk/reservations")]
public class FrontdeskReservationController : BookingHotelController, IFrontdeskReservationAppService
{
    private readonly IFrontdeskReservationAppService _service;

    public FrontdeskReservationController(IFrontdeskReservationAppService service)
    {
        _service = service;
    }

    [HttpGet]
    public Task<PagedResultDto<FrontdeskReservationListItemDto>> GetListAsync([FromQuery] GetFrontdeskReservationsInputDto input)
    {
        return _service.GetListAsync(input);
    }

    [HttpGet("{id}")]
    public Task<FrontdeskReservationDetailDto> GetAsync(Guid id, [FromQuery] Guid hotelId)
    {
        return _service.GetAsync(id, hotelId);
    }

    [HttpGet("{id}/available-rooms")]
    public Task<List<FrontdeskAvailableRoomDto>> GetAvailableRoomsAsync(Guid id, [FromQuery] Guid hotelId)
    {
        return _service.GetAvailableRoomsAsync(id, hotelId);
    }

    [HttpPost("{id}/confirm")]
    public Task<FrontdeskReservationDetailDto> ConfirmAsync(Guid id, [FromBody] ConfirmFrontdeskReservationDto input)
    {
        return _service.ConfirmAsync(id, input);
    }

    [HttpPost("{id}/assign-room")]
    public Task<FrontdeskReservationDetailDto> AssignRoomAsync(Guid id, [FromBody] AssignFrontdeskRoomDto input)
    {
        return _service.AssignRoomAsync(id, input);
    }

    [HttpPut("{id}")]
    public Task<FrontdeskReservationDetailDto> UpdateAsync(Guid id, [FromBody] UpdateFrontdeskReservationDto input)
    {
        return _service.UpdateAsync(id, input);
    }

    [HttpPost("{id}/cancel")]
    public Task<FrontdeskReservationDetailDto> CancelAsync(Guid id, [FromBody] CancelFrontdeskReservationDto input)
    {
        return _service.CancelAsync(id, input);
    }

    [HttpPost("{id}/check-in")]
    public Task<FrontdeskReservationDetailDto> CheckInAsync(Guid id, [FromBody] FrontdeskCheckInDto input)
    {
        return _service.CheckInAsync(id, input);
    }

    [HttpPost("{id}/check-out")]
    public Task<FrontdeskReservationDetailDto> CheckOutAsync(Guid id, [FromBody] FrontdeskCheckOutDto input)
    {
        return _service.CheckOutAsync(id, input);
    }
}
