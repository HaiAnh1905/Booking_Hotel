using System;
using System.ComponentModel.DataAnnotations;

namespace BookingHotel.Hotels.Dtos;

public class CreateUpdateHotelDto
{
    [Required]
    [StringLength(256)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(32)]
    public string Code { get; set; } = string.Empty;

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
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Range(1, 5)]
    public int StarRating { get; set; } = 5;

    [Range(1, 5000)]
    public int RoomCount { get; set; } = 50;

    [StringLength(256)]
    public string ManagerName { get; set; } = string.Empty;

    public Guid? ManagerUserId { get; set; }

    public HotelStatus Status { get; set; } = HotelStatus.Operating;

    public TimeSpan? StandardCheckInTime { get; set; }

    public TimeSpan? StandardCheckOutTime { get; set; }

    public int BookingCutoffDays { get; set; } = 14;
}
