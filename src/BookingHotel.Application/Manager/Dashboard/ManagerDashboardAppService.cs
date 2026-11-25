using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BookingHotel.Bookings;
using BookingHotel.Complaints;
using BookingHotel.Hotels;
using BookingHotel.Identity;
using BookingHotel.Permissions;
using BookingHotel.Rooms;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Domain.Repositories;

namespace BookingHotel.Manager.Dashboard;

[Authorize(BookingHotelPermissions.Reports.HotelPerformance)]
public class ManagerDashboardAppService : ManagerAppServiceBase, IManagerDashboardAppService
{
    private readonly IRepository<Hotel, Guid> _hotelRepository;
    private readonly IRepository<Room, Guid> _roomRepository;
    private readonly IRepository<RoomType, Guid> _roomTypeRepository;
    private readonly IRepository<Booking, Guid> _bookingRepository;
    private readonly IRepository<RoomAssignment, Guid> _roomAssignmentRepository;
    private readonly IRepository<Complaint, Guid> _complaintRepository;

    public ManagerDashboardAppService(
        IRepository<UserHotelAssignment, Guid> userHotelAssignmentRepository,
        IRepository<Hotel, Guid> hotelRepository,
        IRepository<Room, Guid> roomRepository,
        IRepository<RoomType, Guid> roomTypeRepository,
        IRepository<Booking, Guid> bookingRepository,
        IRepository<RoomAssignment, Guid> roomAssignmentRepository,
        IRepository<Complaint, Guid> complaintRepository)
        : base(userHotelAssignmentRepository)
    {
        _hotelRepository = hotelRepository;
        _roomRepository = roomRepository;
        _roomTypeRepository = roomTypeRepository;
        _bookingRepository = bookingRepository;
        _roomAssignmentRepository = roomAssignmentRepository;
        _complaintRepository = complaintRepository;
    }

    public async Task<ManagerDashboardOverviewDto> GetKpisAsync(Guid hotelId)
    {
        await CheckHotelManagerAccessAsync(hotelId);

        var hotel = await _hotelRepository.FindAsync(hotelId);
        var rooms = await _roomRepository.GetListAsync(r => r.HotelId == hotelId);

        var totalRooms = rooms.Count;
        var availableRooms = rooms.Count(r => r.OperationalStatus == RoomOperationalStatus.Available);
        var occupiedRooms = rooms.Count(r => r.OperationalStatus == RoomOperationalStatus.Occupied);
        var reservedRooms = rooms.Count(r => r.OperationalStatus == RoomOperationalStatus.Reserved);
        var maintenanceRooms = rooms.Count(r => r.OperationalStatus == RoomOperationalStatus.Maintenance);
        var outOfServiceRooms = rooms.Count(r => r.OperationalStatus == RoomOperationalStatus.OutOfService);

        var availableInventory = Math.Max(0, totalRooms - outOfServiceRooms);
        var occupancyRate = availableInventory > 0
            ? Math.Round((double)occupiedRooms / availableInventory * 100, 1)
            : 0.0;

        var today = Clock.Now.Date;
        var startOfWeek = today.AddDays(-7);
        var startOfMonth = today.AddDays(-30);

        var bookings = await _bookingRepository.GetListAsync(b => b.HotelId == hotelId);

        var todayArrivals = bookings.Count(b => b.CheckInDate.Date == today &&
            (b.BookingStatus == BookingStatus.Confirmed || b.BookingStatus == BookingStatus.CheckedIn || b.BookingStatus == BookingStatus.PendingConfirmation));
        var todayDepartures = bookings.Count(b => b.CheckOutDate.Date == today &&
            (b.BookingStatus == BookingStatus.CheckedIn || b.BookingStatus == BookingStatus.Completed));

        var todayBookings = bookings.Where(b => b.CreationTime.Date == today || b.CheckInDate.Date == today).ToList();
        var todayRoomRevenue = todayBookings.Sum(b => b.TotalAmount);
        var todayCollectedRevenue = todayBookings.Sum(b => b.DepositAmount);

        var weekBookings = bookings.Where(b => b.CreationTime.Date >= startOfWeek).ToList();
        var weekRoomRevenue = weekBookings.Sum(b => b.TotalAmount);

        var monthBookings = bookings.Where(b => b.CreationTime.Date >= startOfMonth).ToList();
        var monthRoomRevenue = monthBookings.Sum(b => b.TotalAmount);

        var actionItems = await GetActionItemsAsync(hotelId);
        var actionItemsCount = actionItems.PendingConfirmationBookings +
                               actionItems.UnassignedArrivalsToday +
                               actionItems.OverdueHousekeepingRooms +
                               actionItems.OpenComplaintsNearSla;

        return new ManagerDashboardOverviewDto
        {
            HotelId = hotelId,
            HotelName = hotel?.Name ?? string.Empty,
            HotelCode = hotel?.Code ?? string.Empty,
            TotalRooms = totalRooms,
            AvailableRooms = availableRooms,
            OccupiedRooms = occupiedRooms,
            ReservedRooms = reservedRooms,
            MaintenanceRooms = maintenanceRooms,
            OutOfServiceRooms = outOfServiceRooms,
            AvailableInventory = availableInventory,
            OccupancyRate = occupancyRate,
            TodayArrivalsCount = todayArrivals,
            TodayDeparturesCount = todayDepartures,
            ActionItemsCount = actionItemsCount,
            TodayRoomRevenue = todayRoomRevenue,
            TodayCollectedRevenue = todayCollectedRevenue,
            WeekRoomRevenue = weekRoomRevenue,
            MonthRoomRevenue = monthRoomRevenue
        };
    }

    public async Task<List<TodayArrivalDto>> GetTodayArrivalsAsync(Guid hotelId, int maxResultCount = 10)
    {
        await CheckHotelManagerAccessAsync(hotelId);

        var today = Clock.Now.Date;
        var bookings = await _bookingRepository.GetListAsync(b =>
            b.HotelId == hotelId &&
            b.CheckInDate.Date == today &&
            (b.BookingStatus == BookingStatus.Confirmed || b.BookingStatus == BookingStatus.PendingConfirmation || b.BookingStatus == BookingStatus.CheckedIn));

        var pagedBookings = bookings.Take(maxResultCount).ToList();
        var bookingIds = pagedBookings.Select(b => b.Id).ToList();
        var assignments = await _roomAssignmentRepository.GetListAsync(a => bookingIds.Contains(a.BookingId) && a.IsActive);
        var assignedRoomIds = assignments.Select(a => a.RoomId).Distinct().ToList();
        var rooms = await _roomRepository.GetListAsync(r => assignedRoomIds.Contains(r.Id));

        var roomTypeIds = assignments.Select(a => a.RoomTypeId).Distinct().ToList();
        var roomTypes = await _roomTypeRepository.GetListAsync(rt => roomTypeIds.Contains(rt.Id));

        var result = new List<TodayArrivalDto>();
        foreach (var b in pagedBookings)
        {
            var assignment = assignments.FirstOrDefault(a => a.BookingId == b.Id);
            var room = assignment != null ? rooms.FirstOrDefault(r => r.Id == assignment.RoomId) : null;
            var roomType = assignment != null ? roomTypes.FirstOrDefault(rt => rt.Id == assignment.RoomTypeId) : null;

            result.Add(new TodayArrivalDto
            {
                BookingId = b.Id,
                BookingCode = b.BookingCode,
                GuestFullName = b.GuestFullName,
                GuestPhone = b.GuestPhone,
                RoomTypeName = roomType?.Name ?? "Standard",
                AssignedRoomNumber = room?.RoomNumber,
                PaymentStatus = (int)b.PaymentStatus,
                EstimatedArrivalTime = "14:00",
                SpecialRequests = b.SpecialRequests
            });
        }

        return result;
    }

    public async Task<ManagerActionItemsDto> GetActionItemsAsync(Guid hotelId)
    {
        await CheckHotelManagerAccessAsync(hotelId);

        var today = Clock.Now.Date;

        var pendingConfirmation = await _bookingRepository.CountAsync(b =>
            b.HotelId == hotelId &&
            b.BookingStatus == BookingStatus.PendingConfirmation);

        var unassignedArrivals = await _bookingRepository.CountAsync(b =>
            b.HotelId == hotelId &&
            b.CheckInDate.Date == today &&
            b.RoomAssignmentStatus == RoomAssignmentStatus.Unassigned &&
            b.BookingStatus == BookingStatus.Confirmed);

        var overdueHousekeeping = await _roomRepository.CountAsync(r =>
            r.HotelId == hotelId &&
            r.OperationalStatus == RoomOperationalStatus.Available &&
            r.HousekeepingStatus == HousekeepingStatus.Dirty);

        var slaThreshold = Clock.Now.AddHours(12);
        var openComplaintsNearSla = await _complaintRepository.CountAsync(c =>
            c.HotelId == hotelId &&
            c.Status != ComplaintStatus.Resolved &&
            c.SlaDeadline <= slaThreshold);

        return new ManagerActionItemsDto
        {
            PendingConfirmationBookings = (int)pendingConfirmation,
            UnassignedArrivalsToday = (int)unassignedArrivals,
            OverdueHousekeepingRooms = (int)overdueHousekeeping,
            OpenComplaintsNearSla = (int)openComplaintsNearSla
        };
    }
}
