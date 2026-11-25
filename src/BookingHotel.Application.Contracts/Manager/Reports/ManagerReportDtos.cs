using System;

namespace BookingHotel.Manager.Reports;

public class HotelPerformanceReportDto
{
    public Guid HotelId { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public int TotalPhysicalRooms { get; set; }
    public int OutOfServiceRoomNights { get; set; }
    public int AvailableInventoryNights { get; set; }
    public int OccupiedRoomNights { get; set; }
    public double OccupancyRate { get; set; }
    public decimal TotalRoomRevenue { get; set; }
    public decimal AverageDailyRate { get; set; }
    public decimal RevenuePerAvailableRoom { get; set; }
}

public class RevenueBreakdownReportDto
{
    public decimal RoomRevenue { get; set; }
    public decimal ServiceRevenue { get; set; }
    public decimal TotalBilledRevenue { get; set; }
    public decimal CollectedRevenue { get; set; }
    public decimal RefundAmount { get; set; }
    public decimal UncollectedBalance { get; set; }
}

public class CancellationLossReportDto
{
    public int TotalBookings { get; set; }
    public int CancelledCount { get; set; }
    public double CancellationRate { get; set; }
    public int NoShowCount { get; set; }
    public double NoShowRate { get; set; }
    public int WaivedNoShowPenaltiesCount { get; set; }
    public decimal TotalPenaltiesCollected { get; set; }
    public int OutOfServiceLossNights { get; set; }
    public decimal EstimatedRevenueLoss { get; set; }
}
