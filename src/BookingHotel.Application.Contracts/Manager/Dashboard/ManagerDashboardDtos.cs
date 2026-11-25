using System;
using System.Collections.Generic;

namespace BookingHotel.Manager.Dashboard;

public class ManagerDashboardOverviewDto
{
    public Guid HotelId { get; set; }
    public string HotelName { get; set; } = string.Empty;
    public string HotelCode { get; set; } = string.Empty;
    public int TotalRooms { get; set; }
    public int AvailableRooms { get; set; }
    public int OccupiedRooms { get; set; }
    public int ReservedRooms { get; set; }
    public int MaintenanceRooms { get; set; }
    public int OutOfServiceRooms { get; set; }
    public int AvailableInventory { get; set; }
    public double OccupancyRate { get; set; }
    public int TodayArrivalsCount { get; set; }
    public int TodayDeparturesCount { get; set; }
    public int ActionItemsCount { get; set; }
    public decimal TodayRoomRevenue { get; set; }
    public decimal TodayCollectedRevenue { get; set; }
    public decimal WeekRoomRevenue { get; set; }
    public decimal MonthRoomRevenue { get; set; }
}

public class TodayArrivalDto
{
    public Guid BookingId { get; set; }
    public string BookingCode { get; set; } = string.Empty;
    public string GuestFullName { get; set; } = string.Empty;
    public string GuestPhone { get; set; } = string.Empty;
    public string RoomTypeName { get; set; } = string.Empty;
    public string? AssignedRoomNumber { get; set; }
    public int PaymentStatus { get; set; }
    public string EstimatedArrivalTime { get; set; } = "14:00";
    public string? SpecialRequests { get; set; }
}

public class ManagerActionItemsDto
{
    public int PendingConfirmationBookings { get; set; }
    public int UnassignedArrivalsToday { get; set; }
    public int OverdueHousekeepingRooms { get; set; }
    public int OpenComplaintsNearSla { get; set; }
}
