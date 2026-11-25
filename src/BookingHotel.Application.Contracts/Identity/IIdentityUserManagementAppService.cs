using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BookingHotel.Identity.Dtos;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace BookingHotel.Identity;

public interface IIdentityUserManagementAppService : IApplicationService
{
    Task<PagedResultDto<UserWithAssignmentDto>> GetListAsync(GetUsersWithAssignmentInput input);

    Task<UserWithAssignmentDto> GetAsync(Guid id);

    Task<UserWithAssignmentDto> CreateAsync(CreateUserWithAssignmentDto input);

    Task<UserWithAssignmentDto> UpdateAsync(Guid id, UpdateUserWithAssignmentDto input);

    Task DeleteAsync(Guid id);

    Task<UnlockUserResultDto> UnlockUserAsync(Guid id, UnlockUserDto? input = null);

    Task<UserWithAssignmentDto> ToggleStatusAsync(Guid id);

    Task<UserWithAssignmentDto> CreateTenantStaffAsync(CreateTenantStaffDto input);

    Task<UserWithAssignmentDto> UpdateTenantStaffAsync(Guid id, UpdateTenantStaffDto input);

    Task TransferBranchAsync(Guid id, TransferBranchDto input);

    Task<UnlockUserResultDto> UnlockBruteForceAsync(Guid id);

    Task<List<string>> GetAssignableRolesAsync();

    Task<List<UserWithAssignmentDto>> GetStaffByHotelsAsync(List<Guid>? hotelIds = null);
}

