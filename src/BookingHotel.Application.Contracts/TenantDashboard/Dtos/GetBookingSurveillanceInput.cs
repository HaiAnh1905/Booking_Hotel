using System;
using Volo.Abp.Application.Dtos;

namespace BookingHotel.TenantDashboard.Dtos;

public class GetBookingSurveillanceInput : PagedAndSortedResultRequestDto
{
    public Guid? HotelId { get; set; }
    public string? Status { get; set; }
    public string? PaymentStatus { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? Filter { get; set; }
}
