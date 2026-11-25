using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace BookingHotel.Manager.Guests;

public interface IManagerGuestAppService : IApplicationService
{
    Task<PagedResultDto<ManagerGuestDto>> GetListAsync(Guid hotelId, string? keyword = null, int skipCount = 0, int maxResultCount = 20);
    Task<List<GuestStayHistoryDto>> GetStayHistoryAsync(Guid guestId, Guid hotelId);
    Task<List<InternalGuestNoteDto>> GetNotesAsync(Guid guestId, Guid hotelId);
    Task<InternalGuestNoteDto> CreateNoteAsync(CreateInternalGuestNoteDto input);
    Task DeleteNoteAsync(Guid id, Guid hotelId);
}
