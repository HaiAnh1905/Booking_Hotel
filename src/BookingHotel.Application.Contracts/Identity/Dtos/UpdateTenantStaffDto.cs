using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BookingHotel.Identity.Dtos;

public class UpdateTenantStaffDto
{
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

    public string? Password { get; set; }

    [Required]
    public List<string> Roles { get; set; } = new();

    public List<Guid> ManagerHotelIds { get; set; } = new();

    public Guid? PrimaryReceptionistHotelId { get; set; }

    public string? ConcurrencyStamp { get; set; }
}
