using System;
using System.Threading.Tasks;
using BookingHotel.Controllers;
using BookingHotel.Permissions;
using BookingHotel.TenantDashboard;
using BookingHotel.TenantDashboard.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace BookingHotel.Controllers;

[RemoteService(Name = "TenantDashboard")]
[Area("app")]
[Route("api/app/tenant-dashboard")]
[Authorize]
public class TenantDashboardController : BookingHotelController, ITenantDashboardAppService
{
    private readonly ITenantDashboardAppService _tenantDashboardAppService;

    public TenantDashboardController(ITenantDashboardAppService tenantDashboardAppService)
    {
        _tenantDashboardAppService = tenantDashboardAppService;
    }

    [HttpGet]
    [Route("overview")]
    [Authorize(BookingHotelPermissions.Reports.Consolidated)]
    public Task<TenantDashboardOverviewDto> GetOverviewAsync([FromQuery] GetTenantDashboardInput input)
    {
        return _tenantDashboardAppService.GetOverviewAsync(input);
    }

    [HttpGet]
    [Route("surveillance-bookings")]
    [Authorize(BookingHotelPermissions.Bookings.Default)]
    public Task<PagedResultDto<TenantBookingSurveillanceDto>> GetSurveillanceBookingsAsync([FromQuery] GetBookingSurveillanceInput input)
    {
        return _tenantDashboardAppService.GetSurveillanceBookingsAsync(input);
    }

    [HttpGet]
    [Route("surveillance-bookings/{bookingId}")]
    [Authorize(BookingHotelPermissions.Bookings.Default)]
    public Task<TenantBookingSurveillanceDto> GetSurveillanceBookingDetailAsync(Guid bookingId)
    {
        return _tenantDashboardAppService.GetSurveillanceBookingDetailAsync(bookingId);
    }
}
