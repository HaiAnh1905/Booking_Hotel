using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace BookingHotel.Rooms;

public class AffectedBookingResolution : FullAuditedEntity<Guid>, IMultiTenant
{
    public virtual Guid? TenantId { get; set; }

    public virtual Guid RoomClosureId { get; set; }

    public virtual Guid BookingId { get; set; }

    public virtual AffectedBookingResolutionType? ResolutionType { get; set; }

    public virtual Guid? AlternativeRoomId { get; set; }

    public virtual Guid? ResolvedBy { get; set; }

    public virtual DateTime? ResolvedTime { get; set; }

    public virtual string? Notes { get; set; }

    public virtual bool IsCustomerNotified { get; set; }

    protected AffectedBookingResolution()
    {
    }

    public AffectedBookingResolution(
        Guid id,
        Guid roomClosureId,
        Guid bookingId,
        Guid? tenantId = null)
        : base(id)
    {
        RoomClosureId = roomClosureId;
        BookingId = bookingId;
        TenantId = tenantId;
        IsCustomerNotified = false;
    }
}
