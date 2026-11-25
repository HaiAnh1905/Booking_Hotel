using System.ComponentModel.DataAnnotations;

namespace BookingHotel.Approvals.Dtos;

public class ReviewApprovalTicketDto
{
    [Required]
    [StringLength(1000, MinimumLength = 5)]
    public string RejectionReason { get; set; } = string.Empty;
}

