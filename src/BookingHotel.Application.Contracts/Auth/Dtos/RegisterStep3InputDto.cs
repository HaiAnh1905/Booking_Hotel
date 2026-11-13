using System;
using System.ComponentModel.DataAnnotations;

namespace BookingHotel.Auth.Dtos;

public class RegisterStep3InputDto
{
    [Required]
    public Guid RegistrationToken { get; set; }

    [Required]
    [RegularExpression(@"^(\+84|0)(3[2-9]|5[6|8|9]|7[0|6-9]|8[1-9]|9[0-9])[0-9]{7}$", 
        ErrorMessage = "Số điện thoại không hợp lệ.")]
    [StringLength(20)]
    public string PhoneNumber { get; set; } = default!;
}

