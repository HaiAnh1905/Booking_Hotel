using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BookingHotel.Permissions;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;

namespace BookingHotel.Identity;

[Authorize]
public class UserHotelAssignmentAppService : ApplicationService, IUserHotelAssignmentAppService
{
    private readonly IRepository<UserHotelAssignment, Guid> _assignmentRepository;
    private readonly IdentityUserManager _userManager;

    public UserHotelAssignmentAppService(
        IRepository<UserHotelAssignment, Guid> assignmentRepository,
        IdentityUserManager userManager)
    {
        _assignmentRepository = assignmentRepository;
        _userManager = userManager;
    }

    public async Task<List<Guid>> GetAssignedHotelIdsAsync(Guid userId)
    {
        var assignments = await _assignmentRepository.GetListAsync(x => x.UserId == userId);
        return assignments.Select(x => x.HotelId).Distinct().ToList();
    }

    [Authorize(BookingHotelPermissions.Hotels.AssignStaff)]
    public async Task AssignUserToHotelsAsync(AssignUserHotelDto input)
    {
        var user = await _userManager.FindByIdAsync(input.UserId.ToString());
        if (user == null)
        {
            throw new EntityNotFoundException(typeof(IdentityUser), input.UserId);
        }

        var roles = await _userManager.GetRolesAsync(user);

        // Receptionist 1-1 single-property constraint
        if (roles.Contains(BookingHotelRoleConsts.Receptionist) && input.HotelIds.Count > 1)
        {
            throw new UserFriendlyException("Nhân viên Lễ tân chỉ được phân công duy nhất 1 cơ sở khách sạn tại một thời điểm.");
        }

        var existing = await _assignmentRepository.GetListAsync(x => x.UserId == input.UserId);
        if (existing.Count > 0)
        {
            await _assignmentRepository.DeleteManyAsync(existing);
        }

        var hotelIds = input.HotelIds.Distinct().ToList();
        foreach (var hotelId in hotelIds)
        {
            var isPrimary = input.PrimaryHotelId.HasValue
                ? hotelId == input.PrimaryHotelId.Value
                : hotelIds.Count == 1;

            var assignment = new UserHotelAssignment(
                GuidGenerator.Create(),
                input.UserId,
                hotelId,
                CurrentTenant.Id,
                isPrimary
            );

            await _assignmentRepository.InsertAsync(assignment);
        }
    }

    public async Task<bool> HasAccessToHotelAsync(Guid userId, Guid hotelId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return false;
        }

        var roles = await _userManager.GetRolesAsync(user);
        if (roles.Contains(BookingHotelRoleConsts.TenantAdmin))
        {
            return true;
        }

        return await _assignmentRepository.AnyAsync(x => x.UserId == userId && x.HotelId == hotelId);
    }
}
