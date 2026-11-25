using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using BookingHotel.Bookings;
using Volo.Abp.Application.Dtos;

namespace BookingHotel.Manager.Bookings;

public class ManagerBookingDto
{
    public Guid Id { get; set; }
    public Guid HotelId { get; set; }
    public string BookingCode { get; set; } = string.Empty;
    public string GuestFullName { get; set; } = string.Empty;
    public string GuestPhone { get; set; } = string.Empty;
    public string GuestEmail { get; set; } = string.Empty;
    public string GuestIdentityNumberMasked { get; set; } = string.Empty;
    public string RoomTypeName { get; set; } = string.Empty;
    public string? AssignedRoomNumber { get; set; }
    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal DepositAmount { get; set; }
    public BookingStatus BookingStatus { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
    public RoomAssignmentStatus RoomAssignmentStatus { get; set; }
    public bool IsDirectBooking { get; set; }
    public string? ContractNotes { get; set; }
    public string? SpecialRequests { get; set; }
    public string? RejectionReason { get; set; }
    public string? WaivePenaltyReason { get; set; }
    public decimal PenaltyAmount { get; set; }
    public DateTime CreationTime { get; set; }
}

public class GetManagerBookingsInputDto : PagedAndSortedResultRequestDto
{
    [Required]
    public Guid HotelId { get; set; }

    public string? BookingCode { get; set; }

    public string? GuestKeyword { get; set; }

    public BookingStatus? BookingStatus { get; set; }

    public PaymentStatus? PaymentStatus { get; set; }

    public DateTime? FromDate { get; set; }

    public DateTime? ToDate { get; set; }
}

public class CreateDirectBookingDto
{
    [Required]
    public Guid HotelId { get; set; }

    [Required]
    [StringLength(128)]
    public string GuestFullName { get; set; } = string.Empty;

    [Required]
    [StringLength(32)]
    public string GuestPhone { get; set; } = string.Empty;

    [StringLength(128)]
    public string? GuestEmail { get; set; }

    [StringLength(32)]
    public string? GuestIdentityNumber { get; set; }

    [Required]
    public Guid RoomTypeId { get; set; }

    [Required]
    public DateTime CheckInDate { get; set; }

    [Required]
    public DateTime CheckOutDate { get; set; }

    [Range(1, 50)]
    public int NumberOfRooms { get; set; } = 1;

    [Range(0, 1000000000)]
    public decimal AgreedRatePerNight { get; set; }

    [Range(0, 1000000000)]
    public decimal DepositAmountReceived { get; set; }

    [StringLength(2000)]
    public string? ContractNotes { get; set; }

    [StringLength(2000)]
    public string? SpecialRequests { get; set; }
}

public class RejectBookingDto
{
    [Required]
    public Guid HotelId { get; set; }

    [Required]
    [StringLength(1000)]
    public string Reason { get; set; } = string.Empty;
}

public class MarkNoShowDto
{
    [Required]
    public Guid HotelId { get; set; }

    public bool WaivePenalty { get; set; }

    public decimal? PenaltyAmountOverride { get; set; }

    [StringLength(1000)]
    public string? WaivePenaltyReason { get; set; }
}

public class NoShowResultDto
{
    public Guid BookingId { get; set; }
    public BookingStatus BookingStatus { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
    public decimal PenaltyAmountApplied { get; set; }
    public decimal RefundAmountInitiated { get; set; }
    public List<string> ReleasedRoomNumbers { get; set; } = new();
    public string? AuditMessage { get; set; }
}
