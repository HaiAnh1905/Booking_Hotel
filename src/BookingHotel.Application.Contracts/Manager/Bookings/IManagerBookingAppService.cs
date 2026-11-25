using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace BookingHotel.Manager.Bookings;

public interface IManagerBookingAppService : IApplicationService
{
    Task<PagedResultDto<ManagerBookingDto>> GetListAsync(GetManagerBookingsInputDto input);
    Task<ManagerBookingDto> GetAsync(Guid id, Guid hotelId);
    Task<ManagerBookingDto> CreateDirectBookingAsync(CreateDirectBookingDto input);
    Task<ManagerBookingDto> ConfirmAsync(Guid id, Guid hotelId);
    Task<ManagerBookingDto> RejectAsync(Guid id, RejectBookingDto input);
    Task<NoShowResultDto> MarkNoShowAsync(Guid id, MarkNoShowDto input);
}
