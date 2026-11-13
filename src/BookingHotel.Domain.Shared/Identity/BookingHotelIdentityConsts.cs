namespace BookingHotel.Identity;

public static class BookingHotelIdentityConsts
{
    public const string FailedAttemptsInCurrentCycleProperty = "FailedAttemptsInCurrentCycle";
    public const string TotalFailedAttemptsProperty = "TotalFailedAttempts";
    public const string CurrentLockoutMinutesProperty = "CurrentLockoutMinutes";
    public const string DeactivatedReasonProperty = "DeactivatedReason";
    public const string GoogleIdProperty = "GoogleId";

    public const string DeactivatedReasonBruteForce = "BruteForceLimitExceeded";
}

