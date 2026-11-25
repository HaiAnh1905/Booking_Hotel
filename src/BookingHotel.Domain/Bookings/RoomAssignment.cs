using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace BookingHotel.Bookings;

public class RoomAssignment : FullAuditedEntity<Guid>, IMultiTenant
{
    public virtual Guid? TenantId { get; set; }

    public virtual Guid BookingId { get; set; }

    public virtual Guid RoomId { get; set; }

    public virtual Guid RoomTypeId { get; set; }

    public virtual DateTime FromDate { get; set; }

    public virtual DateTime ToDate { get; set; }

    public virtual bool IsActive { get; set; } = true;

    protected RoomAssignment()
    {
    }

    public RoomAssignment(
        Guid id,
        Guid bookingId,
        Guid roomId,
        Guid roomTypeId,
        DateTime fromDate,
        DateTime toDate,
        bool isActive = true,
        Guid? tenantId = null)
        : base(id)
    {
        BookingId = bookingId;
        RoomId = roomId;
        RoomTypeId = roomTypeId;
        FromDate = fromDate;
        ToDate = toDate;
        IsActive = isActive;
        TenantId = tenantId;
    }
}
