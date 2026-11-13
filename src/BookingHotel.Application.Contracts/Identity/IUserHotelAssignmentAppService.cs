using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace BookingHotel.Identity;

public interface IUserHotelAssignmentAppService : IApplicationService
{
    Task<List<Guid>> GetAssignedHotelIdsAsync(Guid userId);

    Task AssignUserToHotelsAsync(AssignUserHotelDto input);

    Task<bool> HasAccessToHotelAsync(Guid userId, Guid hotelId);
}

