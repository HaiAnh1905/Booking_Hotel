using System;
using System.Threading.Tasks;
using BookingHotel.Auth;
using BookingHotel.Auth.Dtos;
using BookingHotel.Identity;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Data;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Volo.Abp.Uow;
using Xunit;

namespace BookingHotel.Tests.Auth;

public abstract class AuthAppService_PasswordReset_Tests<TStartupModule> : BookingHotelApplicationTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    protected IAuthAppService AuthAppService { get; }
    protected IdentityUserManager UserManager { get; }
    protected IUnitOfWorkManager UnitOfWorkManager { get; }

    protected AuthAppService_PasswordReset_Tests()
    {
        AuthAppService = GetRequiredService<IAuthAppService>();
        UserManager = GetRequiredService<IdentityUserManager>();
        UnitOfWorkManager = GetRequiredService<IUnitOfWorkManager>();
    }

    [Fact]
    public async Task Should_Reject_Reset_Request_When_Captcha_Is_Invalid()
    {
        await Should.ThrowAsync<UserFriendlyException>(async () =>
        {
            await AuthAppService.SendPasswordResetCodeAsync(new ForgotPasswordInputDto
            {
                Email = "someone@hotel.com",
                CaptchaToken = "INVALID_CAPTCHA_TOKEN"
            });
        });
    }

    [Fact]
    public async Task Should_Reset_Password_With_Valid_Token_And_Clear_Lockout()
    {
        var email = "reset.pass@hotel.com";
        var oldPassword = "OldPassword123!";
        var newPassword = "NewPassword123!";

        var user = new IdentityUser(Guid.NewGuid(), email, email);
        user.SetProperty(BookingHotelIdentityConsts.FailedAttemptsInCurrentCycleProperty, 3);
        user.SetProperty(BookingHotelIdentityConsts.CurrentLockoutMinutesProperty, 5);
        await UserManager.CreateAsync(user, oldPassword);

        string token;
        using (var uow = UnitOfWorkManager.Begin())
        {
            var freshUser = await UserManager.FindByEmailAsync(email);
            token = await UserManager.GeneratePasswordResetTokenAsync(freshUser!);
            await uow.CompleteAsync();
        }

        await AuthAppService.ResetPasswordAsync(new ResetPasswordInputDto
        {
            UserId = user.Id.ToString(),
            ResetToken = token,
            NewPassword = newPassword,
            ConfirmNewPassword = newPassword
        });

        // Verify new password works
        var updatedUser = await UserManager.FindByIdAsync(user.Id.ToString());
        updatedUser.ShouldNotBeNull();
        var isNewPassCorrect = await UserManager.CheckPasswordAsync(updatedUser, newPassword);
        isNewPassCorrect.ShouldBeTrue();

        // Verify brute-force counters cleared
        updatedUser.GetProperty<int>(BookingHotelIdentityConsts.FailedAttemptsInCurrentCycleProperty).ShouldBe(0);
        updatedUser.GetProperty<int>(BookingHotelIdentityConsts.CurrentLockoutMinutesProperty).ShouldBe(0);
    }

    [Theory]
    [InlineData("short1!")] // < 8 ký tự
    [InlineData("nouppercase1!")] // thiếu chữ hoa
    [InlineData("NOLOWERCASE1!")] // thiếu chữ thường
    [InlineData("NoDigitSpecial!")] // thiếu chữ số
    [InlineData("NoSpecialChar123")] // thiếu ký tự đặc biệt
    public async Task Should_Reject_ResetPassword_When_Password_Fails_Complexity(string weakPassword)
    {
        await Should.ThrowAsync<Volo.Abp.Validation.AbpValidationException>(async () =>
        {
            await AuthAppService.ResetPasswordAsync(new ResetPasswordInputDto
            {
                UserId = Guid.NewGuid().ToString(),
                ResetToken = "dummy-token",
                NewPassword = weakPassword,
                ConfirmNewPassword = weakPassword
            });
        });
    }
}
