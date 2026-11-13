using System;
using System.Threading.Tasks;
using BookingHotel.Identity;
using Shouldly;
using Volo.Abp.Data;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Xunit;

namespace BookingHotel.Domain.Tests.Identity;

public abstract class BruteForceManagerTests<TStartupModule> : BookingHotelDomainTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    protected IBruteForceManager BruteForceManager { get; }
    protected IdentityUserManager UserManager { get; }

    protected BruteForceManagerTests()
    {
        BruteForceManager = GetRequiredService<IBruteForceManager>();
        UserManager = GetRequiredService<IdentityUserManager>();
    }

    protected IdentityUser CreateTestUser(string email)
    {
        var user = new IdentityUser(
            Guid.NewGuid(),
            email,
            email
        );
        user.SetProperty(BookingHotelIdentityConsts.FailedAttemptsInCurrentCycleProperty, 0);
        user.SetProperty(BookingHotelIdentityConsts.TotalFailedAttemptsProperty, 0);
        user.SetProperty(BookingHotelIdentityConsts.CurrentLockoutMinutesProperty, 0);
        return user;
    }

    protected async Task SetLockoutEndAsync(IdentityUser user, DateTimeOffset? lockoutEnd)
    {
        typeof(IdentityUser).GetProperty(nameof(IdentityUser.LockoutEnd))?.SetValue(user, lockoutEnd);
        await UserManager.SetLockoutEndDateAsync(user, lockoutEnd);
    }

    [Fact]
    public async Task Should_Allow_Login_When_Password_Is_Correct()
    {
        var user = CreateTestUser("correct@hotel.com");

        var result = await BruteForceManager.EvaluateLoginAttemptAsync(user, isPasswordCorrect: true);

        result.IsAllowed.ShouldBeTrue();
        result.IsLockedOut.ShouldBeFalse();
        result.IsDeactivated.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Warn_Remaining_Attempts_On_First_4_Failed_Attempts()
    {
        var user = CreateTestUser("warn@hotel.com");

        for (int i = 1; i <= 4; i++)
        {
            var result = await BruteForceManager.EvaluateLoginAttemptAsync(user, isPasswordCorrect: false);
            result.IsAllowed.ShouldBeFalse();
            result.IsLockedOut.ShouldBeFalse();
            result.RemainingAttemptsInCycle.ShouldBe(5 - i);
            result.MessageKey.ShouldBe("Auth:InvalidCredentials");
        }
    }

    [Fact]
    public async Task Should_Lockout_5_Minutes_On_5th_Failed_Attempt()
    {
        var user = CreateTestUser("lock5@hotel.com");

        for (int i = 1; i <= 4; i++)
        {
            await BruteForceManager.EvaluateLoginAttemptAsync(user, isPasswordCorrect: false);
        }

        var result5 = await BruteForceManager.EvaluateLoginAttemptAsync(user, isPasswordCorrect: false);

        result5.IsAllowed.ShouldBeFalse();
        result5.IsLockedOut.ShouldBeTrue();
        result5.LockoutMinutes.ShouldBe(5);
        result5.RemainingLockoutSeconds.ShouldBe(300);
        user.LockoutEnd.ShouldNotBeNull();
        user.LockoutEnd.Value.ShouldBeGreaterThan(DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task Should_Reject_Immediately_When_Account_Is_Already_Locked()
    {
        var user = CreateTestUser("alreadylocked@hotel.com");
        user.SetProperty(BookingHotelIdentityConsts.CurrentLockoutMinutesProperty, 5);
        await SetLockoutEndAsync(user, DateTimeOffset.UtcNow.AddMinutes(5));

        var result = await BruteForceManager.EvaluateLoginAttemptAsync(user, isPasswordCorrect: false);

        result.IsAllowed.ShouldBeFalse();
        result.IsLockedOut.ShouldBeTrue();
        result.MessageKey.ShouldBe("Auth:AccountLocked");
    }

    [Fact]
    public async Task Should_Progress_Through_Tier1_And_Tier2_Lockouts_And_Deactivate()
    {
        var user = CreateTestUser("tiered@hotel.com");

        // Giai đoạn 1: 5 lần đầu -> Khóa 5 phút
        for (int i = 1; i <= 5; i++)
        {
            await BruteForceManager.EvaluateLoginAttemptAsync(user, false);
        }
        user.GetProperty<int>(BookingHotelIdentityConsts.CurrentLockoutMinutesProperty).ShouldBe(5);
        user.GetProperty<int>(BookingHotelIdentityConsts.TotalFailedAttemptsProperty).ShouldBe(5);

        // Hết đợt 5m -> Sai tiếp 5 lần -> Khóa 10 phút
        await SetLockoutEndAsync(user, null);
        for (int i = 1; i <= 5; i++)
        {
            await BruteForceManager.EvaluateLoginAttemptAsync(user, false);
        }
        user.GetProperty<int>(BookingHotelIdentityConsts.CurrentLockoutMinutesProperty).ShouldBe(10);
        user.GetProperty<int>(BookingHotelIdentityConsts.TotalFailedAttemptsProperty).ShouldBe(10);

        // Hết đợt 10m -> Sai tiếp 5 lần -> Khóa 15 phút
        await SetLockoutEndAsync(user, null);
        for (int i = 1; i <= 5; i++)
        {
            await BruteForceManager.EvaluateLoginAttemptAsync(user, false);
        }
        user.GetProperty<int>(BookingHotelIdentityConsts.CurrentLockoutMinutesProperty).ShouldBe(15);
        user.GetProperty<int>(BookingHotelIdentityConsts.TotalFailedAttemptsProperty).ShouldBe(15);

        // Giai đoạn 2: Từ lần 15 trở đi, ngưỡng giảm còn 3 lần
        // Hết đợt 15m -> Sai tiếp 3 lần (lần 16, 17, 18) -> Khóa 20 phút
        await SetLockoutEndAsync(user, null);
        var r16 = await BruteForceManager.EvaluateLoginAttemptAsync(user, false);
        r16.RemainingAttemptsInCycle.ShouldBe(2);
        r16.MessageKey.ShouldBe("Auth:InvalidCredentialsTightened");

        await BruteForceManager.EvaluateLoginAttemptAsync(user, false);
        var r18 = await BruteForceManager.EvaluateLoginAttemptAsync(user, false);
        r18.IsLockedOut.ShouldBeTrue();
        r18.LockoutMinutes.ShouldBe(20);

        // Hết đợt 20m -> Sai tiếp 3 lần (lần 19, 20, 21) -> Khóa 25 phút
        await SetLockoutEndAsync(user, null);
        for (int i = 1; i <= 3; i++)
        {
            await BruteForceManager.EvaluateLoginAttemptAsync(user, false);
        }
        user.GetProperty<int>(BookingHotelIdentityConsts.CurrentLockoutMinutesProperty).ShouldBe(25);

        // Hết đợt 25m -> Sai tiếp 3 lần (lần 22, 23, 24) -> Khóa 30 phút (tối đa)
        await SetLockoutEndAsync(user, null);
        for (int i = 1; i <= 3; i++)
        {
            await BruteForceManager.EvaluateLoginAttemptAsync(user, false);
        }
        user.GetProperty<int>(BookingHotelIdentityConsts.CurrentLockoutMinutesProperty).ShouldBe(30);

        // Hết đợt 30m -> Tiếp tục sai 3 lần nữa (lần 25, 26, 27) -> VÔ HIỆU HÓA TÀI KHOẢN
        await SetLockoutEndAsync(user, null);
        await BruteForceManager.EvaluateLoginAttemptAsync(user, false);
        await BruteForceManager.EvaluateLoginAttemptAsync(user, false);
        var finalResult = await BruteForceManager.EvaluateLoginAttemptAsync(user, false);

        finalResult.IsDeactivated.ShouldBeTrue();
        finalResult.MessageKey.ShouldBe("Auth:AccountDeactivated");
        user.IsActive.ShouldBeFalse();
        user.GetProperty<string>(BookingHotelIdentityConsts.DeactivatedReasonProperty).ShouldBe(BookingHotelIdentityConsts.DeactivatedReasonBruteForce);
    }

    [Fact]
    public async Task Should_Allow_Admin_To_Unlock_Deactivated_Account()
    {
        var user = CreateTestUser("unlock@hotel.com");
        user.SetIsActive(false);
        user.SetProperty(BookingHotelIdentityConsts.CurrentLockoutMinutesProperty, 30);
        user.SetProperty(BookingHotelIdentityConsts.TotalFailedAttemptsProperty, 27);

        await BruteForceManager.UnlockAccountAsync(user);

        user.IsActive.ShouldBeTrue();
        user.LockoutEnd.ShouldBeNull();
        user.GetProperty<int>(BookingHotelIdentityConsts.CurrentLockoutMinutesProperty).ShouldBe(0);
        user.GetProperty<int>(BookingHotelIdentityConsts.TotalFailedAttemptsProperty).ShouldBe(0);
        user.GetProperty<string>(BookingHotelIdentityConsts.DeactivatedReasonProperty).ShouldBeNull();
    }
}

