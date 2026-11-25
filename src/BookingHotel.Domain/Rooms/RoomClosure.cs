using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace BookingHotel.Rooms;

public class RoomClosure : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    public virtual Guid? TenantId { get; set; }

    public virtual Guid HotelId { get; set; }

    public virtual Guid RoomId { get; set; }

    public virtual RoomClosureType ClosureType { get; set; } = RoomClosureType.Planned;

    public virtual string Reason { get; set; } = string.Empty;

    public virtual string? IncidentDescription { get; set; }

    public virtual DateTime StartDate { get; set; }

    public virtual DateTime? EndDate { get; set; }

    public virtual DateTime? EstimatedRecoveryTime { get; set; }

    public virtual RoomClosureStatus Status { get; set; } = RoomClosureStatus.Active;

    public virtual Guid? RecoveryConfirmedBy { get; set; }

    public virtual DateTime? RecoveryConfirmedTime { get; set; }

    protected RoomClosure()
    {
    }

    public RoomClosure(
        Guid id,
        Guid hotelId,
        Guid roomId,
        RoomClosureType closureType,
        string reason,
        DateTime startDate,
        DateTime? endDate = null,
        string? incidentDescription = null,
        DateTime? estimatedRecoveryTime = null,
        Guid? tenantId = null)
        : base(id)
    {
        HotelId = hotelId;
        RoomId = roomId;
        ClosureType = closureType;
        Reason = reason;
        StartDate = startDate;
        EndDate = endDate;
        IncidentDescription = incidentDescription;
        EstimatedRecoveryTime = estimatedRecoveryTime;
        Status = RoomClosureStatus.Active;
        TenantId = tenantId;
    }
}
