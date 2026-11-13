using System.ComponentModel.DataAnnotations;

namespace BookingHotel.Auth.Dtos;

public class RegisterStep1InputDto
{
    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = default!;

    [Required]
    [StringLength(128, MinimumLength = 8)]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).{8,}$", ErrorMessage = "::Auth:PasswordComplexityFailed")]
    public string Password { get; set; } = default!;

    [Required]
    [Compare(nameof(Password))]
    public string ConfirmPassword { get; set; } = default!;
}

