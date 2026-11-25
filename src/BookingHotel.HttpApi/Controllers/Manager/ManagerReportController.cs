using System;
using System.Threading.Tasks;
using BookingHotel.Controllers;
using BookingHotel.Manager.Reports;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace BookingHotel.HttpApi.Controllers.Manager;

[RemoteService]
[Route("api/app/manager/reports")]
public class ManagerReportController : BookingHotelController, IManagerReportAppService
{
    private readonly IManagerReportAppService _reportAppService;

    public ManagerReportController(IManagerReportAppService reportAppService)
    {
        _reportAppService = reportAppService;
    }

    [HttpGet("performance")]
    public Task<HotelPerformanceReportDto> GetPerformanceReportAsync(
        [FromQuery] Guid hotelId,
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate)
    {
        return _reportAppService.GetPerformanceReportAsync(hotelId, fromDate, toDate);
    }

    [HttpGet("revenue-breakdown")]
    public Task<RevenueBreakdownReportDto> GetRevenueBreakdownAsync(
        [FromQuery] Guid hotelId,
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate)
    {
        return _reportAppService.GetRevenueBreakdownAsync(hotelId, fromDate, toDate);
    }

    [HttpGet("cancellations-loss")]
    public Task<CancellationLossReportDto> GetCancellationLossReportAsync(
        [FromQuery] Guid hotelId,
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate)
    {
        return _reportAppService.GetCancellationLossReportAsync(hotelId, fromDate, toDate);
    }
}
