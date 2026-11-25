using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using BookingHotel.Hotels;
using BookingHotel.Identity;
using BookingHotel.Permissions;
using BookingHotel.Rooms;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Users;

namespace BookingHotel.Manager.Rooms;

[Authorize(BookingHotelPermissions.Rooms.SetRates)]
public class ManagerRateAppService : ManagerAppServiceBase, IManagerRateAppService
{
    private static readonly JsonSerializerOptions AuditJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IRepository<Hotel, Guid> _hotelRepository;
    private readonly IRepository<RoomType, Guid> _roomTypeRepository;
    private readonly IRepository<RatePlan, Guid> _ratePlanRepository;
    private readonly IRepository<HotelRatePlan, Guid> _hotelRatePlanRepository;
    private readonly IRepository<RoomPriceOverride, Guid> _overrideRepository;
    private readonly IRepository<PricingAuditLog, Guid> _auditLogRepository;
    private readonly RoomPricingDomainService _pricingDomainService;

    public ManagerRateAppService(
        IRepository<UserHotelAssignment, Guid> userHotelAssignmentRepository,
        IRepository<Hotel, Guid> hotelRepository,
        IRepository<RoomType, Guid> roomTypeRepository,
        IRepository<RatePlan, Guid> ratePlanRepository,
        IRepository<HotelRatePlan, Guid> hotelRatePlanRepository,
        IRepository<RoomPriceOverride, Guid> overrideRepository,
        IRepository<PricingAuditLog, Guid> auditLogRepository,
        RoomPricingDomainService pricingDomainService)
        : base(userHotelAssignmentRepository)
    {
        _hotelRepository = hotelRepository;
        _roomTypeRepository = roomTypeRepository;
        _ratePlanRepository = ratePlanRepository;
        _hotelRatePlanRepository = hotelRatePlanRepository;
        _overrideRepository = overrideRepository;
        _auditLogRepository = auditLogRepository;
        _pricingDomainService = pricingDomainService;
    }

    public async Task<RoomTypePricingContextDto> GetPricingContextAsync(Guid hotelId, Guid roomTypeId)
    {
        var (hotel, roomType) = await ValidateHotelAndRoomTypeAsync(hotelId, roomTypeId);
        var today = Clock.Now.Date;

        var masterPlans = (await _ratePlanRepository.GetListAsync())
            .OrderBy(p => p.Name)
            .ToList();

        var hotelPlans = await _hotelRatePlanRepository.GetListAsync(hp =>
            hp.HotelId == hotelId && hp.RoomTypeId == roomTypeId);

        var allOverrides = await _overrideRepository.GetListAsync(o =>
            o.HotelId == hotelId && o.RoomTypeId == roomTypeId);

        var activeFutureOrCurrentOverridesCount = allOverrides.Count(o => o.EndDate.Date >= today);

        var planItems = new List<RoomRatePlanItemDto>();
        foreach (var master in masterPlans)
        {
            var branchConfig = hotelPlans.FirstOrDefault(x => x.RatePlanId == master.Id);
            var planOverrides = allOverrides.Where(o => o.RatePlanId == master.Id).ToList();
            planItems.Add(BuildRatePlanItemDto(roomType, master, branchConfig, planOverrides, today));
        }

        return new RoomTypePricingContextDto
        {
            HotelId = hotel.Id,
            HotelName = hotel.Name,
            HotelCode = hotel.Code,
            RoomTypeId = roomType.Id,
            RoomTypeName = roomType.Name,
            ReferenceBasePrice = roomType.BasePrice,
            RatePlanCount = planItems.Count,
            ActiveRatePlanCount = planItems.Count(p => p.IsActive),
            ActiveOverrideCount = activeFutureOrCurrentOverridesCount,
            RatePlans = planItems
        };
    }

    public async Task<RoomRatePlanItemDto> ToggleRatePlanStatusAsync(ToggleRoomRatePlanStatusDto input)
    {
        var (_, roomType) = await ValidateHotelAndRoomTypeAsync(input.HotelId, input.RoomTypeId);
        var masterPlan = await GetRequiredRatePlanAsync(input.RatePlanId);
        var today = Clock.Now.Date;

        var hotelPlan = await _hotelRatePlanRepository.FirstOrDefaultAsync(hp =>
            hp.HotelId == input.HotelId &&
            hp.RoomTypeId == input.RoomTypeId &&
            hp.RatePlanId == input.RatePlanId);

        var previousIsActive = hotelPlan?.IsActive ?? true;

        if (hotelPlan != null)
        {
            hotelPlan.IsActive = input.IsActive;
            await _hotelRatePlanRepository.UpdateAsync(hotelPlan, autoSave: true);
        }
        else
        {
            var resolvedBase = _pricingDomainService.ResolveBranchBasePrice(
                roomType.BasePrice,
                masterPlan.DefaultPriceAdjustmentPercent,
                null);

            hotelPlan = new HotelRatePlan(
                GuidGenerator.Create(),
                input.HotelId,
                input.RoomTypeId,
                input.RatePlanId,
                input.IsActive,
                resolvedBase,
                1.0m,
                CurrentTenant.Id);

            await _hotelRatePlanRepository.InsertAsync(hotelPlan, autoSave: true);
        }

        await WriteAuditLogAsync(
            input.HotelId,
            roomType,
            masterPlan,
            PricingAuditActionType.ToggleRatePlanStatus,
            new { isActive = previousIsActive },
            new { isActive = input.IsActive },
            null,
            null,
            input.IsActive ? "Activated rate plan" : "Deactivated rate plan");

        var planOverrides = await _overrideRepository.GetListAsync(o =>
            o.HotelId == input.HotelId &&
            o.RoomTypeId == input.RoomTypeId &&
            o.RatePlanId == input.RatePlanId);

        return BuildRatePlanItemDto(roomType, masterPlan, hotelPlan, planOverrides, today);
    }

    public async Task<RoomRatePlanItemDto> UpdateRatePlanBasePriceAsync(UpdateRoomRatePlanBasePriceDto input)
    {
        if (input.BasePrice <= 0m)
        {
            throw new BusinessException("BookingHotel:InvalidEffectivePrice");
        }

        var (_, roomType) = await ValidateHotelAndRoomTypeAsync(input.HotelId, input.RoomTypeId);
        var masterPlan = await GetRequiredRatePlanAsync(input.RatePlanId);
        var today = Clock.Now.Date;

        var hotelPlan = await _hotelRatePlanRepository.FirstOrDefaultAsync(hp =>
            hp.HotelId == input.HotelId &&
            hp.RoomTypeId == input.RoomTypeId &&
            hp.RatePlanId == input.RatePlanId);

        var previousBasePrice = _pricingDomainService.ResolveBranchBasePrice(
            roomType.BasePrice,
            masterPlan.DefaultPriceAdjustmentPercent,
            hotelPlan?.CustomBasePrice);

        if (hotelPlan != null)
        {
            hotelPlan.CustomBasePrice = input.BasePrice;
            await _hotelRatePlanRepository.UpdateAsync(hotelPlan, autoSave: true);
        }
        else
        {
            hotelPlan = new HotelRatePlan(
                GuidGenerator.Create(),
                input.HotelId,
                input.RoomTypeId,
                input.RatePlanId,
                true,
                input.BasePrice,
                1.0m,
                CurrentTenant.Id);

            await _hotelRatePlanRepository.InsertAsync(hotelPlan, autoSave: true);
        }

        await WriteAuditLogAsync(
            input.HotelId,
            roomType,
            masterPlan,
            PricingAuditActionType.UpdateBasePrice,
            new { basePrice = previousBasePrice },
            new { basePrice = input.BasePrice },
            null,
            null,
            $"BasePrice updated: {previousBasePrice:N0} -> {input.BasePrice:N0}");

        var planOverrides = await _overrideRepository.GetListAsync(o =>
            o.HotelId == input.HotelId &&
            o.RoomTypeId == input.RoomTypeId &&
            o.RatePlanId == input.RatePlanId);

        return BuildRatePlanItemDto(roomType, masterPlan, hotelPlan, planOverrides, today);
    }

    public async Task<MonthlyPricingCalendarDto> GetMonthlyCalendarAsync(
        Guid hotelId,
        Guid roomTypeId,
        Guid ratePlanId,
        int year,
        int month)
    {
        if (year < 2000 || year > 2100 || month < 1 || month > 12)
        {
            throw new BusinessException("BookingHotel:InvalidDateRange");
        }

        var (_, roomType) = await ValidateHotelAndRoomTypeAsync(hotelId, roomTypeId);
        var masterPlan = await GetRequiredRatePlanAsync(ratePlanId);
        var hotelPlan = await _hotelRatePlanRepository.FirstOrDefaultAsync(hp =>
            hp.HotelId == hotelId && hp.RoomTypeId == roomTypeId && hp.RatePlanId == ratePlanId);

        var isActive = hotelPlan?.IsActive ?? true;
        var basePrice = _pricingDomainService.ResolveBranchBasePrice(
            roomType.BasePrice,
            masterPlan.DefaultPriceAdjustmentPercent,
            hotelPlan?.CustomBasePrice);

        var monthStart = new DateTime(year, month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);
        var today = Clock.Now.Date;

        var overrides = await _overrideRepository.GetListAsync(o =>
            o.HotelId == hotelId &&
            o.RoomTypeId == roomTypeId &&
            o.RatePlanId == ratePlanId &&
            o.StartDate <= monthEnd &&
            o.EndDate >= monthStart);

        var days = new List<DailyPricingCellDto>();
        for (var d = monthStart; d <= monthEnd; d = d.AddDays(1))
        {
            var ov = overrides.FirstOrDefault(o => o.ContainsDate(d));
            days.Add(BuildDailyCellDto(d, today, basePrice, isActive, ov));
        }

        return new MonthlyPricingCalendarDto
        {
            HotelId = hotelId,
            RoomTypeId = roomTypeId,
            RatePlanId = ratePlanId,
            RatePlanName = masterPlan.Name,
            RatePlanCode = masterPlan.Code,
            IsRatePlanActive = isActive,
            BasePrice = basePrice,
            Year = year,
            Month = month,
            Days = days
        };
    }

    public async Task<DailyPricingCellDto> QuickEditDailyPricingAsync(QuickEditDailyPricingDto input)
    {
        var (_, roomType) = await ValidateHotelAndRoomTypeAsync(input.HotelId, input.RoomTypeId);
        var masterPlan = await GetRequiredRatePlanAsync(input.RatePlanId);
        var hotelPlan = await _hotelRatePlanRepository.FirstOrDefaultAsync(hp =>
            hp.HotelId == input.HotelId && hp.RoomTypeId == input.RoomTypeId && hp.RatePlanId == input.RatePlanId);

        var isActive = hotelPlan?.IsActive ?? true;
        if (!isActive)
        {
            throw new BusinessException("BookingHotel:RatePlanInactive");
        }

        var today = Clock.Now.Date;
        var targetDate = input.ApplicableDate.Date;
        if (targetDate < today)
        {
            throw new BusinessException("BookingHotel:CannotModifyPastPricing");
        }

        var basePrice = _pricingDomainService.ResolveBranchBasePrice(
            roomType.BasePrice,
            masterPlan.DefaultPriceAdjustmentPercent,
            hotelPlan?.CustomBasePrice);

        var existingOverrides = await _overrideRepository.GetListAsync(o =>
            o.HotelId == input.HotelId &&
            o.RoomTypeId == input.RoomTypeId &&
            o.RatePlanId == input.RatePlanId &&
            o.StartDate <= targetDate &&
            o.EndDate >= targetDate);

        var existingOnDay = existingOverrides.FirstOrDefault(o => o.ContainsDate(targetDate));
        var beforeSnapshot = existingOnDay != null
            ? CreateOverrideAuditSnapshot(existingOnDay, basePrice)
            : new { basePrice, effectivePrice = basePrice, isOverridden = false, cta = false, ctd = false };

        if (input.ClearOverride)
        {
            if (existingOnDay != null)
            {
                await RemoveSingleDateFromOverrideAsync(existingOnDay, targetDate);

                await WriteAuditLogAsync(
                    input.HotelId,
                    roomType,
                    masterPlan,
                    PricingAuditActionType.DeletePriceOverride,
                    beforeSnapshot,
                    new { basePrice, effectivePrice = basePrice, isOverridden = false, cta = false, ctd = false },
                    targetDate,
                    targetDate,
                    "Cleared daily override via Quick-Edit");
            }

            return BuildDailyCellDto(targetDate, today, basePrice, isActive, null);
        }

        var calculatedPrice = _pricingDomainService.CalculateEffectivePrice(
            basePrice,
            input.AdjustmentType,
            input.AdjustmentValue);

        var plan = _pricingDomainService.BuildOverrideResolutionPlan(
            input.HotelId,
            input.RoomTypeId,
            input.RatePlanId,
            existingOverrides,
            new List<DateTime> { targetDate },
            input.AdjustmentType,
            input.AdjustmentValue,
            calculatedPrice,
            input.IsClosedToArrival,
            input.IsClosedToDeparture,
            PriceConflictResolution.ReplaceExisting,
            today,
            GetCurrentActorName(),
            CurrentTenant.Id,
            () => GuidGenerator.Create());

        await PersistResolutionPlanAsync(plan);

        var resultingOverride = plan.ToInsert.FirstOrDefault(o => o.ContainsDate(targetDate));
        var actionType = input.AdjustmentType == PriceAdjustmentType.RestrictionsOnly
            ? PricingAuditActionType.UpdateRestrictions
            : (existingOnDay != null ? PricingAuditActionType.UpdatePriceOverride : PricingAuditActionType.CreatePriceOverride);

        await WriteAuditLogAsync(
            input.HotelId,
            roomType,
            masterPlan,
            actionType,
            beforeSnapshot,
            resultingOverride != null ? CreateOverrideAuditSnapshot(resultingOverride, basePrice) : null,
            targetDate,
            targetDate,
            "Quick-Edit daily pricing");

        return BuildDailyCellDto(targetDate, today, basePrice, isActive, resultingOverride);
    }

    public async Task<BulkPriceAdjustmentPreviewDto> PreviewBulkAdjustmentAsync(BulkPriceAdjustmentPreviewInputDto input)
    {
        var (hotel, roomType) = await ValidateHotelAndRoomTypeAsync(input.HotelId, input.RoomTypeId);
        var today = Clock.Now.Date;
        var start = input.StartDate.Date;
        var end = input.EndDate.Date;

        if (start > end)
        {
            throw new BusinessException("BookingHotel:InvalidDateRange");
        }

        if (start < today)
        {
            throw new BusinessException("BookingHotel:CannotModifyPastPricing");
        }

        var matchingDates = _pricingDomainService.ExpandMatchingDates(start, end, input.DaysOfWeek);
        var uniqueRatePlanIds = input.RatePlanIds.Distinct().ToList();
        if (uniqueRatePlanIds.Count == 0)
        {
            throw new BusinessException("BookingHotel:RatePlanNotFound");
        }

        var masterPlans = await _ratePlanRepository.GetListAsync(p => uniqueRatePlanIds.Contains(p.Id));
        if (masterPlans.Count != uniqueRatePlanIds.Count)
        {
            throw new BusinessException("BookingHotel:RatePlanNotFound");
        }

        var hotelPlans = await _hotelRatePlanRepository.GetListAsync(hp =>
            hp.HotelId == input.HotelId &&
            hp.RoomTypeId == input.RoomTypeId &&
            uniqueRatePlanIds.Contains(hp.RatePlanId));

        var existingOverrides = await _overrideRepository.GetListAsync(o =>
            o.HotelId == input.HotelId &&
            o.RoomTypeId == input.RoomTypeId &&
            uniqueRatePlanIds.Contains(o.RatePlanId) &&
            o.StartDate <= end &&
            o.EndDate >= start);

        var planPreviews = new List<RatePlanBulkPreviewItemDto>();
        foreach (var rpId in uniqueRatePlanIds)
        {
            var master = masterPlans.First(p => p.Id == rpId);
            var hp = hotelPlans.FirstOrDefault(x => x.RatePlanId == rpId);
            var isActive = hp?.IsActive ?? true;
            if (!isActive)
            {
                throw new BusinessException("BookingHotel:RatePlanInactive")
                    .WithData("RatePlanName", master.Name);
            }

            var basePrice = _pricingDomainService.ResolveBranchBasePrice(
                roomType.BasePrice,
                master.DefaultPriceAdjustmentPercent,
                hp?.CustomBasePrice);

            var projectedEffectivePrice = _pricingDomainService.CalculateEffectivePrice(
                basePrice,
                input.AdjustmentType,
                input.AdjustmentValue);

            var planOverrides = existingOverrides.Where(o => o.RatePlanId == rpId).ToList();
            var conflictingDates = matchingDates
                .Where(d => planOverrides.Any(o => o.ContainsDate(d)))
                .ToList();

            planPreviews.Add(new RatePlanBulkPreviewItemDto
            {
                RatePlanId = master.Id,
                RatePlanName = master.Name,
                RatePlanCode = master.Code,
                CurrentBasePrice = basePrice,
                ProjectedEffectivePrice = projectedEffectivePrice ?? basePrice,
                MatchingDatesCount = matchingDates.Count,
                ConflictingDates = conflictingDates,
                NewDatesCount = matchingDates.Count - conflictingDates.Count
            });
        }

        var normalizedDaysOfWeek = (input.DaysOfWeek != null && input.DaysOfWeek.Count > 0)
            ? input.DaysOfWeek.Distinct().OrderBy(d => d).ToList()
            : Enum.GetValues<DayOfWeek>().ToList();

        return new BulkPriceAdjustmentPreviewDto
        {
            HotelName = hotel.Name,
            RoomTypeName = roomType.Name,
            StartDate = start,
            EndDate = end,
            DaysOfWeek = normalizedDaysOfWeek,
            AdjustmentType = input.AdjustmentType,
            AdjustmentValue = input.AdjustmentValue,
            IsClosedToArrival = input.IsClosedToArrival,
            IsClosedToDeparture = input.IsClosedToDeparture,
            TotalAffectedDaysAcrossPlans = planPreviews.Sum(p => p.MatchingDatesCount),
            HasAnyConflicts = planPreviews.Any(p => p.ConflictingDates.Count > 0),
            RatePlanPreviews = planPreviews
        };
    }

    public async Task<BulkPriceAdjustmentResultDto> ApplyBulkAdjustmentAsync(ApplyBulkPriceAdjustmentDto input)
    {
        var (_, roomType) = await ValidateHotelAndRoomTypeAsync(input.HotelId, input.RoomTypeId);
        var today = Clock.Now.Date;
        var start = input.StartDate.Date;
        var end = input.EndDate.Date;

        if (start > end)
        {
            throw new BusinessException("BookingHotel:InvalidDateRange");
        }

        if (start < today)
        {
            throw new BusinessException("BookingHotel:CannotModifyPastPricing");
        }

        var matchingDates = _pricingDomainService.ExpandMatchingDates(start, end, input.DaysOfWeek);
        var uniqueRatePlanIds = input.RatePlanIds.Distinct().ToList();
        if (uniqueRatePlanIds.Count == 0)
        {
            throw new BusinessException("BookingHotel:RatePlanNotFound");
        }

        var masterPlans = await _ratePlanRepository.GetListAsync(p => uniqueRatePlanIds.Contains(p.Id));
        if (masterPlans.Count != uniqueRatePlanIds.Count)
        {
            throw new BusinessException("BookingHotel:RatePlanNotFound");
        }

        var hotelPlans = await _hotelRatePlanRepository.GetListAsync(hp =>
            hp.HotelId == input.HotelId &&
            hp.RoomTypeId == input.RoomTypeId &&
            uniqueRatePlanIds.Contains(hp.RatePlanId));

        var allExistingOverrides = await _overrideRepository.GetListAsync(o =>
            o.HotelId == input.HotelId &&
            o.RoomTypeId == input.RoomTypeId &&
            uniqueRatePlanIds.Contains(o.RatePlanId) &&
            o.StartDate <= end &&
            o.EndDate >= start);

        var actorName = GetCurrentActorName();
        var pendingPlans = new List<(RatePlan Master, decimal BasePrice, OverrideResolutionPlan Plan)>();

        foreach (var rpId in uniqueRatePlanIds)
        {
            var master = masterPlans.First(p => p.Id == rpId);
            var hp = hotelPlans.FirstOrDefault(x => x.RatePlanId == rpId);
            if (!(hp?.IsActive ?? true))
            {
                throw new BusinessException("BookingHotel:RatePlanInactive")
                    .WithData("RatePlanName", master.Name);
            }

            var basePrice = _pricingDomainService.ResolveBranchBasePrice(
                roomType.BasePrice,
                master.DefaultPriceAdjustmentPercent,
                hp?.CustomBasePrice);

            var calculatedPrice = _pricingDomainService.CalculateEffectivePrice(
                basePrice,
                input.AdjustmentType,
                input.AdjustmentValue);

            var existingForPlan = allExistingOverrides.Where(o => o.RatePlanId == rpId).ToList();

            var resolutionPlan = _pricingDomainService.BuildOverrideResolutionPlan(
                input.HotelId,
                input.RoomTypeId,
                rpId,
                existingForPlan,
                matchingDates,
                input.AdjustmentType,
                input.AdjustmentValue,
                calculatedPrice,
                input.IsClosedToArrival,
                input.IsClosedToDeparture,
                input.ConflictResolution,
                today,
                actorName,
                CurrentTenant.Id,
                () => GuidGenerator.Create());

            if (resolutionPlan.WasCancelledByConflict)
            {
                throw new BusinessException("BookingHotel:PriceOverrideConflictDetected")
                    .WithData("ConflictingDatesCount", resolutionPlan.ConflictingDates.Count);
            }

            pendingPlans.Add((master, basePrice, resolutionPlan));
        }

        var totalAppliedDays = 0;
        var totalReplacedDays = 0;
        var totalSkippedDays = 0;
        var createdIds = new List<Guid>();

        foreach (var (master, basePrice, plan) in pendingPlans)
        {
            await PersistResolutionPlanAsync(plan);

            totalAppliedDays += plan.AppliedDates.Count;
            totalReplacedDays += plan.ReplacedDates.Count;
            totalSkippedDays += plan.SkippedDates.Count;
            createdIds.AddRange(plan.ToInsert.Select(x => x.Id));

            if (plan.AppliedDates.Count > 0)
            {
                var actionType = input.AdjustmentType == PriceAdjustmentType.RestrictionsOnly
                    ? PricingAuditActionType.UpdateRestrictions
                    : (plan.ReplacedDates.Count > 0 ? PricingAuditActionType.UpdatePriceOverride : PricingAuditActionType.CreatePriceOverride);

                await WriteAuditLogAsync(
                    input.HotelId,
                    roomType,
                    master,
                    actionType,
                    new
                    {
                        conflictingDatesCount = plan.ConflictingDates.Count,
                        conflictResolution = input.ConflictResolution.ToString()
                    },
                    new
                    {
                        adjustmentType = input.AdjustmentType.ToString(),
                        adjustmentValue = input.AdjustmentValue,
                        calculatedEffectivePrice = _pricingDomainService.CalculateEffectivePrice(basePrice, input.AdjustmentType, input.AdjustmentValue) ?? basePrice,
                        isClosedToArrival = input.IsClosedToArrival,
                        isClosedToDeparture = input.IsClosedToDeparture,
                        appliedDaysCount = plan.AppliedDates.Count,
                        replacedDaysCount = plan.ReplacedDates.Count,
                        skippedDaysCount = plan.SkippedDates.Count
                    },
                    plan.AppliedDates.Min(),
                    plan.AppliedDates.Max(),
                    $"Bulk adjustment ({input.ConflictResolution})");
            }
        }

        return new BulkPriceAdjustmentResultDto
        {
            AppliedRatePlanCount = pendingPlans.Count(x => x.Plan.AppliedDates.Count > 0),
            AffectedDaysCount = totalAppliedDays,
            ReplacedDaysCount = totalReplacedDays,
            SkippedDaysCount = totalSkippedDays,
            CreatedOverrideIds = createdIds
        };
    }

    public async Task<List<RoomPriceOverrideDto>> GetPriceOverridesAsync(Guid hotelId, Guid roomTypeId, Guid ratePlanId)
    {
        await ValidateHotelAndRoomTypeAsync(hotelId, roomTypeId);
        await GetRequiredRatePlanAsync(ratePlanId);
        var today = Clock.Now.Date;

        var overrides = await _overrideRepository.GetListAsync(o =>
            o.HotelId == hotelId &&
            o.RoomTypeId == roomTypeId &&
            o.RatePlanId == ratePlanId);

        return overrides
            .OrderByDescending(o => o.StartDate)
            .ThenByDescending(o => o.CreationTime)
            .Select(o => MapOverrideDto(o, today))
            .ToList();
    }

    public async Task<RoomPriceOverrideDto> UpdatePriceOverrideAsync(Guid id, UpdateRoomPriceOverrideDto input)
    {
        await CheckHotelManagerAccessAsync(input.HotelId);

        var existing = await _overrideRepository.GetAsync(id);
        if (existing.HotelId != input.HotelId)
        {
            throw new BusinessException("BookingHotel:CrossHotelOperationForbidden");
        }

        var (_, roomType) = await ValidateHotelAndRoomTypeAsync(existing.HotelId, existing.RoomTypeId);
        var masterPlan = await GetRequiredRatePlanAsync(existing.RatePlanId);
        var hotelPlan = await _hotelRatePlanRepository.FirstOrDefaultAsync(hp =>
            hp.HotelId == existing.HotelId &&
            hp.RoomTypeId == existing.RoomTypeId &&
            hp.RatePlanId == existing.RatePlanId);

        if (!(hotelPlan?.IsActive ?? true))
        {
            throw new BusinessException("BookingHotel:RatePlanInactive");
        }

        var today = Clock.Now.Date;
        if (existing.EndDate.Date < today)
        {
            throw new BusinessException("BookingHotel:CannotModifyPastPricing");
        }

        var newStart = input.StartDate.Date;
        var newEnd = input.EndDate.Date;
        if (newStart > newEnd)
        {
            throw new BusinessException("BookingHotel:InvalidDateRange");
        }

        if (newStart < today)
        {
            throw new BusinessException("BookingHotel:CannotModifyPastPricing");
        }

        var basePrice = _pricingDomainService.ResolveBranchBasePrice(
            roomType.BasePrice,
            masterPlan.DefaultPriceAdjustmentPercent,
            hotelPlan?.CustomBasePrice);

        var calculatedPrice = _pricingDomainService.CalculateEffectivePrice(
            basePrice,
            input.AdjustmentType,
            input.AdjustmentValue);

        var beforeSnapshot = CreateOverrideAuditSnapshot(existing, basePrice);

        var siblingOverrides = await _overrideRepository.GetListAsync(o =>
            o.HotelId == existing.HotelId &&
            o.RoomTypeId == existing.RoomTypeId &&
            o.RatePlanId == existing.RatePlanId &&
            o.Id != existing.Id &&
            o.StartDate <= newEnd &&
            o.EndDate >= newStart);

        var targetDates = _pricingDomainService.ExpandMatchingDates(newStart, newEnd);
        var hasSiblingConflicts = targetDates.Any(d => siblingOverrides.Any(s => s.ContainsDate(d)));

        if (hasSiblingConflicts && input.ConflictResolution == PriceConflictResolution.Cancel)
        {
            var conflictDates = targetDates.Where(d => siblingOverrides.Any(s => s.ContainsDate(d))).ToList();
            throw new BusinessException("BookingHotel:PriceOverrideConflictDetected")
                .WithData("ConflictingDatesCount", conflictDates.Count);
        }

        RoomPriceOverride primaryResult;
        var isStraddlingToday = existing.StartDate.Date < today && existing.EndDate.Date >= today;

        if (!hasSiblingConflicts && !isStraddlingToday)
        {
            existing.UpdateRangeAndPricing(
                newStart,
                newEnd,
                input.AdjustmentType,
                input.AdjustmentValue,
                calculatedPrice,
                input.IsClosedToArrival,
                input.IsClosedToDeparture);

            await _overrideRepository.UpdateAsync(existing, autoSave: true);
            primaryResult = existing;
        }
        else
        {
            if (isStraddlingToday)
            {
                existing.UpdateDateRange(existing.StartDate.Date, today.AddDays(-1));
                await _overrideRepository.UpdateAsync(existing, autoSave: true);
            }
            else
            {
                await _overrideRepository.DeleteAsync(existing, autoSave: true);
            }

            var plan = _pricingDomainService.BuildOverrideResolutionPlan(
                existing.HotelId,
                existing.RoomTypeId,
                existing.RatePlanId,
                siblingOverrides,
                targetDates,
                input.AdjustmentType,
                input.AdjustmentValue,
                calculatedPrice,
                input.IsClosedToArrival,
                input.IsClosedToDeparture,
                hasSiblingConflicts ? input.ConflictResolution : PriceConflictResolution.ReplaceExisting,
                today,
                GetCurrentActorName(),
                CurrentTenant.Id,
                () => GuidGenerator.Create());

            await PersistResolutionPlanAsync(plan);
            primaryResult = plan.ToInsert.FirstOrDefault() ?? existing;
        }

        var actionType = input.AdjustmentType == PriceAdjustmentType.RestrictionsOnly
            ? PricingAuditActionType.UpdateRestrictions
            : PricingAuditActionType.UpdatePriceOverride;

        await WriteAuditLogAsync(
            existing.HotelId,
            roomType,
            masterPlan,
            actionType,
            beforeSnapshot,
            CreateOverrideAuditSnapshot(primaryResult, basePrice),
            newStart,
            newEnd,
            isStraddlingToday ? "Updated future portion; past history frozen" : "Updated price override");

        return MapOverrideDto(primaryResult, today);
    }

    public async Task DeletePriceOverrideAsync(Guid id, Guid hotelId)
    {
        await CheckHotelManagerAccessAsync(hotelId);

        var existing = await _overrideRepository.GetAsync(id);
        if (existing.HotelId != hotelId)
        {
            throw new BusinessException("BookingHotel:CrossHotelOperationForbidden");
        }

        var (_, roomType) = await ValidateHotelAndRoomTypeAsync(existing.HotelId, existing.RoomTypeId);
        var masterPlan = await GetRequiredRatePlanAsync(existing.RatePlanId);
        var hotelPlan = await _hotelRatePlanRepository.FirstOrDefaultAsync(hp =>
            hp.HotelId == existing.HotelId &&
            hp.RoomTypeId == existing.RoomTypeId &&
            hp.RatePlanId == existing.RatePlanId);

        var basePrice = _pricingDomainService.ResolveBranchBasePrice(
            roomType.BasePrice,
            masterPlan.DefaultPriceAdjustmentPercent,
            hotelPlan?.CustomBasePrice);

        var beforeSnapshot = CreateOverrideAuditSnapshot(existing, basePrice);
        var today = Clock.Now.Date;

        var (shouldDeleteRecord, affectedStart, affectedEnd) =
            _pricingDomainService.PrepareDeleteWithPastProtection(existing, today);

        if (shouldDeleteRecord)
        {
            await _overrideRepository.DeleteAsync(existing, autoSave: true);
        }
        else
        {
            await _overrideRepository.UpdateAsync(existing, autoSave: true);
        }

        await WriteAuditLogAsync(
            existing.HotelId,
            roomType,
            masterPlan,
            PricingAuditActionType.DeletePriceOverride,
            beforeSnapshot,
            new { basePrice, revertedToBasePrice = true, preservedPastUntil = shouldDeleteRecord ? (DateTime?)null : today.AddDays(-1) },
            affectedStart,
            affectedEnd,
            shouldDeleteRecord ? "Deleted price override" : "Trimmed future portion of straddling override; past history preserved");
    }

    public async Task<PagedResultDto<PricingAuditLogDto>> GetPricingAuditLogsAsync(GetPricingAuditLogsInputDto input)
    {
        await CheckHotelManagerAccessAsync(input.HotelId);

        var queryable = await _auditLogRepository.GetQueryableAsync();
        queryable = queryable.Where(x => x.HotelId == input.HotelId);

        if (input.RoomTypeId.HasValue)
        {
            queryable = queryable.Where(x => x.RoomTypeId == input.RoomTypeId.Value);
        }

        if (input.RatePlanId.HasValue)
        {
            queryable = queryable.Where(x => x.RatePlanId == input.RatePlanId.Value);
        }

        if (input.ActionType.HasValue)
        {
            queryable = queryable.Where(x => x.ActionType == input.ActionType.Value);
        }

        if (input.FromDate.HasValue)
        {
            var from = input.FromDate.Value.Date;
            queryable = queryable.Where(x => x.ActionTime >= from);
        }

        if (input.ToDate.HasValue)
        {
            var toExclusive = input.ToDate.Value.Date.AddDays(1);
            queryable = queryable.Where(x => x.ActionTime < toExclusive);
        }

        var totalCount = await AsyncExecuter.CountAsync(queryable);
        var maxCount = input.MaxResultCount <= 0 ? 20 : Math.Min(input.MaxResultCount, 100);
        var skipCount = Math.Max(0, input.SkipCount);

        var items = await AsyncExecuter.ToListAsync(
            queryable
                .OrderByDescending(x => x.ActionTime)
                .Skip(skipCount)
                .Take(maxCount));

        var dtos = items.Select(x => new PricingAuditLogDto
        {
            Id = x.Id,
            HotelId = x.HotelId,
            RoomTypeId = x.RoomTypeId,
            RoomTypeName = x.RoomTypeName,
            RatePlanId = x.RatePlanId,
            RatePlanCode = x.RatePlanCode,
            RatePlanName = x.RatePlanName,
            ActionType = x.ActionType,
            ActorId = x.ActorId,
            ActorName = x.ActorName,
            ActionTime = x.ActionTime,
            BeforeValueJson = x.BeforeValueJson,
            AfterValueJson = x.AfterValueJson,
            AffectedStartDate = x.AffectedStartDate,
            AffectedEndDate = x.AffectedEndDate,
            SummaryNote = x.SummaryNote
        }).ToList();

        return new PagedResultDto<PricingAuditLogDto>(totalCount, dtos);
    }

    private async Task<(Hotel Hotel, RoomType RoomType)> ValidateHotelAndRoomTypeAsync(Guid hotelId, Guid roomTypeId)
    {
        await CheckHotelManagerAccessAsync(hotelId);

        var hotel = await _hotelRepository.FindAsync(hotelId);
        if (hotel == null)
        {
            throw new BusinessException("BookingHotel:CrossHotelOperationForbidden");
        }

        var roomType = await _roomTypeRepository.FindAsync(roomTypeId);
        if (roomType == null || roomType.HotelId != hotelId)
        {
            throw new BusinessException("BookingHotel:CrossHotelOperationForbidden");
        }

        return (hotel, roomType);
    }

    private async Task<RatePlan> GetRequiredRatePlanAsync(Guid ratePlanId)
    {
        var masterPlan = await _ratePlanRepository.FindAsync(ratePlanId);
        if (masterPlan == null)
        {
            throw new BusinessException("BookingHotel:RatePlanNotFound");
        }

        return masterPlan;
    }

    private RoomRatePlanItemDto BuildRatePlanItemDto(
        RoomType roomType,
        RatePlan masterPlan,
        HotelRatePlan? branchConfig,
        List<RoomPriceOverride> planOverrides,
        DateTime today)
    {
        var isActive = branchConfig?.IsActive ?? true;
        var basePrice = _pricingDomainService.ResolveBranchBasePrice(
            roomType.BasePrice,
            masterPlan.DefaultPriceAdjustmentPercent,
            branchConfig?.CustomBasePrice);

        var todayOverride = planOverrides.FirstOrDefault(o => o.ContainsDate(today));
        var effectivePrice = _pricingDomainService.ResolveDailyEffectivePrice(basePrice, todayOverride);
        var isTodayOverridden = todayOverride != null &&
                                todayOverride.AdjustmentType != PriceAdjustmentType.RestrictionsOnly &&
                                todayOverride.CalculatedEffectivePrice.HasValue;

        return new RoomRatePlanItemDto
        {
            RatePlanId = masterPlan.Id,
            Code = masterPlan.Code,
            Name = masterPlan.Name,
            RatePlanType = masterPlan.IsRefundable ? "Refundable" : "NonRefundable",
            Description = masterPlan.Description,
            IsRefundable = masterPlan.IsRefundable,
            CancellationCutoffHours = masterPlan.CancellationCutoffHours,
            IncludesBreakfast = masterPlan.IncludesBreakfast,
            DefaultPriceAdjustmentPercent = masterPlan.DefaultPriceAdjustmentPercent,
            BasePrice = basePrice,
            EffectivePrice = effectivePrice,
            IsTodayOverridden = isTodayOverridden,
            IsActive = isActive,
            OverrideCount = planOverrides.Count(o => o.EndDate.Date >= today)
        };
    }

    private DailyPricingCellDto BuildDailyCellDto(
        DateTime date,
        DateTime today,
        decimal basePrice,
        bool isRatePlanActive,
        RoomPriceOverride? ov)
    {
        var effectivePrice = _pricingDomainService.ResolveDailyEffectivePrice(basePrice, ov);
        var isPriceOverridden = ov != null &&
                                ov.AdjustmentType != PriceAdjustmentType.RestrictionsOnly &&
                                ov.CalculatedEffectivePrice.HasValue;

        return new DailyPricingCellDto
        {
            ApplicableDate = date.Date,
            IsPastDate = date.Date < today.Date,
            IsToday = date.Date == today.Date,
            BasePrice = basePrice,
            EffectivePrice = effectivePrice,
            IsPriceOverridden = isPriceOverridden,
            HasRestrictionsOverride = ov != null && (ov.IsClosedToArrival || ov.IsClosedToDeparture),
            OverrideId = ov?.Id,
            AdjustmentType = ov?.AdjustmentType,
            AdjustmentValue = ov?.AdjustmentValue,
            IsClosedToArrival = ov?.IsClosedToArrival ?? false,
            IsClosedToDeparture = ov?.IsClosedToDeparture ?? false,
            IsOpenForSale = isRatePlanActive
        };
    }

    private static RoomPriceOverrideDto MapOverrideDto(RoomPriceOverride o, DateTime today)
    {
        return new RoomPriceOverrideDto
        {
            Id = o.Id,
            HotelId = o.HotelId,
            RoomTypeId = o.RoomTypeId,
            RatePlanId = o.RatePlanId,
            StartDate = o.StartDate.Date,
            EndDate = o.EndDate.Date,
            AdjustmentType = o.AdjustmentType,
            AdjustmentValue = o.AdjustmentValue,
            CalculatedEffectivePrice = o.CalculatedEffectivePrice,
            IsClosedToArrival = o.IsClosedToArrival,
            IsClosedToDeparture = o.IsClosedToDeparture,
            CreationTime = o.CreationTime,
            CreatorId = o.CreatorId,
            CreatorName = o.CreatorName,
            IsPastOnly = o.EndDate.Date < today.Date,
            IsStraddlingToday = o.StartDate.Date < today.Date && o.EndDate.Date >= today.Date
        };
    }

    private async Task RemoveSingleDateFromOverrideAsync(RoomPriceOverride ov, DateTime targetDate)
    {
        var target = targetDate.Date;
        var s = ov.StartDate.Date;
        var e = ov.EndDate.Date;

        if (s == target && e == target)
        {
            await _overrideRepository.DeleteAsync(ov, autoSave: true);
        }
        else if (s == target)
        {
            ov.UpdateDateRange(target.AddDays(1), e);
            await _overrideRepository.UpdateAsync(ov, autoSave: true);
        }
        else if (e == target)
        {
            ov.UpdateDateRange(s, target.AddDays(-1));
            await _overrideRepository.UpdateAsync(ov, autoSave: true);
        }
        else
        {
            // Split into left [s .. target - 1] and right [target + 1 .. e]
            ov.UpdateDateRange(s, target.AddDays(-1));
            await _overrideRepository.UpdateAsync(ov, autoSave: true);

            var rightSegment = new RoomPriceOverride(
                GuidGenerator.Create(),
                ov.HotelId,
                ov.RoomTypeId,
                ov.RatePlanId,
                target.AddDays(1),
                e,
                ov.AdjustmentType,
                ov.AdjustmentValue,
                ov.CalculatedEffectivePrice,
                ov.IsClosedToArrival,
                ov.IsClosedToDeparture,
                ov.CreatorName,
                ov.TenantId);

            await _overrideRepository.InsertAsync(rightSegment, autoSave: true);
        }
    }

    private async Task PersistResolutionPlanAsync(OverrideResolutionPlan plan)
    {
        if (plan.ToDelete.Count > 0)
        {
            foreach (var del in plan.ToDelete)
            {
                await _overrideRepository.DeleteAsync(del, autoSave: true);
            }
        }

        if (plan.ToUpdate.Count > 0)
        {
            foreach (var upd in plan.ToUpdate)
            {
                await _overrideRepository.UpdateAsync(upd, autoSave: true);
            }
        }

        if (plan.ToInsert.Count > 0)
        {
            foreach (var ins in plan.ToInsert)
            {
                await _overrideRepository.InsertAsync(ins, autoSave: true);
            }
        }
    }

    private object CreateOverrideAuditSnapshot(RoomPriceOverride ov, decimal basePrice)
    {
        return new
        {
            startDate = ov.StartDate.ToString("yyyy-MM-dd"),
            endDate = ov.EndDate.ToString("yyyy-MM-dd"),
            adjustmentType = ov.AdjustmentType.ToString(),
            adjustmentValue = ov.AdjustmentValue,
            effectivePrice = _pricingDomainService.ResolveDailyEffectivePrice(basePrice, ov),
            isClosedToArrival = ov.IsClosedToArrival,
            isClosedToDeparture = ov.IsClosedToDeparture
        };
    }

    private string GetCurrentActorName()
    {
        var fullName = $"{CurrentUser.Name} {CurrentUser.SurName}".Trim();
        if (!string.IsNullOrWhiteSpace(fullName))
        {
            return fullName;
        }

        return CurrentUser.UserName ?? CurrentUser.Email ?? "Manager";
    }

    private async Task WriteAuditLogAsync(
        Guid hotelId,
        RoomType roomType,
        RatePlan ratePlan,
        PricingAuditActionType actionType,
        object? beforeValue,
        object? afterValue,
        DateTime? affectedStartDate,
        DateTime? affectedEndDate,
        string? summaryNote)
    {
        var log = new PricingAuditLog(
            GuidGenerator.Create(),
            hotelId,
            roomType.Id,
            roomType.Name,
            ratePlan.Id,
            ratePlan.Code,
            ratePlan.Name,
            actionType,
            CurrentUser.Id ?? Guid.Empty,
            GetCurrentActorName(),
            Clock.Now,
            beforeValue != null ? JsonSerializer.Serialize(beforeValue, AuditJsonOptions) : null,
            afterValue != null ? JsonSerializer.Serialize(afterValue, AuditJsonOptions) : null,
            affectedStartDate,
            affectedEndDate,
            summaryNote,
            CurrentTenant.Id);

        await _auditLogRepository.InsertAsync(log, autoSave: true);
    }
}
