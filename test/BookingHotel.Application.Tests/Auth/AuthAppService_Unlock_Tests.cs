using System;
using System.Threading.Tasks;
using BookingHotel.Auth;
using BookingHotel.Identity;
using Shouldly;
using Volo.Abp.Authorization;
using Volo.Abp.Data;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Volo.Abp.MultiTenancy;
using Xunit;

namespace BookingHotel.Tests.Auth;

public abstract class AuthAppService_Unlock_Tests<TStartupModule> : BookingHotelApplicationTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    protected IAuthAppService AuthAppService { get; }
    protected IdentityUserManager UserManager { get; }
    protected ICurrentTenant CurrentTenant { get; }

    protected AuthAppService_Unlock_Tests()
    {
        AuthAppService = GetRequiredService<IAuthAppService>();
        UserManager = GetRequiredService<IdentityUserManager>();
        CurrentTenant = GetRequiredService<ICurrentTenant>();
    }

    [Fact]
    public async Task Tenant_Admin_Should_Unlock_Internal_Staff_And_Fail_On_Host_User()
    {
        var tenantId = Guid.NewGuid();

        // 1. Create a tenant user locked out
        IdentityUser tenantUser;
        using (CurrentTenant.Change(tenantId))
        {
            tenantUser = new IdentityUser(Guid.NewGuid(), "staff.hotel@chain.com", "staff.hotel@chain.com", tenantId);
            tenantUser.SetIsActive(false);
            tenantUser.SetProperty(BookingHotelIdentityConsts.DeactivatedReasonProperty, BookingHotelIdentityConsts.DeactivatedReasonBruteForce);
            await UserManager.CreateAsync(tenantUser, "Password123!");
        }

        // 2. Create a host user locked out
        IdentityUser hostUser;
        using (CurrentTenant.Change(null))
        {
            hostUser = new IdentityUser(Guid.NewGuid(), "host.admin@booking.com", "host.admin@booking.com", tenantId: null);
            hostUser.SetIsActive(false);
            hostUser.SetProperty(BookingHotelIdentityConsts.DeactivatedReasonProperty, BookingHotelIdentityConsts.DeactivatedReasonBruteForce);
            await UserManager.CreateAsync(hostUser, "Password123!");
        }

        // 3. In Tenant context: Tenant Admin unlocks internal staff -> SUCCESS
        using (CurrentTenant.Change(tenantId))
        {
            await AuthAppService.UnlockUserAsync(tenantUser.Id);

            var refreshedTenantUser = await UserManager.FindByIdAsync(tenantUser.Id.ToString());
            refreshedTenantUser.ShouldNotBeNull();
            refreshedTenantUser.IsActive.ShouldBeTrue();

            // Tenant Admin attempts to unlock Host user -> Should throw AbpAuthorizationException
            await Should.ThrowAsync<AbpAuthorizationException>(async () =>
            {
                await AuthAppService.UnlockUserAsync(hostUser.Id);
            });
        }
    }

    [Fact]
    public async Task Host_Admin_Should_Unlock_Host_User_And_Fail_On_Tenant_User_Without_Context()
    {
        var tenantId = Guid.NewGuid();

        IdentityUser hostUser;
        using (CurrentTenant.Change(null))
        {
            hostUser = new IdentityUser(Guid.NewGuid(), "sys.admin@booking.com", "sys.admin@booking.com", tenantId: null);
            hostUser.SetIsActive(false);
            hostUser.SetProperty(BookingHotelIdentityConsts.DeactivatedReasonProperty, BookingHotelIdentityConsts.DeactivatedReasonBruteForce);
            await UserManager.CreateAsync(hostUser, "Password123!");
        }

        IdentityUser tenantUser;
        using (CurrentTenant.Change(tenantId))
        {
            tenantUser = new IdentityUser(Guid.NewGuid(), "tenant.staff@hotel.com", "tenant.staff@hotel.com", tenantId);
            tenantUser.SetIsActive(false);
            tenantUser.SetProperty(BookingHotelIdentityConsts.DeactivatedReasonProperty, BookingHotelIdentityConsts.DeactivatedReasonBruteForce);
            await UserManager.CreateAsync(tenantUser, "Password123!");
        }

        // In Host context
        using (CurrentTenant.Change(null))
        {
            await AuthAppService.UnlockUserAsync(hostUser.Id);

            var refreshed = await UserManager.FindByIdAsync(hostUser.Id.ToString());
            refreshed.ShouldNotBeNull();
            refreshed.IsActive.ShouldBeTrue();

            // Host Admin directly targeting Tenant staff without tenant switching -> Should throw AbpAuthorizationException
            await Should.ThrowAsync<AbpAuthorizationException>(async () =>
            {
                await AuthAppService.UnlockUserAsync(tenantUser.Id);
            });
        }
    }
}
