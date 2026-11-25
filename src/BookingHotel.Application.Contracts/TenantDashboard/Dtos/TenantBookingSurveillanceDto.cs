using System;
using Volo.Abp.Application.Dtos;

namespace BookingHotel.TenantDashboard.Dtos;

public class TenantBookingSurveillanceDto : EntityDto<Guid>
{
    public Guid BookingId { get; set; }
    public string BookingCode { get; set; } = string.Empty;
    public Guid HotelId { get; set; }
    public string HotelName { get; set; } = string.Empty;
    public string HotelCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }
    public string RoomTypeName { get; set; } = string.Empty;
    public string RoomNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public DateTime CreationTime { get; set; }
}
