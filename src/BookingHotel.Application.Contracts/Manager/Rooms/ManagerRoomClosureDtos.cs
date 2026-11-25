using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using BookingHotel.Rooms;

namespace BookingHotel.Manager.Rooms;

public class CheckAffectedBookingsInputDto
{
    [Required]
    public Guid HotelId { get; set; }

    [Required]
    [MinLength(1)]
    public List<Guid> RoomIds { get; set; } = new();

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }
}

public class AffectedBookingDto
{
    public Guid BookingId { get; set; }
    public string BookingCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }
    public Guid RoomId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public Guid RoomTypeId { get; set; }
    public string RoomTypeName { get; set; } = string.Empty;
    public decimal BasePrice { get; set; }
}

public class CreatePlannedClosureDto
{
    [Required]
    public Guid HotelId { get; set; }

    [Required]
    [MinLength(1)]
    public List<Guid> RoomIds { get; set; } = new();

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    [Required]
    [StringLength(500)]
    public string Reason { get; set; } = string.Empty;
}

public class CreateEmergencyClosureDto
{
    [Required]
    public Guid HotelId { get; set; }

    [Required]
    [MinLength(1)]
    public List<Guid> RoomIds { get; set; } = new();

    [Required]
    [StringLength(500)]
    public string Reason { get; set; } = string.Empty;

    [Required]
    [StringLength(2000)]
    public string IncidentDescription { get; set; } = string.Empty;

    [Required]
    public DateTime StartDate { get; set; }

    public DateTime? EstimatedRecoveryTime { get; set; }
}

public class RoomClosureDto
{
    public Guid Id { get; set; }
    public Guid HotelId { get; set; }
    public Guid RoomId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public RoomClosureType ClosureType { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? IncidentDescription { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime? EstimatedRecoveryTime { get; set; }
    public RoomClosureStatus Status { get; set; }
    public Guid? RecoveryConfirmedBy { get; set; }
    public DateTime? RecoveryConfirmedTime { get; set; }
    public DateTime CreationTime { get; set; }
    public string? CreatorName { get; set; }
    public List<AffectedBookingResolutionDto> Resolutions { get; set; } = new();
}

public class AffectedBookingResolutionDto
{
    public Guid Id { get; set; }
    public Guid RoomClosureId { get; set; }
    public Guid BookingId { get; set; }
    public string BookingCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string RoomNumber { get; set; } = string.Empty;
    public AffectedBookingResolutionType? ResolutionType { get; set; }
    public Guid? AlternativeRoomId { get; set; }
    public string? AlternativeRoomNumber { get; set; }
    public Guid? ResolvedBy { get; set; }
    public DateTime? ResolvedTime { get; set; }
    public string? Notes { get; set; }
    public bool IsCustomerNotified { get; set; }
}

public class ResolveAffectedBookingDto
{
    [Required]
    public Guid HotelId { get; set; }

    [Required]
    public Guid ResolutionId { get; set; }

    [Required]
    public AffectedBookingResolutionType ResolutionType { get; set; }

    public Guid? AlternativeRoomId { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }
}

public class ConfirmRoomRecoveryDto
{
    [Required]
    public Guid HotelId { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }
}

public class AlternativeRoomSuggestionDto
{
    public Guid RoomId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public int Floor { get; set; }
    public Guid RoomTypeId { get; set; }
    public string RoomTypeName { get; set; } = string.Empty;
    public decimal BasePrice { get; set; }
    public bool IsUpgrade { get; set; }
}
