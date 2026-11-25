using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace BookingHotel.Frontdesk;

public interface IFrontdeskGuestAppService : IApplicationService
{
    Task<PagedResultDto<FrontdeskGuestListItemDto>> GetListAsync(GetFrontdeskGuestsInputDto input);
    Task<FrontdeskGuestDetailDto> GetDetailAsync(Guid hotelId, string guestKey);
    Task<FrontdeskGuestDetailDto> UpdateAsync(string guestKey, UpdateFrontdeskGuestDto input);
}
