using System;

namespace BookingHotel.Auth.Dtos;

public class LoginResultDto
{
    public bool IsSuccess { get; set; }
    public string? AccessToken { get; set; }
    public string? TokenType { get; set; } = "Bearer";
    public int ExpiresIn { get; set; }
    public bool IsLockedOut { get; set; }
    public int? RemainingLockoutSeconds { get; set; }
    public int? RemainingAttemptsInCycle { get; set; }
    public bool IsDeactivated { get; set; }
    public string? Message { get; set; }
    public bool RequiresRegistration { get; set; }
    public Guid? RegistrationToken { get; set; }
    public string? Email { get; set; }
    public string? PrefilledName { get; set; }
}


