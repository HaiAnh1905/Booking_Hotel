using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace BookingHotel.Rooms;

public class RatePlan : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    public virtual Guid? TenantId { get; set; }

    public virtual string Name { get; set; } = string.Empty;

    public virtual string Code { get; set; } = string.Empty;

    public virtual string Description { get; set; } = string.Empty;

    public virtual bool IsRefundable { get; set; } = true;

    public virtual bool IncludesBreakfast { get; set; }

    public virtual int CancellationCutoffHours { get; set; } = 24;

    public virtual decimal DefaultPriceAdjustmentPercent { get; set; }

    protected RatePlan()
    {
    }

    public RatePlan(
        Guid id,
        string name,
        string code,
        string description = "",
        bool isRefundable = true,
        bool includesBreakfast = false,
        int cancellationCutoffHours = 24,
        decimal defaultPriceAdjustmentPercent = 0m,
        Guid? tenantId = null)
        : base(id)
    {
        Name = name;
        Code = code;
        Description = description;
        IsRefundable = isRefundable;
        IncludesBreakfast = includesBreakfast;
        CancellationCutoffHours = cancellationCutoffHours;
        DefaultPriceAdjustmentPercent = defaultPriceAdjustmentPercent;
        TenantId = tenantId;
    }
}
