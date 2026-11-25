using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BookingHotel.Controllers;
using BookingHotel.Manager.Guests;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace BookingHotel.HttpApi.Controllers.Manager;

[RemoteService]
[Route("api/app/manager/guests")]
public class ManagerGuestController : BookingHotelController, IManagerGuestAppService
{
    private readonly IManagerGuestAppService _guestAppService;

    public ManagerGuestController(IManagerGuestAppService guestAppService)
    {
        _guestAppService = guestAppService;
    }

    [HttpGet]
    public Task<PagedResultDto<ManagerGuestDto>> GetListAsync(
        [FromQuery] Guid hotelId,
        [FromQuery] string? keyword = null,
        [FromQuery] int skipCount = 0,
        [FromQuery] int maxResultCount = 20)
    {
        return _guestAppService.GetListAsync(hotelId, keyword, skipCount, maxResultCount);
    }

    [HttpGet("{guestId}/history")]
    public Task<List<GuestStayHistoryDto>> GetStayHistoryAsync(Guid guestId, [FromQuery] Guid hotelId)
    {
        return _guestAppService.GetStayHistoryAsync(guestId, hotelId);
    }

    [HttpGet("{guestId}/notes")]
    public Task<List<InternalGuestNoteDto>> GetNotesAsync(Guid guestId, [FromQuery] Guid hotelId)
    {
        return _guestAppService.GetNotesAsync(guestId, hotelId);
    }

    [HttpPost("{guestId}/notes")]
    public Task<InternalGuestNoteDto> CreateNoteAsync([FromBody] CreateInternalGuestNoteDto input)
    {
        return _guestAppService.CreateNoteAsync(input);
    }

    [HttpDelete("notes/{id}")]
    public Task DeleteNoteAsync(Guid id, [FromQuery] Guid hotelId)
    {
        return _guestAppService.DeleteNoteAsync(id, hotelId);
    }
}
