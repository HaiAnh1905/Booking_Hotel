using System.Threading.Tasks;
using BookingHotel.TenantReports.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Content;

namespace BookingHotel.TenantReports;

public interface ITenantReportAppService : IApplicationService
{
    Task<RevenueReportDto> GetRevenueReportAsync(GetTenantReportInput input);

    Task<HotelPerformanceReportDto> GetHotelPerformanceReportAsync(GetTenantReportInput input);

    Task<IRemoteStreamContent> ExportReportAsync(string reportType, string format, GetTenantReportInput input);
}
