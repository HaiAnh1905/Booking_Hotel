using System;
using System.Collections.Generic;

namespace BookingHotel.TenantReports.Dtos;

public class RevenueReportItemDto
{
    public DateTime Date { get; set; }
    public string PeriodLabel { get; set; } = string.Empty;
    public decimal RoomRevenue { get; set; }
    public decimal ServiceRevenue { get; set; }
    public decimal RefundAmount { get; set; }
    public decimal NetRevenue { get; set; }
    public int CompletedBookingsCount { get; set; }
}

public class RevenueReportDto
{
    public decimal TotalNetRevenue { get; set; }
    public decimal TotalRoomRevenue { get; set; }
    public decimal TotalServiceRevenue { get; set; }
    public decimal TotalRefundAmount { get; set; }
    public int TotalBookingsCount { get; set; }
    public List<RevenueReportItemDto> Items { get; set; } = new();
}
