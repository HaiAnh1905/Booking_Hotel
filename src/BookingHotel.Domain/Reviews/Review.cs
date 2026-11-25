using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace BookingHotel.Reviews;

public class Review : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    public virtual Guid? TenantId { get; set; }

    public virtual Guid HotelId { get; set; }

    public virtual Guid GuestId { get; set; }

    public virtual Guid BookingId { get; set; }

    public virtual int CleanlinessScore { get; set; } = 5;

    public virtual int StaffScore { get; set; } = 5;

    public virtual int AmenitiesScore { get; set; } = 5;

    public virtual int LocationScore { get; set; } = 5;

    public virtual double OverallScore { get; set; } = 5.0;

    public virtual string Comment { get; set; } = string.Empty;

    public virtual string? OfficialReply { get; set; }

    public virtual Guid? RepliedById { get; set; }

    public virtual DateTime? RepliedAt { get; set; }

    protected Review()
    {
    }

    public Review(
        Guid id,
        Guid hotelId,
        Guid guestId,
        Guid bookingId,
        int cleanlinessScore,
        int staffScore,
        int amenitiesScore,
        int locationScore,
        string comment,
        Guid? tenantId = null)
        : base(id)
    {
        HotelId = hotelId;
        GuestId = guestId;
        BookingId = bookingId;
        CleanlinessScore = cleanlinessScore;
        StaffScore = staffScore;
        AmenitiesScore = amenitiesScore;
        LocationScore = locationScore;
        OverallScore = Math.Round((cleanlinessScore + staffScore + amenitiesScore + locationScore) / 4.0, 1);
        Comment = comment;
        TenantId = tenantId;
    }
}
