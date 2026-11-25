using System;
using System.Threading.Tasks;
using BookingHotel.Identity;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Users;

namespace BookingHotel.Manager;

public abstract class ManagerAppServiceBase : BookingHotelAppService
{
    protected IRepository<UserHotelAssignment, Guid> UserHotelAssignmentRepository { get; }

    protected ManagerAppServiceBase(IRepository<UserHotelAssignment, Guid> userHotelAssignmentRepository)
    {
        UserHotelAssignmentRepository = userHotelAssignmentRepository;
    }

    protected virtual async Task CheckHotelManagerAccessAsync(Guid hotelId)
    {
        if (CurrentUser.IsInRole("TenantAdmin"))
        {
            return;
        }

        var userId = CurrentUser.GetId();
        var hasAccess = await UserHotelAssignmentRepository.AnyAsync(x =>
            x.UserId == userId &&
            x.HotelId == hotelId &&
            x.IsActive &&
            x.UnassignedAt == null);

        if (!hasAccess)
        {
            throw new BusinessException("BookingHotel:CrossHotelOperationForbidden")
                .WithData("HotelId", hotelId)
                .WithData("UserId", userId);
        }
    }
}
