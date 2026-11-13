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

public abstract class AuthAppService_GoogleAuth_Tests<TStartupModule> : BookingHotelApplicationTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    protected IAuthAppService AuthAppService { get; }
    protected IdentityUserManager UserManager { get; }

    protected AuthAppService_GoogleAuth_Tests()
    {
        AuthAppService = GetRequiredService<IAuthAppService>();
        UserManager = GetRequiredService<IdentityUserManager>();
    }

    [Fact]
    public async Task Should_Login_Existing_User_With_Google()
    {
        var email = "google.existing@hotel.com";
        var user = new IdentityUser(Guid.NewGuid(), email, email);
        user.SetProperty(BookingHotelIdentityConsts.GoogleIdProperty, "google-sub-12345");
        await UserManager.CreateAsync(user);

        var result = await AuthAppService.LoginWithGoogleAsync(new GoogleAuthInputDto
        {
            IdToken = email
        });

        result.IsSuccess.ShouldBeTrue();
        result.AccessToken.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Should_Return_Not_Registered_When_Google_User_Does_Not_Exist()
    {
        var email = "google.newbie@hotel.com";

        var result = await AuthAppService.LoginWithGoogleAsync(new GoogleAuthInputDto
        {
            IdToken = email
        });

        result.IsSuccess.ShouldBeFalse();
        result.Message.ShouldBe("UserNotRegistered");
        result.RequiresRegistration.ShouldBeTrue();
        result.RegistrationToken.ShouldNotBeNull();
        result.Email.ShouldBe(email);
    }

    [Fact]
    public async Task Should_Initialize_Onboarding_For_New_Google_User()
    {
        var email = "google.onboarding@hotel.com";

        var result = await AuthAppService.RegisterGoogleInitAsync(new GoogleAuthInputDto
        {
            IdToken = email
        });

        result.RegistrationToken.ShouldNotBe(Guid.Empty);
        result.Email.ShouldBe(email);
        result.NextStep.ShouldBe("Step2");
    }
}

