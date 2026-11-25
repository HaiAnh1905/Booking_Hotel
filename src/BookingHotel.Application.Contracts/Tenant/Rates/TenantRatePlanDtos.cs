using System;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;

namespace BookingHotel.TenantRates;

public class CreateUpdateRatePlanDto
{
    [Required]
    [StringLength(128)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(32)]
    public string Code { get; set; } = string.Empty;

    [StringLength(512)]
    public string Description { get; set; } = string.Empty;

    public bool IsRefundable { get; set; } = true;

    public bool IncludesBreakfast { get; set; }

    [Range(0, 720)]
    public int CancellationCutoffHours { get; set; } = 24;

    [Range(-99, 500)]
    public decimal DefaultPriceAdjustmentPercent { get; set; }
}

public class RatePlanDto : FullAuditedEntityDto<Guid>
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsRefundable { get; set; }
    public bool IncludesBreakfast { get; set; }
    public int CancellationCutoffHours { get; set; }
    public decimal DefaultPriceAdjustmentPercent { get; set; }
}
