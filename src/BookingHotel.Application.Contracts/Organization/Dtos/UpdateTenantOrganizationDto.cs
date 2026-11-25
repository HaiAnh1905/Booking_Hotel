using System;
using System.ComponentModel.DataAnnotations;

namespace BookingHotel.Organization.Dtos;

public class UpdateTenantOrganizationDto
{
    [Required]
    [StringLength(256)]
    public string OrganizationName { get; set; } = string.Empty;

    [StringLength(1024)]
    public string LogoUrl { get; set; } = string.Empty;

    [StringLength(512)]
    public string HeadquarterAddress { get; set; } = string.Empty;

    [StringLength(32)]
    public string Hotline { get; set; } = string.Empty;

    [EmailAddress]
    [StringLength(256)]
    public string ContactEmail { get; set; } = string.Empty;

    [StringLength(512)]
    public string WebsiteUrl { get; set; } = string.Empty;

    public TimeSpan DefaultCheckInTime { get; set; } = new(14, 0, 0);

    public TimeSpan DefaultCheckOutTime { get; set; } = new(12, 0, 0);

    [StringLength(4000)]
    public string CancellationPolicyJson { get; set; } = string.Empty;
}
