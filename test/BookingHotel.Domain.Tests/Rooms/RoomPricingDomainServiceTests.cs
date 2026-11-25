using System;
using System.Collections.Generic;
using System.Linq;
using BookingHotel.Rooms;
using Shouldly;
using Volo.Abp;
using Xunit;

namespace BookingHotel.Rooms;

public class RoomPricingDomainServiceTests
{
    private readonly RoomPricingDomainService _service = new();
    private readonly Guid _hotelId = Guid.NewGuid();
    private readonly Guid _roomTypeId = Guid.NewGuid();
    private readonly Guid _ratePlanId = Guid.NewGuid();
    private readonly DateTime _today = new(2026, 11, 25);

    [Fact]
    public void Should_Calculate_Percentage_And_Round_To_Nearest_1000_VND()
    {
        // 400,000 * 1.20 = 480,000
        _service.CalculateEffectivePrice(400_000m, PriceAdjustmentType.Percentage, 20m)
            .ShouldBe(480_000m);

        // 405,000 * 1.15 = 465,750 -> rounds AwayFromZero to 466,000
        _service.CalculateEffectivePrice(405_000m, PriceAdjustmentType.Percentage, 15m)
            .ShouldBe(466_000m);

        // 405,000 * 1.01 = 409,050 -> rounds to 409,000
        _service.CalculateEffectivePrice(405_000m, PriceAdjustmentType.Percentage, 1m)
            .ShouldBe(409_000m);

        // Minimum positive floor is 1,000 VND (e.g., 1,000 * 0.4 = 400 -> clamped to 1,000)
        _service.CalculateEffectivePrice(1_000m, PriceAdjustmentType.Percentage, -60m)
            .ShouldBe(1_000m);
    }

    [Theory]
    [InlineData(-100)]
    [InlineData(-120)]
    public void Should_Reject_Extreme_Negative_Percentage_Producing_Zero_Or_Less(decimal pct)
    {
        var ex = Should.Throw<BusinessException>(() =>
            _service.CalculateEffectivePrice(400_000m, PriceAdjustmentType.Percentage, pct));

        ex.Code.ShouldBe("BookingHotel:InvalidEffectivePrice");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-50000)]
    public void Should_Reject_Non_Positive_Fixed_Price(decimal fixedPrice)
    {
        var ex = Should.Throw<BusinessException>(() =>
            _service.CalculateEffectivePrice(400_000m, PriceAdjustmentType.FixedPrice, fixedPrice));

        ex.Code.ShouldBe("BookingHotel:InvalidEffectivePrice");
    }

    [Fact]
    public void Should_Return_Null_EffectivePrice_For_RestrictionsOnly_And_Fallback_To_BasePrice()
    {
        var calc = _service.CalculateEffectivePrice(400_000m, PriceAdjustmentType.RestrictionsOnly, null);
        calc.ShouldBeNull();

        var ov = new RoomPriceOverride(
            Guid.NewGuid(),
            _hotelId,
            _roomTypeId,
            _ratePlanId,
            _today.AddDays(1),
            _today.AddDays(2),
            PriceAdjustmentType.RestrictionsOnly,
            null,
            null,
            isClosedToArrival: true,
            isClosedToDeparture: false,
            creatorName: "Manager");

        _service.ResolveDailyEffectivePrice(400_000m, ov).ShouldBe(400_000m);
    }

    [Fact]
    public void Should_Filter_By_DaysOfWeek_And_Group_Contiguous_Ranges()
    {
        // 2026-12-01 (Tue) .. 2026-12-14 (Mon) -> Sat/Sun are Dec 5-6 and Dec 12-13
        var dates = _service.ExpandMatchingDates(
            new DateTime(2026, 12, 1),
            new DateTime(2026, 12, 14),
            new[] { DayOfWeek.Saturday, DayOfWeek.Sunday });

        dates.Count.ShouldBe(4);
        var ranges = _service.GroupIntoContiguousRanges(dates);
        ranges.Count.ShouldBe(2);
        ranges[0].ShouldBe((new DateTime(2026, 12, 5), new DateTime(2026, 12, 6)));
        ranges[1].ShouldBe((new DateTime(2026, 12, 12), new DateTime(2026, 12, 13)));
    }

    [Fact]
    public void Should_Reject_Invalid_DateRange_And_Past_Dates()
    {
        Should.Throw<BusinessException>(() =>
            _service.ExpandMatchingDates(_today.AddDays(5), _today.AddDays(2)))
            .Code.ShouldBe("BookingHotel:InvalidDateRange");

        Should.Throw<BusinessException>(() =>
            _service.BuildOverrideResolutionPlan(
                _hotelId,
                _roomTypeId,
                _ratePlanId,
                Array.Empty<RoomPriceOverride>(),
                new[] { _today.AddDays(-1), _today },
                PriceAdjustmentType.FixedPrice,
                1_000_000m,
                1_000_000m,
                false,
                false,
                PriceConflictResolution.ReplaceExisting,
                _today,
                "Manager",
                null,
                Guid.NewGuid))
            .Code.ShouldBe("BookingHotel:CannotModifyPastPricing");
    }

    [Fact]
    public void Should_Handle_Partial_Overlap_Cancel_Skip_And_Replace_Without_Duplicate_Dates()
    {
        var existing = new RoomPriceOverride(
            Guid.NewGuid(),
            _hotelId,
            _roomTypeId,
            _ratePlanId,
            new DateTime(2026, 12, 10),
            new DateTime(2026, 12, 20),
            PriceAdjustmentType.FixedPrice,
            1_500_000m,
            1_500_000m,
            false,
            false,
            "Manager");

        var targets = _service.ExpandMatchingDates(new DateTime(2026, 12, 5), new DateTime(2026, 12, 15));

        // 1. Cancel
        var cancelPlan = _service.BuildOverrideResolutionPlan(
            _hotelId, _roomTypeId, _ratePlanId, new[] { existing }, targets,
            PriceAdjustmentType.FixedPrice, 2_000_000m, 2_000_000m, true, false,
            PriceConflictResolution.Cancel, _today, "Manager", null, Guid.NewGuid);

        cancelPlan.WasCancelledByConflict.ShouldBeTrue();
        cancelPlan.ConflictingDates.Count.ShouldBe(6); // Dec 10..15

        // 2. SkipExisting
        var skipPlan = _service.BuildOverrideResolutionPlan(
            _hotelId, _roomTypeId, _ratePlanId, new[] { existing }, targets,
            PriceAdjustmentType.FixedPrice, 2_000_000m, 2_000_000m, true, false,
            PriceConflictResolution.SkipExisting, _today, "Manager", null, Guid.NewGuid);

        skipPlan.WasCancelledByConflict.ShouldBeFalse();
        skipPlan.AppliedDates.Count.ShouldBe(5); // Dec 5..9
        skipPlan.SkippedDates.Count.ShouldBe(6); // Dec 10..15
        skipPlan.ToInsert.Count.ShouldBe(1);
        skipPlan.ToInsert[0].StartDate.ShouldBe(new DateTime(2026, 12, 5));
        skipPlan.ToInsert[0].EndDate.ShouldBe(new DateTime(2026, 12, 9));

        // 3. ReplaceExisting
        var replacePlan = _service.BuildOverrideResolutionPlan(
            _hotelId, _roomTypeId, _ratePlanId, new[] { existing }, targets,
            PriceAdjustmentType.FixedPrice, 2_000_000m, 2_000_000m, true, false,
            PriceConflictResolution.ReplaceExisting, _today, "Manager", null, Guid.NewGuid);

        replacePlan.ToUpdate.Count.ShouldBe(1);
        existing.StartDate.ShouldBe(new DateTime(2026, 12, 16));
        existing.EndDate.ShouldBe(new DateTime(2026, 12, 20));
        replacePlan.ToInsert.Count.ShouldBe(1);
        replacePlan.ToInsert[0].StartDate.ShouldBe(new DateTime(2026, 12, 5));
        replacePlan.ToInsert[0].EndDate.ShouldBe(new DateTime(2026, 12, 15));
        replacePlan.ToInsert[0].Overlaps(existing.StartDate, existing.EndDate).ShouldBeFalse();
    }

    [Fact]
    public void Should_Preserve_Existing_Override_Price_When_RestrictionsOnly_Replaces_Existing()
    {
        var existing = new RoomPriceOverride(
            Guid.NewGuid(),
            _hotelId,
            _roomTypeId,
            _ratePlanId,
            new DateTime(2026, 12, 10),
            new DateTime(2026, 12, 12),
            PriceAdjustmentType.FixedPrice,
            2_000_000m,
            2_000_000m,
            false,
            false,
            "Manager");

        // Target Dec 11..13 with RestrictionsOnly (CTA=true)
        var targets = _service.ExpandMatchingDates(new DateTime(2026, 12, 11), new DateTime(2026, 12, 13));

        var plan = _service.BuildOverrideResolutionPlan(
            _hotelId, _roomTypeId, _ratePlanId, new[] { existing }, targets,
            PriceAdjustmentType.RestrictionsOnly, null, null, true, false,
            PriceConflictResolution.ReplaceExisting, _today, "Manager", null, Guid.NewGuid);

        // Existing trimmed to Dec 10..10
        existing.StartDate.ShouldBe(new DateTime(2026, 12, 10));
        existing.EndDate.ShouldBe(new DateTime(2026, 12, 10));

        // Inserted segments: Dec 11..12 keeps 2,000,000 VND + CTA=true; Dec 13..13 is RestrictionsOnly + CTA=true
        plan.ToInsert.Count.ShouldBe(2);
        var preservedPriceSeg = plan.ToInsert.First(x => x.StartDate == new DateTime(2026, 12, 11));
        preservedPriceSeg.EndDate.ShouldBe(new DateTime(2026, 12, 12));
        preservedPriceSeg.CalculatedEffectivePrice.ShouldBe(2_000_000m);
        preservedPriceSeg.IsClosedToArrival.ShouldBeTrue();

        var pureRestrictionSeg = plan.ToInsert.First(x => x.StartDate == new DateTime(2026, 12, 13));
        pureRestrictionSeg.AdjustmentType.ShouldBe(PriceAdjustmentType.RestrictionsOnly);
        pureRestrictionSeg.CalculatedEffectivePrice.ShouldBeNull();
        pureRestrictionSeg.IsClosedToArrival.ShouldBeTrue();
    }

    [Fact]
    public void Should_Protect_Past_Dates_On_Delete_And_Validate_CTA_CTD_Stay_Restrictions()
    {
        var pastOnly = new RoomPriceOverride(
            Guid.NewGuid(), _hotelId, _roomTypeId, _ratePlanId,
            _today.AddDays(-5), _today.AddDays(-1),
            PriceAdjustmentType.FixedPrice, 900_000m, 900_000m);

        Should.Throw<BusinessException>(() => _service.PrepareDeleteWithPastProtection(pastOnly, _today))
            .Code.ShouldBe("BookingHotel:CannotModifyPastPricing");

        var straddling = new RoomPriceOverride(
            Guid.NewGuid(), _hotelId, _roomTypeId, _ratePlanId,
            _today.AddDays(-3), _today.AddDays(4),
            PriceAdjustmentType.FixedPrice, 1_200_000m, 1_200_000m,
            isClosedToArrival: true, isClosedToDeparture: false);

        var (shouldDelete, affectedStart, affectedEnd) = _service.PrepareDeleteWithPastProtection(straddling, _today);
        shouldDelete.ShouldBeFalse();
        straddling.StartDate.ShouldBe(_today.AddDays(-3));
        straddling.EndDate.ShouldBe(_today.AddDays(-1));
        affectedStart.ShouldBe(_today);
        affectedEnd.ShouldBe(_today.AddDays(4));

        var futureCta = new RoomPriceOverride(
            Guid.NewGuid(), _hotelId, _roomTypeId, _ratePlanId,
            _today.AddDays(2), _today.AddDays(2),
            PriceAdjustmentType.RestrictionsOnly, null, null,
            isClosedToArrival: true, isClosedToDeparture: false);

        var futureCtd = new RoomPriceOverride(
            Guid.NewGuid(), _hotelId, _roomTypeId, _ratePlanId,
            _today.AddDays(5), _today.AddDays(5),
            PriceAdjustmentType.RestrictionsOnly, null, null,
            isClosedToArrival: false, isClosedToDeparture: true);

        var check = _service.ValidateStayRestrictions(_today.AddDays(2), _today.AddDays(5), new[] { futureCta, futureCtd });
        check.IsAllowed.ShouldBeFalse();
        check.IsBlockedByCta.ShouldBeTrue();
        check.IsBlockedByCtd.ShouldBeTrue();
    }
}
