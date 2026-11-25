using System;
using System.Collections.Generic;

namespace BookingHotel.TenantReports.Dtos;

public class HotelPerformanceItemDto
{
    public Guid HotelId { get; set; }
    public string HotelName { get; set; } = string.Empty;
    public string HotelCode { get; set; } = string.Empty;
    public int RoomCount { get; set; }
    public decimal RoomRevenue { get; set; }
    public int AvailableRoomNights { get; set; }
    public int SoldRoomNights { get; set; }
    public double OccupancyPercent { get; set; }
    public decimal Adr { get; set; }
    public decimal RevPar { get; set; }
    public int Rank { get; set; }
}

public class HotelPerformanceReportDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public double ChainAverageOccupancy { get; set; }
    public decimal ChainAverageRevPar { get; set; }
    public decimal ChainAverageAdr { get; set; }
    public List<HotelPerformanceItemDto> Items { get; set; } = new();
}
