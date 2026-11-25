using System.Threading.Tasks;
using BookingHotel.Controllers;
using BookingHotel.SystemDashboard.Dtos;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace BookingHotel.SystemDashboard;

[RemoteService(Name = "SystemDashboard")]
[Area("app")]
[Route("api/app/system-dashboard")]
public class SystemDashboardController : BookingHotelController, ISystemDashboardAppService
{
    private readonly ISystemDashboardAppService _dashboardAppService;

    public SystemDashboardController(ISystemDashboardAppService dashboardAppService)
    {
        _dashboardAppService = dashboardAppService;
    }

    [HttpGet]
    [Route("overview")]
    public Task<SystemDashboardOverviewDto> GetOverviewAsync()
    {
        return _dashboardAppService.GetOverviewAsync();
    }
}
