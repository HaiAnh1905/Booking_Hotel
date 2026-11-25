using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BookingHotel.Controllers;
using BookingHotel.Manager.Dashboard;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace BookingHotel.HttpApi.Controllers.Manager;

[RemoteService]
[Route("api/app/manager/dashboard")]
public class ManagerDashboardController : BookingHotelController, IManagerDashboardAppService
{
    private readonly IManagerDashboardAppService _dashboardAppService;

    public ManagerDashboardController(IManagerDashboardAppService dashboardAppService)
    {
        _dashboardAppService = dashboardAppService;
    }

    [HttpGet("kpis")]
    public Task<ManagerDashboardOverviewDto> GetKpisAsync([FromQuery] Guid hotelId)
    {
        return _dashboardAppService.GetKpisAsync(hotelId);
    }

    [HttpGet("today-arrivals")]
    public Task<List<TodayArrivalDto>> GetTodayArrivalsAsync([FromQuery] Guid hotelId, [FromQuery] int maxResultCount = 10)
    {
        return _dashboardAppService.GetTodayArrivalsAsync(hotelId, maxResultCount);
    }

    [HttpGet("action-items")]
    public Task<ManagerActionItemsDto> GetActionItemsAsync([FromQuery] Guid hotelId)
    {
        return _dashboardAppService.GetActionItemsAsync(hotelId);
    }
}
