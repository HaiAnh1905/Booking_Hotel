namespace BookingHotel.Auth.Dtos;

public class ResetPasswordInfoDto
{
    public string Email { get; set; } = string.Empty;
    public string MaskedEmail { get; set; } = string.Empty;
}

