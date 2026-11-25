using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BookingHotel.Hotels.Dtos;
using BookingHotel.Identity;
using BookingHotel.Permissions;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.Users;

namespace BookingHotel.Hotels;

[RemoteService(IsEnabled = false)]
[Authorize]
public class HotelAppService : ApplicationService, IHotelAppService
{
    private static readonly Regex CodeRegex = new("^[A-Z0-9\\-_]{3,20}$", RegexOptions.Compiled);

    private readonly IRepository<Hotel, Guid> _hotelRepository;
    private readonly IRepository<HotelSuspensionRecord, Guid> _suspensionRecordRepository;
    private readonly IRepository<UserHotelAssignment, Guid> _assignmentRepository;
    private readonly IdentityUserManager _userManager;
    private readonly IRepository<IdentityUser, Guid> _userRepository;

    public HotelAppService(
        IRepository<Hotel, Guid> hotelRepository,
        IRepository<HotelSuspensionRecord, Guid> suspensionRecordRepository,
        IRepository<UserHotelAssignment, Guid> assignmentRepository,
        IdentityUserManager userManager,
        IRepository<IdentityUser, Guid> userRepository)
    {
        _hotelRepository = hotelRepository;
        _suspensionRecordRepository = suspensionRecordRepository;
        _assignmentRepository = assignmentRepository;
        _userManager = userManager;
        _userRepository = userRepository;
    }

    [Authorize(BookingHotelPermissions.Hotels.Default)]
    public async Task<PagedResultDto<HotelDto>> GetListAsync(GetHotelsInput input)
    {
        var query = await _hotelRepository.GetQueryableAsync();

        if (!string.IsNullOrWhiteSpace(input.Filter))
        {
            var filter = input.Filter.Trim().ToLower();
            query = query.Where(h =>
                h.Name.ToLower().Contains(filter) ||
                h.Code.ToLower().Contains(filter) ||
                h.City.ToLower().Contains(filter) ||
                h.Address.ToLower().Contains(filter));
        }

        if (input.Status.HasValue)
        {
            query = query.Where(h => h.Status == input.Status.Value);
        }

        // Nếu là vai trò Hotel Manager (và không phải TenantAdmin/admin), chỉ hiển thị các cơ sở được phân công phụ trách
        if (!CurrentUser.IsInRole(BookingHotelRoleConsts.TenantAdmin) &&
            !CurrentUser.IsInRole(BookingHotelRoleConsts.SystemAdmin) &&
            !CurrentUser.IsInRole("admin") &&
            CurrentUser.IsInRole(BookingHotelRoleConsts.HotelManager))
        {
            var userId = CurrentUser.GetId();
            var myAssignmentsQuery = await _assignmentRepository.GetQueryableAsync();
            var myHotelIds = myAssignmentsQuery
                .Where(a => a.UserId == userId && a.IsActive && a.UnassignedAt == null)
                .Select(a => a.HotelId);

            query = query.Where(h => myHotelIds.Contains(h.Id));
        }

        var totalCount = await AsyncExecuter.CountAsync(query);

        var pagedQuery = query
            .OrderByDescending(h => h.CreationTime)
            .Skip(input.SkipCount)
            .Take(input.MaxResultCount);

        var hotels = await AsyncExecuter.ToListAsync(pagedQuery);
        var hotelIds = hotels.Select(h => h.Id).ToList();
        var assignments = await _assignmentRepository.GetListAsync(a => hotelIds.Contains(a.HotelId) && a.IsActive);
        var usersQuery = await _userRepository.GetQueryableAsync();
        var tenantUsers = await AsyncExecuter.ToListAsync(usersQuery.Where(u => u.TenantId == CurrentTenant.Id));

        var dtoList = new List<HotelDto>();
        foreach (var hotel in hotels)
        {
            var dto = MapToDto(hotel);
            IdentityUser? matchedManager = null;
            if (!string.IsNullOrWhiteSpace(hotel.ManagerName))
            {
                var trimmed = hotel.ManagerName.Trim();
                var clean = trimmed.Split('(')[0].Trim();
                matchedManager = tenantUsers.FirstOrDefault(u =>
                    string.Equals(GetUserDisplayName(u), trimmed, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(GetUserDisplayName(u), clean, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(u.UserName, trimmed, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(u.UserName, clean, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals($"{u.Surname} {u.Name}".Trim(), clean, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals($"{u.Name} {u.Surname}".Trim(), clean, StringComparison.OrdinalIgnoreCase));
            }

            if (matchedManager != null)
            {
                dto.ManagerUserId = matchedManager.Id;
            }
            else
            {
                var activeAssign = assignments.FirstOrDefault(a => a.HotelId == hotel.Id && !a.IsPrimary)
                    ?? assignments.FirstOrDefault(a => a.HotelId == hotel.Id);
                if (activeAssign != null)
                {
                    dto.ManagerUserId = activeAssign.UserId;
                }
            }
            dtoList.Add(dto);
        }

        return new PagedResultDto<HotelDto>(
            totalCount,
            dtoList
        );
    }

    [Authorize(BookingHotelPermissions.Hotels.Default)]
    public async Task<HotelDto> GetAsync(Guid id)
    {
        var hotel = await _hotelRepository.GetAsync(id);
        var dto = MapToDto(hotel);

        var usersQuery = await _userRepository.GetQueryableAsync();
        var tenantUsers = await AsyncExecuter.ToListAsync(usersQuery.Where(u => u.TenantId == CurrentTenant.Id));

        if (!string.IsNullOrWhiteSpace(hotel.ManagerName))
        {
            var trimmed = hotel.ManagerName.Trim();
            var clean = trimmed.Split('(')[0].Trim();
            var matchedManager = tenantUsers.FirstOrDefault(u =>
                string.Equals(GetUserDisplayName(u), trimmed, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(GetUserDisplayName(u), clean, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(u.UserName, trimmed, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(u.UserName, clean, StringComparison.OrdinalIgnoreCase) ||
                string.Equals($"{u.Surname} {u.Name}".Trim(), clean, StringComparison.OrdinalIgnoreCase) ||
                string.Equals($"{u.Name} {u.Surname}".Trim(), clean, StringComparison.OrdinalIgnoreCase));
            if (matchedManager != null)
            {
                dto.ManagerUserId = matchedManager.Id;
                return dto;
            }
        }

        var activeAssigns = await _assignmentRepository.GetListAsync(a => a.HotelId == id && a.IsActive);
        var firstAssign = activeAssigns.FirstOrDefault(a => !a.IsPrimary) ?? activeAssigns.FirstOrDefault();
        if (firstAssign != null)
        {
            dto.ManagerUserId = firstAssign.UserId;
        }
        return dto;
    }

    [Authorize(BookingHotelPermissions.Hotels.Create)]
    public async Task<HotelDto> CreateAsync(CreateUpdateHotelDto input)
    {
        var name = input.Name.Trim();
        var code = input.Code.Trim().ToUpper();
        if (!CodeRegex.IsMatch(code))
        {
            throw new UserFriendlyException(L["TenantHotels:CodeValidation"]);
        }

        var existingName = await _hotelRepository.FirstOrDefaultAsync(h => h.Name.ToLower() == name.ToLower());
        if (existingName != null)
        {
            throw new UserFriendlyException(string.Format(L["TenantHotels:ErrNameAlreadyExists"], name));
        }

        var existing = await _hotelRepository.FirstOrDefaultAsync(h => h.Code == code);
        if (existing != null)
        {
            throw new UserFriendlyException(string.Format(L["TenantHotels:ErrCodeAlreadyExists"], code));
        }

        var checkIn = input.StandardCheckInTime ?? new TimeSpan(14, 0, 0);
        var checkOut = input.StandardCheckOutTime ?? new TimeSpan(12, 0, 0);
        var cutoff = input.BookingCutoffDays > 0 ? input.BookingCutoffDays : 14;

        var hotel = new Hotel(
            GuidGenerator.Create(),
            input.Name.Trim(),
            code,
            input.Address.Trim(),
            input.City.Trim(),
            input.Phone.Trim(),
            input.Email.Trim(),
            input.StarRating,
            input.RoomCount,
            input.ManagerName?.Trim() ?? string.Empty,
            input.Status,
            CurrentTenant.Id,
            checkIn,
            checkOut,
            cutoff
        );

        await _hotelRepository.InsertAsync(hotel, autoSave: true);

        if (input.ManagerUserId.HasValue || !string.IsNullOrWhiteSpace(input.ManagerName))
        {
            await SyncHotelManagerAsync(hotel, input.ManagerUserId, input.ManagerName);
            await _hotelRepository.UpdateAsync(hotel, autoSave: true);
        }

        var dto = MapToDto(hotel);
        dto.ManagerUserId = input.ManagerUserId;
        return dto;
    }

    [Authorize(BookingHotelPermissions.Hotels.Edit)]
    public async Task<HotelDto> UpdateAsync(Guid id, CreateUpdateHotelDto input)
    {
        var hotel = await _hotelRepository.GetAsync(id);

        var name = input.Name.Trim();
        if (!string.Equals(hotel.Name, name, StringComparison.OrdinalIgnoreCase))
        {
            var existingName = await _hotelRepository.FirstOrDefaultAsync(h => h.Name.ToLower() == name.ToLower() && h.Id != id);
            if (existingName != null)
            {
                throw new UserFriendlyException(string.Format(L["TenantHotels:ErrNameAlreadyExists"], name));
            }
        }

        var code = input.Code.Trim().ToUpper();
        if (hotel.Code != code)
        {
            if (!CodeRegex.IsMatch(code))
            {
                throw new UserFriendlyException(L["TenantHotels:CodeValidation"]);
            }

            var existing = await _hotelRepository.FirstOrDefaultAsync(h => h.Code == code && h.Id != id);
            if (existing != null)
            {
                throw new UserFriendlyException(string.Format(L["TenantHotels:ErrCodeAlreadyExists"], code));
            }
        }

        hotel.Name = input.Name.Trim();
        hotel.Code = code;
        hotel.Address = input.Address.Trim();
        hotel.City = input.City.Trim();
        hotel.Phone = input.Phone.Trim();
        hotel.Email = input.Email.Trim();
        hotel.StarRating = input.StarRating;
        hotel.RoomCount = input.RoomCount;
        hotel.Status = input.Status;

        if (input.StandardCheckInTime.HasValue)
        {
            hotel.StandardCheckInTime = input.StandardCheckInTime.Value;
        }

        if (input.StandardCheckOutTime.HasValue)
        {
            hotel.StandardCheckOutTime = input.StandardCheckOutTime.Value;
        }

        if (input.BookingCutoffDays > 0)
        {
            hotel.BookingCutoffDays = input.BookingCutoffDays;
        }

        await SyncHotelManagerAsync(hotel, input.ManagerUserId, input.ManagerName);

        await _hotelRepository.UpdateAsync(hotel, autoSave: true);

        var dto = MapToDto(hotel);
        dto.ManagerUserId = input.ManagerUserId;
        return dto;
    }

    [Authorize(BookingHotelPermissions.Hotels.Delete)]
    public async Task DeleteAsync(Guid id)
    {
        await _hotelRepository.DeleteAsync(id, autoSave: true);
    }

    [Authorize(BookingHotelPermissions.Hotels.ManageBranches)]
    public async Task<HotelSuspensionResultDto> SuspendAsync(Guid id, SuspendHotelInputDto input)
    {
        var hotel = await _hotelRepository.GetAsync(id);

        if (input.SuspensionType == SuspensionType.Scheduled)
        {
            var minNoticeDays = 30;
            if ((input.StartDate.Date - Clock.Now.Date).TotalDays < minNoticeDays)
            {
                throw new UserFriendlyException(L["TenantHotels:ErrScheduledAdvance30Days"]);
            }

            hotel.Status = HotelStatus.ScheduledSuspended;
        }
        else if (input.SuspensionType == SuspensionType.Emergency)
        {
            if (string.IsNullOrWhiteSpace(input.Reason) || input.Reason.Trim().Length < 20)
            {
                throw new UserFriendlyException(L["TenantHotels:ErrEmergencyReasonMin20Chars"]);
            }

            hotel.Status = HotelStatus.EmergencySuspended;
        }
        else
        {
            hotel.Status = HotelStatus.ScheduledSuspended;
        }

        await _hotelRepository.UpdateAsync(hotel, autoSave: true);

        var suspensionRecord = new HotelSuspensionRecord(
            GuidGenerator.Create(),
            hotel.Id,
            input.SuspensionType,
            input.StartDate,
            input.EndDate,
            input.Reason?.Trim() ?? string.Empty,
            affectedBookingsCount: 0,
            tenantId: CurrentTenant.Id
        );

        await _suspensionRecordRepository.InsertAsync(suspensionRecord, autoSave: true);

        return new HotelSuspensionResultDto
        {
            HotelId = hotel.Id,
            Status = hotel.Status,
            AffectedBookingsCount = 0,
            Message = L["TenantHotels:SuspendSuccess"]
        };
    }

    [Authorize(BookingHotelPermissions.Hotels.ManageBranches)]
    public async Task<HotelDto> ActivateAsync(Guid id)
    {
        var hotel = await _hotelRepository.GetAsync(id);
        hotel.Status = HotelStatus.Operating;
        await _hotelRepository.UpdateAsync(hotel, autoSave: true);
        return MapToDto(hotel);
    }

    [Authorize(BookingHotelPermissions.Hotels.AssignStaff)]
    public async Task<HotelDto> AssignManagerAsync(Guid id, AssignHotelManagerDto input)
    {
        var hotel = await _hotelRepository.GetAsync(id);
        await SyncHotelManagerAsync(hotel, input.ManagerUserId, null);
        await _hotelRepository.UpdateAsync(hotel, autoSave: true);

        var dto = MapToDto(hotel);
        dto.ManagerUserId = input.ManagerUserId;
        return dto;
    }

    private static string GetUserDisplayName(IdentityUser user)
    {
        var fullName = $"{user.Surname} {user.Name}".Trim();
        return string.IsNullOrWhiteSpace(fullName) ? user.UserName : fullName;
    }

    private async Task SyncHotelManagerAsync(Hotel hotel, Guid? managerUserId, string? managerName)
    {
        IdentityUser? managerUser = null;
        if (managerUserId.HasValue && managerUserId.Value != Guid.Empty)
        {
            managerUser = await _userManager.FindByIdAsync(managerUserId.Value.ToString());
        }
        else if (!string.IsNullOrWhiteSpace(managerName))
        {
            var trimmed = managerName.Trim();
            managerUser = await _userManager.FindByNameAsync(trimmed);
            if (managerUser == null)
            {
                var cleanName = trimmed.Split('(')[0].Trim();
                var usersQuery = await _userRepository.GetQueryableAsync();
        var tenantUsers = await AsyncExecuter.ToListAsync(usersQuery.Where(u => u.TenantId == CurrentTenant.Id));
                managerUser = tenantUsers.FirstOrDefault(u =>
                    string.Equals(GetUserDisplayName(u), trimmed, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(GetUserDisplayName(u), cleanName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(u.UserName, trimmed, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(u.UserName, cleanName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals($"{u.Surname} {u.Name}".Trim(), cleanName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals($"{u.Name} {u.Surname}".Trim(), cleanName, StringComparison.OrdinalIgnoreCase));
            }
        }

        if (managerUser != null)
        {
            var isManager = await _userManager.IsInRoleAsync(managerUser, BookingHotelRoleConsts.HotelManager);
            if (!isManager)
            {
                await _userManager.AddToRoleAsync(managerUser, BookingHotelRoleConsts.HotelManager);
            }

            // Vô hiệu hóa phân công quản lý hiện tại của khách sạn này (chỉ unassign các manager khác, không động vào Lễ tân)
            var activeAssignments = await _assignmentRepository.GetListAsync(a => a.HotelId == hotel.Id && a.IsActive);
            foreach (var assignment in activeAssignments)
            {
                if (assignment.UserId != managerUser.Id)
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
                                    a => a.UserId == otherUser.Id && a.HotelId != hotel.Id && a.IsActive && a.IsPrimary);
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

                            assignment.Unassign(Clock.Now);
                            await _assignmentRepository.UpdateAsync(assignment, autoSave: true);
                        }
                    }
                }
            }

            // Kích hoạt hoặc tạo mới phân công cho manager này
            var existingAssign = await _assignmentRepository.FirstOrDefaultAsync(a => a.HotelId == hotel.Id && a.UserId == managerUser.Id);
            if (existingAssign != null)
            {
                existingAssign.IsActive = true;
                existingAssign.UnassignedAt = null;
                await _assignmentRepository.UpdateAsync(existingAssign, autoSave: true);
            }
            else
            {
                var newAssign = new UserHotelAssignment(
                    GuidGenerator.Create(),
                    managerUser.Id,
                    hotel.Id,
                    CurrentTenant.Id,
                    isPrimary: false,
                    isActive: true
                );
                await _assignmentRepository.InsertAsync(newAssign, autoSave: true);
            }

            hotel.ManagerName = GetUserDisplayName(managerUser);
        }
        else if (managerUserId == Guid.Empty || (managerUserId == null && string.IsNullOrWhiteSpace(managerName)))
        {
            // Explicitly unassigned / để trống
            var activeAssignments = await _assignmentRepository.GetListAsync(a => a.HotelId == hotel.Id && a.IsActive);
            foreach (var assignment in activeAssignments)
            {
                var otherUser = await _userManager.FindByIdAsync(assignment.UserId.ToString());
                if (otherUser != null)
                {
                    var isOtherManager = await _userManager.IsInRoleAsync(otherUser, BookingHotelRoleConsts.HotelManager);
                    var isOtherReceptionist = await _userManager.IsInRoleAsync(otherUser, BookingHotelRoleConsts.Receptionist);
                    if (isOtherManager)
                    {
                        if (isOtherReceptionist)
                        {
                            var hasOtherPrimary = await _assignmentRepository.AnyAsync(
                                a => a.UserId == otherUser.Id && a.HotelId != hotel.Id && a.IsActive && a.IsPrimary);
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

                        assignment.Unassign(Clock.Now);
                        await _assignmentRepository.UpdateAsync(assignment, autoSave: true);
                    }
                }
            }
            hotel.ManagerName = string.Empty;
        }
    }

    [Authorize(BookingHotelPermissions.Hotels.Default)]
    public async Task<bool> CheckNameExistsAsync(string name, Guid? excludeId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        var trimmed = name.Trim().ToLower();
        var query = await _hotelRepository.GetQueryableAsync();
        if (excludeId.HasValue && excludeId.Value != Guid.Empty)
        {
            query = query.Where(h => h.Id != excludeId.Value);
        }

        return await AsyncExecuter.AnyAsync(query.Where(h => h.Name.ToLower() == trimmed));
    }

    private static HotelDto MapToDto(Hotel hotel)
    {
        return new HotelDto
        {
            Id = hotel.Id,
            TenantId = hotel.TenantId,
            Name = hotel.Name,
            Code = hotel.Code,
            Address = hotel.Address,
            City = hotel.City,
            Phone = hotel.Phone,
            Email = hotel.Email,
            StarRating = hotel.StarRating,
            RoomCount = hotel.RoomCount,
            ManagerName = hotel.ManagerName,
            Status = hotel.Status,
            StandardCheckInTime = hotel.StandardCheckInTime,
            StandardCheckOutTime = hotel.StandardCheckOutTime,
            BookingCutoffDays = hotel.BookingCutoffDays,
            CreationTime = hotel.CreationTime,
            CreatorId = hotel.CreatorId,
            LastModificationTime = hotel.LastModificationTime,
            LastModifierId = hotel.LastModifierId
        };
    }
}
