using System;
using Volo.Abp.Application.Dtos;

namespace BookingHotel.Organization.Dtos;

public class TenantOrganizationProfileDto : EntityDto<Guid>
{
    public Guid? TenantId { get; set; }
    public string OrganizationName { get; set; } = string.Empty;
    public string LogoUrl { get; set; } = string.Empty;
    public string HeadquarterAddress { get; set; } = string.Empty;
    public string Hotline { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string WebsiteUrl { get; set; } = string.Empty;
    public TimeSpan DefaultCheckInTime { get; set; }
    public TimeSpan DefaultCheckOutTime { get; set; }
    public string CancellationPolicyJson { get; set; } = string.Empty;
}
