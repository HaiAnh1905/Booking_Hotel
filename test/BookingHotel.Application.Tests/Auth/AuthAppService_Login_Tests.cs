using System;
using System.Threading.Tasks;
using BookingHotel.Auth;
using BookingHotel.Auth.Dtos;
using BookingHotel.Identity;
using Shouldly;
using Volo.Abp.Data;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Xunit;

namespace BookingHotel.Tests.Auth;

public abstract class AuthAppService_Login_Tests<TStartupModule> : BookingHotelApplicationTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    protected IAuthAppService AuthAppService { get; }
    protected IdentityUserManager UserManager { get; }

    protected AuthAppService_Login_Tests()
    {
        AuthAppService = GetRequiredService<IAuthAppService>();
        UserManager = GetRequiredService<IdentityUserManager>();
    }

    protected async Task<IdentityUser> CreateTestUserAsync(string email, string password)
    {
        var user = new IdentityUser(
            Guid.NewGuid(),
            email,
            email
        );
        user.SetProperty(BookingHotelIdentityConsts.FailedAttemptsInCurrentCycleProperty, 0);
        user.SetProperty(BookingHotelIdentityConsts.TotalFailedAttemptsProperty, 0);
        user.SetProperty(BookingHotelIdentityConsts.CurrentLockoutMinutesProperty, 0);

        var result = await UserManager.CreateAsync(user, password);
        result.Succeeded.ShouldBeTrue();
        return user;
    }

    [Fact]
    public async Task Should_Login_Successfully_With_Valid_Credentials()
    {
        var email = "login.success@hotel.com";
        var password = "Password123!";
        await CreateTestUserAsync(email, password);

        var result = await AuthAppService.LoginWithEmailAsync(new LoginWithEmailDto
        {
            Email = email,
            Password = password
        });

        result.IsSuccess.ShouldBeTrue();
        result.AccessToken.ShouldNotBeNullOrWhiteSpace();
        result.ExpiresIn.ShouldBeGreaterThan(0);
        result.IsLockedOut.ShouldBeFalse();
        result.IsDeactivated.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Return_Remaining_Attempts_On_Failed_Password()
    {
        var email = "login.fail@hotel.com";
        var password = "CorrectPassword123!";
        await CreateTestUserAsync(email, password);

        var result = await AuthAppService.LoginWithEmailAsync(new LoginWithEmailDto
        {
            Email = email,
            Password = "WrongPassword!"
        });

        result.IsSuccess.ShouldBeFalse();
        result.IsLockedOut.ShouldBeFalse();
        result.RemainingAttemptsInCycle.ShouldBe(4);
    }

    [Fact]
    public async Task Should_Lockout_5_Minutes_On_5th_Failure()
    {
        var email = "login.lock5@hotel.com";
        var password = "CorrectPassword123!";
        await CreateTestUserAsync(email, password);

        LoginResultDto lastResult = default!;
        for (int i = 1; i <= 5; i++)
        {
            lastResult = await AuthAppService.LoginWithEmailAsync(new LoginWithEmailDto
            {
                Email = email,
                Password = "WrongPassword!"
            });
        }

        lastResult.IsSuccess.ShouldBeFalse();
        lastResult.IsLockedOut.ShouldBeTrue();
        lastResult.RemainingLockoutSeconds.ShouldNotBeNull();
        lastResult.RemainingLockoutSeconds.Value.ShouldBeGreaterThan(0);
    }
}

