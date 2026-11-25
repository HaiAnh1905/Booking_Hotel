using System;
using System.Threading.Tasks;
using BookingHotel.Controllers;
using BookingHotel.Manager.Bookings;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace BookingHotel.HttpApi.Controllers.Manager;

[RemoteService]
[Route("api/app/manager/bookings")]
public class ManagerBookingController : BookingHotelController, IManagerBookingAppService
{
    private readonly IManagerBookingAppService _bookingAppService;

    public ManagerBookingController(IManagerBookingAppService bookingAppService)
    {
        _bookingAppService = bookingAppService;
    }

    [HttpGet]
    public Task<PagedResultDto<ManagerBookingDto>> GetListAsync([FromQuery] GetManagerBookingsInputDto input)
    {
        return _bookingAppService.GetListAsync(input);
    }

    [HttpGet("{id}")]
    public Task<ManagerBookingDto> GetAsync(Guid id, [FromQuery] Guid hotelId)
    {
        return _bookingAppService.GetAsync(id, hotelId);
    }

    [HttpPost("direct-booking")]
    public Task<ManagerBookingDto> CreateDirectBookingAsync([FromBody] CreateDirectBookingDto input)
    {
        return _bookingAppService.CreateDirectBookingAsync(input);
    }

    [HttpPost("{id}/confirm")]
    public Task<ManagerBookingDto> ConfirmAsync(Guid id, [FromQuery] Guid hotelId)
    {
        return _bookingAppService.ConfirmAsync(id, hotelId);
    }

    [HttpPost("{id}/reject")]
    public Task<ManagerBookingDto> RejectAsync(Guid id, [FromBody] RejectBookingDto input)
    {
        return _bookingAppService.RejectAsync(id, input);
    }

    [HttpPost("{id}/mark-no-show")]
    public Task<NoShowResultDto> MarkNoShowAsync(Guid id, [FromBody] MarkNoShowDto input)
    {
        return _bookingAppService.MarkNoShowAsync(id, input);
    }
}
