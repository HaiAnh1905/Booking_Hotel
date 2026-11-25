using System;

namespace BookingHotel.Identity.Dtos;

public class AssignedHotelDto
{
    public Guid HotelId { get; set; }

    public string HotelName { get; set; } = string.Empty;

    public string HotelCode { get; set; } = string.Empty;

    public bool IsPrimary { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime AssignedAt { get; set; }

    public DateTime? UnassignedAt { get; set; }
}

