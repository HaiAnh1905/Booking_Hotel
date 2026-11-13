using System.ComponentModel.DataAnnotations;

namespace BookingHotel.Auth.Dtos;

public class LoginWithEmailDto
{
    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = default!;

    [Required]
    [StringLength(128, MinimumLength = 6)]
    public string Password { get; set; } = default!;

    public bool RememberMe { get; set; } = false;
}

