using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace BookingHotel.Manager.Hotels;

public interface IManagerHotelProfileAppService : IApplicationService
{
    Task<ManagerHotelProfileDto> GetAsync(Guid hotelId);
    Task<ManagerHotelProfileDto> UpdateAsync(UpdateManagerHotelProfileDto input);
}
