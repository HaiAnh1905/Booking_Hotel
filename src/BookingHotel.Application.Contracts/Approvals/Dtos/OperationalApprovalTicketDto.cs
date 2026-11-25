using System;
using Volo.Abp.Application.Dtos;

namespace BookingHotel.Approvals.Dtos;

public class OperationalApprovalTicketDto : FullAuditedEntityDto<Guid>
{
    public Guid? TenantId { get; set; }

    public Guid HotelId { get; set; }

    public string HotelName { get; set; } = string.Empty;

    public ApprovalTicketType TicketType { get; set; }

    public string TicketTypeName => TicketType.ToString();

    public ApprovalTicketStatus Status { get; set; }

    public string StatusName => Status.ToString();

    public Guid RequestedByStaffId { get; set; }

    public string RequestedByStaffName { get; set; } = string.Empty;

    public Guid? BookingId { get; set; }

    public Guid? FolioId { get; set; }

    public string? RoomNumber { get; set; }

    public decimal? Amount { get; set; }

    public decimal? DiscountPercent { get; set; }

    public string Reason { get; set; } = string.Empty;

    public Guid? ReviewedByManagerId { get; set; }

    public string? ReviewedByManagerName { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public string? RejectionReason { get; set; }
}

