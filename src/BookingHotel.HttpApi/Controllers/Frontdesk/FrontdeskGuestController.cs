using System;
using System.Threading.Tasks;
using BookingHotel.Controllers;
using BookingHotel.Frontdesk;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace BookingHotel.HttpApi.Controllers.Frontdesk;

[RemoteService(Name = "Frontdesk")]
[Area("app")]
[Route("api/app/frontdesk/guests")]
public class FrontdeskGuestController : BookingHotelController, IFrontdeskGuestAppService
{
    private readonly IFrontdeskGuestAppService _service;

    public FrontdeskGuestController(IFrontdeskGuestAppService service)
    {
        _service = service;
    }

    [HttpGet]
    public Task<PagedResultDto<FrontdeskGuestListItemDto>> GetListAsync([FromQuery] GetFrontdeskGuestsInputDto input)
    {
        return _service.GetListAsync(input);
    }

    [HttpGet("detail")]
    public Task<FrontdeskGuestDetailDto> GetDetailAsync([FromQuery] Guid hotelId, [FromQuery] string guestKey)
    {
        return _service.GetDetailAsync(hotelId, guestKey);
    }

    [HttpPut("detail")]
    public Task<FrontdeskGuestDetailDto> UpdateAsync([FromQuery] string guestKey, [FromBody] UpdateFrontdeskGuestDto input)
    {
        return _service.UpdateAsync(guestKey, input);
    }
}
