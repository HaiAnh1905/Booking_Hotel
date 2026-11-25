using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace BookingHotel.Identity.Dtos;

public class UserWithAssignmentDto : EntityDto<Guid>
{
    public string UserName { get; set; } = string.Empty;

    public string? Name { get; set; }

    public string? Surname { get; set; }

    public string Email { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    public bool IsActive { get; set; }

    public DateTimeOffset? LockoutEnd { get; set; }

    public bool IsLockedOut { get; set; }

    public bool ShouldChangePasswordOnNextLogin { get; set; }

    public List<UserRoleDto> Roles { get; set; } = new();

    public List<string> RoleNames { get; set; } = new();

    public List<AssignedHotelDto> AssignedHotels { get; set; } = new();

    public DateTime CreationTime { get; set; }

    public string? ConcurrencyStamp { get; set; }
}

