using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using BookingHotel.Bookings;

namespace BookingHotel.Manager.Guests;

public class ManagerGuestDto
{
    public Guid GuestId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string IdentityNumberMasked { get; set; } = string.Empty;
    public int TotalStaysAtThisHotel { get; set; }
    public decimal TotalSpentAtThisHotel { get; set; }
    public DateTime? LastStayDate { get; set; }
    public bool HasInternalNotes { get; set; }
}

public class GuestStayHistoryDto
{
    public Guid BookingId { get; set; }
    public string BookingCode { get; set; } = string.Empty;
    public string RoomTypeName { get; set; } = string.Empty;
    public string? RoomNumber { get; set; }
    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }
    public decimal TotalAmount { get; set; }
    public BookingStatus BookingStatus { get; set; }
}

public class InternalGuestNoteDto
{
    public Guid Id { get; set; }
    public Guid HotelId { get; set; }
    public Guid GuestId { get; set; }
    public string Category { get; set; } = "General";
    public string NoteText { get; set; } = string.Empty;
    public bool IsAlert { get; set; }
    public string? CreatorName { get; set; }
    public DateTime CreationTime { get; set; }
    public string? LastModifierName { get; set; }
    public DateTime? LastModificationTime { get; set; }
}

public class CreateInternalGuestNoteDto
{
    [Required]
    public Guid HotelId { get; set; }

    [Required]
    public Guid GuestId { get; set; }

    [Required]
    [StringLength(64)]
    public string Category { get; set; } = "General";

    [Required]
    [StringLength(2000)]
    public string NoteText { get; set; } = string.Empty;

    public bool IsAlert { get; set; }
}
