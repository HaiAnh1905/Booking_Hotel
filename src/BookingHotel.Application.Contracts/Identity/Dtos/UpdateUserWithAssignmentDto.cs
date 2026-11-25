using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BookingHotel.Identity.Dtos;

public class UpdateUserWithAssignmentDto
{
    [StringLength(64, MinimumLength = 2)]
    public string? UserName { get; set; }

    [StringLength(64)]
    public string? Name { get; set; }

    [StringLength(64)]
    public string? Surname { get; set; }

    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Phone]
    [StringLength(32)]
    public string? PhoneNumber { get; set; }

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

    public string? ConcurrencyStamp { get; set; }

    public bool? IsActive { get; set; }
}

