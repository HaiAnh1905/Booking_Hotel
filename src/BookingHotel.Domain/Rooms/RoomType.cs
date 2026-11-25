using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace BookingHotel.Rooms;

public class RoomType : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    public virtual Guid? TenantId { get; set; }

    public virtual Guid HotelId { get; set; }

    public virtual string Name { get; set; } = string.Empty;

    public virtual string Description { get; set; } = string.Empty;

    public virtual int StandardOccupancy { get; set; } = 2;

    public virtual int MaxOccupancy { get; set; } = 3;

    public virtual decimal BasePrice { get; set; }

    public virtual double SizeSquareMeters { get; set; }

    public virtual string AmenitiesJson { get; set; } = "[]";

    protected RoomType()
    {
    }

    public RoomType(
        Guid id,
        Guid hotelId,
        string name,
        decimal basePrice,
        int standardOccupancy = 2,
        int maxOccupancy = 3,
        double sizeSquareMeters = 30.0,
        string description = "",
        string amenitiesJson = "[]",
        Guid? tenantId = null)
        : base(id)
    {
        HotelId = hotelId;
        Name = name;
        BasePrice = basePrice;
        StandardOccupancy = standardOccupancy;
        MaxOccupancy = maxOccupancy;
        SizeSquareMeters = sizeSquareMeters;
        Description = description;
        AmenitiesJson = amenitiesJson;
        TenantId = tenantId;
    }
}
