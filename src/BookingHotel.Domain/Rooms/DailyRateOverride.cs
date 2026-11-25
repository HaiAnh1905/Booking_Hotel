using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace BookingHotel.Rooms;

public class DailyRateOverride : FullAuditedEntity<Guid>, IMultiTenant
{
    public virtual Guid? TenantId { get; set; }

    public virtual Guid HotelId { get; set; }

    public virtual Guid RoomTypeId { get; set; }

    public virtual DateTime Date { get; set; }

    public virtual decimal OverridePrice { get; set; }

    public virtual bool IsClosedToArrival { get; set; }

    public virtual bool IsClosedToDeparture { get; set; }

    protected DailyRateOverride()
    {
    }

    public DailyRateOverride(
        Guid id,
        Guid hotelId,
        Guid roomTypeId,
        DateTime date,
        decimal overridePrice,
        bool isClosedToArrival = false,
        bool isClosedToDeparture = false,
        Guid? tenantId = null)
        : base(id)
    {
        HotelId = hotelId;
        RoomTypeId = roomTypeId;
        Date = date.Date;
        OverridePrice = overridePrice;
        IsClosedToArrival = isClosedToArrival;
        IsClosedToDeparture = isClosedToDeparture;
        TenantId = tenantId;
    }
}
