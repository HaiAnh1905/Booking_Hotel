using System;
using System.Threading.Tasks;
using BookingHotel.Approvals.Dtos;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace BookingHotel.Approvals;

public interface IOperationalApprovalAppService : IApplicationService
{
    Task<PagedResultDto<OperationalApprovalTicketDto>> GetListAsync(GetApprovalTicketListDto input);

    Task<OperationalApprovalTicketDto> GetAsync(Guid id);

    Task<OperationalApprovalTicketDto> CreateAsync(CreateApprovalTicketDto input);

    Task<OperationalApprovalTicketDto> ApproveAsync(Guid id);

    Task<OperationalApprovalTicketDto> RejectAsync(Guid id, ReviewApprovalTicketDto input);

    Task<OperationalApprovalTicketDto> CancelAsync(Guid id);
}

