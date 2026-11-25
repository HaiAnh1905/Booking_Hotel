using System;

namespace BookingHotel.TenantReports.Dtos;

public class GetTenantReportInput
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public Guid? HotelId { get; set; }
    public string? GroupBy { get; set; } = "Day";
}
