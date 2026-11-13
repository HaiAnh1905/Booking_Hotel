using System.ComponentModel.DataAnnotations;

namespace BookingHotel.Auth.Dtos;

public class ResetPasswordInputDto
{
    [Required]
    public string UserId { get; set; } = default!;

    [Required]
    public string ResetToken { get; set; } = default!;

    [Required]
    [StringLength(128, MinimumLength = 8)]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).{8,}$", ErrorMessage = "::Auth:PasswordComplexityFailed")]
    public string NewPassword { get; set; } = default!;

    [Required]
    [Compare(nameof(NewPassword))]
    public string ConfirmNewPassword { get; set; } = default!;
}

