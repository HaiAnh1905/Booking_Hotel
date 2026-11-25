using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace BookingHotel.Rooms;

public class PricingAuditLog : CreationAuditedEntity<Guid>, IMultiTenant
{
    public virtual Guid? TenantId { get; set; }

    public virtual Guid HotelId { get; set; }

    public virtual Guid RoomTypeId { get; set; }

    public virtual string RoomTypeName { get; set; } = string.Empty;

    public virtual Guid RatePlanId { get; set; }

    public virtual string RatePlanCode { get; set; } = string.Empty;

    public virtual string RatePlanName { get; set; } = string.Empty;

    public virtual PricingAuditActionType ActionType { get; set; }

    public virtual Guid ActorId { get; set; }

    public virtual string ActorName { get; set; } = string.Empty;

    public virtual DateTime ActionTime { get; set; }

    public virtual string? BeforeValueJson { get; set; }

    public virtual string? AfterValueJson { get; set; }

    public virtual DateTime? AffectedStartDate { get; set; }

    public virtual DateTime? AffectedEndDate { get; set; }

    public virtual string? SummaryNote { get; set; }

    protected PricingAuditLog()
    {
    }

    public PricingAuditLog(
        Guid id,
        Guid hotelId,
        Guid roomTypeId,
        string roomTypeName,
        Guid ratePlanId,
        string ratePlanCode,
        string ratePlanName,
        PricingAuditActionType actionType,
        Guid actorId,
        string actorName,
        DateTime actionTime,
        string? beforeValueJson = null,
        string? afterValueJson = null,
        DateTime? affectedStartDate = null,
        DateTime? affectedEndDate = null,
        string? summaryNote = null,
        Guid? tenantId = null)
        : base(id)
    {
        HotelId = hotelId;
        RoomTypeId = roomTypeId;
        RoomTypeName = Truncate(roomTypeName, 128);
        RatePlanId = ratePlanId;
        RatePlanCode = Truncate(ratePlanCode, 32);
        RatePlanName = Truncate(ratePlanName, 128);
        ActionType = actionType;
        ActorId = actorId;
        ActorName = Truncate(string.IsNullOrWhiteSpace(actorName) ? "System" : actorName, 256);
        ActionTime = actionTime;
        BeforeValueJson = TruncateNullable(beforeValueJson, 4000);
        AfterValueJson = TruncateNullable(afterValueJson, 4000);
        AffectedStartDate = affectedStartDate?.Date;
        AffectedEndDate = affectedEndDate?.Date;
        SummaryNote = TruncateNullable(summaryNote, 500);
        TenantId = tenantId;
    }

    private static string Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value.Length <= maxLength ? value : value[..maxLength];
    }

    private static string? TruncateNullable(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
