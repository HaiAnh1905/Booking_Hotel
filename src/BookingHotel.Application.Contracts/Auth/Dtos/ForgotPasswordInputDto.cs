using System.ComponentModel.DataAnnotations;

namespace BookingHotel.Auth.Dtos;

public class ForgotPasswordInputDto
{
    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = default!;

    [Required]
    public string CaptchaToken { get; set; } = default!;
}

