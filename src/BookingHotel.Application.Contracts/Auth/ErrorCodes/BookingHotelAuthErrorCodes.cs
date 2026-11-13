namespace BookingHotel.Auth.ErrorCodes;

public static class BookingHotelAuthErrorCodes
{
    public const string EmailAlreadyExists = "Auth:010001";
    public const string InvalidCaptcha = "Auth:010002";
    public const string InvalidOnboardingToken = "Auth:010003";
    public const string InvalidCredentials = "Auth:010004";
    public const string AccountTemporarilyLocked = "Auth:010005";
    public const string AccountDeactivated = "Auth:010006";
    public const string InvalidResetToken = "Auth:010007";
    public const string UserNotFound = "Auth:010008";
}

