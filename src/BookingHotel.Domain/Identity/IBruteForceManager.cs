using System.Threading.Tasks;
using Volo.Abp.Identity;

namespace BookingHotel.Identity;

public class BruteForceCheckResult
{
    public bool IsAllowed { get; set; }
    public bool IsLockedOut { get; set; }
    public bool IsDeactivated { get; set; }
    public int LockoutMinutes { get; set; }
    public int RemainingLockoutSeconds { get; set; }
    public int RemainingAttemptsInCycle { get; set; }
    public string? MessageKey { get; set; }
}

public interface IBruteForceManager
{
    Task<BruteForceCheckResult> EvaluateLoginAttemptAsync(IdentityUser user, bool isPasswordCorrect);
    Task ResetCountersAsync(IdentityUser user);
    Task UnlockAccountAsync(IdentityUser user);
}
