using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace BookingHotel.Identity;

public class UserHotelAssignment : FullAuditedEntity<Guid>, IMultiTenant
{
    public virtual Guid? TenantId { get; set; }

    public virtual Guid UserId { get; set; }

    public virtual Guid HotelId { get; set; }

    public virtual DateTime AssignedDate { get; set; }

    public virtual DateTime AssignedAt
    {
        get => AssignedDate;
        set => AssignedDate = value;
    }

    public virtual DateTime? UnassignedAt { get; set; }

    public virtual bool IsPrimary { get; set; }

    public virtual bool IsActive { get; set; } = true;

    protected UserHotelAssignment()
    {
    }

    public UserHotelAssignment(
        Guid id,
        Guid userId,
        Guid hotelId,
        Guid? tenantId = null,
        bool isPrimary = true,
        bool isActive = true)
        : base(id)
    {
        UserId = userId;
        HotelId = hotelId;
        TenantId = tenantId;
        AssignedDate = DateTime.UtcNow;
        IsPrimary = isPrimary;
        IsActive = isActive;
    }

    public void Unassign(DateTime unassignedAt)
    {
        IsActive = false;
        UnassignedAt = unassignedAt;
    }
}
