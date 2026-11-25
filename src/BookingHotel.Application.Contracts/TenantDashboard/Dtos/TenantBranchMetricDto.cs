using System;

namespace BookingHotel.TenantDashboard.Dtos;

public class TenantBranchMetricDto
{
    public Guid HotelId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public int Rooms { get; set; }
    public double OccupancyPercent { get; set; }
    public decimal MonthlyRevenue { get; set; }
    public decimal Adr { get; set; }
    public decimal RevPar { get; set; }
    public int Rank { get; set; }
}
