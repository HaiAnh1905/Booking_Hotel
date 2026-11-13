using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BookingHotel.Identity;

public class AssignUserHotelDto
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    public List<Guid> HotelIds { get; set; } = new();

    public Guid? PrimaryHotelId { get; set; }
}

