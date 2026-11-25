using System.Threading.Tasks;
using BookingHotel.SystemDashboard.Dtos;
using Volo.Abp.Application.Services;

namespace BookingHotel.SystemDashboard;

public interface ISystemDashboardAppService : IApplicationService
{
    Task<SystemDashboardOverviewDto> GetOverviewAsync();
}
