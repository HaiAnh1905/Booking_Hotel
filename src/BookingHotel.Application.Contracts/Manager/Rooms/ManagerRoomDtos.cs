using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using BookingHotel.Rooms;

namespace BookingHotel.Manager.Rooms;

public class RoomTypeDto
{
    public Guid Id { get; set; }
    public Guid HotelId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int StandardOccupancy { get; set; }
    public int MaxOccupancy { get; set; }
    public decimal BasePrice { get; set; }
    public double SizeSquareMeters { get; set; }
    public List<string> Amenities { get; set; } = new();
    public int TotalRoomCount { get; set; }
}

public class CreateUpdateRoomTypeDto
{
    [Required]
    public Guid HotelId { get; set; }

    [Required]
    [StringLength(128)]
    public string Name { get; set; } = string.Empty;

    [StringLength(2048)]
    public string Description { get; set; } = string.Empty;

    [Range(1, 20)]
    public int StandardOccupancy { get; set; } = 2;

    [Range(1, 30)]
    public int MaxOccupancy { get; set; } = 3;

    [Range(0, 1000000000)]
    public decimal BasePrice { get; set; }

    [Range(0, 1000)]
    public double SizeSquareMeters { get; set; } = 30.0;

    public List<string> Amenities { get; set; } = new();
}

public class RoomDto
{
    public Guid Id { get; set; }
    public Guid HotelId { get; set; }
    public Guid RoomTypeId { get; set; }
    public string RoomTypeName { get; set; } = string.Empty;
    public string RoomNumber { get; set; } = string.Empty;
    public int Floor { get; set; }
    public RoomOperationalStatus OperationalStatus { get; set; }
    public RoomOccupancyStatus OccupancyStatus { get; set; }
    public HousekeepingStatus HousekeepingStatus { get; set; }
    public bool IsActive { get; set; }
    public string? Building { get; set; }
    public string? Description { get; set; }
    public bool IsSellable { get; set; }
    public string? CurrentOccupantName { get; set; }
    public string? MaintenanceReason { get; set; }
    public DateTime? MaintenanceEndDate { get; set; }
    public string? OutOfServiceReason { get; set; }
    public DateTime? OutOfServiceEndDate { get; set; }
}

public class CreateUpdateRoomDto
{
    [Required]
    public Guid HotelId { get; set; }

    [Required]
    public Guid RoomTypeId { get; set; }

    [Required]
    [StringLength(32)]
    public string RoomNumber { get; set; } = string.Empty;

    [Range(1, 200)]
    public int Floor { get; set; } = 1;

    [StringLength(64)]
    public string? Building { get; set; }

    [StringLength(2048)]
    public string? Description { get; set; }
}

public class DeactivateRoomDto
{
    [Required]
    public Guid HotelId { get; set; }

    [StringLength(500)]
    public string? Reason { get; set; }
}

public class ChangeOperationalStatusDto
{
    [Required]
    public Guid HotelId { get; set; }

    [Required]
    public RoomOperationalStatus TargetStatus { get; set; }

    [StringLength(1000)]
    public string? Reason { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? ExpectedEndDate { get; set; }
}

public class UpdateHousekeepingStatusDto
{
    [Required]
    public Guid HotelId { get; set; }

    [Required]
    public HousekeepingStatus TargetStatus { get; set; }
}

public class RatePlanDto
{
    public Guid RatePlanId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsRefundable { get; set; }
    public bool IncludesBreakfast { get; set; }
    public bool IsActiveAtHotel { get; set; }
    public decimal? CustomBasePrice { get; set; }
    public decimal PriceMultiplier { get; set; } = 1.0m;
}

public class ToggleHotelRatePlanDto
{
    [Required]
    public Guid HotelId { get; set; }

    [Required]
    public Guid RoomTypeId { get; set; }

    [Required]
    public Guid RatePlanId { get; set; }

    public bool IsActive { get; set; }

    public decimal? CustomBasePrice { get; set; }

    public decimal PriceMultiplier { get; set; } = 1.0m;
}

public class DailyRateOverrideDto
{
    public Guid Id { get; set; }
    public Guid HotelId { get; set; }
    public Guid RoomTypeId { get; set; }
    public DateTime Date { get; set; }
    public decimal OverridePrice { get; set; }
    public bool IsClosedToArrival { get; set; }
    public bool IsClosedToDeparture { get; set; }
}

public class BatchOverrideDailyRatesDto
{
    [Required]
    public Guid HotelId { get; set; }

    [Required]
    public Guid RoomTypeId { get; set; }

    [Required]
    public DateTime FromDate { get; set; }

    [Required]
    public DateTime ToDate { get; set; }

    [Range(0, 1000000000)]
    public decimal OverridePrice { get; set; }

    public bool IsClosedToArrival { get; set; }

    public bool IsClosedToDeparture { get; set; }
}
