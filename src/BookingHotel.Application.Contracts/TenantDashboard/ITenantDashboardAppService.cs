using System;
using System.Threading.Tasks;
using BookingHotel.TenantDashboard.Dtos;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace BookingHotel.TenantDashboard;

public interface ITenantDashboardAppService : IApplicationService
{
    Task<TenantDashboardOverviewDto> GetOverviewAsync(GetTenantDashboardInput input);

    Task<PagedResultDto<TenantBookingSurveillanceDto>> GetSurveillanceBookingsAsync(GetBookingSurveillanceInput input);

    Task<TenantBookingSurveillanceDto> GetSurveillanceBookingDetailAsync(Guid bookingId);
}
