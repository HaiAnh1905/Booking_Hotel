using System.Threading.Tasks;
using BookingHotel.Controllers;
using BookingHotel.Frontdesk;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace BookingHotel.HttpApi.Controllers.Frontdesk;

[RemoteService(Name = "Frontdesk")]
[Area("app")]
[Route("api/app/frontdesk/room-rack")]
public class FrontdeskRoomRackController : BookingHotelController, IFrontdeskRoomRackAppService
{
    private readonly IFrontdeskRoomRackAppService _service;

    public FrontdeskRoomRackController(IFrontdeskRoomRackAppService service)
    {
        _service = service;
    }

    [HttpGet]
    public Task<FrontdeskRoomRackDto> GetAsync([FromQuery] GetFrontdeskRoomRackInputDto input)
    {
        return _service.GetAsync(input);
    }
}
