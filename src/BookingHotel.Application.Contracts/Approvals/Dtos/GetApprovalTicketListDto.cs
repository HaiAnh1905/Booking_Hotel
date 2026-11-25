using System;
using BookingHotel.Approvals;
using Volo.Abp.Application.Dtos;

namespace BookingHotel.Approvals.Dtos;

public class GetApprovalTicketListDto : PagedAndSortedResultRequestDto
{
    public Guid? HotelId { get; set; }

    public ApprovalTicketStatus? Status { get; set; }

    public ApprovalTicketType? TicketType { get; set; }
}

