using System;
using System.Threading.Tasks;
using BookingHotel.Auth;
using BookingHotel.Identity;
using Shouldly;
using Volo.Abp.Data;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Xunit;

namespace BookingHotel.Tests.Auth;

public abstract class AdminUnlockUser_Tests<TStartupModule> : BookingHotelApplicationTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    protected IAuthAppService AuthAppService { get; }
    protected IdentityUserManager UserManager { get; }

    protected AdminUnlockUser_Tests()
    {
        AuthAppService = GetRequiredService<IAuthAppService>();
        UserManager = GetRequiredService<IdentityUserManager>();
    }

    [Fact]
    public async Task Should_Unlock_Deactivated_User_And_Reset_Counters()
    {
        var email = "admin.unlock@hotel.com";
        var user = new IdentityUser(Guid.NewGuid(), email, email);
        user.SetIsActive(false);
        user.SetProperty(BookingHotelIdentityConsts.DeactivatedReasonProperty, BookingHotelIdentityConsts.DeactivatedReasonBruteForce);
        user.SetProperty(BookingHotelIdentityConsts.FailedAttemptsInCurrentCycleProperty, 3);
        user.SetProperty(BookingHotelIdentityConsts.TotalFailedAttemptsProperty, 24);
        user.SetProperty(BookingHotelIdentityConsts.CurrentLockoutMinutesProperty, 30);
        await UserManager.CreateAsync(user, "Password123!");

        await AuthAppService.UnlockUserAsync(user.Id);

        var restoredUser = await UserManager.FindByIdAsync(user.Id.ToString());
        restoredUser.ShouldNotBeNull();
        restoredUser.IsActive.ShouldBeTrue();
        restoredUser.GetProperty<int>(BookingHotelIdentityConsts.FailedAttemptsInCurrentCycleProperty).ShouldBe(0);
        restoredUser.GetProperty<int>(BookingHotelIdentityConsts.TotalFailedAttemptsProperty).ShouldBe(0);
        restoredUser.GetProperty<int>(BookingHotelIdentityConsts.CurrentLockoutMinutesProperty).ShouldBe(0);
        restoredUser.GetProperty<string?>(BookingHotelIdentityConsts.DeactivatedReasonProperty).ShouldBeNull();
    }
}

