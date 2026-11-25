using System;
using System.ComponentModel.DataAnnotations;
using BookingHotel.Approvals;

namespace BookingHotel.Approvals.Dtos;

public class CreateApprovalTicketDto
{
    [Required]
    public Guid HotelId { get; set; }

    [Required]
    public ApprovalTicketType TicketType { get; set; }

    public Guid? BookingId { get; set; }

    public Guid? FolioId { get; set; }

    [StringLength(32)]
    public string? RoomNumber { get; set; }

    [Range(0, 999999999)]
    public decimal? Amount { get; set; }

    [Range(0.01, 100.00)]
    public decimal? DiscountPercent { get; set; }

    [Required]
    [StringLength(1000, MinimumLength = 5)]
    public string Reason { get; set; } = string.Empty;
}

