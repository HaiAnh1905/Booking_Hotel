using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace BookingHotel.Manager.Reports;

public interface IManagerReportAppService : IApplicationService
{
    Task<HotelPerformanceReportDto> GetPerformanceReportAsync(Guid hotelId, DateTime fromDate, DateTime toDate);
    Task<RevenueBreakdownReportDto> GetRevenueBreakdownAsync(Guid hotelId, DateTime fromDate, DateTime toDate);
    Task<CancellationLossReportDto> GetCancellationLossReportAsync(Guid hotelId, DateTime fromDate, DateTime toDate);
}
