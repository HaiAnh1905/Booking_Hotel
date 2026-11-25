using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using BookingHotel.Bookings;
using Volo.Abp.Application.Dtos;

namespace BookingHotel.Frontdesk;

public class GetFrontdeskGuestsInputDto : PagedResultRequestDto
{
    [Required]
    public Guid HotelId { get; set; }
    [StringLength(128)]
    public string? Keyword { get; set; }
}

public class FrontdeskGuestListItemDto
{
    public string GuestKey { get; set; } = string.Empty;
    public Guid? GuestId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string IdentityNumberMasked { get; set; } = string.Empty;
    public bool IsIdentityVerified { get; set; }
    public int TotalBookingsCount { get; set; }
    public int ActiveBookingsCount { get; set; }
    public DateTime? LastCheckInDate { get; set; }
}

public class FrontdeskGuestBookingItemDto
{
    public Guid BookingId { get; set; }
    public string BookingCode { get; set; } = string.Empty;
    public string RoomTypeName { get; set; } = string.Empty;
    public string? AssignedRoomNumber { get; set; }
    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }
    public DateTime? ActualCheckInTime { get; set; }
    public DateTime? ActualCheckOutTime { get; set; }
    public BookingStatus BookingStatus { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
    public decimal TotalAmount { get; set; }
}

public class FrontdeskGuestDetailDto : FrontdeskGuestListItemDto
{
    public List<FrontdeskGuestBookingItemDto> Bookings { get; set; } = new();
}

public class UpdateFrontdeskGuestDto
{
    [Required]
    public Guid HotelId { get; set; }
    [Required]
    [StringLength(128, MinimumLength = 2)]
    public string FullName { get; set; } = string.Empty;
    [Required]
    [StringLength(20, MinimumLength = 8)]
    public string PhoneNumber { get; set; } = string.Empty;
    [EmailAddress]
    [StringLength(128)]
    public string? Email { get; set; }
    [StringLength(32, MinimumLength = 6)]
    public string? IdentityNumber { get; set; }
}
