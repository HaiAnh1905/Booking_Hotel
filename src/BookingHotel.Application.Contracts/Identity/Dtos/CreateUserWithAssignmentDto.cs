using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BookingHotel.Identity.Dtos;

public class CreateUserWithAssignmentDto
{
    [Required]
    [StringLength(256, MinimumLength = 3)]
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
    public List<string> RoleNames { get; set; } = new();

    public List<string>? Roles
    {
        get => RoleNames;
        set { if (value != null && value.Count > 0) RoleNames = value; }
    }

    public List<Guid>? ManagerHotelIds
    {
        get => HotelIds;
        set { if (value != null) HotelIds = value; }
    }

    public Guid? PrimaryReceptionistHotelId
    {
        get => PrimaryHotelId;
        set { if (value != null) PrimaryHotelId = value; }
    }

    public List<Guid> HotelIds { get; set; } = new();

    public Guid? PrimaryHotelId { get; set; }
}

