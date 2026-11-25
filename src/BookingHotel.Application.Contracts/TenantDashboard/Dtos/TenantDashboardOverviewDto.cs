using System;
using System.Collections.Generic;

namespace BookingHotel.TenantDashboard.Dtos;

public class TenantDashboardOverviewDto
{
    public string ChainName { get; set; } = string.Empty;
    public int TotalHotelCount { get; set; }
    public int OperatingBranchCount { get; set; }
    public int SuspendedBranchCount { get; set; }
    public int TotalStaffCount { get; set; }
    public int TotalBookingCount { get; set; }
    public decimal MonthlyCashRevenue { get; set; }
    public double AverageOccupancyPercent { get; set; }
    public decimal AverageRevPar { get; set; }
    public decimal AverageAdr { get; set; }
    public List<TenantBranchMetricDto> Branches { get; set; } = new();
}
