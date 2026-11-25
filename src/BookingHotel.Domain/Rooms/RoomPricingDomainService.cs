using System;
using System.Collections.Generic;
using System.Linq;
using Volo.Abp;
using Volo.Abp.Domain.Services;

namespace BookingHotel.Rooms;

public class OverrideResolutionPlan
{
    public List<DateTime> ConflictingDates { get; init; } = new();
    public List<DateTime> AppliedDates { get; init; } = new();
    public List<DateTime> ReplacedDates { get; init; } = new();
    public List<DateTime> SkippedDates { get; init; } = new();
    public List<RoomPriceOverride> ToInsert { get; init; } = new();
    public List<RoomPriceOverride> ToUpdate { get; init; } = new();
    public List<RoomPriceOverride> ToDelete { get; init; } = new();
    public bool WasCancelledByConflict { get; init; }
}

public class StayRestrictionCheckResult
{
    public bool IsAllowed => !IsBlockedByCta && !IsBlockedByCtd;
    public bool IsBlockedByCta { get; init; }
    public bool IsBlockedByCtd { get; init; }
}

public class RoomPricingDomainService : DomainService
{
    private const decimal RoundingUnitVnd = 1000m;

    public decimal RoundToNearestThousandVnd(decimal rawAmount)
    {
        if (rawAmount <= 0m)
        {
            throw new BusinessException("BookingHotel:InvalidEffectivePrice");
        }

        var rounded = Math.Round(rawAmount / RoundingUnitVnd, 0, MidpointRounding.AwayFromZero) * RoundingUnitVnd;
        return Math.Max(RoundingUnitVnd, rounded);
    }

    public decimal ResolveBranchBasePrice(
        decimal roomTypeBasePrice,
        decimal defaultPriceAdjustmentPercent,
        decimal? customBasePrice)
    {
        if (customBasePrice.HasValue)
        {
            if (customBasePrice.Value <= 0m)
            {
                throw new BusinessException("BookingHotel:InvalidEffectivePrice");
            }

            return customBasePrice.Value;
        }

        var raw = roomTypeBasePrice * (1m + defaultPriceAdjustmentPercent / 100m);
        if (raw <= 0m)
        {
            return roomTypeBasePrice > 0m ? RoundToNearestThousandVnd(roomTypeBasePrice) : RoundingUnitVnd;
        }

        return RoundToNearestThousandVnd(raw);
    }

    public decimal? CalculateEffectivePrice(
        decimal basePrice,
        PriceAdjustmentType adjustmentType,
        decimal? adjustmentValue)
    {
        if (basePrice <= 0m)
        {
            throw new BusinessException("BookingHotel:InvalidEffectivePrice");
        }

        switch (adjustmentType)
        {
            case PriceAdjustmentType.FixedPrice:
                if (!adjustmentValue.HasValue || adjustmentValue.Value <= 0m)
                {
                    throw new BusinessException("BookingHotel:InvalidEffectivePrice");
                }

                return adjustmentValue.Value;

            case PriceAdjustmentType.Percentage:
                if (!adjustmentValue.HasValue)
                {
                    throw new BusinessException("BookingHotel:InvalidEffectivePrice");
                }

                var raw = basePrice * (1m + adjustmentValue.Value / 100m);
                if (raw <= 0m)
                {
                    throw new BusinessException("BookingHotel:InvalidEffectivePrice");
                }

                return RoundToNearestThousandVnd(raw);

            case PriceAdjustmentType.RestrictionsOnly:
                return null;

            default:
                throw new BusinessException("BookingHotel:InvalidEffectivePrice");
        }
    }

    public decimal ResolveDailyEffectivePrice(decimal basePrice, RoomPriceOverride? dailyOverride)
    {
        if (dailyOverride != null &&
            dailyOverride.AdjustmentType != PriceAdjustmentType.RestrictionsOnly &&
            dailyOverride.CalculatedEffectivePrice.HasValue &&
            dailyOverride.CalculatedEffectivePrice.Value > 0m)
        {
            return dailyOverride.CalculatedEffectivePrice.Value;
        }

        return basePrice;
    }

    public List<DateTime> ExpandMatchingDates(
        DateTime startDate,
        DateTime endDate,
        IReadOnlyCollection<DayOfWeek>? daysOfWeek = null)
    {
        var start = startDate.Date;
        var end = endDate.Date;

        if (start > end)
        {
            throw new BusinessException("BookingHotel:InvalidDateRange");
        }

        HashSet<DayOfWeek>? allowedDays = null;
        if (daysOfWeek != null && daysOfWeek.Count > 0)
        {
            allowedDays = new HashSet<DayOfWeek>(daysOfWeek);
        }

        var result = new List<DateTime>();
        for (var d = start; d <= end; d = d.AddDays(1))
        {
            if (allowedDays == null || allowedDays.Contains(d.DayOfWeek))
            {
                result.Add(d);
            }
        }

        if (result.Count == 0)
        {
            throw new BusinessException("BookingHotel:NoMatchingDaysInRange");
        }

        return result;
    }

    public List<(DateTime Start, DateTime End)> GroupIntoContiguousRanges(IEnumerable<DateTime> dates)
    {
        var sorted = dates.Select(d => d.Date).Distinct().OrderBy(d => d).ToList();
        var ranges = new List<(DateTime Start, DateTime End)>();
        if (sorted.Count == 0)
        {
            return ranges;
        }

        var rangeStart = sorted[0];
        var prev = sorted[0];

        for (var i = 1; i < sorted.Count; i++)
        {
            var curr = sorted[i];
            if (curr == prev.AddDays(1))
            {
                prev = curr;
            }
            else
            {
                ranges.Add((rangeStart, prev));
                rangeStart = curr;
                prev = curr;
            }
        }

        ranges.Add((rangeStart, prev));
        return ranges;
    }

    public OverrideResolutionPlan BuildOverrideResolutionPlan(
        Guid hotelId,
        Guid roomTypeId,
        Guid ratePlanId,
        IReadOnlyList<RoomPriceOverride> existingOverrides,
        IReadOnlyList<DateTime> targetDates,
        PriceAdjustmentType adjustmentType,
        decimal? adjustmentValue,
        decimal? calculatedEffectivePrice,
        bool isClosedToArrival,
        bool isClosedToDeparture,
        PriceConflictResolution conflictResolution,
        DateTime today,
        string creatorName,
        Guid? tenantId,
        Func<Guid> idFactory)
    {
        var normalizedToday = today.Date;
        var orderedTargets = targetDates.Select(d => d.Date).Distinct().OrderBy(d => d).ToList();

        if (orderedTargets.Count == 0)
        {
            throw new BusinessException("BookingHotel:NoMatchingDaysInRange");
        }

        if (orderedTargets[0] < normalizedToday)
        {
            throw new BusinessException("BookingHotel:CannotModifyPastPricing");
        }

        var existingByDate = new Dictionary<DateTime, RoomPriceOverride>();
        foreach (var ov in existingOverrides)
        {
            for (var d = ov.StartDate.Date; d <= ov.EndDate.Date; d = d.AddDays(1))
            {
                existingByDate[d] = ov;
            }
        }

        var conflictingDates = orderedTargets.Where(d => existingByDate.ContainsKey(d)).ToList();

        if (conflictingDates.Count > 0 && conflictResolution == PriceConflictResolution.Cancel)
        {
            return new OverrideResolutionPlan
            {
                ConflictingDates = conflictingDates,
                WasCancelledByConflict = true
            };
        }

        var toInsert = new List<RoomPriceOverride>();
        var toUpdate = new List<RoomPriceOverride>();
        var toDelete = new List<RoomPriceOverride>();

        if (conflictResolution == PriceConflictResolution.SkipExisting)
        {
            var conflictSet = new HashSet<DateTime>(conflictingDates);
            var datesToApply = orderedTargets.Where(d => !conflictSet.Contains(d)).ToList();

            foreach (var (s, e) in GroupIntoContiguousRanges(datesToApply))
            {
                toInsert.Add(new RoomPriceOverride(
                    idFactory(),
                    hotelId,
                    roomTypeId,
                    ratePlanId,
                    s,
                    e,
                    adjustmentType,
                    adjustmentValue,
                    calculatedEffectivePrice,
                    isClosedToArrival,
                    isClosedToDeparture,
                    creatorName,
                    tenantId));
            }

            return new OverrideResolutionPlan
            {
                ConflictingDates = conflictingDates,
                AppliedDates = datesToApply,
                SkippedDates = conflictingDates,
                ToInsert = toInsert
            };
        }

        // ReplaceExisting (or no conflicts)
        var targetSet = new HashSet<DateTime>(orderedTargets);

        // 1. Split / trim / remove intersecting existing overrides while keeping < today immutable
        foreach (var old in existingOverrides)
        {
            if (!orderedTargets.Any(d => old.ContainsDate(d)))
            {
                continue;
            }

            var untouchedDates = new List<DateTime>();
            for (var d = old.StartDate.Date; d <= old.EndDate.Date; d = d.AddDays(1))
            {
                if (!targetSet.Contains(d))
                {
                    untouchedDates.Add(d);
                }
            }

            if (untouchedDates.Count == 0)
            {
                toDelete.Add(old);
            }
            else
            {
                var remainingRanges = GroupIntoContiguousRanges(untouchedDates);
                old.UpdateDateRange(remainingRanges[0].Start, remainingRanges[0].End);
                toUpdate.Add(old);

                for (var i = 1; i < remainingRanges.Count; i++)
                {
                    var (segStart, segEnd) = remainingRanges[i];
                    toInsert.Add(new RoomPriceOverride(
                        idFactory(),
                        old.HotelId,
                        old.RoomTypeId,
                        old.RatePlanId,
                        segStart,
                        segEnd,
                        old.AdjustmentType,
                        old.AdjustmentValue,
                        old.CalculatedEffectivePrice,
                        old.IsClosedToArrival,
                        old.IsClosedToDeparture,
                        old.CreatorName,
                        old.TenantId));
                }
            }
        }

        // 2. Build resolved entries per target date
        // Analysis safeguard (I1): When RestrictionsOnly replaces a day that already had a price override,
        // preserve that day's price snapshot and update only CTA/CTD.
        var dailySpecs = new List<(DateTime Date, PriceAdjustmentType Type, decimal? AdjVal, decimal? CalcPrice, bool Cta, bool Ctd)>();
        foreach (var d in orderedTargets)
        {
            if (adjustmentType == PriceAdjustmentType.RestrictionsOnly &&
                existingByDate.TryGetValue(d, out var existingOnDay) &&
                existingOnDay.AdjustmentType != PriceAdjustmentType.RestrictionsOnly &&
                existingOnDay.CalculatedEffectivePrice.HasValue)
            {
                dailySpecs.Add((
                    d,
                    existingOnDay.AdjustmentType,
                    existingOnDay.AdjustmentValue,
                    existingOnDay.CalculatedEffectivePrice,
                    isClosedToArrival,
                    isClosedToDeparture));
            }
            else
            {
                dailySpecs.Add((
                    d,
                    adjustmentType,
                    adjustmentValue,
                    calculatedEffectivePrice,
                    isClosedToArrival,
                    isClosedToDeparture));
            }
        }

        // 3. Coalesce consecutive target days with identical pricing & restriction signature
        var idx = 0;
        while (idx < dailySpecs.Count)
        {
            var first = dailySpecs[idx];
            var lastDate = first.Date;
            var j = idx + 1;
            while (j < dailySpecs.Count &&
                   dailySpecs[j].Date == lastDate.AddDays(1) &&
                   dailySpecs[j].Type == first.Type &&
                   dailySpecs[j].AdjVal == first.AdjVal &&
                   dailySpecs[j].CalcPrice == first.CalcPrice &&
                   dailySpecs[j].Cta == first.Cta &&
                   dailySpecs[j].Ctd == first.Ctd)
            {
                lastDate = dailySpecs[j].Date;
                j++;
            }

            toInsert.Add(new RoomPriceOverride(
                idFactory(),
                hotelId,
                roomTypeId,
                ratePlanId,
                first.Date,
                lastDate,
                first.Type,
                first.AdjVal,
                first.CalcPrice,
                first.Cta,
                first.Ctd,
                creatorName,
                tenantId));

            idx = j;
        }

        return new OverrideResolutionPlan
        {
            ConflictingDates = conflictingDates,
            AppliedDates = orderedTargets,
            ReplacedDates = conflictingDates,
            ToInsert = toInsert,
            ToUpdate = toUpdate,
            ToDelete = toDelete
        };
    }

    public (bool ShouldDeleteRecord, DateTime AffectedStart, DateTime AffectedEnd) PrepareDeleteWithPastProtection(
        RoomPriceOverride targetOverride,
        DateTime today)
    {
        var normalizedToday = today.Date;
        if (targetOverride.EndDate.Date < normalizedToday)
        {
            throw new BusinessException("BookingHotel:CannotModifyPastPricing");
        }

        if (targetOverride.StartDate.Date < normalizedToday)
        {
            var affectedEnd = targetOverride.EndDate.Date;
            targetOverride.UpdateDateRange(targetOverride.StartDate.Date, normalizedToday.AddDays(-1));
            return (ShouldDeleteRecord: false, AffectedStart: normalizedToday, AffectedEnd: affectedEnd);
        }

        return (ShouldDeleteRecord: true, AffectedStart: targetOverride.StartDate.Date, AffectedEnd: targetOverride.EndDate.Date);
    }

    public StayRestrictionCheckResult ValidateStayRestrictions(
        DateTime checkInDate,
        DateTime checkOutDate,
        IReadOnlyList<RoomPriceOverride> overrides)
    {
        var checkIn = checkInDate.Date;
        var checkOut = checkOutDate.Date;

        var blockedByCta = overrides.Any(o => o.ContainsDate(checkIn) && o.IsClosedToArrival);
        var blockedByCtd = overrides.Any(o => o.ContainsDate(checkOut) && o.IsClosedToDeparture);

        return new StayRestrictionCheckResult
        {
            IsBlockedByCta = blockedByCta,
            IsBlockedByCtd = blockedByCtd
        };
    }
}
