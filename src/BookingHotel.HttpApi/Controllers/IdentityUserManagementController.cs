using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BookingHotel.Controllers;
using BookingHotel.Identity.Dtos;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace BookingHotel.Identity;

[RemoteService(Name = "IdentityUserManagement")]
[Area("app")]
[Route("api/app/identity-user-management")]
public class IdentityUserManagementController : BookingHotelController, IIdentityUserManagementAppService
{
    private readonly IIdentityUserManagementAppService _userManagementAppService;

    public IdentityUserManagementController(IIdentityUserManagementAppService userManagementAppService)
    {
        _userManagementAppService = userManagementAppService;
    }

    [HttpGet]
    public Task<PagedResultDto<UserWithAssignmentDto>> GetListAsync([FromQuery] GetUsersWithAssignmentInput input)
    {
        return _userManagementAppService.GetListAsync(input);
    }

    [HttpGet("{id}")]
    public Task<UserWithAssignmentDto> GetAsync(Guid id)
    {
        return _userManagementAppService.GetAsync(id);
    }

    [HttpPost]
    public Task<UserWithAssignmentDto> CreateAsync([FromBody] CreateUserWithAssignmentDto input)
    {
        return _userManagementAppService.CreateAsync(input);
    }

    [HttpPut("{id}")]
    public Task<UserWithAssignmentDto> UpdateAsync(Guid id, [FromBody] UpdateUserWithAssignmentDto input)
    {
        return _userManagementAppService.UpdateAsync(id, input);
    }

    [HttpDelete("{id}")]
    public Task DeleteAsync(Guid id)
    {
        return _userManagementAppService.DeleteAsync(id);
    }

    [HttpPost("tenant-staff")]
    public Task<UserWithAssignmentDto> CreateTenantStaffAsync([FromBody] CreateTenantStaffDto input)
    {
        return _userManagementAppService.CreateTenantStaffAsync(input);
    }

    [HttpPut("tenant-staff/{id}")]
    public Task<UserWithAssignmentDto> UpdateTenantStaffAsync(Guid id, [FromBody] UpdateTenantStaffDto input)
    {
        return _userManagementAppService.UpdateTenantStaffAsync(id, input);
    }

    [HttpPost("{id}/transfer-branch")]
    public Task TransferBranchAsync(Guid id, [FromBody] TransferBranchDto input)
    {
        return _userManagementAppService.TransferBranchAsync(id, input);
    }

    [HttpPost("{id}/unlock-brute-force")]
    public Task<UnlockUserResultDto> UnlockBruteForceAsync(Guid id)
    {
        return _userManagementAppService.UnlockBruteForceAsync(id);
    }

    [HttpPost("{id}/unlock")]
    public Task<UnlockUserResultDto> UnlockUserAsync(Guid id, [FromBody] UnlockUserDto? input = null)
    {
        return _userManagementAppService.UnlockUserAsync(id, input);
    }

    [HttpPost("{id}/toggle-status")]
    public Task<UserWithAssignmentDto> ToggleStatusAsync(Guid id)
    {
        return _userManagementAppService.ToggleStatusAsync(id);
    }

    [HttpGet("assignable-roles")]
    public Task<List<string>> GetAssignableRolesAsync()
    {
        return _userManagementAppService.GetAssignableRolesAsync();
    }

    [HttpGet("staff-by-hotels")]
    public Task<List<UserWithAssignmentDto>> GetStaffByHotelsAsync([FromQuery] List<Guid>? hotelIds = null)
    {
        return _userManagementAppService.GetStaffByHotelsAsync(hotelIds);
    }
}
