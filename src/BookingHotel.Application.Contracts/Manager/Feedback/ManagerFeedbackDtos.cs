using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using BookingHotel.Complaints;

namespace BookingHotel.Manager.Feedback;

public class ReviewDto
{
    public Guid Id { get; set; }
    public Guid HotelId { get; set; }
    public string BookingCode { get; set; } = string.Empty;
    public string GuestFullName { get; set; } = string.Empty;
    public int CleanlinessScore { get; set; }
    public int StaffScore { get; set; }
    public int AmenitiesScore { get; set; }
    public int LocationScore { get; set; }
    public double OverallScore { get; set; }
    public string Comment { get; set; } = string.Empty;
    public DateTime CreationTime { get; set; }
    public string? OfficialReply { get; set; }
    public string? RepliedByName { get; set; }
    public DateTime? RepliedAt { get; set; }
}

public class ReplyReviewDto
{
    [Required]
    public Guid HotelId { get; set; }

    [Required]
    [StringLength(4000)]
    public string ReplyContent { get; set; } = string.Empty;
}

public class ComplaintDto
{
    public Guid Id { get; set; }
    public Guid HotelId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string GuestFullName { get; set; } = string.Empty;
    public string GuestPhone { get; set; } = string.Empty;
    public string? BookingCode { get; set; }
    public string Priority { get; set; } = "Medium";
    public ComplaintStatus Status { get; set; }
    public DateTime CreationTime { get; set; }
    public DateTime SlaDeadline { get; set; }
    public double HoursRemaining { get; set; }
    public bool IsOverdue { get; set; }
    public DateTime? EscalatedAt { get; set; }
    public string? ResolutionNotes { get; set; }
    public string? ResolvedByName { get; set; }
    public DateTime? ResolvedAt { get; set; }
}

public class CreateComplaintDto
{
    [Required]
    public Guid HotelId { get; set; }

    [Required]
    [StringLength(256)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(4000)]
    public string Description { get; set; } = string.Empty;

    public string Priority { get; set; } = "Medium";

    public Guid? GuestId { get; set; }

    public Guid? BookingId { get; set; }
}

public class UpdateComplaintStatusDto
{
    [Required]
    public Guid HotelId { get; set; }

    [Required]
    public ComplaintStatus TargetStatus { get; set; }

    [StringLength(4000)]
    public string? ResolutionNotes { get; set; }
}
