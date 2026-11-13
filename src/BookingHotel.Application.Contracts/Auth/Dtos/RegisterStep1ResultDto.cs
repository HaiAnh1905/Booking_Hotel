using System;

namespace BookingHotel.Auth.Dtos;

public class RegisterStep1ResultDto
{
    public Guid RegistrationToken { get; set; }
    public string Email { get; set; } = default!;
    public string? PrefilledName { get; set; }
    public string NextStep { get; set; } = "Step2";
}

