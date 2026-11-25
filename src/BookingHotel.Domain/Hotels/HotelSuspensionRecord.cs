using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace BookingHotel.Hotels;

public class HotelSuspensionRecord : CreationAuditedEntity<Guid>, IMultiTenant
{
    public virtual Guid? TenantId { get; set; }

    public virtual Guid HotelId { get; set; }

    public virtual SuspensionType SuspensionType { get; set; }

    public virtual DateTime StartDate { get; set; }

    public virtual DateTime? EndDate { get; set; }

    public virtual string Reason { get; set; } = string.Empty;

    public virtual int AffectedBookingsCount { get; set; }

    protected HotelSuspensionRecord()
    {
    }

    public HotelSuspensionRecord(
        Guid id,
        Guid hotelId,
        SuspensionType suspensionType,
        DateTime startDate,
        DateTime? endDate = null,
        string reason = "",
        int affectedBookingsCount = 0,
        Guid? tenantId = null)
        : base(id)
    {
        HotelId = hotelId;
        SuspensionType = suspensionType;
        StartDate = startDate;
        EndDate = endDate;
        Reason = reason;
        AffectedBookingsCount = affectedBookingsCount;
        TenantId = tenantId;
    }
}
