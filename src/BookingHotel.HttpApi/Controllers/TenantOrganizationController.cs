using System.Threading.Tasks;
using BookingHotel.Controllers;
using BookingHotel.Organization;
using BookingHotel.Organization.Dtos;
using BookingHotel.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace BookingHotel.Controllers;

[RemoteService(Name = "TenantOrganization")]
[Area("app")]
[Route("api/app/tenant-organization")]
[Authorize]
public class TenantOrganizationController : BookingHotelController, ITenantOrganizationAppService
{
    private readonly ITenantOrganizationAppService _tenantOrganizationAppService;

    public TenantOrganizationController(ITenantOrganizationAppService tenantOrganizationAppService)
    {
        _tenantOrganizationAppService = tenantOrganizationAppService;
    }

    [HttpGet]
    [Route("profile")]
    [Authorize(BookingHotelPermissions.Hotels.Default)]
    public Task<TenantOrganizationProfileDto> GetProfileAsync()
    {
        return _tenantOrganizationAppService.GetProfileAsync();
    }

    [HttpPut]
    [Route("profile")]
    [Authorize(BookingHotelPermissions.Hotels.ManageBranches)]
    public Task<TenantOrganizationProfileDto> UpdateProfileAsync([FromBody] UpdateTenantOrganizationDto input)
    {
        return _tenantOrganizationAppService.UpdateProfileAsync(input);
    }
}
