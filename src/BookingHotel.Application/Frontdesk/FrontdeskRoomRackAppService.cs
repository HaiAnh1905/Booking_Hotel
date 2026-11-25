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
using Volo.Abp.Domain.Repositories;

namespace BookingHotel.Frontdesk;

[RemoteService(IsEnabled = false)]
[Authorize(BookingHotelPermissions.Rooms.Default)]
public class FrontdeskRoomRackAppService : FrontdeskAppServiceBase, IFrontdeskRoomRackAppService
{
    private readonly IRepository<Room, Guid> _roomRepository;
    private readonly IRepository<RoomType, Guid> _roomTypeRepository;
    private readonly IRepository<RoomAssignment, Guid> _roomAssignmentRepository;
    private readonly IRepository<Booking, Guid> _bookingRepository;
    private readonly FrontdeskStayDomainService _stayDomainService;

    public FrontdeskRoomRackAppService(
        IRepository<UserHotelAssignment, Guid> userHotelAssignmentRepository,
        IRepository<Room, Guid> roomRepository,
        IRepository<RoomType, Guid> roomTypeRepository,
        IRepository<RoomAssignment, Guid> roomAssignmentRepository,
        IRepository<Booking, Guid> bookingRepository,
        FrontdeskStayDomainService stayDomainService)
        : base(userHotelAssignmentRepository)
    {
        _roomRepository = roomRepository;
        _roomTypeRepository = roomTypeRepository;
        _roomAssignmentRepository = roomAssignmentRepository;
        _bookingRepository = bookingRepository;
        _stayDomainService = stayDomainService;
    }

    public async Task<FrontdeskRoomRackDto> GetAsync(GetFrontdeskRoomRackInputDto input)
    {
        await CheckFrontdeskHotelAccessAsync(input.HotelId);

        var allRooms = await _roomRepository.GetListAsync(r => r.HotelId == input.HotelId);
        var roomTypes = await _roomTypeRepository.GetListAsync(rt => rt.HotelId == input.HotelId);
        var roomTypeDict = roomTypes.ToDictionary(rt => rt.Id);

        var roomIds = allRooms.Select(r => r.Id).ToList();
        var activeAssignments = roomIds.Count > 0
            ? await _roomAssignmentRepository.GetListAsync(a => roomIds.Contains(a.RoomId) && a.IsActive)
            : new List<RoomAssignment>();

        var bookingIds = activeAssignments.Select(a => a.BookingId).Distinct().ToList();
        var bookings = bookingIds.Count > 0
            ? await _bookingRepository.GetListAsync(b => bookingIds.Contains(b.Id))
            : new List<Booking>();
        var bookingDict = bookings.ToDictionary(b => b.Id);

        var assignmentsByRoom = activeAssignments
            .GroupBy(a => a.RoomId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var mappedAll = allRooms.Select(room =>
        {
            var rackStatus = _stayDomainService.ComputeRoomRackStatus(room);
            roomTypeDict.TryGetValue(room.RoomTypeId, out var rt);
            var roomTypeName = rt?.Name ?? string.Empty;
            var guestCapacity = room.Capacity > 0 ? room.Capacity : (rt?.StandardOccupancy ?? 2);
            var maxOccupancy = rt?.MaxOccupancy ?? guestCapacity;

            FrontdeskRoomBookingSummaryDto? linkedBooking = null;
            if (assignmentsByRoom.TryGetValue(room.Id, out var roomAssignments))
            {
                var linked = roomAssignments
                    .Select(a => bookingDict.TryGetValue(a.BookingId, out var b) ? b : null)
                    .Where(b => b != null && (b.BookingStatus == BookingStatus.CheckedIn || b.BookingStatus == BookingStatus.Confirmed))
                    .OrderByDescending(b => b!.BookingStatus == BookingStatus.CheckedIn)
                    .ThenBy(b => b!.CheckInDate)
                    .FirstOrDefault();

                if (linked != null)
                {
                    linkedBooking = new FrontdeskRoomBookingSummaryDto
                    {
                        BookingId = linked.Id,
                        BookingCode = linked.BookingCode,
                        GuestFullName = linked.GuestFullName,
                        GuestPhone = linked.GuestPhone,
                        CheckInDate = linked.CheckInDate,
                        CheckOutDate = linked.CheckOutDate,
                        BookingStatus = linked.BookingStatus,
                        PaymentStatus = linked.PaymentStatus,
                        TotalAmount = linked.TotalAmount,
                        DepositAmount = linked.DepositAmount,
                        GuestCount = guestCapacity,
                        IsIdentityVerified = linked.IsIdentityVerified
                    };
                }
            }

            return new FrontdeskRoomRackItemDto
            {
                RoomId = room.Id,
                RoomNumber = room.RoomNumber,
                Floor = room.Floor,
                Building = room.Building,
                RoomTypeId = room.RoomTypeId,
                RoomTypeName = roomTypeName,
                Capacity = guestCapacity,
                MaxOccupancy = maxOccupancy,
                SizeSquareMeters = rt?.SizeSquareMeters ?? 0,
                BedType = room.BedType,
                Description = room.Description,
                RackStatus = rackStatus,
                OccupancyStatus = room.OccupancyStatus,
                OperationalStatus = room.OperationalStatus,
                IsReadyForCheckIn = rackStatus == FrontdeskRoomRackStatus.Ready || rackStatus == FrontdeskRoomRackStatus.Reserved,
                UnavailableReason = rackStatus == FrontdeskRoomRackStatus.Unavailable
                    ? (room.OutOfServiceReason ?? room.MaintenanceReason)
                    : null,
                CurrentBooking = linkedBooking
            };
        })
        .OrderBy(r => r.Floor)
        .ThenBy(r => r.RoomNumber, StringComparer.OrdinalIgnoreCase)
        .ToList();

        var filtered = mappedAll.AsEnumerable();
        if (input.Floor.HasValue)
        {
            filtered = filtered.Where(r => r.Floor == input.Floor.Value);
        }
        if (input.RoomTypeId.HasValue)
        {
            filtered = filtered.Where(r => r.RoomTypeId == input.RoomTypeId.Value);
        }
        if (input.RackStatus.HasValue)
        {
            filtered = filtered.Where(r => r.RackStatus == input.RackStatus.Value);
        }
        if (!string.IsNullOrWhiteSpace(input.Search))
        {
            var q = input.Search.Trim();
            filtered = filtered.Where(r =>
                r.RoomNumber.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                r.RoomTypeName.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                (r.CurrentBooking != null && (
                    r.CurrentBooking.GuestFullName.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    r.CurrentBooking.BookingCode.Contains(q, StringComparison.OrdinalIgnoreCase))));
        }

        return new FrontdeskRoomRackDto
        {
            HotelId = input.HotelId,
            TotalRooms = mappedAll.Count,
            ReadyCount = mappedAll.Count(r => r.RackStatus == FrontdeskRoomRackStatus.Ready),
            ReservedCount = mappedAll.Count(r => r.RackStatus == FrontdeskRoomRackStatus.Reserved),
            OccupiedCount = mappedAll.Count(r => r.RackStatus == FrontdeskRoomRackStatus.Occupied),
            NotReadyCount = mappedAll.Count(r => r.RackStatus == FrontdeskRoomRackStatus.NotReady),
            UnavailableCount = mappedAll.Count(r => r.RackStatus == FrontdeskRoomRackStatus.Unavailable),
            Floors = mappedAll.Select(r => r.Floor).Distinct().OrderBy(f => f).ToList(),
            Rooms = filtered.ToList()
        };
    }
}
