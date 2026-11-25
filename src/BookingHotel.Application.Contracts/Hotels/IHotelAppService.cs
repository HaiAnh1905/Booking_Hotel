using System;
using System.Threading.Tasks;
using BookingHotel.Hotels.Dtos;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace BookingHotel.Hotels;

public interface IHotelAppService : IApplicationService
{
    Task<PagedResultDto<HotelDto>> GetListAsync(GetHotelsInput input);
    Task<HotelDto> GetAsync(Guid id);
    Task<HotelDto> CreateAsync(CreateUpdateHotelDto input);
    Task<HotelDto> UpdateAsync(Guid id, CreateUpdateHotelDto input);
    Task DeleteAsync(Guid id);
    Task<HotelSuspensionResultDto> SuspendAsync(Guid id, SuspendHotelInputDto input);
    Task<HotelDto> ActivateAsync(Guid id);
    Task<HotelDto> AssignManagerAsync(Guid id, AssignHotelManagerDto input);
    Task<bool> CheckNameExistsAsync(string name, Guid? excludeId = null);
}
