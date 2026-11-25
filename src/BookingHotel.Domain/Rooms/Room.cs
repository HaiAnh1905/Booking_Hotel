using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace BookingHotel.Rooms;

public class Room : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    public virtual Guid? TenantId { get; set; }

    public virtual Guid HotelId { get; set; }

    public virtual Guid RoomTypeId { get; set; }

    public virtual string RoomNumber { get; set; } = string.Empty;

    public virtual int Floor { get; set; } = 1;

    public virtual RoomOperationalStatus OperationalStatus { get; set; } = RoomOperationalStatus.Active;

    public virtual RoomOccupancyStatus OccupancyStatus { get; set; } = RoomOccupancyStatus.Vacant;

    public virtual HousekeepingStatus HousekeepingStatus { get; set; } = HousekeepingStatus.Clean;

    public virtual bool IsActive { get; set; } = true;

    public virtual string? Building { get; set; }

    public virtual string? Description { get; set; }

    public virtual string? BedType { get; set; }

    public virtual int Capacity { get; set; } = 2;

    public virtual string? RoomAmenities { get; set; }

    public virtual string? MaintenanceReason { get; set; }

    public virtual DateTime? MaintenanceEndDate { get; set; }

    public virtual string? OutOfServiceReason { get; set; }

    public virtual DateTime? OutOfServiceEndDate { get; set; }

    protected Room()
    {
    }

    public Room(
        Guid id,
        Guid hotelId,
        Guid roomTypeId,
        string roomNumber,
        int floor,
        RoomOperationalStatus operationalStatus = RoomOperationalStatus.Active,
        HousekeepingStatus housekeepingStatus = HousekeepingStatus.Clean,
        Guid? tenantId = null,
        RoomOccupancyStatus occupancyStatus = RoomOccupancyStatus.Vacant,
        bool isActive = true,
        string? building = null,
        string? description = null)
        : base(id)
    {
        HotelId = hotelId;
        RoomTypeId = roomTypeId;
        RoomNumber = roomNumber;
        Floor = floor;
        OperationalStatus = operationalStatus;
        HousekeepingStatus = housekeepingStatus;
        TenantId = tenantId;
        OccupancyStatus = occupancyStatus;
        IsActive = isActive;
        Building = building;
        Description = description;
    }
}
