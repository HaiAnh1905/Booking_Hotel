using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using BookingHotel.Rooms;
using Volo.Abp.Application.Dtos;

namespace BookingHotel.Manager.Rooms;

public class RoomTypePricingContextDto
{
    public Guid HotelId { get; set; }
    public string HotelName { get; set; } = string.Empty;
    public string HotelCode { get; set; } = string.Empty;
    public Guid RoomTypeId { get; set; }
    public string RoomTypeName { get; set; } = string.Empty;
    public decimal ReferenceBasePrice { get; set; }
    public int RatePlanCount { get; set; }
    public int ActiveRatePlanCount { get; set; }
    public int ActiveOverrideCount { get; set; }
    public List<RoomRatePlanItemDto> RatePlans { get; set; } = new();
}

public class RoomRatePlanItemDto
{
    public Guid RatePlanId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string RatePlanType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsRefundable { get; set; }
    public int CancellationCutoffHours { get; set; }
    public bool IncludesBreakfast { get; set; }
    public decimal DefaultPriceAdjustmentPercent { get; set; }
    public decimal BasePrice { get; set; }
    public decimal EffectivePrice { get; set; }
    public bool IsTodayOverridden { get; set; }
    public bool IsActive { get; set; }
    public int OverrideCount { get; set; }
}

public class ToggleRoomRatePlanStatusDto
{
    [Required]
    public Guid HotelId { get; set; }

    [Required]
    public Guid RoomTypeId { get; set; }

    [Required]
    public Guid RatePlanId { get; set; }

    public bool IsActive { get; set; }
}

public class UpdateRoomRatePlanBasePriceDto
{
    [Required]
    public Guid HotelId { get; set; }

    [Required]
    public Guid RoomTypeId { get; set; }

    [Required]
    public Guid RatePlanId { get; set; }

    [Required]
    [Range(1, 1000000000)]
    public decimal BasePrice { get; set; }
}

public class MonthlyPricingCalendarDto
{
    public Guid HotelId { get; set; }
    public Guid RoomTypeId { get; set; }
    public Guid RatePlanId { get; set; }
    public string RatePlanName { get; set; } = string.Empty;
    public string RatePlanCode { get; set; } = string.Empty;
    public bool IsRatePlanActive { get; set; }
    public decimal BasePrice { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public List<DailyPricingCellDto> Days { get; set; } = new();
}

public class DailyPricingCellDto
{
    public DateTime ApplicableDate { get; set; }
    public bool IsPastDate { get; set; }
    public bool IsToday { get; set; }
    public decimal BasePrice { get; set; }
    public decimal EffectivePrice { get; set; }
    public bool IsPriceOverridden { get; set; }
    public bool HasRestrictionsOverride { get; set; }
    public Guid? OverrideId { get; set; }
    public PriceAdjustmentType? AdjustmentType { get; set; }
    public decimal? AdjustmentValue { get; set; }
    public bool IsClosedToArrival { get; set; }
    public bool IsClosedToDeparture { get; set; }
    public bool IsOpenForSale { get; set; }
}

public class QuickEditDailyPricingDto
{
    [Required]
    public Guid HotelId { get; set; }

    [Required]
    public Guid RoomTypeId { get; set; }

    [Required]
    public Guid RatePlanId { get; set; }

    [Required]
    public DateTime ApplicableDate { get; set; }

    [Required]
    public PriceAdjustmentType AdjustmentType { get; set; } = PriceAdjustmentType.FixedPrice;

    public decimal? AdjustmentValue { get; set; }

    public bool IsClosedToArrival { get; set; }

    public bool IsClosedToDeparture { get; set; }

    public bool ClearOverride { get; set; }
}

public class BulkPriceAdjustmentPreviewInputDto
{
    [Required]
    public Guid HotelId { get; set; }

    [Required]
    public Guid RoomTypeId { get; set; }

    [Required]
    [MinLength(1)]
    public List<Guid> RatePlanIds { get; set; } = new();

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    public List<DayOfWeek>? DaysOfWeek { get; set; }

    [Required]
    public PriceAdjustmentType AdjustmentType { get; set; } = PriceAdjustmentType.FixedPrice;

    public decimal? AdjustmentValue { get; set; }

    public bool IsClosedToArrival { get; set; }

    public bool IsClosedToDeparture { get; set; }
}

public class BulkPriceAdjustmentPreviewDto
{
    public string HotelName { get; set; } = string.Empty;
    public string RoomTypeName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public List<DayOfWeek> DaysOfWeek { get; set; } = new();
    public PriceAdjustmentType AdjustmentType { get; set; }
    public decimal? AdjustmentValue { get; set; }
    public bool IsClosedToArrival { get; set; }
    public bool IsClosedToDeparture { get; set; }
    public int TotalAffectedDaysAcrossPlans { get; set; }
    public bool HasAnyConflicts { get; set; }
    public List<RatePlanBulkPreviewItemDto> RatePlanPreviews { get; set; } = new();
}

public class RatePlanBulkPreviewItemDto
{
    public Guid RatePlanId { get; set; }
    public string RatePlanName { get; set; } = string.Empty;
    public string RatePlanCode { get; set; } = string.Empty;
    public decimal CurrentBasePrice { get; set; }
    public decimal? ProjectedEffectivePrice { get; set; }
    public int MatchingDatesCount { get; set; }
    public List<DateTime> ConflictingDates { get; set; } = new();
    public int NewDatesCount { get; set; }
}

public class ApplyBulkPriceAdjustmentDto : BulkPriceAdjustmentPreviewInputDto
{
    public PriceConflictResolution ConflictResolution { get; set; } = PriceConflictResolution.Cancel;
}

public class BulkPriceAdjustmentResultDto
{
    public int AppliedRatePlanCount { get; set; }
    public int AffectedDaysCount { get; set; }
    public int ReplacedDaysCount { get; set; }
    public int SkippedDaysCount { get; set; }
    public List<Guid> CreatedOverrideIds { get; set; } = new();
}

public class RoomPriceOverrideDto
{
    public Guid Id { get; set; }
    public Guid HotelId { get; set; }
    public Guid RoomTypeId { get; set; }
    public Guid RatePlanId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public PriceAdjustmentType AdjustmentType { get; set; }
    public decimal? AdjustmentValue { get; set; }
    public decimal? CalculatedEffectivePrice { get; set; }
    public bool IsClosedToArrival { get; set; }
    public bool IsClosedToDeparture { get; set; }
    public DateTime CreationTime { get; set; }
    public Guid? CreatorId { get; set; }
    public string CreatorName { get; set; } = string.Empty;
    public bool IsPastOnly { get; set; }
    public bool IsStraddlingToday { get; set; }
}

public class UpdateRoomPriceOverrideDto
{
    [Required]
    public Guid HotelId { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    [Required]
    public PriceAdjustmentType AdjustmentType { get; set; } = PriceAdjustmentType.FixedPrice;

    public decimal? AdjustmentValue { get; set; }

    public bool IsClosedToArrival { get; set; }

    public bool IsClosedToDeparture { get; set; }

    public PriceConflictResolution ConflictResolution { get; set; } = PriceConflictResolution.Cancel;
}

public class GetPricingAuditLogsInputDto : PagedResultRequestDto
{
    [Required]
    public Guid HotelId { get; set; }

    public Guid? RoomTypeId { get; set; }

    public Guid? RatePlanId { get; set; }

    public PricingAuditActionType? ActionType { get; set; }

    public DateTime? FromDate { get; set; }

    public DateTime? ToDate { get; set; }
}

public class PricingAuditLogDto
{
    public Guid Id { get; set; }
    public Guid HotelId { get; set; }
    public Guid RoomTypeId { get; set; }
    public string RoomTypeName { get; set; } = string.Empty;
    public Guid RatePlanId { get; set; }
    public string RatePlanCode { get; set; } = string.Empty;
    public string RatePlanName { get; set; } = string.Empty;
    public PricingAuditActionType ActionType { get; set; }
    public Guid ActorId { get; set; }
    public string ActorName { get; set; } = string.Empty;
    public DateTime ActionTime { get; set; }
    public string? BeforeValueJson { get; set; }
    public string? AfterValueJson { get; set; }
    public DateTime? AffectedStartDate { get; set; }
    public DateTime? AffectedEndDate { get; set; }
    public string? SummaryNote { get; set; }
}
