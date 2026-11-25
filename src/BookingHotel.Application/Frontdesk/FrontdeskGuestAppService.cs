using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BookingHotel.Bookings;
using BookingHotel.Identity;
using BookingHotel.Permissions;
using BookingHotel.Rooms;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;

namespace BookingHotel.Frontdesk;

[RemoteService(IsEnabled = false)]
[Authorize(BookingHotelPermissions.Bookings.Default)]
public class FrontdeskGuestAppService : FrontdeskAppServiceBase, IFrontdeskGuestAppService
{
    private readonly IRepository<Booking, Guid> _bookingRepository;
    private readonly IRepository<RoomAssignment, Guid> _roomAssignmentRepository;
    private readonly IRepository<Room, Guid> _roomRepository;
    private readonly IRepository<RoomType, Guid> _roomTypeRepository;
    private readonly FrontdeskStayDomainService _stayDomainService;

    public FrontdeskGuestAppService(
        IRepository<UserHotelAssignment, Guid> userHotelAssignmentRepository,
        IRepository<Booking, Guid> bookingRepository,
        IRepository<RoomAssignment, Guid> roomAssignmentRepository,
        IRepository<Room, Guid> roomRepository,
        IRepository<RoomType, Guid> roomTypeRepository,
        FrontdeskStayDomainService stayDomainService)
        : base(userHotelAssignmentRepository)
    {
        _bookingRepository = bookingRepository;
        _roomAssignmentRepository = roomAssignmentRepository;
        _roomRepository = roomRepository;
        _roomTypeRepository = roomTypeRepository;
        _stayDomainService = stayDomainService;
    }

    public async Task<PagedResultDto<FrontdeskGuestListItemDto>> GetListAsync(GetFrontdeskGuestsInputDto input)
    {
        await CheckFrontdeskHotelAccessAsync(input.HotelId);

        var bookings = await _bookingRepository.GetListAsync(b => b.HotelId == input.HotelId);

        var grouped = bookings
            .GroupBy(b => _stayDomainService.NormalizeGuestKey(b.GuestId, b.GuestPhone, b.Id))
            .Select(g =>
            {
                var latest = g.OrderByDescending(b => b.LastModificationTime ?? b.CreationTime).First();
                var anyVerified = g.Any(b => b.IsIdentityVerified);
                var maskedId = g
                    .OrderByDescending(b => b.IsIdentityVerified)
                    .ThenByDescending(b => b.LastModificationTime ?? b.CreationTime)
                    .Select(b => b.GuestIdentityNumberMasked)
                    .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? string.Empty;

                return new FrontdeskGuestListItemDto
                {
                    GuestKey = g.Key,
                    GuestId = latest.GuestId,
                    FullName = latest.GuestFullName,
                    PhoneNumber = latest.GuestPhone,
                    Email = latest.GuestEmail,
                    IdentityNumberMasked = maskedId,
                    IsIdentityVerified = anyVerified,
                    TotalBookingsCount = g.Count(),
                    ActiveBookingsCount = g.Count(b =>
                        b.BookingStatus == BookingStatus.PendingConfirmation ||
                        b.BookingStatus == BookingStatus.Confirmed ||
                        b.BookingStatus == BookingStatus.CheckedIn),
                    LastCheckInDate = g.Max(b => b.CheckInDate)
                };
            })
            .AsEnumerable();

        if (!string.IsNullOrWhiteSpace(input.Keyword))
        {
            var kw = input.Keyword.Trim();
            grouped = grouped.Where(g =>
                g.FullName.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                g.PhoneNumber.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                g.Email.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                g.IdentityNumberMasked.Contains(kw, StringComparison.OrdinalIgnoreCase));
        }

        var ordered = grouped.OrderByDescending(g => g.LastCheckInDate).ToList();
        var maxCount = input.MaxResultCount > 0 ? input.MaxResultCount : 20;
        var pageItems = ordered.Skip(input.SkipCount).Take(maxCount).ToList();

        return new PagedResultDto<FrontdeskGuestListItemDto>(ordered.Count, pageItems);
    }

    public async Task<FrontdeskGuestDetailDto> GetDetailAsync(Guid hotelId, string guestKey)
    {
        await CheckFrontdeskHotelAccessAsync(hotelId);

        var bookings = await _bookingRepository.GetListAsync(b => b.HotelId == hotelId);
        var matched = bookings
            .Where(b => string.Equals(_stayDomainService.NormalizeGuestKey(b.GuestId, b.GuestPhone, b.Id), guestKey, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(b => b.CheckInDate)
            .ToList();

        if (matched.Count == 0)
        {
            throw new EntityNotFoundException(typeof(Booking), guestKey);
        }

        return await BuildGuestDetailAsync(hotelId, guestKey, matched);
    }

    public async Task<FrontdeskGuestDetailDto> UpdateAsync(string guestKey, UpdateFrontdeskGuestDto input)
    {
        await CheckFrontdeskHotelAccessAsync(input.HotelId);

        var bookings = await _bookingRepository.GetListAsync(b => b.HotelId == input.HotelId);
        var matched = bookings
            .Where(b => string.Equals(_stayDomainService.NormalizeGuestKey(b.GuestId, b.GuestPhone, b.Id), guestKey, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (matched.Count == 0)
        {
            throw new EntityNotFoundException(typeof(Booking), guestKey);
        }

        string? maskedIdentity = null;
        if (!string.IsNullOrWhiteSpace(input.IdentityNumber))
        {
            maskedIdentity = _stayDomainService.MaskIdentityNumber(input.IdentityNumber);
        }

        var trimmedName = input.FullName.Trim();
        var trimmedPhone = input.PhoneNumber.Trim();
        var trimmedEmail = input.Email?.Trim() ?? string.Empty;

        // Đồng bộ trên các booking đang hoạt động (PendingConfirmation, Confirmed, CheckedIn); nếu khách chỉ có booking lịch sử, cập nhật bản ghi mới nhất
        var targets = matched.Where(b =>
            b.BookingStatus == BookingStatus.PendingConfirmation ||
            b.BookingStatus == BookingStatus.Confirmed ||
            b.BookingStatus == BookingStatus.CheckedIn).ToList();

        if (targets.Count == 0)
        {
            targets = new List<Booking> { matched.OrderByDescending(b => b.CheckInDate).First() };
        }

        foreach (var booking in targets)
        {
            booking.GuestFullName = trimmedName;
            booking.GuestPhone = trimmedPhone;
            booking.GuestEmail = trimmedEmail;
            if (!string.IsNullOrWhiteSpace(maskedIdentity))
            {
                booking.GuestIdentityNumberMasked = maskedIdentity;
            }
            await _bookingRepository.UpdateAsync(booking);
        }

        await CurrentUnitOfWork!.SaveChangesAsync();

        var latest = targets.OrderByDescending(b => b.CheckInDate).First();
        var newGuestKey = _stayDomainService.NormalizeGuestKey(latest.GuestId, latest.GuestPhone, latest.Id);
        return await BuildGuestDetailAsync(input.HotelId, newGuestKey, matched.OrderByDescending(b => b.CheckInDate).ToList());
    }

    private async Task<FrontdeskGuestDetailDto> BuildGuestDetailAsync(Guid hotelId, string guestKey, List<Booking> matched)
    {
        var bookingIds = matched.Select(b => b.Id).ToList();
        var assigns = await _roomAssignmentRepository.GetListAsync(a => bookingIds.Contains(a.BookingId));
        var rooms = await _roomRepository.GetListAsync(r => r.HotelId == hotelId);
        var roomTypes = await _roomTypeRepository.GetListAsync(rt => rt.HotelId == hotelId);

        var roomDict = rooms.ToDictionary(r => r.Id);
        var roomTypeDict = roomTypes.ToDictionary(rt => rt.Id);
        var assignsByBooking = assigns.GroupBy(a => a.BookingId).ToDictionary(g => g.Key, g => g.ToList());

        var latest = matched.OrderByDescending(b => b.LastModificationTime ?? b.CreationTime).First();
        var maskedId = matched
            .OrderByDescending(b => b.IsIdentityVerified)
            .ThenByDescending(b => b.LastModificationTime ?? b.CreationTime)
            .Select(b => b.GuestIdentityNumberMasked)
            .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? string.Empty;

        var history = matched.Select(b =>
        {
            assignsByBooking.TryGetValue(b.Id, out var bAssigns);
            var activeAssign = bAssigns?.FirstOrDefault(a => a.IsActive);
            var latestAssign = activeAssign ?? bAssigns?.OrderByDescending(a => a.CreationTime).FirstOrDefault();
            Room? room = latestAssign != null && roomDict.TryGetValue(latestAssign.RoomId, out var r) ? r : null;
            var rtId = b.RoomTypeId ?? latestAssign?.RoomTypeId ?? room?.RoomTypeId;
            var rtName = rtId.HasValue && roomTypeDict.TryGetValue(rtId.Value, out var rt) ? rt.Name : string.Empty;

            return new FrontdeskGuestBookingItemDto
            {
                BookingId = b.Id,
                BookingCode = b.BookingCode,
                RoomTypeName = rtName,
                AssignedRoomNumber = room?.RoomNumber,
                CheckInDate = b.CheckInDate,
                CheckOutDate = b.CheckOutDate,
                ActualCheckInTime = b.ActualCheckInTime,
                ActualCheckOutTime = b.ActualCheckOutTime,
                BookingStatus = b.BookingStatus,
                PaymentStatus = b.PaymentStatus,
                TotalAmount = b.TotalAmount
            };
        }).ToList();

        return new FrontdeskGuestDetailDto
        {
            GuestKey = guestKey,
            GuestId = latest.GuestId,
            FullName = latest.GuestFullName,
            PhoneNumber = latest.GuestPhone,
            Email = latest.GuestEmail,
            IdentityNumberMasked = maskedId,
            IsIdentityVerified = matched.Any(b => b.IsIdentityVerified),
            TotalBookingsCount = matched.Count,
            ActiveBookingsCount = matched.Count(b =>
                b.BookingStatus == BookingStatus.PendingConfirmation ||
                b.BookingStatus == BookingStatus.Confirmed ||
                b.BookingStatus == BookingStatus.CheckedIn),
            LastCheckInDate = matched.Max(b => b.CheckInDate),
            Bookings = history
        };
    }
}
