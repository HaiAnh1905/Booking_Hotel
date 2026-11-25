using System;

namespace BookingHotel.Identity.Dtos;

public class UserRoleDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;
}

