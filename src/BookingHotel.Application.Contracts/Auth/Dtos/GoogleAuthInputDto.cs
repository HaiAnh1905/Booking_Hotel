using System.ComponentModel.DataAnnotations;

namespace BookingHotel.Auth.Dtos;

public class GoogleAuthInputDto
{
    [Required]
    public string IdToken { get; set; } = default!;
}

