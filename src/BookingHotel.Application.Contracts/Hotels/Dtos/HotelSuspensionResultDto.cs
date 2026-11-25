using System;

namespace BookingHotel.Hotels.Dtos;

public class HotelSuspensionResultDto
{
    public Guid HotelId { get; set; }
    public HotelStatus Status { get; set; }
    public int AffectedBookingsCount { get; set; }
    public string Message { get; set; } = string.Empty;
}
