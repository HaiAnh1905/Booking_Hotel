using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace BookingHotel.Rooms;

public class HotelRatePlan : FullAuditedEntity<Guid>, IMultiTenant
{
    public virtual Guid? TenantId { get; set; }

    public virtual Guid HotelId { get; set; }

    public virtual Guid RoomTypeId { get; set; }

    public virtual Guid RatePlanId { get; set; }

    public virtual bool IsActive { get; set; } = true;

    public virtual decimal? CustomBasePrice { get; set; }

    public virtual decimal PriceMultiplier { get; set; } = 1.0m;

    protected HotelRatePlan()
    {
    }

    public HotelRatePlan(
        Guid id,
        Guid hotelId,
        Guid roomTypeId,
        Guid ratePlanId,
        bool isActive = true,
        decimal? customBasePrice = null,
        decimal priceMultiplier = 1.0m,
        Guid? tenantId = null)
        : base(id)
    {
        HotelId = hotelId;
        RoomTypeId = roomTypeId;
        RatePlanId = ratePlanId;
        IsActive = isActive;
        CustomBasePrice = customBasePrice;
        PriceMultiplier = priceMultiplier;
        TenantId = tenantId;
    }
}
