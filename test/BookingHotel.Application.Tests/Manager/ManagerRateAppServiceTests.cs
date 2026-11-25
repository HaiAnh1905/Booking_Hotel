using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BookingHotel.Bookings;
using BookingHotel.Hotels;
using BookingHotel.Identity;
using BookingHotel.Manager.Rooms;
using BookingHotel.Rooms;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Modularity;
using Volo.Abp.Timing;
using Volo.Abp.Users;
using Xunit;

namespace BookingHotel.Manager;

public abstract class ManagerRateAppServiceTests<TStartupModule> : BookingHotelApplicationTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    protected IManagerRateAppService RateAppService { get; }
    protected IRepository<Hotel, Guid> HotelRepository { get; }
    protected IRepository<RoomType, Guid> RoomTypeRepository { get; }
    protected IRepository<RatePlan, Guid> RatePlanRepository { get; }
    protected IRepository<RoomPriceOverride, Guid> OverrideRepository { get; }
    protected IRepository<PricingAuditLog, Guid> AuditLogRepository { get; }
    protected IRepository<UserHotelAssignment, Guid> AssignmentRepository { get; }
    protected IRepository<Booking, Guid> BookingRepository { get; }
    protected ICurrentUser CurrentUser { get; }
    protected IClock Clock { get; }

    protected ManagerRateAppServiceTests()
    {
        RateAppService = GetRequiredService<IManagerRateAppService>();
        HotelRepository = GetRequiredService<IRepository<Hotel, Guid>>();
        RoomTypeRepository = GetRequiredService<IRepository<RoomType, Guid>>();
        RatePlanRepository = GetRequiredService<IRepository<RatePlan, Guid>>();
        OverrideRepository = GetRequiredService<IRepository<RoomPriceOverride, Guid>>();
        AuditLogRepository = GetRequiredService<IRepository<PricingAuditLog, Guid>>();
        AssignmentRepository = GetRequiredService<IRepository<UserHotelAssignment, Guid>>();
        BookingRepository = GetRequiredService<IRepository<Booking, Guid>>();
        CurrentUser = GetRequiredService<ICurrentUser>();
        Clock = GetRequiredService<IClock>();
    }

    private async Task<(Hotel Hotel, RoomType RoomType, RatePlan StdPlan, RatePlan PromoPlan)> SeedPricingFixtureAsync()
    {
        var hotelId = Guid.NewGuid();
        var hotel = new Hotel(
            hotelId,
            "Booking Hotel Saigon Test",
            "BHSG-" + hotelId.ToString("N")[..4].ToUpperInvariant(),
            "123 Nguyen Hue",
            "Ho Chi Minh",
            "02838225588",
            "sg@bookinghotel.vn",
            5,
            50,
            "Test Manager");

        await HotelRepository.InsertAsync(hotel, autoSave: true);

        if (CurrentUser.Id.HasValue)
        {
            await AssignmentRepository.InsertAsync(
                new UserHotelAssignment(Guid.NewGuid(), CurrentUser.Id.Value, hotelId),
                autoSave: true);
        }

        var roomType = new RoomType(Guid.NewGuid(), hotelId, "Superior Double", 400_000m);
        await RoomTypeRepository.InsertAsync(roomType, autoSave: true);

        var stdPlan = new RatePlan(Guid.NewGuid(), "Standard Rate", "STD_" + hotelId.ToString("N")[..4].ToUpperInvariant(), "Flexible", true, false, 24, 0m);
        var promoPlan = new RatePlan(Guid.NewGuid(), "Breakfast Saver", "BB_" + hotelId.ToString("N")[..4].ToUpperInvariant(), "With breakfast", false, true, 0, 10m);
        await RatePlanRepository.InsertAsync(stdPlan, autoSave: true);
        await RatePlanRepository.InsertAsync(promoPlan, autoSave: true);

        return (hotel, roomType, stdPlan, promoPlan);
    }

    [Fact]
    public async Task Should_Complete_Full_Pricing_Lifecycle_Audit_All_6_Actions_And_Preserve_Booking()
    {
        var (hotel, roomType, stdPlan, promoPlan) = await SeedPricingFixtureAsync();
        var today = Clock.Now.Date;
        var stayDate = today.AddDays(3);

        // Seed confirmed booking with locked 400,000 VND total (BR-12)
        var booking = new Booking(
            Guid.NewGuid(),
            hotel.Id,
            "BK-" + Guid.NewGuid().ToString("N")[..6].ToUpperInvariant(),
            "Nguyen Van Khach",
            "0909123456",
            "guest@example.com",
            stayDate,
            stayDate.AddDays(1),
            400_000m);
        await BookingRepository.InsertAsync(booking, autoSave: true);

        // 1. Get Pricing Context (US-1)
        var ctx = await RateAppService.GetPricingContextAsync(hotel.Id, roomType.Id);
        ctx.HotelName.ShouldBe(hotel.Name);
        ctx.RatePlanCount.ShouldBeGreaterThanOrEqualTo(2);

        // 2. Bulk Preview & Apply Fixed Price 2,000,000 VND (US-3 -> Action 1: CreatePriceOverride)
        var preview = await RateAppService.PreviewBulkAdjustmentAsync(new BulkPriceAdjustmentPreviewInputDto
        {
            HotelId = hotel.Id,
            RoomTypeId = roomType.Id,
            RatePlanIds = new List<Guid> { stdPlan.Id },
            StartDate = today.AddDays(2),
            EndDate = today.AddDays(5),
            AdjustmentType = PriceAdjustmentType.FixedPrice,
            AdjustmentValue = 2_000_000m,
            IsClosedToArrival = true,
            IsClosedToDeparture = false
        });
        preview.HasAnyConflicts.ShouldBeFalse();
        preview.TotalAffectedDaysAcrossPlans.ShouldBe(4);

        var applyResult = await RateAppService.ApplyBulkAdjustmentAsync(new ApplyBulkPriceAdjustmentDto
        {
            HotelId = hotel.Id,
            RoomTypeId = roomType.Id,
            RatePlanIds = new List<Guid> { stdPlan.Id },
            StartDate = today.AddDays(2),
            EndDate = today.AddDays(5),
            AdjustmentType = PriceAdjustmentType.FixedPrice,
            AdjustmentValue = 2_000_000m,
            IsClosedToArrival = true,
            IsClosedToDeparture = false,
            ConflictResolution = PriceConflictResolution.ReplaceExisting
        });
        applyResult.AffectedDaysCount.ShouldBe(4);

        // 3. Update Base Price -> must NOT overwrite existing 2,000,000 VND override (BR-08 -> Action 4: UpdateBasePrice)
        await RateAppService.UpdateRatePlanBasePriceAsync(new UpdateRoomRatePlanBasePriceDto
        {
            HotelId = hotel.Id,
            RoomTypeId = roomType.Id,
            RatePlanId = stdPlan.Id,
            BasePrice = 500_000m
        });

        var calendar = await RateAppService.GetMonthlyCalendarAsync(hotel.Id, roomType.Id, stdPlan.Id, stayDate.Year, stayDate.Month);
        var overriddenDay = calendar.Days.Single(d => d.ApplicableDate == stayDate);
        overriddenDay.EffectivePrice.ShouldBe(2_000_000m);
        overriddenDay.IsClosedToArrival.ShouldBeTrue();

        // 4. Bulk overlap with Percentage +20% (500,000 * 1.2 = 600,000) using ReplaceExisting (Action 2: UpdatePriceOverride)
        var overlapPreview = await RateAppService.PreviewBulkAdjustmentAsync(new BulkPriceAdjustmentPreviewInputDto
        {
            HotelId = hotel.Id,
            RoomTypeId = roomType.Id,
            RatePlanIds = new List<Guid> { stdPlan.Id },
            StartDate = today.AddDays(4),
            EndDate = today.AddDays(7),
            AdjustmentType = PriceAdjustmentType.Percentage,
            AdjustmentValue = 20m,
            IsClosedToArrival = false,
            IsClosedToDeparture = true
        });
        overlapPreview.HasAnyConflicts.ShouldBeTrue();
        overlapPreview.RatePlanPreviews[0].ConflictingDates.Count.ShouldBe(2);
        overlapPreview.RatePlanPreviews[0].ProjectedEffectivePrice.ShouldBe(600_000m);

        await RateAppService.ApplyBulkAdjustmentAsync(new ApplyBulkPriceAdjustmentDto
        {
            HotelId = hotel.Id,
            RoomTypeId = roomType.Id,
            RatePlanIds = new List<Guid> { stdPlan.Id },
            StartDate = today.AddDays(4),
            EndDate = today.AddDays(7),
            AdjustmentType = PriceAdjustmentType.Percentage,
            AdjustmentValue = 20m,
            IsClosedToArrival = false,
            IsClosedToDeparture = true,
            ConflictResolution = PriceConflictResolution.ReplaceExisting
        });

        // 5. Quick-Edit RestrictionsOnly on non-overridden day (Action 6: UpdateRestrictions)
        var quickEdited = await RateAppService.QuickEditDailyPricingAsync(new QuickEditDailyPricingDto
        {
            HotelId = hotel.Id,
            RoomTypeId = roomType.Id,
            RatePlanId = stdPlan.Id,
            ApplicableDate = today.AddDays(10),
            AdjustmentType = PriceAdjustmentType.RestrictionsOnly,
            IsClosedToArrival = true,
            IsClosedToDeparture = true
        });
        quickEdited.EffectivePrice.ShouldBe(500_000m);
        quickEdited.IsClosedToArrival.ShouldBeTrue();
        quickEdited.IsClosedToDeparture.ShouldBeTrue();

        // 6. Delete Override (US-4 -> Action 3: DeletePriceOverride)
        var overrides = await RateAppService.GetPriceOverridesAsync(hotel.Id, roomType.Id, stdPlan.Id);
        overrides.Count.ShouldBeGreaterThanOrEqualTo(2);
        await RateAppService.DeletePriceOverrideAsync(overrides[0].Id, hotel.Id);

        // 7. Toggle Rate Plan Inactive -> blocks new bulk adjustment (BR-11 -> Action 5: ToggleRatePlanStatus)
        await RateAppService.ToggleRatePlanStatusAsync(new ToggleRoomRatePlanStatusDto
        {
            HotelId = hotel.Id,
            RoomTypeId = roomType.Id,
            RatePlanId = promoPlan.Id,
            IsActive = false
        });

        await Should.ThrowAsync<BusinessException>(() =>
            RateAppService.PreviewBulkAdjustmentAsync(new BulkPriceAdjustmentPreviewInputDto
            {
                HotelId = hotel.Id,
                RoomTypeId = roomType.Id,
                RatePlanIds = new List<Guid> { promoPlan.Id },
                StartDate = today.AddDays(1),
                EndDate = today.AddDays(2),
                AdjustmentType = PriceAdjustmentType.FixedPrice,
                AdjustmentValue = 900_000m
            }));

        // Verify ALL 6 PricingAuditActionType values recorded (SC-006)
        var logs = await RateAppService.GetPricingAuditLogsAsync(new GetPricingAuditLogsInputDto
        {
            HotelId = hotel.Id,
            RoomTypeId = roomType.Id,
            MaxResultCount = 50
        });

        var recordedActions = logs.Items.Select(l => l.ActionType).Distinct().ToHashSet();
        recordedActions.ShouldContain(PricingAuditActionType.CreatePriceOverride);
        recordedActions.ShouldContain(PricingAuditActionType.UpdatePriceOverride);
        recordedActions.ShouldContain(PricingAuditActionType.DeletePriceOverride);
        recordedActions.ShouldContain(PricingAuditActionType.UpdateBasePrice);
        recordedActions.ShouldContain(PricingAuditActionType.ToggleRatePlanStatus);
        recordedActions.ShouldContain(PricingAuditActionType.UpdateRestrictions);

        // Verify Booking TotalAmount is untouched (BR-12, SC-005)
        var reloadedBooking = await BookingRepository.GetAsync(booking.Id);
        reloadedBooking.TotalAmount.ShouldBe(400_000m);
    }

    [Fact]
    public async Task Should_Block_Cross_Hotel_RoomType_Access_And_Preserve_Straddling_Past_History()
    {
        var (hotelA, roomTypeA, stdPlan, _) = await SeedPricingFixtureAsync();
        var today = Clock.Now.Date;

        // Hotel B not assigned to manager
        var hotelB = new Hotel(
            Guid.NewGuid(),
            "Other Hotel B",
            "OHB-" + Guid.NewGuid().ToString("N")[..4].ToUpperInvariant(),
            "99 Le Loi",
            "Da Nang",
            "0236123456",
            "dn@bookinghotel.vn",
            4,
            30,
            "Other Manager");
        await HotelRepository.InsertAsync(hotelB, autoSave: true);

        var roomTypeB = new RoomType(Guid.NewGuid(), hotelB.Id, "Deluxe B", 600_000m);
        await RoomTypeRepository.InsertAsync(roomTypeB, autoSave: true);

        // Cross-Hotel roomType mismatch must throw CrossHotelOperationForbidden (BR-14)
        var ex = await Should.ThrowAsync<BusinessException>(() =>
            RateAppService.GetPricingContextAsync(hotelA.Id, roomTypeB.Id));
        ex.Code.ShouldBe("BookingHotel:CrossHotelOperationForbidden");

        // Straddling-today override: deleting must freeze [Start..Yesterday] and only remove [Today..End] (BR-05)
        var straddling = new RoomPriceOverride(
            Guid.NewGuid(),
            hotelA.Id,
            roomTypeA.Id,
            stdPlan.Id,
            today.AddDays(-2),
            today.AddDays(3),
            PriceAdjustmentType.FixedPrice,
            1_800_000m,
            1_800_000m,
            false,
            false,
            "SeedManager");
        await OverrideRepository.InsertAsync(straddling, autoSave: true);

        await RateAppService.DeletePriceOverrideAsync(straddling.Id, hotelA.Id);

        var kept = await OverrideRepository.GetAsync(straddling.Id);
        kept.StartDate.ShouldBe(today.AddDays(-2));
        kept.EndDate.ShouldBe(today.AddDays(-1));
    }
}
