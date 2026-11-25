using System;
using System.ComponentModel.DataAnnotations;

namespace BookingHotel.Identity.Dtos;

public class TransferBranchDto
{
    [Required]
    public Guid FromHotelId { get; set; }

    [Required]
    public Guid ToHotelId { get; set; }

    [Required]
    public string RoleName { get; set; } = string.Empty;

    public string? Note { get; set; }
}
