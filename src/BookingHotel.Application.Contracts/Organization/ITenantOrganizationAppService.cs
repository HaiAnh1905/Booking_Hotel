using System.Threading.Tasks;
using BookingHotel.Organization.Dtos;
using Volo.Abp.Application.Services;

namespace BookingHotel.Organization;

public interface ITenantOrganizationAppService : IApplicationService
{
    Task<TenantOrganizationProfileDto> GetProfileAsync();

    Task<TenantOrganizationProfileDto> UpdateProfileAsync(UpdateTenantOrganizationDto input);
}
