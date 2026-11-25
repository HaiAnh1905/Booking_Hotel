using System;
using System.Threading.Tasks;
using BookingHotel.Controllers;
using BookingHotel.Frontdesk;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace BookingHotel.HttpApi.Controllers.Frontdesk;

[RemoteService(Name = "Frontdesk")]
[Area("app")]
[Route("api/app/frontdesk/dashboard")]
public class FrontdeskDashboardController : BookingHotelController, IFrontdeskDashboardAppService
{
    private readonly IFrontdeskDashboardAppService _service;

    public FrontdeskDashboardController(IFrontdeskDashboardAppService service)
    {
        _service = service;
    }

    [HttpGet("overview")]
    public Task<FrontdeskDashboardDto> GetOverviewAsync([FromQuery] Guid hotelId)
    {
        return _service.GetOverviewAsync(hotelId);
    }
}
