using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BookingHotel.Identity.Dtos;

public class CreateTenantStaffDto
{
    [Required]
    [StringLength(256, MinimumLength = 2)]
    public string UserName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [StringLength(64)]
    public string? Name { get; set; }

    [StringLength(64)]
    public string? Surname { get; set; }

    [Phone]
    [StringLength(32)]
    public string? PhoneNumber { get; set; }

    [Required]
    [StringLength(128, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;

    [Required]
    public List<string> Roles { get; set; } = new();

    public List<Guid> ManagerHotelIds { get; set; } = new();

    public Guid? PrimaryReceptionistHotelId { get; set; }
}
