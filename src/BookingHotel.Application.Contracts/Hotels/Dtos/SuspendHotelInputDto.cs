using System;
using System.ComponentModel.DataAnnotations;

namespace BookingHotel.Hotels.Dtos;

public class SuspendHotelInputDto
{
    [Required]
    public SuspensionType SuspensionType { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public string Reason { get; set; } = string.Empty;
}
