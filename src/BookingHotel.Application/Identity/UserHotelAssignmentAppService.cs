using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BookingHotel.Hotels;
using BookingHotel.Permissions;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Authorization;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.Users;

namespace BookingHotel.Identity;

[Authorize]
public class UserHotelAssignmentAppService : ApplicationService, IUserHotelAssignmentAppService
{
    private readonly IRepository<UserHotelAssignment, Guid> _assignmentRepository;
    private readonly IRepository<Hotel, Guid> _hotelRepository;
    private readonly IdentityUserManager _userManager;

    public UserHotelAssignmentAppService(
        IRepository<UserHotelAssignment, Guid> assignmentRepository,
        IRepository<Hotel, Guid> hotelRepository,
        IdentityUserManager userManager)
    {
        _assignmentRepository = assignmentRepository;
        _hotelRepository = hotelRepository;
        _userManager = userManager;
    }

    public async Task<List<Guid>> GetAssignedHotelIdsAsync(Guid userId)
    {
        await CheckCanInspectUserAssignmentsAsync(userId);

        var assignments = await _assignmentRepository.GetListAsync(x => x.UserId == userId && x.IsActive);
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

        // Receptionist 1-1 single-property constraint (chỉ áp dụng khi tài khoản thuần túy là Lễ tân, không kiêm nhiệm Quản lý)
        var isManager = roles.Contains(BookingHotelRoleConsts.HotelManager);
        var isReceptionist = roles.Contains(BookingHotelRoleConsts.Receptionist);
        if (isReceptionist && !isManager && input.HotelIds.Count > 1)
        {
            throw new UserFriendlyException("Nhân viên Lễ tân chỉ được phân công duy nhất 1 cơ sở khách sạn tại một thời điểm.");
        }

        var existingAssignments = await _assignmentRepository.GetListAsync(x => x.UserId == input.UserId);
        var previouslyAssignedHotelIds = existingAssignments.Where(x => x.IsActive).Select(x => x.HotelId).ToList();
        var inputHotelIds = input.HotelIds.Distinct().ToList();
        var now = Clock.Now;

        // 1. Soft unlinking for assignments not in inputHotelIds
        foreach (var existing in existingAssignments)
        {
            if (!inputHotelIds.Contains(existing.HotelId))
            {
                if (existing.IsActive)
                {
                    existing.Unassign(now);
                    await _assignmentRepository.UpdateAsync(existing);
                }
            }
        }

        // 2. Add or reactivate assignments
        foreach (var hotelId in inputHotelIds)
        {
            var isPrimary = isReceptionist && (input.PrimaryHotelId.HasValue
                ? hotelId == input.PrimaryHotelId.Value
                : inputHotelIds.Count == 1);

            var existing = existingAssignments.FirstOrDefault(x => x.HotelId == hotelId);
            if (existing != null)
            {
                existing.IsActive = true;
                existing.UnassignedAt = null;
                existing.IsPrimary = isPrimary;
                await _assignmentRepository.UpdateAsync(existing);
            }
            else
            {
                var assignment = new UserHotelAssignment(
                    GuidGenerator.Create(),
                    input.UserId,
                    hotelId,
                    CurrentTenant.Id,
                    isPrimary,
                    isActive: true
                );

                await _assignmentRepository.InsertAsync(assignment);
            }
        }

        // 3. Đồng bộ tên người quản lý cho cơ sở khách sạn
        var fullName = $"{user.Surname} {user.Name}".Trim();
        var displayName = string.IsNullOrWhiteSpace(fullName) ? user.UserName : fullName;
        var namesToCheck = new List<string> { user.UserName };
        if (!string.IsNullOrWhiteSpace(displayName))
        {
            namesToCheck.Add(displayName);
        }

        if (isManager)
        {
            foreach (var hotelId in inputHotelIds)
            {
                // Vô hiệu hóa quản lý khác nếu có để tránh 2 manager cùng quản lý 1 khách sạn
                var otherAssignments = await _assignmentRepository.GetListAsync(
                    a => a.HotelId == hotelId && a.IsActive && a.UserId != user.Id);
                foreach (var otherAssign in otherAssignments)
                {
                    var otherUser = await _userManager.FindByIdAsync(otherAssign.UserId.ToString());
                    if (otherUser != null)
                    {
                        var isOtherManager = await _userManager.IsInRoleAsync(otherUser, BookingHotelRoleConsts.HotelManager);
                        var isOtherReceptionist = await _userManager.IsInRoleAsync(otherUser, BookingHotelRoleConsts.Receptionist);
                        if (isOtherManager)
                        {
                            // Nếu nhân sự này kiêm nhiệm Lễ tân, giữ nguyên phân bổ để họ tiếp tục làm Lễ tân
                            if (isOtherReceptionist)
                            {
                                var hasOtherPrimary = await _assignmentRepository.AnyAsync(
                                    a => a.UserId == otherUser.Id && a.HotelId != hotelId && a.IsActive && a.IsPrimary);
                                if (otherAssign.IsPrimary || !hasOtherPrimary)
                                {
                                    if (!otherAssign.IsPrimary)
                                    {
                                        otherAssign.IsPrimary = true;
                                        await _assignmentRepository.UpdateAsync(otherAssign);
                                    }
                                    continue;
                                }
                            }
                            otherAssign.Unassign(Clock.Now);
                            await _assignmentRepository.UpdateAsync(otherAssign);
                        }
                    }
                }

                var hotel = await _hotelRepository.FindAsync(hotelId);
                if (hotel != null)
                {
                    hotel.ManagerName = displayName;
                    await _hotelRepository.UpdateAsync(hotel, autoSave: true);
                }
            }

            var unassignedHotelIds = previouslyAssignedHotelIds.Except(inputHotelIds).ToList();
            foreach (var hotelId in unassignedHotelIds)
            {
                var hotel = await _hotelRepository.FindAsync(hotelId);
                if (hotel != null && namesToCheck.Any(n => string.Equals(n, hotel.ManagerName, StringComparison.OrdinalIgnoreCase) ||
                                                          string.Equals(n, hotel.ManagerName.Split('(')[0].Trim(), StringComparison.OrdinalIgnoreCase)))
                {
                    hotel.ManagerName = string.Empty;
                    await _hotelRepository.UpdateAsync(hotel, autoSave: true);
                }
            }
        }
        else
        {
            foreach (var hotelId in previouslyAssignedHotelIds)
            {
                var hotel = await _hotelRepository.FindAsync(hotelId);
                if (hotel != null && namesToCheck.Any(n => string.Equals(n, hotel.ManagerName, StringComparison.OrdinalIgnoreCase) ||
                                                          string.Equals(n, hotel.ManagerName.Split('(')[0].Trim(), StringComparison.OrdinalIgnoreCase)))
                {
                    hotel.ManagerName = string.Empty;
                    await _hotelRepository.UpdateAsync(hotel, autoSave: true);
                }
            }
        }
    }

    public async Task<bool> HasAccessToHotelAsync(Guid userId, Guid hotelId)
    {
        await CheckCanInspectUserAssignmentsAsync(userId);

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

        return await _assignmentRepository.AnyAsync(x => x.UserId == userId && x.HotelId == hotelId && x.IsActive);
    }

    private async Task CheckCanInspectUserAssignmentsAsync(Guid targetUserId)
    {
        if (CurrentUser.Id == targetUserId)
        {
            return;
        }

        if (CurrentUser.IsInRole(BookingHotelRoleConsts.TenantAdmin) ||
            CurrentUser.IsInRole(BookingHotelRoleConsts.SystemAdmin) ||
            CurrentUser.IsInRole("admin") ||
            await AuthorizationService.IsGrantedAsync(BookingHotelPermissions.Hotels.AssignStaff))
        {
            return;
        }

        throw new AbpAuthorizationException("You are not authorized to inspect another user's hotel assignments.");
    }
}
