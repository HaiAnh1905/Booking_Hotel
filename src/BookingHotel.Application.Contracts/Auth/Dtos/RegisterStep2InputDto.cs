using System;
using System.ComponentModel.DataAnnotations;

namespace BookingHotel.Auth.Dtos;

public class RegisterStep2InputDto
{
    [Required]
    public Guid RegistrationToken { get; set; }

    [Required]
    [StringLength(128, MinimumLength = 2)]
    public string FullName { get; set; } = default!;
}

