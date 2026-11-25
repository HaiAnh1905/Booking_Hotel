using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace BookingHotel.Rooms;

public class RoomPriceOverride : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    public virtual Guid? TenantId { get; set; }

    public virtual Guid HotelId { get; set; }

    public virtual Guid RoomTypeId { get; set; }

    public virtual Guid RatePlanId { get; set; }

    public virtual DateTime StartDate { get; set; }

    public virtual DateTime EndDate { get; set; }

    public virtual PriceAdjustmentType AdjustmentType { get; set; }

    public virtual decimal? AdjustmentValue { get; set; }

    public virtual decimal? CalculatedEffectivePrice { get; set; }

    public virtual bool IsClosedToArrival { get; set; }

    public virtual bool IsClosedToDeparture { get; set; }

    public virtual string CreatorName { get; set; } = string.Empty;

    protected RoomPriceOverride()
    {
    }

    public RoomPriceOverride(
        Guid id,
        Guid hotelId,
        Guid roomTypeId,
        Guid ratePlanId,
        DateTime startDate,
        DateTime endDate,
        PriceAdjustmentType adjustmentType,
        decimal? adjustmentValue,
        decimal? calculatedEffectivePrice,
        bool isClosedToArrival = false,
        bool isClosedToDeparture = false,
        string creatorName = "",
        Guid? tenantId = null)
        : base(id)
    {
        HotelId = hotelId;
        RoomTypeId = roomTypeId;
        RatePlanId = ratePlanId;
        TenantId = tenantId;
        CreatorName = string.IsNullOrWhiteSpace(creatorName) ? "System" : creatorName.Trim();

        UpdateRangeAndPricing(
            startDate,
            endDate,
            adjustmentType,
            adjustmentValue,
            calculatedEffectivePrice,
            isClosedToArrival,
            isClosedToDeparture);
    }

    public void UpdateRangeAndPricing(
        DateTime startDate,
        DateTime endDate,
        PriceAdjustmentType adjustmentType,
        decimal? adjustmentValue,
        decimal? calculatedEffectivePrice,
        bool isClosedToArrival,
        bool isClosedToDeparture)
    {
        var normalizedStart = startDate.Date;
        var normalizedEnd = endDate.Date;

        if (normalizedStart > normalizedEnd)
        {
            throw new BusinessException("BookingHotel:InvalidDateRange");
        }

        switch (adjustmentType)
        {
            case PriceAdjustmentType.FixedPrice:
                if (!adjustmentValue.HasValue || adjustmentValue.Value <= 0m ||
                    !calculatedEffectivePrice.HasValue || calculatedEffectivePrice.Value <= 0m)
                {
                    throw new BusinessException("BookingHotel:InvalidEffectivePrice");
                }
                break;

            case PriceAdjustmentType.Percentage:
                if (!adjustmentValue.HasValue ||
                    !calculatedEffectivePrice.HasValue || calculatedEffectivePrice.Value <= 0m)
                {
                    throw new BusinessException("BookingHotel:InvalidEffectivePrice");
                }
                break;

            case PriceAdjustmentType.RestrictionsOnly:
                adjustmentValue = null;
                calculatedEffectivePrice = null;
                break;

            default:
                throw new BusinessException("BookingHotel:InvalidEffectivePrice");
        }

        StartDate = normalizedStart;
        EndDate = normalizedEnd;
        AdjustmentType = adjustmentType;
        AdjustmentValue = adjustmentValue;
        CalculatedEffectivePrice = calculatedEffectivePrice;
        IsClosedToArrival = isClosedToArrival;
        IsClosedToDeparture = isClosedToDeparture;
    }

    public void UpdateDateRange(DateTime startDate, DateTime endDate)
    {
        var normalizedStart = startDate.Date;
        var normalizedEnd = endDate.Date;
        if (normalizedStart > normalizedEnd)
        {
            throw new BusinessException("BookingHotel:InvalidDateRange");
        }

        StartDate = normalizedStart;
        EndDate = normalizedEnd;
    }

    public bool ContainsDate(DateTime date)
    {
        var d = date.Date;
        return d >= StartDate.Date && d <= EndDate.Date;
    }

    public bool Overlaps(DateTime startDate, DateTime endDate)
    {
        var s = startDate.Date;
        var e = endDate.Date;
        return StartDate.Date <= e && EndDate.Date >= s;
    }
}
