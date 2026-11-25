using System;
using System.Threading.Tasks;
using BookingHotel.Approvals.Dtos;
using BookingHotel.Controllers;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace BookingHotel.Approvals;

[RemoteService(Name = "OperationalApproval")]
[Area("app")]
[Route("api/app/operational-approval")]
public class OperationalApprovalController : BookingHotelController, IOperationalApprovalAppService
{
    private readonly IOperationalApprovalAppService _approvalAppService;

    public OperationalApprovalController(IOperationalApprovalAppService approvalAppService)
    {
        _approvalAppService = approvalAppService;
    }

    [HttpGet]
    public Task<PagedResultDto<OperationalApprovalTicketDto>> GetListAsync([FromQuery] GetApprovalTicketListDto input)
    {
        return _approvalAppService.GetListAsync(input);
    }

    [HttpGet("{id}")]
    public Task<OperationalApprovalTicketDto> GetAsync(Guid id)
    {
        return _approvalAppService.GetAsync(id);
    }

    [HttpPost]
    public Task<OperationalApprovalTicketDto> CreateAsync([FromBody] CreateApprovalTicketDto input)
    {
        return _approvalAppService.CreateAsync(input);
    }

    [HttpPost("{id}/approve")]
    public Task<OperationalApprovalTicketDto> ApproveAsync(Guid id)
    {
        return _approvalAppService.ApproveAsync(id);
    }

    [HttpPost("{id}/reject")]
    public Task<OperationalApprovalTicketDto> RejectAsync(Guid id, [FromBody] ReviewApprovalTicketDto input)
    {
        return _approvalAppService.RejectAsync(id, input);
    }

    [HttpPost("{id}/cancel")]
    public Task<OperationalApprovalTicketDto> CancelAsync(Guid id)
    {
        return _approvalAppService.CancelAsync(id);
    }
}

