using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace BookingHotel.Frontdesk;

public interface IFrontdeskReservationAppService : IApplicationService
{
    Task<PagedResultDto<FrontdeskReservationListItemDto>> GetListAsync(GetFrontdeskReservationsInputDto input);
    Task<FrontdeskReservationDetailDto> GetAsync(Guid id, Guid hotelId);
    Task<List<FrontdeskAvailableRoomDto>> GetAvailableRoomsAsync(Guid id, Guid hotelId);
    Task<FrontdeskReservationDetailDto> ConfirmAsync(Guid id, ConfirmFrontdeskReservationDto input);
    Task<FrontdeskReservationDetailDto> AssignRoomAsync(Guid id, AssignFrontdeskRoomDto input);
    Task<FrontdeskReservationDetailDto> UpdateAsync(Guid id, UpdateFrontdeskReservationDto input);
    Task<FrontdeskReservationDetailDto> CancelAsync(Guid id, CancelFrontdeskReservationDto input);
    Task<FrontdeskReservationDetailDto> CheckInAsync(Guid id, FrontdeskCheckInDto input);
    Task<FrontdeskReservationDetailDto> CheckOutAsync(Guid id, FrontdeskCheckOutDto input);
}
