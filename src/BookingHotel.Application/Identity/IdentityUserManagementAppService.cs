using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BookingHotel.Identity.Dtos;
using BookingHotel.Hotels;
using BookingHotel.Localization;
using BookingHotel.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Data;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.MultiTenancy;

namespace BookingHotel.Identity;

[RemoteService(IsEnabled = false)]
[Authorize]
public class IdentityUserManagementAppService : ApplicationService, IIdentityUserManagementAppService
{
    private readonly IdentityUserManager _userManager;
    private readonly IdentityRoleManager _roleManager;
    private readonly IRepository<IdentityUser, Guid> _userRepository;
    private readonly IRepository<UserHotelAssignment, Guid> _assignmentRepository;
    private readonly IRepository<Hotel, Guid> _hotelRepository;
    private readonly IBruteForceManager _bruteForceManager;
    private readonly IdentitySecurityLogManager _securityLogManager;

    public IdentityUserManagementAppService(
        IdentityUserManager userManager,
        IdentityRoleManager roleManager,
        IRepository<IdentityUser, Guid> userRepository,
        IRepository<UserHotelAssignment, Guid> assignmentRepository,
        IRepository<Hotel, Guid> hotelRepository,
        IBruteForceManager bruteForceManager,
        IdentitySecurityLogManager securityLogManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _userRepository = userRepository;
        _assignmentRepository = assignmentRepository;
        _hotelRepository = hotelRepository;
        _bruteForceManager = bruteForceManager;
        _securityLogManager = securityLogManager;
        LocalizationResource = typeof(BookingHotelResource);
    }

    private async Task CheckUserPermissionAsync(string permissionName)
    {
        if (CurrentUser.IsInRole(BookingHotelRoleConsts.SystemAdmin) ||
            CurrentUser.IsInRole("admin") ||
            CurrentUser.IsInRole(BookingHotelRoleConsts.TenantAdmin))
        {
            return;
        }

        if (await AuthorizationService.IsGrantedAsync(BookingHotelPermissions.Tenants.Default) ||
            await AuthorizationService.IsGrantedAsync(BookingHotelPermissions.Hotels.AssignStaff) ||
            await AuthorizationService.IsGrantedAsync(BookingHotelPermissions.Hotels.Default))
        {
            return;
        }

        await AuthorizationService.CheckAsync(permissionName);
    }

    private async Task CheckStaffPermissionAsync()
    {
        if (CurrentUser.IsInRole(BookingHotelRoleConsts.SystemAdmin) ||
            CurrentUser.IsInRole("admin") ||
            CurrentUser.IsInRole(BookingHotelRoleConsts.TenantAdmin))
        {
            return;
        }

        if (await AuthorizationService.IsGrantedAsync(BookingHotelPermissions.Hotels.AssignStaff) ||
            await AuthorizationService.IsGrantedAsync(BookingHotelPermissions.Tenants.Default) ||
            await AuthorizationService.IsGrantedAsync(IdentityPermissions.Users.Update))
        {
            return;
        }

        await AuthorizationService.CheckAsync(BookingHotelPermissions.Hotels.AssignStaff);
    }

    public async Task<PagedResultDto<UserWithAssignmentDto>> GetListAsync(GetUsersWithAssignmentInput input)
    {
        await CheckUserPermissionAsync(IdentityPermissions.Users.Default);

        var query = await _userRepository.GetQueryableAsync();

        // Host filter: If Host, restrict to TenantId == null
        if (CurrentTenant.Id == null)
        {
            query = query.Where(u => u.TenantId == null);
        }

        if (!string.IsNullOrWhiteSpace(input.Filter))
        {
            var filter = input.Filter.Trim().ToLower();
            query = query.Where(u =>
                (u.UserName != null && u.UserName.ToLower().Contains(filter)) ||
                (u.Email != null && u.Email.ToLower().Contains(filter)) ||
                (u.Name != null && u.Name.ToLower().Contains(filter)) ||
                (u.Surname != null && u.Surname.ToLower().Contains(filter)) ||
                (u.PhoneNumber != null && u.PhoneNumber.Contains(filter)));
        }

        if (input.IsActive.HasValue)
        {
            query = query.Where(u => u.IsActive == input.IsActive.Value);
        }

        if (input.IsLockedOut.HasValue)
        {
            var now = DateTimeOffset.UtcNow;
            if (input.IsLockedOut.Value)
            {
                query = query.Where(u =>
                    !u.IsActive ||
                    (u.LockoutEnd.HasValue && u.LockoutEnd.Value > now));
            }
            else
            {
                query = query.Where(u =>
                    u.IsActive &&
                    (!u.LockoutEnd.HasValue || u.LockoutEnd.Value <= now));
            }
        }

        if (input.HotelId.HasValue)
        {
            var userIdsWithHotel = (await _assignmentRepository.GetListAsync(
                    a => a.HotelId == input.HotelId.Value && a.IsActive))
                .Select(a => a.UserId)
                .Distinct()
                .ToList();

            query = query.Where(u => userIdsWithHotel.Contains(u.Id));
        }

        var totalCount = await AsyncExecuter.CountAsync(query);

        var pagedQuery = query
            .OrderByDescending(u => u.CreationTime)
            .Skip(input.SkipCount)
            .Take(input.MaxResultCount);

        var users = await AsyncExecuter.ToListAsync(pagedQuery);

        var userIds = users.Select(u => u.Id).ToList();
        var allAssignments = await _assignmentRepository.GetListAsync(
            a => userIds.Contains(a.UserId) && a.IsActive);

        var hotelIdsInAssignments = allAssignments.Select(a => a.HotelId).Distinct().ToList();
        var hotelsInAssignments = await _hotelRepository.GetListAsync(h => hotelIdsInAssignments.Contains(h.Id));
        var hotelDict = hotelsInAssignments.ToDictionary(h => h.Id);

        var dtos = new List<UserWithAssignmentDto>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);

            if (!string.IsNullOrWhiteSpace(input.RoleName) &&
                !roles.Contains(input.RoleName, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            var userAssignments = allAssignments.Where(a => a.UserId == user.Id).ToList();
            var isLocked = (!user.IsActive && user.GetProperty<string>(BookingHotelIdentityConsts.DeactivatedReasonProperty) == BookingHotelIdentityConsts.DeactivatedReasonBruteForce)
                           || (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow);

            var isReceptionist = roles.Contains(BookingHotelRoleConsts.Receptionist, StringComparer.OrdinalIgnoreCase);

            dtos.Add(new UserWithAssignmentDto
            {
                Id = user.Id,
                UserName = user.UserName,
                Name = user.Name,
                Surname = user.Surname,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                IsActive = user.IsActive,
                LockoutEnd = user.LockoutEnd,
                IsLockedOut = isLocked,
                ShouldChangePasswordOnNextLogin = user.GetProperty<bool>(BookingHotelIdentityConsts.ShouldChangePasswordOnNextLoginProperty),
                Roles = roles.Select(r => new UserRoleDto { Name = r }).ToList(),
                RoleNames = roles.ToList(),
                AssignedHotels = userAssignments.Select(a => new AssignedHotelDto
                {
                    HotelId = a.HotelId,
                    HotelName = hotelDict.TryGetValue(a.HotelId, out var h) ? h.Name : $"Hotel {a.HotelId.ToString()[..8]}",
                    HotelCode = hotelDict.TryGetValue(a.HotelId, out var hc) ? hc.Code : a.HotelId.ToString()[..6].ToUpper(),
                    IsPrimary = isReceptionist && a.IsPrimary,
                    IsActive = a.IsActive,
                    AssignedAt = a.AssignedDate,
                    UnassignedAt = a.UnassignedAt
                }).ToList(),
                CreationTime = user.CreationTime,
                ConcurrencyStamp = user.ConcurrencyStamp
            });
        }

        return new PagedResultDto<UserWithAssignmentDto>(totalCount, dtos);
    }

    public async Task<UserWithAssignmentDto> GetAsync(Guid id)
    {
        await CheckUserPermissionAsync(IdentityPermissions.Users.Default);
        var user = await FindUserWithCrossTenantCheckAsync(id);

        var roles = await _userManager.GetRolesAsync(user);
        var assignments = await _assignmentRepository.GetListAsync(a => a.UserId == user.Id && a.IsActive);
        var isLocked = (!user.IsActive && user.GetProperty<string>(BookingHotelIdentityConsts.DeactivatedReasonProperty) == BookingHotelIdentityConsts.DeactivatedReasonBruteForce)
                       || (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow);

        var hotelIds = assignments.Select(a => a.HotelId).Distinct().ToList();
        var hotels = await _hotelRepository.GetListAsync(h => hotelIds.Contains(h.Id));
        var hotelDict = hotels.ToDictionary(h => h.Id);

        return new UserWithAssignmentDto
        {
            Id = user.Id,
            UserName = user.UserName,
            Name = user.Name,
            Surname = user.Surname,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            IsActive = user.IsActive,
            LockoutEnd = user.LockoutEnd,
            IsLockedOut = isLocked,
            ShouldChangePasswordOnNextLogin = user.GetProperty<bool>(BookingHotelIdentityConsts.ShouldChangePasswordOnNextLoginProperty),
            Roles = roles.Select(r => new UserRoleDto { Name = r }).ToList(),
            RoleNames = roles.ToList(),
            AssignedHotels = assignments.Select(a => new AssignedHotelDto
            {
                HotelId = a.HotelId,
                HotelName = hotelDict.TryGetValue(a.HotelId, out var h) ? h.Name : $"Hotel {a.HotelId.ToString()[..8]}",
                HotelCode = hotelDict.TryGetValue(a.HotelId, out var hc) ? hc.Code : a.HotelId.ToString()[..6].ToUpper(),
                IsPrimary = roles.Contains(BookingHotelRoleConsts.Receptionist, StringComparer.OrdinalIgnoreCase) && a.IsPrimary,
                IsActive = a.IsActive,
                AssignedAt = a.AssignedDate,
                UnassignedAt = a.UnassignedAt
            }).ToList(),
            CreationTime = user.CreationTime,
            ConcurrencyStamp = user.ConcurrencyStamp
        };
    }

    public async Task<UserWithAssignmentDto> CreateAsync(CreateUserWithAssignmentDto input)
    {
        await CheckUserPermissionAsync(IdentityPermissions.Users.Create);

        if (!IsPasswordComplex(input.Password))
        {
            throw new BusinessException("BookingHotel:WeakPassword");
        }

        ValidateAssignableRoles(input.RoleNames);

        var isHotelManager = input.RoleNames.Contains(BookingHotelRoleConsts.HotelManager, StringComparer.OrdinalIgnoreCase);
        var isReceptionist = input.RoleNames.Contains(BookingHotelRoleConsts.Receptionist, StringComparer.OrdinalIgnoreCase);

        if (isReceptionist && !isHotelManager)
        {
            if (input.HotelIds == null || input.HotelIds.Count != 1)
            {
                throw new BusinessException("BookingHotel:ReceptionistSingleHotelRequired");
            }
        }
        else if (isHotelManager || isReceptionist)
        {
            if (input.HotelIds == null || input.HotelIds.Count == 0)
            {
                throw new BusinessException("BookingHotel:AtLeastOneHotelRequired");
            }
        }

        var user = new IdentityUser(GuidGenerator.Create(), input.UserName, input.Email, CurrentTenant.Id)
        {
            Name = input.Name,
            Surname = input.Surname
        };

        if (!string.IsNullOrWhiteSpace(input.PhoneNumber))
        {
            user.SetPhoneNumber(input.PhoneNumber, false);
        }

        user.SetProperty(BookingHotelIdentityConsts.ShouldChangePasswordOnNextLoginProperty, true);

        (await _userManager.CreateAsync(user, input.Password)).CheckErrors();

        if (input.RoleNames.Count > 0)
        {
            (await _userManager.AddToRolesAsync(user, input.RoleNames)).CheckErrors();
        }

        if (input.HotelIds != null && input.HotelIds.Count > 0)
        {
            var distinctHotels = input.HotelIds.Distinct().ToList();
            foreach (var hotelId in distinctHotels)
            {
                var isPrimary = isReceptionist && (input.PrimaryHotelId.HasValue
                    ? hotelId == input.PrimaryHotelId.Value
                    : distinctHotels.Count == 1);

                var assignment = new UserHotelAssignment(
                    GuidGenerator.Create(),
                    user.Id,
                    hotelId,
                    CurrentTenant.Id,
                    isPrimary,
                    isActive: true
                );
                await _assignmentRepository.InsertAsync(assignment, autoSave: true);
            }

            await SyncHotelManagerNamesForUserAsync(user, distinctHotels, isHotelManager);
        }

        return await GetAsync(user.Id);
    }

    public async Task<UserWithAssignmentDto> UpdateAsync(Guid id, UpdateUserWithAssignmentDto input)
    {
        await CheckUserPermissionAsync(IdentityPermissions.Users.Update);

        var user = await FindUserWithCrossTenantCheckAsync(id);

        ValidateAssignableRoles(input.RoleNames);

        var isHotelManager = input.RoleNames.Contains(BookingHotelRoleConsts.HotelManager, StringComparer.OrdinalIgnoreCase);
        var isReceptionist = input.RoleNames.Contains(BookingHotelRoleConsts.Receptionist, StringComparer.OrdinalIgnoreCase);

        if (isReceptionist && !isHotelManager)
        {
            if (input.HotelIds == null || input.HotelIds.Count != 1)
            {
                throw new BusinessException("BookingHotel:ReceptionistSingleHotelRequired");
            }
        }
        else if (isHotelManager || isReceptionist)
        {
            if (input.HotelIds == null || input.HotelIds.Count == 0)
            {
                throw new BusinessException("BookingHotel:AtLeastOneHotelRequired");
            }
        }

        // 1. Gán ConcurrencyStamp trước tiên để kiểm tra đồng thời lạc quan
        if (!string.IsNullOrEmpty(input.ConcurrencyStamp))
        {
            user.ConcurrencyStamp = input.ConcurrencyStamp;
        }

        // 2. Cập nhật Họ, Tên, Số điện thoại và Trạng thái hoạt động trước
        user.Name = input.Name;
        user.Surname = input.Surname;

        if (!string.Equals(user.PhoneNumber, input.PhoneNumber, StringComparison.OrdinalIgnoreCase))
        {
            user.SetPhoneNumber(input.PhoneNumber ?? string.Empty, false);
        }

        if (input.IsActive.HasValue)
        {
            user.SetIsActive(input.IsActive.Value);
        }

        (await _userManager.UpdateAsync(user)).CheckErrors();

        // 3. Cập nhật Tên đăng nhập (UserName) nếu có và khác tên cũ (chạy sau UpdateAsync để tránh xung đột ConcurrencyStamp)
        if (!string.IsNullOrWhiteSpace(input.UserName) && !string.Equals(user.UserName, input.UserName.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            (await _userManager.SetUserNameAsync(user, input.UserName.Trim())).CheckErrors();
        }

        // 4. Cập nhật Email chỉ khi người dùng thay đổi giá trị
        if (!string.Equals(user.Email, input.Email, StringComparison.OrdinalIgnoreCase))
        {
            (await _userManager.SetEmailAsync(user, input.Email)).CheckErrors();
        }

        // Update roles
        var currentRoles = await _userManager.GetRolesAsync(user);
        var rolesToRemove = currentRoles.Except(input.RoleNames).ToList();
        var rolesToAdd = input.RoleNames.Except(currentRoles).ToList();

        if (rolesToRemove.Count > 0)
        {
            (await _userManager.RemoveFromRolesAsync(user, rolesToRemove)).CheckErrors();
        }
        if (rolesToAdd.Count > 0)
        {
            (await _userManager.AddToRolesAsync(user, rolesToAdd)).CheckErrors();
        }

        // Soft unlinking & update assignments
        var existingAssignments = await _assignmentRepository.GetListAsync(x => x.UserId == user.Id);
        var previouslyAssignedHotelIds = existingAssignments.Where(x => x.IsActive).Select(x => x.HotelId).ToList();
        var inputHotelIds = (input.HotelIds ?? new List<Guid>()).Distinct().ToList();
        var now = Clock.Now;

        foreach (var existing in existingAssignments)
        {
            if (!inputHotelIds.Contains(existing.HotelId))
            {
                if (existing.IsActive)
                {
                    existing.Unassign(now);
                    await _assignmentRepository.UpdateAsync(existing, autoSave: true);
                }
            }
        }

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
                await _assignmentRepository.UpdateAsync(existing, autoSave: true);
            }
            else
            {
                var assignment = new UserHotelAssignment(
                    GuidGenerator.Create(),
                    user.Id,
                    hotelId,
                    CurrentTenant.Id,
                    isPrimary,
                    isActive: true
                );
                await _assignmentRepository.InsertAsync(assignment, autoSave: true);
            }
        }

        await SyncHotelManagerNamesForUserAsync(user, inputHotelIds, isHotelManager, previouslyAssignedHotelIds);

        return await GetAsync(user.Id);
    }

    public async Task DeleteAsync(Guid id)
    {
        await CheckUserPermissionAsync(IdentityPermissions.Users.Delete);

        if (CurrentUser.Id == id)
        {
            throw new BusinessException("BookingHotel:CannotDeleteSelf");
        }

        var user = await FindUserWithCrossTenantCheckAsync(id);

        var roles = await _userManager.GetRolesAsync(user);
        if (string.Equals(user.UserName, "admin", StringComparison.OrdinalIgnoreCase) ||
            roles.Contains(BookingHotelRoleConsts.TenantAdmin) ||
            roles.Contains(BookingHotelRoleConsts.SystemAdmin))
        {
            throw new BusinessException("BookingHotel:CannotDeleteAdmin");
        }

        // 1. Dọn dẹp các bản ghi phân bổ chi nhánh của người dùng bị xóa
        var assignments = await _assignmentRepository.GetListAsync(a => a.UserId == user.Id);
        if (assignments.Count > 0)
        {
            await _assignmentRepository.DeleteManyAsync(assignments, autoSave: true);
        }

        // 2. Tự động gỡ người dùng khỏi vị trí Quản lý phụ trách ở tất cả các cơ sở khách sạn trực thuộc
        var namesToCheck = new List<string> { user.UserName };
        var fullName = $"{user.Surname} {user.Name}".Trim();
        if (!string.IsNullOrWhiteSpace(fullName))
        {
            namesToCheck.Add(fullName);
        }

        var hotels = await _hotelRepository.GetListAsync(h => !string.IsNullOrEmpty(h.ManagerName));
        foreach (var hotel in hotels)
        {
            var cleanManager = hotel.ManagerName.Split('(')[0].Trim();
            if (namesToCheck.Any(n => string.Equals(n, hotel.ManagerName, StringComparison.OrdinalIgnoreCase) ||
                                      string.Equals(n, cleanManager, StringComparison.OrdinalIgnoreCase)))
            {
                var otherManager = await FindActiveManagerForHotelAsync(hotel.Id, user.Id);
                hotel.ManagerName = otherManager != null ? GetUserDisplayName(otherManager) : string.Empty;
                await _hotelRepository.UpdateAsync(hotel, autoSave: true);
            }
        }

        (await _userManager.DeleteAsync(user)).CheckErrors();
    }

    [Authorize(BookingHotelPermissions.Identity.UnlockUser)]
    public async Task<UnlockUserResultDto> UnlockUserAsync(Guid id, UnlockUserDto? input = null)
    {
        var user = await FindUserWithCrossTenantCheckAsync(id);

        var isLockedOut = (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow)
                          || (!user.IsActive && user.GetProperty<string>(BookingHotelIdentityConsts.DeactivatedReasonProperty) == BookingHotelIdentityConsts.DeactivatedReasonBruteForce);

        if (!isLockedOut && user.IsActive)
        {
            throw new BusinessException("BookingHotel:UserAlreadyActive");
        }

        await _bruteForceManager.UnlockAccountAsync(user);

        await _securityLogManager.SaveAsync(new IdentitySecurityLogContext
        {
            Identity = "Identity",
            Action = "UnlockUser",
            UserName = user.UserName
        });

        Logger.LogInformation(
            "Security Unlock: User {UserId} ({UserName}) was unlocked by operator {OperatorId}. Reason: {Reason}",
            user.Id, user.UserName, CurrentUser.Id, input?.Reason ?? "N/A");

        return new UnlockUserResultDto
        {
            Success = true,
            UserId = user.Id,
            UnlockedAt = Clock.Now,
            Message = L["Identity:UnlockSuccess"]
        };
    }

    public async Task<UserWithAssignmentDto> ToggleStatusAsync(Guid id)
    {
        await CheckUserPermissionAsync(IdentityPermissions.Users.Update);

        if (CurrentUser.Id == id)
        {
            throw new BusinessException("BookingHotel:CannotDeactivateSelf");
        }

        var user = await FindUserWithCrossTenantCheckAsync(id);

        var roles = await _userManager.GetRolesAsync(user);
        if (string.Equals(user.UserName, "admin", StringComparison.OrdinalIgnoreCase) ||
            roles.Contains(BookingHotelRoleConsts.TenantAdmin) ||
            roles.Contains(BookingHotelRoleConsts.SystemAdmin))
        {
            throw new BusinessException("BookingHotel:CannotDeactivateAdmin");
        }

        user.SetIsActive(!user.IsActive);
        (await _userManager.UpdateAsync(user)).CheckErrors();

        return await GetAsync(id);
    }

    public async Task<UserWithAssignmentDto> CreateTenantStaffAsync(CreateTenantStaffDto input)
    {
        await CheckStaffPermissionAsync();

        if (input.Roles == null || input.Roles.Count == 0)
        {
            throw new UserFriendlyException(L["TenantStaff:ErrMustSelectAtLeastOneRole"]);
        }

        var allowedRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            BookingHotelRoleConsts.HotelManager,
            BookingHotelRoleConsts.Receptionist
        };

        if (input.Roles.Any(r => !allowedRoles.Contains(r)))
        {
            throw new BusinessException("BookingHotel:CannotAssignUnauthorizedRole")
                .WithData("Message", L["TenantStaff:ErrSystemAdminForbidden"]);
        }

        if (!IsPasswordComplex(input.Password))
        {
            throw new UserFriendlyException(L["Auth:PasswordComplexityFailed"]);
        }

        var user = new IdentityUser(
            GuidGenerator.Create(),
            input.UserName.Trim(),
            input.Email.Trim(),
            CurrentTenant.Id)
        {
            Name = input.Name?.Trim(),
            Surname = input.Surname?.Trim()
        };

        if (!string.IsNullOrWhiteSpace(input.PhoneNumber))
        {
            user.SetPhoneNumber(input.PhoneNumber.Trim(), confirmed: false);
        }

        user.SetProperty(BookingHotelIdentityConsts.ShouldChangePasswordOnNextLoginProperty, true);

        (await _userManager.CreateAsync(user, input.Password)).CheckErrors();

        (await _userManager.AddToRolesAsync(user, input.Roles)).CheckErrors();

        if (input.Roles.Contains(BookingHotelRoleConsts.HotelManager, StringComparer.OrdinalIgnoreCase))
        {
            foreach (var hId in input.ManagerHotelIds ?? new List<Guid>())
            {
                var assign = new UserHotelAssignment(GuidGenerator.Create(), user.Id, hId, CurrentTenant.Id, isPrimary: false, isActive: true);
                await _assignmentRepository.InsertAsync(assign, autoSave: true);
            }
        }

        if (input.Roles.Contains(BookingHotelRoleConsts.Receptionist, StringComparer.OrdinalIgnoreCase) && input.PrimaryReceptionistHotelId.HasValue)
        {
            var recAssign = new UserHotelAssignment(GuidGenerator.Create(), user.Id, input.PrimaryReceptionistHotelId.Value, CurrentTenant.Id, isPrimary: true, isActive: true);
            await _assignmentRepository.InsertAsync(recAssign, autoSave: true);
        }

        var isStaffManager = input.Roles.Contains(BookingHotelRoleConsts.HotelManager, StringComparer.OrdinalIgnoreCase);
        if (isStaffManager && input.ManagerHotelIds != null && input.ManagerHotelIds.Count > 0)
        {
            await SyncHotelManagerNamesForUserAsync(user, input.ManagerHotelIds, true);
        }

        return await GetAsync(user.Id);
    }

    public async Task<UserWithAssignmentDto> UpdateTenantStaffAsync(Guid id, UpdateTenantStaffDto input)
    {
        await CheckStaffPermissionAsync();

        var user = await FindUserWithCrossTenantCheckAsync(id);

        if (input.Roles == null || input.Roles.Count == 0)
        {
            throw new UserFriendlyException(L["TenantStaff:ErrMustSelectAtLeastOneRole"]);
        }

        var allowedRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            BookingHotelRoleConsts.HotelManager,
            BookingHotelRoleConsts.Receptionist
        };

        if (input.Roles.Any(r => !allowedRoles.Contains(r)))
        {
            throw new BusinessException("BookingHotel:CannotAssignUnauthorizedRole")
                .WithData("Message", L["TenantStaff:ErrSystemAdminForbidden"]);
        }

        (await _userManager.SetEmailAsync(user, input.Email.Trim())).CheckErrors();
        user.Name = input.Name?.Trim();
        user.Surname = input.Surname?.Trim();

        if (!string.IsNullOrWhiteSpace(input.PhoneNumber))
        {
            user.SetPhoneNumber(input.PhoneNumber.Trim(), confirmed: false);
        }

        if (!string.IsNullOrWhiteSpace(input.Password))
        {
            if (!IsPasswordComplex(input.Password))
            {
                throw new UserFriendlyException(L["Auth:PasswordComplexityFailed"]);
            }
            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            (await _userManager.ResetPasswordAsync(user, resetToken, input.Password)).CheckErrors();
        }

        (await _userManager.UpdateAsync(user)).CheckErrors();

        var currentRoles = await _userManager.GetRolesAsync(user);
        var rolesToRemove = currentRoles.Except(input.Roles, StringComparer.OrdinalIgnoreCase).ToList();
        var rolesToAdd = input.Roles.Except(currentRoles, StringComparer.OrdinalIgnoreCase).ToList();

        if (rolesToRemove.Count > 0)
        {
            (await _userManager.RemoveFromRolesAsync(user, rolesToRemove)).CheckErrors();
        }
        if (rolesToAdd.Count > 0)
        {
            (await _userManager.AddToRolesAsync(user, rolesToAdd)).CheckErrors();
        }

        var existingAssignments = await _assignmentRepository.GetListAsync(a => a.UserId == id);
        var prevActiveHotelIds = existingAssignments.Where(a => a.IsActive).Select(a => a.HotelId).ToList();
        var managerHotelIds = (input.ManagerHotelIds ?? new List<Guid>()).Distinct().ToList();
        var now = Clock.Now;

        var targetHotelIds = new HashSet<Guid>();
        if (input.Roles.Contains(BookingHotelRoleConsts.HotelManager, StringComparer.OrdinalIgnoreCase))
        {
            foreach (var hId in managerHotelIds)
            {
                targetHotelIds.Add(hId);
            }
        }
        if (input.Roles.Contains(BookingHotelRoleConsts.Receptionist, StringComparer.OrdinalIgnoreCase) && input.PrimaryReceptionistHotelId.HasValue)
        {
            targetHotelIds.Add(input.PrimaryReceptionistHotelId.Value);
        }

        // Unassign hotels not in targetHotelIds
        foreach (var a in existingAssignments)
        {
            if (!targetHotelIds.Contains(a.HotelId))
            {
                if (a.IsActive)
                {
                    a.Unassign(now);
                    await _assignmentRepository.UpdateAsync(a, autoSave: true);
                }
            }
        }

        // Reactivate or insert target hotels
        foreach (var hId in targetHotelIds)
        {
            var isPrimary = input.PrimaryReceptionistHotelId.HasValue && hId == input.PrimaryReceptionistHotelId.Value;
            var existing = existingAssignments.FirstOrDefault(a => a.HotelId == hId);
            if (existing != null)
            {
                existing.IsActive = true;
                existing.UnassignedAt = null;
                existing.IsPrimary = isPrimary;
                await _assignmentRepository.UpdateAsync(existing, autoSave: true);
            }
            else
            {
                var assign = new UserHotelAssignment(GuidGenerator.Create(), user.Id, hId, CurrentTenant.Id, isPrimary: isPrimary, isActive: true);
                await _assignmentRepository.InsertAsync(assign, autoSave: true);
            }
        }

        var isStaffManager = input.Roles.Contains(BookingHotelRoleConsts.HotelManager, StringComparer.OrdinalIgnoreCase);
        await SyncHotelManagerNamesForUserAsync(user, managerHotelIds, isStaffManager, prevActiveHotelIds);

        return await GetAsync(user.Id);
    }

    public async Task TransferBranchAsync(Guid id, TransferBranchDto input)
    {
        await CheckStaffPermissionAsync();

        var user = await FindUserWithCrossTenantCheckAsync(id);

        var activeFrom = await _assignmentRepository.FirstOrDefaultAsync(
            a => a.UserId == id && a.HotelId == input.FromHotelId && a.IsActive);

        if (activeFrom != null)
        {
            activeFrom.Unassign(Clock.Now);
            await _assignmentRepository.UpdateAsync(activeFrom, autoSave: true);
        }

        var newAssign = new UserHotelAssignment(
            GuidGenerator.Create(),
            user.Id,
            input.ToHotelId,
            CurrentTenant.Id,
            isPrimary: activeFrom?.IsPrimary ?? true,
            isActive: true
        );
        await _assignmentRepository.InsertAsync(newAssign, autoSave: true);

        var isManager = await _userManager.IsInRoleAsync(user, BookingHotelRoleConsts.HotelManager);
        if (isManager)
        {
            await SyncHotelManagerNamesForUserAsync(user, new List<Guid> { input.ToHotelId }, true, new List<Guid> { input.FromHotelId });
        }
    }

    [Authorize(BookingHotelPermissions.Identity.UnlockUser)]
    public async Task<UnlockUserResultDto> UnlockBruteForceAsync(Guid id)
    {
        return await UnlockUserAsync(id, new UnlockUserDto { Reason = "Tenant admin brute-force reset" });
    }

    public async Task<List<string>> GetAssignableRolesAsync()
    {
        await CheckUserPermissionAsync(IdentityPermissions.Users.Default);

        if (CurrentTenant.Id == null)
        {
            return new List<string>
            {
                BookingHotelRoleConsts.SystemAdmin,
                BookingHotelRoleConsts.Customer
            };
        }

        return new List<string>
        {
            BookingHotelRoleConsts.HotelManager,
            BookingHotelRoleConsts.Receptionist
        };
    }

    public async Task<List<UserWithAssignmentDto>> GetStaffByHotelsAsync(List<Guid>? hotelIds = null)
    {
        await CheckUserPermissionAsync(IdentityPermissions.Users.Default);

        var targetHotelIds = hotelIds ?? new List<Guid>();

        if (targetHotelIds.Count == 0 && CurrentUser.Id.HasValue)
        {
            var myAssignments = await _assignmentRepository.GetListAsync(
                a => a.UserId == CurrentUser.Id.Value && a.IsActive);
            targetHotelIds = myAssignments.Select(a => a.HotelId).Distinct().ToList();
        }

        if (targetHotelIds.Count == 0)
        {
            return new List<UserWithAssignmentDto>();
        }

        var assignments = await _assignmentRepository.GetListAsync(
            a => targetHotelIds.Contains(a.HotelId) && a.IsActive);
        var userIds = assignments.Select(a => a.UserId).Distinct().ToList();

        if (userIds.Count == 0)
        {
            return new List<UserWithAssignmentDto>();
        }

        var hotelIdsInAssignments = assignments.Select(a => a.HotelId).Distinct().ToList();
        var hotelsInAssignments = await _hotelRepository.GetListAsync(h => hotelIdsInAssignments.Contains(h.Id));
        var hotelDict = hotelsInAssignments.ToDictionary(h => h.Id);

        var userQuery = await _userRepository.GetQueryableAsync();
        var staffQuery = userQuery.Where(u => userIds.Contains(u.Id));
        var users = await AsyncExecuter.ToListAsync(staffQuery);
        var result = new List<UserWithAssignmentDto>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var userAssignments = assignments.Where(a => a.UserId == user.Id).ToList();
            var isLocked = (!user.IsActive && user.GetProperty<string>(BookingHotelIdentityConsts.DeactivatedReasonProperty) == BookingHotelIdentityConsts.DeactivatedReasonBruteForce)
                           || (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow);

            var isReceptionist = roles.Contains(BookingHotelRoleConsts.Receptionist, StringComparer.OrdinalIgnoreCase);

            result.Add(new UserWithAssignmentDto
            {
                Id = user.Id,
                UserName = user.UserName,
                Name = user.Name,
                Surname = user.Surname,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                IsActive = user.IsActive,
                LockoutEnd = user.LockoutEnd,
                IsLockedOut = isLocked,
                ShouldChangePasswordOnNextLogin = user.GetProperty<bool>(BookingHotelIdentityConsts.ShouldChangePasswordOnNextLoginProperty),
                Roles = roles.Select(r => new UserRoleDto { Name = r }).ToList(),
                RoleNames = roles.ToList(),
                AssignedHotels = userAssignments.Select(a => new AssignedHotelDto
                {
                    HotelId = a.HotelId,
                    HotelName = hotelDict.TryGetValue(a.HotelId, out var h) ? h.Name : $"Hotel {a.HotelId.ToString()[..8]}",
                    HotelCode = hotelDict.TryGetValue(a.HotelId, out var hc) ? hc.Code : a.HotelId.ToString()[..6].ToUpper(),
                    IsPrimary = isReceptionist && a.IsPrimary,
                    IsActive = a.IsActive,
                    AssignedAt = a.AssignedDate,
                    UnassignedAt = a.UnassignedAt
                }).ToList(),
                CreationTime = user.CreationTime,
                ConcurrencyStamp = user.ConcurrencyStamp
            });
        }

        return result;
    }

    private async Task<IdentityUser> FindUserWithCrossTenantCheckAsync(Guid id)
    {
        IdentityUser? user;
        using (DataFilter.Disable<IMultiTenant>())
        {
            user = await _userManager.FindByIdAsync(id.ToString());
        }

        if (user == null)
        {
            throw new EntityNotFoundException(typeof(IdentityUser), id);
        }

        if (user.TenantId != CurrentTenant.Id)
        {
            throw new BusinessException("BookingHotel:CrossTenantOperationForbidden");
        }

        return user;
    }

    private void ValidateAssignableRoles(IEnumerable<string> roles)
    {
        if (roles == null || !roles.Any())
        {
            throw new BusinessException("BookingHotel:CannotAssignUnauthorizedRole");
        }

        var isStaffRoleRequest = roles.All(r =>
            string.Equals(r, BookingHotelRoleConsts.HotelManager, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(r, BookingHotelRoleConsts.Receptionist, StringComparison.OrdinalIgnoreCase));

        if (CurrentTenant.Id != null || isStaffRoleRequest)
        {
            var allowedRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                BookingHotelRoleConsts.HotelManager,
                BookingHotelRoleConsts.Receptionist
            };

            if (roles.Any(r => !allowedRoles.Contains(r)))
            {
                throw new BusinessException("BookingHotel:CannotAssignUnauthorizedRole");
            }
        }
        else
        {
            var allowedRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                BookingHotelRoleConsts.SystemAdmin,
                BookingHotelRoleConsts.Customer
            };

            if (roles.Any(r => !allowedRoles.Contains(r)))
            {
                throw new BusinessException("BookingHotel:CannotAssignUnauthorizedRole");
            }
        }
    }

    private static bool IsPasswordComplex(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
        {
            return false;
        }

        var hasUpper = password.Any(char.IsUpper);
        var hasLower = password.Any(char.IsLower);
        var hasDigit = password.Any(char.IsDigit);
        var hasSpecial = password.Any(ch => !char.IsLetterOrDigit(ch));

        return hasUpper && hasLower && hasDigit && hasSpecial;
    }

    private static string GetUserDisplayName(IdentityUser user)
    {
        var fullName = $"{user.Surname} {user.Name}".Trim();
        return string.IsNullOrWhiteSpace(fullName) ? user.UserName : fullName;
    }

    private async Task<IdentityUser?> FindActiveManagerForHotelAsync(Guid hotelId, Guid? excludeUserId = null)
    {
        var hotel = await _hotelRepository.FindAsync(hotelId);
        if (hotel != null && !string.IsNullOrWhiteSpace(hotel.ManagerName))
        {
            var trimmed = hotel.ManagerName.Trim();
            var clean = trimmed.Split('(')[0].Trim();
            var managerUsers = await _userManager.GetUsersInRoleAsync(BookingHotelRoleConsts.HotelManager);
            var managerUser = managerUsers.FirstOrDefault(u =>
                (!excludeUserId.HasValue || u.Id != excludeUserId.Value) &&
                (string.Equals(GetUserDisplayName(u), trimmed, StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(GetUserDisplayName(u), clean, StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(u.UserName, trimmed, StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(u.UserName, clean, StringComparison.OrdinalIgnoreCase)));

            if (managerUser != null)
            {
                return managerUser;
            }
        }

        var assignments = await _assignmentRepository.GetListAsync(a => a.HotelId == hotelId && a.IsActive);
        foreach (var assign in assignments)
        {
            if (excludeUserId.HasValue && assign.UserId == excludeUserId.Value)
            {
                continue;
            }

            var candidate = await _userManager.FindByIdAsync(assign.UserId.ToString());
            if (candidate != null && await _userManager.IsInRoleAsync(candidate, BookingHotelRoleConsts.HotelManager))
            {
                return candidate;
            }
        }
        return null;
    }

    private async Task UnassignOtherManagersForHotelAsync(Guid hotelId, Guid excludeUserId)
    {
        var activeAssignments = await _assignmentRepository.GetListAsync(
            a => a.HotelId == hotelId && a.IsActive && a.UserId != excludeUserId);

        if (activeAssignments.Count == 0)
        {
            return;
        }

        foreach (var assignment in activeAssignments)
        {
            var otherUser = await _userManager.FindByIdAsync(assignment.UserId.ToString());
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
                        if (assignment.IsPrimary || !hasOtherPrimary)
                        {
                            if (!assignment.IsPrimary)
                            {
                                assignment.IsPrimary = true;
                                await _assignmentRepository.UpdateAsync(assignment, autoSave: true);
                            }
                            continue;
                        }
                    }

                    // Nếu chỉ là Quản lý đơn thuần (không phải lễ tân tại cơ sở này), unassign
                    assignment.Unassign(Clock.Now);
                    await _assignmentRepository.UpdateAsync(assignment, autoSave: true);
                }
            }
        }
    }

    private async Task SyncHotelManagerNamesForUserAsync(
        IdentityUser user,
        List<Guid> activeHotelIds,
        bool isHotelManager,
        List<Guid>? previouslyAssignedHotelIds = null)
    {
        var displayName = GetUserDisplayName(user);
        var namesToCheck = new List<string> { user.UserName };
        if (!string.IsNullOrWhiteSpace(displayName))
        {
            namesToCheck.Add(displayName);
        }

        if (isHotelManager)
        {
            foreach (var hotelId in activeHotelIds)
            {
                // Đảm bảo không cho phép 2 manager cùng active 1 khách sạn
                await UnassignOtherManagersForHotelAsync(hotelId, user.Id);

                var hotel = await _hotelRepository.FindAsync(hotelId);
                if (hotel != null)
                {
                    hotel.ManagerName = displayName;
                    await _hotelRepository.UpdateAsync(hotel, autoSave: true);
                }
            }

            if (previouslyAssignedHotelIds != null)
            {
                var unassignedHotelIds = previouslyAssignedHotelIds.Except(activeHotelIds).ToList();
                foreach (var hotelId in unassignedHotelIds)
                {
                    var hotel = await _hotelRepository.FindAsync(hotelId);
                    if (hotel != null && namesToCheck.Any(n =>
                        string.Equals(n, hotel.ManagerName, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(n, hotel.ManagerName.Split('(')[0].Trim(), StringComparison.OrdinalIgnoreCase)))
                    {
                        var otherManager = await FindActiveManagerForHotelAsync(hotel.Id, user.Id);
                        hotel.ManagerName = otherManager != null ? GetUserDisplayName(otherManager) : string.Empty;
                        await _hotelRepository.UpdateAsync(hotel, autoSave: true);
                    }
                }
            }
        }
        else
        {
            var hotelsToCheck = previouslyAssignedHotelIds ??
                (await _assignmentRepository.GetListAsync(a => a.UserId == user.Id)).Select(a => a.HotelId).ToList();

            foreach (var hotelId in hotelsToCheck)
            {
                var hotel = await _hotelRepository.FindAsync(hotelId);
                if (hotel != null && namesToCheck.Any(n =>
                    string.Equals(n, hotel.ManagerName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(n, hotel.ManagerName.Split('(')[0].Trim(), StringComparison.OrdinalIgnoreCase)))
                {
                    var otherManager = await FindActiveManagerForHotelAsync(hotel.Id, user.Id);
                    hotel.ManagerName = otherManager != null ? GetUserDisplayName(otherManager) : string.Empty;
                    await _hotelRepository.UpdateAsync(hotel, autoSave: true);
                }
            }
        }
    }
}
