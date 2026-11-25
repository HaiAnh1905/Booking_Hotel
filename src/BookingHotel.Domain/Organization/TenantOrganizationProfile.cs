using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace BookingHotel.Organization;

public class TenantOrganizationProfile : FullAuditedEntity<Guid>, IMultiTenant
{
    public virtual Guid? TenantId { get; set; }

    public virtual string OrganizationName { get; set; } = string.Empty;

    public virtual string LogoUrl { get; set; } = string.Empty;

    public virtual string HeadquarterAddress { get; set; } = string.Empty;

    public virtual string Hotline { get; set; } = string.Empty;

    public virtual string ContactEmail { get; set; } = string.Empty;

    public virtual string WebsiteUrl { get; set; } = string.Empty;

    public virtual TimeSpan DefaultCheckInTime { get; set; } = new(14, 0, 0);

    public virtual TimeSpan DefaultCheckOutTime { get; set; } = new(12, 0, 0);

    public virtual string CancellationPolicyJson { get; set; } = string.Empty;

    protected TenantOrganizationProfile()
    {
    }

    public TenantOrganizationProfile(
        Guid id,
        string organizationName,
        string logoUrl = "",
        string headquarterAddress = "",
        string hotline = "",
        string contactEmail = "",
        string websiteUrl = "",
        TimeSpan? defaultCheckInTime = null,
        TimeSpan? defaultCheckOutTime = null,
        string cancellationPolicyJson = "",
        Guid? tenantId = null)
        : base(id)
    {
        OrganizationName = organizationName;
        LogoUrl = logoUrl;
        HeadquarterAddress = headquarterAddress;
        Hotline = hotline;
        ContactEmail = contactEmail;
        WebsiteUrl = websiteUrl;
        DefaultCheckInTime = defaultCheckInTime ?? new TimeSpan(14, 0, 0);
        DefaultCheckOutTime = defaultCheckOutTime ?? new TimeSpan(12, 0, 0);
        CancellationPolicyJson = cancellationPolicyJson;
        TenantId = tenantId;
    }
}
