using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using BookingHotel.Bookings;
using Volo.Abp.Application.Dtos;

namespace BookingHotel.Frontdesk;

public class GetFrontdeskReservationsInputDto : PagedResultRequestDto
{
    [Required]
    public Guid HotelId { get; set; }
    [StringLength(128)]
    public string? Keyword { get; set; }
    public BookingStatus? BookingStatus { get; set; }
    public RoomAssignmentStatus? RoomAssignmentStatus { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public bool? ArrivalTodayOnly { get; set; }
    public bool? DepartureTodayOnly { get; set; }
}

public class FrontdeskReservationListItemDto
{
    public Guid Id { get; set; }
    public Guid HotelId { get; set; }
    public string BookingCode { get; set; } = string.Empty;
    public string GuestFullName { get; set; } = string.Empty;
    public string GuestPhone { get; set; } = string.Empty;
    public string GuestEmail { get; set; } = string.Empty;
    public string GuestIdentityNumberMasked { get; set; } = string.Empty;
    public bool IsIdentityVerified { get; set; }
    public Guid? RoomTypeId { get; set; }
    public string RoomTypeName { get; set; } = string.Empty;
    public Guid? AssignedRoomId { get; set; }
    public string? AssignedRoomNumber { get; set; }
    public int? AssignedRoomFloor { get; set; }
    public bool AssignedRoomIsReady { get; set; }
    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }
    public DateTime? ActualCheckInTime { get; set; }
    public DateTime? ActualCheckOutTime { get; set; }
    public BookingStatus BookingStatus { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
    public RoomAssignmentStatus RoomAssignmentStatus { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal DepositAmount { get; set; }
    public DateTime CreationTime { get; set; }
}

public class FrontdeskReservationDetailDto : FrontdeskReservationListItemDto
{
    public Guid? GuestId { get; set; }
    public string GuestKey { get; set; } = string.Empty;
    public string? SpecialRequests { get; set; }
    public string? FrontdeskNotes { get; set; }
    public string? CancellationReason { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime? LastModificationTime { get; set; }
}

public class FrontdeskAvailableRoomDto
{
    public Guid RoomId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public int Floor { get; set; }
    public string? Building { get; set; }
    public Guid RoomTypeId { get; set; }
    public string RoomTypeName { get; set; } = string.Empty;
}

public class ConfirmFrontdeskReservationDto
{
    [Required]
    public Guid HotelId { get; set; }
}

public class AssignFrontdeskRoomDto
{
    [Required]
    public Guid HotelId { get; set; }
    [Required]
    public Guid RoomId { get; set; }
}

public class UpdateFrontdeskReservationDto
{
    [Required]
    public Guid HotelId { get; set; }
    [Required]
    [StringLength(128, MinimumLength = 2)]
    public string GuestFullName { get; set; } = string.Empty;
    [Required]
    [StringLength(20, MinimumLength = 8)]
    public string GuestPhone { get; set; } = string.Empty;
    [EmailAddress]
    [StringLength(128)]
    public string? GuestEmail { get; set; }
    [StringLength(2000)]
    public string? SpecialRequests { get; set; }
    [StringLength(2000)]
    public string? FrontdeskNotes { get; set; }
}

public class CancelFrontdeskReservationDto
{
    [Required]
    public Guid HotelId { get; set; }
    [Required]
    [StringLength(1000, MinimumLength = 5)]
    public string Reason { get; set; } = string.Empty;
}

public class FrontdeskCheckInDto
{
    [Required]
    public Guid HotelId { get; set; }
    [Required]
    public Guid RoomId { get; set; }
    [Required]
    [StringLength(128, MinimumLength = 2)]
    public string GuestFullName { get; set; } = string.Empty;
    [Required]
    [StringLength(20, MinimumLength = 8)]
    public string GuestPhone { get; set; } = string.Empty;
    [EmailAddress]
    [StringLength(128)]
    public string? GuestEmail { get; set; }
    [Required]
    [StringLength(32, MinimumLength = 6)]
    public string IdentityNumber { get; set; } = string.Empty;
    [StringLength(2000)]
    public string? FrontdeskNotes { get; set; }
}

public class FrontdeskCheckOutDto
{
    [Required]
    public Guid HotelId { get; set; }
    [StringLength(2000)]
    public string? FrontdeskNotes { get; set; }
}
