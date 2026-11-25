using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace BookingHotel.Manager.Dashboard;

public interface IManagerDashboardAppService : IApplicationService
{
    Task<ManagerDashboardOverviewDto> GetKpisAsync(Guid hotelId);
    Task<List<TodayArrivalDto>> GetTodayArrivalsAsync(Guid hotelId, int maxResultCount = 10);
    Task<ManagerActionItemsDto> GetActionItemsAsync(Guid hotelId);
}
