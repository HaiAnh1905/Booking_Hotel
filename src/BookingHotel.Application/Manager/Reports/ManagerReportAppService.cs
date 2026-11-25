using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BookingHotel.Bookings;
using BookingHotel.Identity;
using BookingHotel.Permissions;
using BookingHotel.Rooms;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Domain.Repositories;

namespace BookingHotel.Manager.Reports;

[Authorize(BookingHotelPermissions.Reports.HotelPerformance)]
public class ManagerReportAppService : ManagerAppServiceBase, IManagerReportAppService
{
    private readonly IRepository<Room, Guid> _roomRepository;
    private readonly IRepository<Booking, Guid> _bookingRepository;

    public ManagerReportAppService(
        IRepository<UserHotelAssignment, Guid> userHotelAssignmentRepository,
        IRepository<Room, Guid> roomRepository,
        IRepository<Booking, Guid> bookingRepository)
        : base(userHotelAssignmentRepository)
    {
        _roomRepository = roomRepository;
        _bookingRepository = bookingRepository;
    }

    public async Task<HotelPerformanceReportDto> GetPerformanceReportAsync(Guid hotelId, DateTime fromDate, DateTime toDate)
    {
        await CheckHotelManagerAccessAsync(hotelId);

        var start = fromDate.Date;
        var end = toDate.Date;
        var totalDays = Math.Max(1, (end - start).Days + 1);

        var rooms = await _roomRepository.GetListAsync(r => r.HotelId == hotelId);
        var totalPhysicalRooms = rooms.Count;
        var totalPotentialNights = totalPhysicalRooms * totalDays;

        var oosRooms = rooms.Where(r => r.OperationalStatus == RoomOperationalStatus.OutOfService).ToList();
        var outOfServiceNights = oosRooms.Count * totalDays;

        var availableInventoryNights = Math.Max(0, totalPotentialNights - outOfServiceNights);

        var bookings = await _bookingRepository.GetListAsync(b =>
            b.HotelId == hotelId &&
            b.CheckInDate < end.AddDays(1) &&
            b.CheckOutDate > start);

        var validStays = bookings.Where(b =>
            b.BookingStatus == BookingStatus.Completed ||
            b.BookingStatus == BookingStatus.CheckedIn ||
            b.BookingStatus == BookingStatus.Confirmed).ToList();

        var occupiedRoomNights = 0;
        foreach (var b in validStays)
        {
            var stayStart = b.CheckInDate.Date < start ? start : b.CheckInDate.Date;
            var stayEnd = b.CheckOutDate.Date > end ? end : b.CheckOutDate.Date;
            var stayDays = Math.Max(1, (stayEnd - stayStart).Days);
            occupiedRoomNights += stayDays;
        }

        var occupancyRate = availableInventoryNights > 0
            ? Math.Round((double)occupiedRoomNights / availableInventoryNights * 100, 1)
            : 0.0;

        var totalRoomRevenue = validStays.Sum(b => b.TotalAmount);

        var adr = occupiedRoomNights > 0
            ? Math.Round(totalRoomRevenue / occupiedRoomNights, 0)
            : 0m;

        var revPar = availableInventoryNights > 0
            ? Math.Round(totalRoomRevenue / availableInventoryNights, 0)
            : 0m;

        return new HotelPerformanceReportDto
        {
            HotelId = hotelId,
            FromDate = start,
            ToDate = end,
            TotalPhysicalRooms = totalPhysicalRooms,
            OutOfServiceRoomNights = outOfServiceNights,
            AvailableInventoryNights = availableInventoryNights,
            OccupiedRoomNights = occupiedRoomNights,
            OccupancyRate = occupancyRate,
            TotalRoomRevenue = totalRoomRevenue,
            AverageDailyRate = adr,
            RevenuePerAvailableRoom = revPar
        };
    }

    public async Task<RevenueBreakdownReportDto> GetRevenueBreakdownAsync(Guid hotelId, DateTime fromDate, DateTime toDate)
    {
        await CheckHotelManagerAccessAsync(hotelId);

        var start = fromDate.Date;
        var end = toDate.Date;

        var bookings = await _bookingRepository.GetListAsync(b =>
            b.HotelId == hotelId &&
            b.CheckInDate < end.AddDays(1) &&
            b.CheckOutDate > start);

        var validStays = bookings.Where(b =>
            b.BookingStatus == BookingStatus.Completed ||
            b.BookingStatus == BookingStatus.CheckedIn ||
            b.BookingStatus == BookingStatus.Confirmed).ToList();

        var roomRevenue = validStays.Sum(b => b.TotalAmount);
        var serviceRevenue = Math.Round(roomRevenue * 0.12m, 0); // 12% dịch vụ ẩm thực, minibar, giặt là
        var totalBilled = roomRevenue + serviceRevenue;

        var collected = bookings.Sum(b => b.DepositAmount);
        var refunds = bookings.Where(b => b.PaymentStatus == PaymentStatus.Refunded)
            .Sum(b => Math.Max(0m, b.DepositAmount - b.PenaltyAmount));

        var uncollected = Math.Max(0m, totalBilled - collected);

        return new RevenueBreakdownReportDto
        {
            RoomRevenue = roomRevenue,
            ServiceRevenue = serviceRevenue,
            TotalBilledRevenue = totalBilled,
            CollectedRevenue = collected,
            RefundAmount = refunds,
            UncollectedBalance = uncollected
        };
    }

    public async Task<CancellationLossReportDto> GetCancellationLossReportAsync(Guid hotelId, DateTime fromDate, DateTime toDate)
    {
        await CheckHotelManagerAccessAsync(hotelId);

        var start = fromDate.Date;
        var end = toDate.Date;
        var totalDays = Math.Max(1, (end - start).Days + 1);

        var bookings = await _bookingRepository.GetListAsync(b =>
            b.HotelId == hotelId &&
            b.CreationTime >= start &&
            b.CreationTime < end.AddDays(1));

        var totalBookings = bookings.Count;
        var cancelledCount = bookings.Count(b => b.BookingStatus == BookingStatus.Cancelled || b.BookingStatus == BookingStatus.Rejected);
        var cancelRate = totalBookings > 0 ? Math.Round((double)cancelledCount / totalBookings * 100, 1) : 0.0;

        var noShowBookings = bookings.Where(b => b.BookingStatus == BookingStatus.NoShow).ToList();
        var noShowCount = noShowBookings.Count;
        var noShowRate = totalBookings > 0 ? Math.Round((double)noShowCount / totalBookings * 100, 1) : 0.0;

        var waivedCount = noShowBookings.Count(b => !string.IsNullOrWhiteSpace(b.WaivePenaltyReason));
        var penaltiesCollected = noShowBookings.Sum(b => b.PenaltyAmount);

        var rooms = await _roomRepository.GetListAsync(r => r.HotelId == hotelId);
        var oosCount = rooms.Count(r => r.OperationalStatus == RoomOperationalStatus.OutOfService);
        var oosLossNights = oosCount * totalDays;
        var estimatedLoss = oosLossNights * 1600000m; // Ước tính 1.6M/đêm

        return new CancellationLossReportDto
        {
            TotalBookings = totalBookings,
            CancelledCount = cancelledCount,
            CancellationRate = cancelRate,
            NoShowCount = noShowCount,
            NoShowRate = noShowRate,
            WaivedNoShowPenaltiesCount = waivedCount,
            TotalPenaltiesCollected = penaltiesCollected,
            OutOfServiceLossNights = oosLossNights,
            EstimatedRevenueLoss = estimatedLoss
        };
    }
}
