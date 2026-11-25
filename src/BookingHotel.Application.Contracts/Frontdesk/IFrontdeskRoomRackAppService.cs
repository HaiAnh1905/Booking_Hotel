using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace BookingHotel.Frontdesk;

public interface IFrontdeskRoomRackAppService : IApplicationService
{
    Task<FrontdeskRoomRackDto> GetAsync(GetFrontdeskRoomRackInputDto input);
}
