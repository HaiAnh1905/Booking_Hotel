using System;
using System.Collections.Generic;

namespace BookingHotel.Manager.Rooms;

public class RoomDashboardOverviewDto
{
    public Guid HotelId { get; set; }
    public int TotalRooms { get; set; }
    public int OccupiedRooms { get; set; }
    public int VacantRooms { get; set; }
    public int ReservedRooms { get; set; }
    public int UnavailableRooms { get; set; }
    public double OccupancyRate { get; set; }
    public Dictionary<string, int> OccupancyBreakdown { get; set; } = new();
    public Dictionary<string, int> OperationalBreakdown { get; set; } = new();
}
