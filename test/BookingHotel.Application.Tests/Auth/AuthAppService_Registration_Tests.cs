using System;
using System.Threading.Tasks;
using BookingHotel.Auth;
using BookingHotel.Auth.Dtos;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Xunit;

namespace BookingHotel.Tests.Auth;

public abstract class AuthAppService_Registration_Tests<TStartupModule> : BookingHotelApplicationTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    protected IAuthAppService AuthAppService { get; }
    protected IdentityUserManager UserManager { get; }

    protected AuthAppService_Registration_Tests()
    {
        AuthAppService = GetRequiredService<IAuthAppService>();
        UserManager = GetRequiredService<IdentityUserManager>();
    }

    [Fact]
    public async Task Should_Complete_3_Step_Registration_Successfully()
    {
        var email = "reg.wizard@hotel.com";
        var password = "StrongPassword123!";

        // Step 1
        var step1Res = await AuthAppService.RegisterStep1Async(new RegisterStep1InputDto
        {
            Email = email,
            Password = password,
            ConfirmPassword = password
        });
        step1Res.RegistrationToken.ShouldNotBe(Guid.Empty);
        step1Res.NextStep.ShouldBe("Step2");

        // Step 2
        await AuthAppService.RegisterStep2Async(new RegisterStep2InputDto
        {
            RegistrationToken = step1Res.RegistrationToken,
            FullName = "Nguyen Van A"
        });

        // Step 3
        var step3Res = await AuthAppService.RegisterStep3Async(new RegisterStep3InputDto
        {
            RegistrationToken = step1Res.RegistrationToken,
            PhoneNumber = "0912345678"
        });

        step3Res.IsSuccess.ShouldBeTrue();
        step3Res.AccessToken.ShouldNotBeNullOrWhiteSpace();

        // Verify user in Database
        var user = await UserManager.FindByEmailAsync(email);
        user.ShouldNotBeNull();
        user.Name.ShouldBe("Nguyen Van A");
        user.PhoneNumber.ShouldBe("0912345678");
    }

    [Fact]
    public async Task Should_Reject_Step2_With_Invalid_Token()
    {
        await Should.ThrowAsync<UserFriendlyException>(async () =>
        {
            await AuthAppService.RegisterStep2Async(new RegisterStep2InputDto
            {
                RegistrationToken = Guid.NewGuid(),
                FullName = "Hacker"
            });
        });
    }

    [Fact]
    public async Task Should_Reject_Step3_Skipping_Step2()
    {
        var email = "reg.skip@hotel.com";
        var password = "StrongPassword123!";

        var step1Res = await AuthAppService.RegisterStep1Async(new RegisterStep1InputDto
        {
            Email = email,
            Password = password,
            ConfirmPassword = password
        });

        // Skip Step 2 and call Step 3 directly
        await Should.ThrowAsync<UserFriendlyException>(async () =>
        {
            await AuthAppService.RegisterStep3Async(new RegisterStep3InputDto
            {
                RegistrationToken = step1Res.RegistrationToken,
                PhoneNumber = "0912345678"
            });
        });
    }

    [Fact]
    public async Task Should_Reject_Duplicate_Email_At_Step1()
    {
        var email = "reg.duplicate@hotel.com";
        var password = "StrongPassword123!";

        var user = new IdentityUser(Guid.NewGuid(), email, email);
        await UserManager.CreateAsync(user, password);

        await Should.ThrowAsync<UserFriendlyException>(async () =>
        {
            await AuthAppService.RegisterStep1Async(new RegisterStep1InputDto
            {
                Email = email,
                Password = password,
                ConfirmPassword = password
            });
        });
    }

    [Theory]
    [InlineData("short1!")] // < 8 ký tự
    [InlineData("nouppercase1!")] // thiếu chữ hoa
    [InlineData("NOLOWERCASE1!")] // thiếu chữ thường
    [InlineData("NoDigitSpecial!")] // thiếu chữ số
    [InlineData("NoSpecialChar123")] // thiếu ký tự đặc biệt
    public async Task Should_Reject_RegisterStep1_When_Password_Fails_Complexity(string weakPassword)
    {
        await Should.ThrowAsync<Volo.Abp.Validation.AbpValidationException>(async () =>
        {
            await AuthAppService.RegisterStep1Async(new RegisterStep1InputDto
            {
                Email = "weak.pwd@hotel.com",
                Password = weakPassword,
                ConfirmPassword = weakPassword
            });
        });
    }
}

