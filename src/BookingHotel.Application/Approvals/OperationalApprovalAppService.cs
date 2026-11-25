using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BookingHotel.Approvals.Dtos;
using BookingHotel.Identity;
using BookingHotel.Permissions;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Users;

namespace BookingHotel.Approvals;

[RemoteService(IsEnabled = false)]
[Authorize(BookingHotelPermissions.Bookings.Default)]
public class OperationalApprovalAppService : ApplicationService, IOperationalApprovalAppService
{
    private readonly IRepository<OperationalApprovalTicket, Guid> _ticketRepository;
    private readonly IRepository<UserHotelAssignment, Guid> _assignmentRepository;

    public OperationalApprovalAppService(
        IRepository<OperationalApprovalTicket, Guid> ticketRepository,
        IRepository<UserHotelAssignment, Guid> assignmentRepository)
    {
        _ticketRepository = ticketRepository;
        _assignmentRepository = assignmentRepository;
    }

    public virtual async Task<PagedResultDto<OperationalApprovalTicketDto>> GetListAsync(GetApprovalTicketListDto input)
    {
        var query = await _ticketRepository.GetQueryableAsync();

        if (input.HotelId.HasValue)
        {
            query = query.Where(t => t.HotelId == input.HotelId.Value);
        }
        else if (CurrentUser.IsInRole(BookingHotelRoleConsts.HotelManager) || CurrentUser.IsInRole(BookingHotelRoleConsts.Receptionist))
        {
            // Scoping: Nếu là Hotel Manager hoặc Receptionist không chỉ định hotelId, chỉ lấy các khách sạn họ được phân công
            var userId = CurrentUser.GetId();
            var assignments = await _assignmentRepository.GetListAsync(a => a.UserId == userId && a.IsActive);
            var assignedHotelIds = assignments.Select(a => a.HotelId).ToList();

            query = query.Where(t => assignedHotelIds.Contains(t.HotelId));
        }

        if (input.Status.HasValue)
        {
            query = query.Where(t => t.Status == input.Status.Value);
        }

        if (input.TicketType.HasValue)
        {
            query = query.Where(t => t.TicketType == input.TicketType.Value);
        }

        var totalCount = await AsyncExecuter.CountAsync(query);

        var pagedQuery = query
            .OrderByDescending(t => t.CreationTime)
            .Skip(input.SkipCount)
            .Take(input.MaxResultCount);

        var items = await AsyncExecuter.ToListAsync(pagedQuery);

        return new PagedResultDto<OperationalApprovalTicketDto>(
            totalCount,
            items.Select(MapToDto).ToList());
    }

    public virtual async Task<OperationalApprovalTicketDto> GetAsync(Guid id)
    {
        var ticket = await _ticketRepository.FindAsync(id);
        if (ticket == null)
        {
            throw new BusinessException("BookingHotel:TicketNotFound");
        }

        await EnsureUserHasAccessToHotelAsync(ticket.HotelId);

        return MapToDto(ticket);
    }

    public virtual async Task<OperationalApprovalTicketDto> CreateAsync(CreateApprovalTicketDto input)
    {
        var staffId = CurrentUser.Id ?? Guid.NewGuid();
        var staffName = CurrentUser.UserName ?? CurrentUser.Name ?? "Staff";

        var ticket = new OperationalApprovalTicket(
            GuidGenerator.Create(),
            input.HotelId,
            input.TicketType,
            staffId,
            staffName,
            input.Reason,
            CurrentTenant.Id,
            input.BookingId,
            input.FolioId,
            input.RoomNumber,
            input.Amount,
            input.DiscountPercent);

        await _ticketRepository.InsertAsync(ticket, autoSave: true);

        return MapToDto(ticket);
    }

    [Authorize(BookingHotelPermissions.Bookings.ApproveException)]
    public virtual async Task<OperationalApprovalTicketDto> ApproveAsync(Guid id)
    {
        var ticket = await _ticketRepository.FindAsync(id);
        if (ticket == null)
        {
            throw new BusinessException("BookingHotel:TicketNotFound");
        }

        await EnsureUserHasAccessToHotelAsync(ticket.HotelId);

        var managerId = CurrentUser.Id ?? Guid.NewGuid();
        var managerName = CurrentUser.UserName ?? CurrentUser.Name ?? "Manager";

        ticket.Approve(managerId, managerName, Clock.Now);
        await _ticketRepository.UpdateAsync(ticket, autoSave: true);

        return MapToDto(ticket);
    }

    [Authorize(BookingHotelPermissions.Bookings.ApproveException)]
    public virtual async Task<OperationalApprovalTicketDto> RejectAsync(Guid id, ReviewApprovalTicketDto input)
    {
        var ticket = await _ticketRepository.FindAsync(id);
        if (ticket == null)
        {
            throw new BusinessException("BookingHotel:TicketNotFound");
        }

        if (string.IsNullOrWhiteSpace(input.RejectionReason))
        {
            throw new BusinessException("BookingHotel:RejectionReasonRequired");
        }

        await EnsureUserHasAccessToHotelAsync(ticket.HotelId);

        var managerId = CurrentUser.Id ?? Guid.NewGuid();
        var managerName = CurrentUser.UserName ?? CurrentUser.Name ?? "Manager";

        ticket.Reject(managerId, managerName, input.RejectionReason, Clock.Now);
        await _ticketRepository.UpdateAsync(ticket, autoSave: true);

        return MapToDto(ticket);
    }

    public virtual async Task<OperationalApprovalTicketDto> CancelAsync(Guid id)
    {
        var ticket = await _ticketRepository.FindAsync(id);
        if (ticket == null)
        {
            throw new BusinessException("BookingHotel:TicketNotFound");
        }

        if (CurrentUser.IsAuthenticated &&
            ticket.RequestedByStaffId != CurrentUser.GetId() &&
            !CurrentUser.IsInRole(BookingHotelRoleConsts.SystemAdmin) &&
            !CurrentUser.IsInRole(BookingHotelRoleConsts.TenantAdmin))
        {
            throw new BusinessException("BookingHotel:OnlyCreatorCanCancelTicket");
        }

        ticket.Cancel();
        await _ticketRepository.UpdateAsync(ticket, autoSave: true);

        return MapToDto(ticket);
    }

    private async Task EnsureUserHasAccessToHotelAsync(Guid hotelId)
    {
        var isHotelStaff = CurrentUser.IsInRole(BookingHotelRoleConsts.HotelManager) ||
                           CurrentUser.IsInRole(BookingHotelRoleConsts.Receptionist);

        if (!isHotelStaff)
        {
            return;
        }

        var userId = CurrentUser.GetId();
        var hasAccess = await _assignmentRepository.AnyAsync(a => a.UserId == userId && a.HotelId == hotelId && a.IsActive);

        if (!hasAccess)
        {
            throw new BusinessException("BookingHotel:CrossHotelOperationForbidden");
        }
    }

    private static OperationalApprovalTicketDto MapToDto(OperationalApprovalTicket ticket)
    {
        return new OperationalApprovalTicketDto
        {
            Id = ticket.Id,
            TenantId = ticket.TenantId,
            HotelId = ticket.HotelId,
            TicketType = ticket.TicketType,
            Status = ticket.Status,
            RequestedByStaffId = ticket.RequestedByStaffId,
            RequestedByStaffName = ticket.RequestedByStaffName,
            BookingId = ticket.BookingId,
            FolioId = ticket.FolioId,
            RoomNumber = ticket.RoomNumber,
            Amount = ticket.Amount,
            DiscountPercent = ticket.DiscountPercent,
            Reason = ticket.Reason,
            ReviewedByManagerId = ticket.ReviewedByManagerId,
            ReviewedByManagerName = ticket.ReviewedByManagerName,
            ReviewedAt = ticket.ReviewedAt,
            RejectionReason = ticket.RejectionReason,
            CreationTime = ticket.CreationTime,
            CreatorId = ticket.CreatorId,
            LastModificationTime = ticket.LastModificationTime,
            LastModifierId = ticket.LastModifierId
        };
    }
}
