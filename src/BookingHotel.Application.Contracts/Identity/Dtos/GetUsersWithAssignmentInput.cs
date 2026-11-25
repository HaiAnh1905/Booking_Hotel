using System;
using Volo.Abp.Application.Dtos;

namespace BookingHotel.Identity.Dtos;

public class GetUsersWithAssignmentInput : PagedAndSortedResultRequestDto
{
    public string? Filter { get; set; }

    public string? RoleName { get; set; }

    public Guid? HotelId { get; set; }

    public bool? IsActive { get; set; }

    public bool? IsLockedOut { get; set; }
}

