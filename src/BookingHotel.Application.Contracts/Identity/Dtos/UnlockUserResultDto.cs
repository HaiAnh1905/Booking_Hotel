using System;

namespace BookingHotel.Identity.Dtos;

public class UnlockUserResultDto
{
    public bool Success { get; set; }

    public Guid UserId { get; set; }

    public DateTime UnlockedAt { get; set; }

    public string Message { get; set; } = string.Empty;
}

