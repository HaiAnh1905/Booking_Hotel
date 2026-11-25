using System.Threading.Tasks;
using BookingHotel.Controllers;
using BookingHotel.Permissions;
using BookingHotel.TenantReports;
using BookingHotel.TenantReports.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Content;

namespace BookingHotel.Controllers;

[RemoteService(Name = "TenantReport")]
[Area("app")]
[Route("api/app/tenant-reports")]
[Authorize]
public class TenantReportController : BookingHotelController, ITenantReportAppService
{
    private readonly ITenantReportAppService _tenantReportAppService;

    public TenantReportController(ITenantReportAppService tenantReportAppService)
    {
        _tenantReportAppService = tenantReportAppService;
    }

    [HttpGet]
    [Route("revenue")]
    [Authorize(BookingHotelPermissions.Reports.Consolidated)]
    public Task<RevenueReportDto> GetRevenueReportAsync([FromQuery] GetTenantReportInput input)
    {
        return _tenantReportAppService.GetRevenueReportAsync(input);
    }

    [HttpGet]
    [Route("hotel-performance")]
    [Authorize(BookingHotelPermissions.Reports.Consolidated)]
    public Task<HotelPerformanceReportDto> GetHotelPerformanceReportAsync([FromQuery] GetTenantReportInput input)
    {
        return _tenantReportAppService.GetHotelPerformanceReportAsync(input);
    }

    [HttpGet]
    [Route("export")]
    [Authorize(BookingHotelPermissions.Reports.Consolidated)]
    public Task<IRemoteStreamContent> ExportReportAsync([FromQuery] string reportType, [FromQuery] string format, [FromQuery] GetTenantReportInput input)
    {
        return _tenantReportAppService.ExportReportAsync(reportType, format, input);
    }
}
