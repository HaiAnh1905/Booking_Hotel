using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace BookingHotel.Complaints;

public class Complaint : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    public virtual Guid? TenantId { get; set; }

    public virtual Guid HotelId { get; set; }

    public virtual Guid? GuestId { get; set; }

    public virtual Guid? BookingId { get; set; }

    public virtual string Title { get; set; } = string.Empty;

    public virtual string Description { get; set; } = string.Empty;

    public virtual string Priority { get; set; } = "Medium";

    public virtual ComplaintStatus Status { get; set; } = ComplaintStatus.Open;

    public virtual DateTime SlaDeadline { get; set; }

    public virtual DateTime? EscalatedAt { get; set; }

    public virtual string? ResolutionNotes { get; set; }

    public virtual Guid? ResolvedById { get; set; }

    public virtual DateTime? ResolvedAt { get; set; }

    protected Complaint()
    {
    }

    public Complaint(
        Guid id,
        Guid hotelId,
        string title,
        string description,
        DateTime slaDeadline,
        string priority = "Medium",
        ComplaintStatus status = ComplaintStatus.Open,
        Guid? guestId = null,
        Guid? bookingId = null,
        Guid? tenantId = null)
        : base(id)
    {
        HotelId = hotelId;
        Title = title;
        Description = description;
        SlaDeadline = slaDeadline;
        Priority = priority;
        Status = status;
        GuestId = guestId;
        BookingId = bookingId;
        TenantId = tenantId;
    }
}
