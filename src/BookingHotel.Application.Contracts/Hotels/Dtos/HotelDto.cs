using System;
using Volo.Abp.Application.Dtos;

namespace BookingHotel.Hotels.Dtos;

public class HotelDto : FullAuditedEntityDto<Guid>
{
    public Guid? TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int StarRating { get; set; }
    public int RoomCount { get; set; }
    public string ManagerName { get; set; } = string.Empty;
    public Guid? ManagerUserId { get; set; }
    public HotelStatus Status { get; set; }
    public TimeSpan StandardCheckInTime { get; set; }
    public TimeSpan StandardCheckOutTime { get; set; }
    public int BookingCutoffDays { get; set; }
}
