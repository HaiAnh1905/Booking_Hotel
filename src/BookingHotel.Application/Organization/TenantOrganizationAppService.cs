using System;
using System.Threading.Tasks;
using BookingHotel.Organization.Dtos;
using BookingHotel.Permissions;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace BookingHotel.Organization;

[RemoteService(IsEnabled = false)]
[Authorize]
public class TenantOrganizationAppService : ApplicationService, ITenantOrganizationAppService
{
    private readonly IRepository<TenantOrganizationProfile, Guid> _profileRepository;

    public TenantOrganizationAppService(IRepository<TenantOrganizationProfile, Guid> profileRepository)
    {
        _profileRepository = profileRepository;
    }

    [Authorize(BookingHotelPermissions.Hotels.Default)]
    public async Task<TenantOrganizationProfileDto> GetProfileAsync()
    {
        var profile = await _profileRepository.FirstOrDefaultAsync(p => p.TenantId == CurrentTenant.Id);
        if (profile == null)
        {
            return new TenantOrganizationProfileDto
            {
                TenantId = CurrentTenant.Id,
                OrganizationName = CurrentTenant.Name ?? "Booking Hotel Chain Vietnam",
                LogoUrl = "/assets/images/logo.png",
                HeadquarterAddress = "123 Phố Tràng Tiền, Hoàn Kiếm, Hà Nội",
                Hotline = "1900 6868",
                ContactEmail = "contact@bookinghotel.vn",
                WebsiteUrl = "https://bookinghotel.vn",
                DefaultCheckInTime = new TimeSpan(14, 0, 0),
                DefaultCheckOutTime = new TimeSpan(12, 0, 0),
                CancellationPolicyJson = "{\"freeCancellationHours\":48,\"partialPenaltyHours\":24,\"partialPenaltyPercent\":50,\"fullPenaltyHours\":0,\"fullPenaltyPercent\":100}"
            };
        }

        return MapToDto(profile);
    }

    [Authorize(BookingHotelPermissions.Hotels.ManageBranches)]
    public async Task<TenantOrganizationProfileDto> UpdateProfileAsync(UpdateTenantOrganizationDto input)
    {
        var profile = await _profileRepository.FirstOrDefaultAsync(p => p.TenantId == CurrentTenant.Id);
        if (profile == null)
        {
            profile = new TenantOrganizationProfile(
                GuidGenerator.Create(),
                input.OrganizationName.Trim(),
                input.LogoUrl?.Trim() ?? string.Empty,
                input.HeadquarterAddress?.Trim() ?? string.Empty,
                input.Hotline?.Trim() ?? string.Empty,
                input.ContactEmail?.Trim() ?? string.Empty,
                input.WebsiteUrl?.Trim() ?? string.Empty,
                input.DefaultCheckInTime,
                input.DefaultCheckOutTime,
                input.CancellationPolicyJson?.Trim() ?? string.Empty,
                CurrentTenant.Id
            );
            await _profileRepository.InsertAsync(profile, autoSave: true);
        }
        else
        {
            profile.OrganizationName = input.OrganizationName.Trim();
            profile.LogoUrl = input.LogoUrl?.Trim() ?? string.Empty;
            profile.HeadquarterAddress = input.HeadquarterAddress?.Trim() ?? string.Empty;
            profile.Hotline = input.Hotline?.Trim() ?? string.Empty;
            profile.ContactEmail = input.ContactEmail?.Trim() ?? string.Empty;
            profile.WebsiteUrl = input.WebsiteUrl?.Trim() ?? string.Empty;
            profile.DefaultCheckInTime = input.DefaultCheckInTime;
            profile.DefaultCheckOutTime = input.DefaultCheckOutTime;
            profile.CancellationPolicyJson = input.CancellationPolicyJson?.Trim() ?? string.Empty;
            await _profileRepository.UpdateAsync(profile, autoSave: true);
        }

        return MapToDto(profile);
    }

    private static TenantOrganizationProfileDto MapToDto(TenantOrganizationProfile profile)
    {
        return new TenantOrganizationProfileDto
        {
            Id = profile.Id,
            TenantId = profile.TenantId,
            OrganizationName = profile.OrganizationName,
            LogoUrl = profile.LogoUrl,
            HeadquarterAddress = profile.HeadquarterAddress,
            Hotline = profile.Hotline,
            ContactEmail = profile.ContactEmail,
            WebsiteUrl = profile.WebsiteUrl,
            DefaultCheckInTime = profile.DefaultCheckInTime,
            DefaultCheckOutTime = profile.DefaultCheckOutTime,
            CancellationPolicyJson = profile.CancellationPolicyJson
        };
    }
}
