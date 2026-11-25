using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BookingHotel.Bookings;
using BookingHotel.Hotels;
using BookingHotel.Identity;
using BookingHotel.Permissions;
using BookingHotel.Rooms;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;

namespace BookingHotel.Frontdesk;

[RemoteService(IsEnabled = false)]
[Authorize(BookingHotelPermissions.Bookings.Default)]
public class FrontdeskDashboardAppService : FrontdeskAppServiceBase, IFrontdeskDashboardAppService
{
    private readonly IRepository<Hotel, Guid> _hotelRepository;
    private readonly IRepository<Booking, Guid> _bookingRepository;
    private readonly IRepository<RoomAssignment, Guid> _roomAssignmentRepository;
    private readonly IRepository<Room, Guid> _roomRepository;
    private readonly IRepository<RoomType, Guid> _roomTypeRepository;

    public FrontdeskDashboardAppService(
        IRepository<UserHotelAssignment, Guid> userHotelAssignmentRepository,
        IRepository<Hotel, Guid> hotelRepository,
        IRepository<Booking, Guid> bookingRepository,
        IRepository<RoomAssignment, Guid> roomAssignmentRepository,
        IRepository<Room, Guid> roomRepository,
        IRepository<RoomType, Guid> roomTypeRepository)
        : base(userHotelAssignmentRepository)
    {
        _hotelRepository = hotelRepository;
        _bookingRepository = bookingRepository;
        _roomAssignmentRepository = roomAssignmentRepository;
        _roomRepository = roomRepository;
        _roomTypeRepository = roomTypeRepository;
    }

    public async Task<FrontdeskDashboardDto> GetOverviewAsync(Guid hotelId)
    {
        await CheckFrontdeskHotelAccessAsync(hotelId);

        var hotel = await _hotelRepository.FindAsync(hotelId);
        var today = Clock.Now.Date;
        var bookings = await _bookingRepository.GetListAsync(b => b.HotelId == hotelId);

        var activeBookings = bookings.Where(b =>
            b.BookingStatus == BookingStatus.PendingConfirmation ||
            b.BookingStatus == BookingStatus.Confirmed ||
            b.BookingStatus == BookingStatus.CheckedIn).ToList();

        var pendingBookings = bookings
            .Where(b => b.BookingStatus == BookingStatus.PendingConfirmation)
            .OrderBy(b => b.CheckInDate)
            .ThenByDescending(b => b.CreationTime)
            .ToList();

        var arrivalsToday = bookings
            .Where(b => b.CheckInDate.Date == today &&
                (b.BookingStatus == BookingStatus.Confirmed || b.BookingStatus == BookingStatus.PendingConfirmation))
            .OrderBy(b => b.BookingStatus)
            .ThenBy(b => b.CheckInDate)
            .ToList();

        var inHouse = bookings
            .Where(b => b.BookingStatus == BookingStatus.CheckedIn)
            .ToList();

        var departuresToday = bookings
            .Where(b => b.BookingStatus == BookingStatus.CheckedIn && b.CheckOutDate.Date <= today)
            .OrderBy(b => b.CheckOutDate)
            .ToList();

        var targetIds = arrivalsToday.Take(10)
            .Concat(departuresToday.Take(10))
            .Concat(pendingBookings.Take(10))
            .Select(b => b.Id)
            .Distinct()
            .ToList();

        var assignments = targetIds.Count > 0
            ? await _roomAssignmentRepository.GetListAsync(a => targetIds.Contains(a.BookingId) && a.IsActive)
            : new List<RoomAssignment>();

        var roomIds = assignments.Select(a => a.RoomId).Distinct().ToList();
        var rooms = roomIds.Count > 0
            ? await _roomRepository.GetListAsync(r => roomIds.Contains(r.Id))
            : new List<Room>();

        var roomTypes = await _roomTypeRepository.GetListAsync(rt => rt.HotelId == hotelId);
        var roomDict = rooms.ToDictionary(r => r.Id);
        var roomTypeDict = roomTypes.ToDictionary(rt => rt.Id);
        var assignmentDict = assignments
            .GroupBy(a => a.BookingId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.CreationTime).First());

        FrontdeskQuickBookingItemDto MapQuick(Booking b)
        {
            assignmentDict.TryGetValue(b.Id, out var assign);
            Room? room = assign != null && roomDict.TryGetValue(assign.RoomId, out var foundRoom) ? foundRoom : null;
            var resolvedRoomTypeId = b.RoomTypeId ?? assign?.RoomTypeId ?? room?.RoomTypeId;
            var roomTypeName = resolvedRoomTypeId.HasValue && roomTypeDict.TryGetValue(resolvedRoomTypeId.Value, out var rt)
                ? rt.Name
                : string.Empty;

            return new FrontdeskQuickBookingItemDto
            {
                BookingId = b.Id,
                BookingCode = b.BookingCode,
                GuestFullName = b.GuestFullName,
                GuestPhone = b.GuestPhone,
                RoomTypeName = roomTypeName,
                AssignedRoomId = room?.Id,
                AssignedRoomNumber = room?.RoomNumber,
                CheckInDate = b.CheckInDate,
                CheckOutDate = b.CheckOutDate,
                BookingStatus = b.BookingStatus,
                RoomAssignmentStatus = b.RoomAssignmentStatus,
                IsIdentityVerified = b.IsIdentityVerified
            };
        }

        return new FrontdeskDashboardDto
        {
            HotelId = hotelId,
            HotelName = hotel?.Name ?? string.Empty,
            HotelCode = hotel?.Code ?? string.Empty,
            BusinessDate = today,
            TotalActiveBookings = activeBookings.Count,
            PendingConfirmationCount = pendingBookings.Count,
            ExpectedArrivalsTodayCount = arrivalsToday.Count,
            InHouseGuestsCount = inHouse.Count,
            ExpectedDeparturesTodayCount = departuresToday.Count,
            TodayArrivals = arrivalsToday.Take(10).Select(MapQuick).ToList(),
            TodayDepartures = departuresToday.Take(10).Select(MapQuick).ToList(),
            PendingBookings = pendingBookings.Take(10).Select(MapQuick).ToList()
        };
    }
}
