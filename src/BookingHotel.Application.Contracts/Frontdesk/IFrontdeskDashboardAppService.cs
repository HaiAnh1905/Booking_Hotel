using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace BookingHotel.Frontdesk;

public interface IFrontdeskDashboardAppService : IApplicationService
{
    Task<FrontdeskDashboardDto> GetOverviewAsync(Guid hotelId);
}
