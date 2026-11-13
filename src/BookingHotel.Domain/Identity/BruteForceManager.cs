using System;
using System.Threading.Tasks;
using Volo.Abp.Data;
using Volo.Abp.Domain.Services;
using Volo.Abp.Identity;

namespace BookingHotel.Identity;

public class BruteForceManager : DomainService, IBruteForceManager
{
    public const int InitialCycleThreshold = 5;
    public const int TightenedCycleThreshold = 3;
    public const int TightenedAttemptThreshold = 15;
    public const int MaxLockoutMinutes = 30;
    public const int LockoutStepMinutes = 5;

    protected IdentityUserManager UserManager { get; }

    public BruteForceManager(IdentityUserManager userManager)
    {
        UserManager = userManager;
    }

    public async Task<BruteForceCheckResult> EvaluateLoginAttemptAsync(IdentityUser user, bool isPasswordCorrect)
    {
        ArgumentNullException.ThrowIfNull(user);

        // 1. Kiểm tra tài khoản đã bị vô hiệu hóa
        if (!user.IsActive)
        {
            return new BruteForceCheckResult
            {
                IsAllowed = false,
                IsDeactivated = true,
                MessageKey = "Auth:AccountDeactivated"
            };
        }

        // 2. Kiểm tra tài khoản đang trong thời gian tạm khóa
        if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow)
        {
            var remainingSeconds = (int)Math.Ceiling((user.LockoutEnd.Value - DateTimeOffset.UtcNow).TotalSeconds);
            var currentLockout = user.GetProperty<int>(BookingHotelIdentityConsts.CurrentLockoutMinutesProperty);

            return new BruteForceCheckResult
            {
                IsAllowed = false,
                IsLockedOut = true,
                LockoutMinutes = currentLockout,
                RemainingLockoutSeconds = Math.Max(1, remainingSeconds),
                MessageKey = "Auth:AccountLocked"
            };
        }

        // 3. Đăng nhập thành công -> Reset toàn bộ bộ đếm
        if (isPasswordCorrect)
        {
            await ResetCountersAsync(user);
            return new BruteForceCheckResult
            {
                IsAllowed = true
            };
        }

        // 4. Mật khẩu không chính xác -> Xử lý tăng đếm và phạt lũy tiến
        var totalFailed = user.GetProperty<int>(BookingHotelIdentityConsts.TotalFailedAttemptsProperty) + 1;
        var failedInCycle = user.GetProperty<int>(BookingHotelIdentityConsts.FailedAttemptsInCurrentCycleProperty) + 1;
        var currentLockoutMin = user.GetProperty<int>(BookingHotelIdentityConsts.CurrentLockoutMinutesProperty);

        user.SetProperty(BookingHotelIdentityConsts.TotalFailedAttemptsProperty, totalFailed);
        user.SetProperty(BookingHotelIdentityConsts.FailedAttemptsInCurrentCycleProperty, failedInCycle);

        // Xác định ngưỡng cho phép trong chu kỳ hiện tại
        // Từ lần sai thứ 15 trở đi, ngưỡng giảm xuống còn 3 lần
        var cycleThreshold = (totalFailed <= TightenedAttemptThreshold) 
            ? InitialCycleThreshold 
            : TightenedCycleThreshold;

        // Nếu chưa vượt ngưỡng trong chu kỳ
        if (failedInCycle < cycleThreshold)
        {
            await UserManager.UpdateAsync(user);
            var remaining = cycleThreshold - failedInCycle;
            var messageKey = (totalFailed <= TightenedAttemptThreshold)
                ? "Auth:InvalidCredentials"
                : "Auth:InvalidCredentialsTightened";

            return new BruteForceCheckResult
            {
                IsAllowed = false,
                IsLockedOut = false,
                RemainingAttemptsInCycle = remaining,
                MessageKey = messageKey
            };
        }

        // Đã đạt đủ số lần sai trong chu kỳ -> Kích hoạt khóa hoặc Vô hiệu hóa
        if (currentLockoutMin >= MaxLockoutMinutes)
        {
            // Đã qua đợt phạt tối đa 30m mà tiếp tục sai đủ chu kỳ -> Vô hiệu hóa tài khoản ngay lập tức
            user.SetIsActive(false);
            user.SetProperty(BookingHotelIdentityConsts.DeactivatedReasonProperty, BookingHotelIdentityConsts.DeactivatedReasonBruteForce);
            user.SetProperty(BookingHotelIdentityConsts.FailedAttemptsInCurrentCycleProperty, 0);
            typeof(IdentityUser).GetProperty(nameof(IdentityUser.LockoutEnd))?.SetValue(user, null);
            await UserManager.SetLockoutEndDateAsync(user, null);
            await UserManager.UpdateAsync(user);

            return new BruteForceCheckResult
            {
                IsAllowed = false,
                IsDeactivated = true,
                MessageKey = "Auth:AccountDeactivated"
            };
        }
        else
        {
            // Tăng thời gian phạt thêm 5 phút
            var nextLockoutMin = Math.Min(MaxLockoutMinutes, currentLockoutMin + LockoutStepMinutes);
            user.SetProperty(BookingHotelIdentityConsts.CurrentLockoutMinutesProperty, nextLockoutMin);
            user.SetProperty(BookingHotelIdentityConsts.FailedAttemptsInCurrentCycleProperty, 0);
            var lockoutEnd = DateTimeOffset.UtcNow.AddMinutes(nextLockoutMin);
            typeof(IdentityUser).GetProperty(nameof(IdentityUser.LockoutEnd))?.SetValue(user, lockoutEnd);
            await UserManager.SetLockoutEndDateAsync(user, lockoutEnd);
            await UserManager.UpdateAsync(user);

            return new BruteForceCheckResult
            {
                IsAllowed = false,
                IsLockedOut = true,
                LockoutMinutes = nextLockoutMin,
                RemainingLockoutSeconds = nextLockoutMin * 60,
                MessageKey = "Auth:AccountLocked"
            };
        }
    }

    public async Task ResetCountersAsync(IdentityUser user)
    {
        ArgumentNullException.ThrowIfNull(user);

        user.SetProperty(BookingHotelIdentityConsts.FailedAttemptsInCurrentCycleProperty, 0);
        user.SetProperty(BookingHotelIdentityConsts.TotalFailedAttemptsProperty, 0);
        user.SetProperty(BookingHotelIdentityConsts.CurrentLockoutMinutesProperty, 0);
        user.SetProperty(BookingHotelIdentityConsts.DeactivatedReasonProperty, null);
        typeof(IdentityUser).GetProperty(nameof(IdentityUser.LockoutEnd))?.SetValue(user, null);
        await UserManager.SetLockoutEndDateAsync(user, null);
        await UserManager.UpdateAsync(user);
    }

    public async Task UnlockAccountAsync(IdentityUser user)
    {
        ArgumentNullException.ThrowIfNull(user);

        user.SetIsActive(true);
        await ResetCountersAsync(user);
    }
}
