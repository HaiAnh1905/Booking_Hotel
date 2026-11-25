using System;
using System.ComponentModel.DataAnnotations;

namespace BookingHotel.Manager.Hotels;

public class ManagerHotelProfileDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int StarRating { get; set; }
    public int RoomCount { get; set; }
    public string ManagerName { get; set; } = string.Empty;
    public TimeSpan StandardCheckInTime { get; set; }
    public TimeSpan StandardCheckOutTime { get; set; }
    public string? Description { get; set; }
    public string? AmenitiesJson { get; set; }
    public string? HouseRules { get; set; }
}

public class UpdateManagerHotelProfileDto
{
    [Required]
    public Guid HotelId { get; set; }

    [Required]
    [StringLength(512)]
    public string Address { get; set; } = string.Empty;

    [Required]
    [StringLength(128)]
    public string City { get; set; } = string.Empty;

    [Required]
    [StringLength(32)]
    public string Phone { get; set; } = string.Empty;

    [Required]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    public TimeSpan StandardCheckInTime { get; set; }

    public TimeSpan StandardCheckOutTime { get; set; }

    [StringLength(4000)]
    public string? Description { get; set; }

    [StringLength(4000)]
    public string? AmenitiesJson { get; set; }

    [StringLength(4000)]
    public string? HouseRules { get; set; }
}
