using System;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace BookingHotel.Approvals;

public class OperationalApprovalTicket : FullAuditedEntity<Guid>, IMultiTenant
{
    public virtual Guid? TenantId { get; set; }

    public virtual Guid HotelId { get; set; }

    public virtual ApprovalTicketType TicketType { get; set; }

    public virtual ApprovalTicketStatus Status { get; set; } = ApprovalTicketStatus.Pending;

    public virtual Guid RequestedByStaffId { get; set; }

    public virtual string RequestedByStaffName { get; set; } = string.Empty;

    public virtual Guid? BookingId { get; set; }

    public virtual Guid? FolioId { get; set; }

    public virtual string? RoomNumber { get; set; }

    public virtual decimal? Amount { get; set; }

    public virtual decimal? DiscountPercent { get; set; }

    public virtual string Reason { get; set; } = string.Empty;

    public virtual Guid? ReviewedByManagerId { get; set; }

    public virtual string? ReviewedByManagerName { get; set; }

    public virtual DateTime? ReviewedAt { get; set; }

    public virtual string? RejectionReason { get; set; }

    protected OperationalApprovalTicket()
    {
    }

    public OperationalApprovalTicket(
        Guid id,
        Guid hotelId,
        ApprovalTicketType ticketType,
        Guid requestedByStaffId,
        string requestedByStaffName,
        string reason,
        Guid? tenantId = null,
        Guid? bookingId = null,
        Guid? folioId = null,
        string? roomNumber = null,
        decimal? amount = null,
        decimal? discountPercent = null)
        : base(id)
    {
        HotelId = Check.NotNull(hotelId, nameof(hotelId));
        TicketType = ticketType;
        RequestedByStaffId = Check.NotNull(requestedByStaffId, nameof(requestedByStaffId));
        RequestedByStaffName = Check.NotNullOrWhiteSpace(requestedByStaffName, nameof(requestedByStaffName));
        Reason = Check.NotNullOrWhiteSpace(reason, nameof(reason));
        TenantId = tenantId;
        BookingId = bookingId;
        FolioId = folioId;
        RoomNumber = roomNumber;
        Amount = amount;
        DiscountPercent = discountPercent;
        Status = ApprovalTicketStatus.Pending;
    }

    public void Approve(Guid managerId, string managerName, DateTime reviewedAt)
    {
        if (Status != ApprovalTicketStatus.Pending)
        {
            throw new BusinessException("BookingHotel:TicketNotInPendingState");
        }

        ReviewedByManagerId = Check.NotNull(managerId, nameof(managerId));
        ReviewedByManagerName = Check.NotNullOrWhiteSpace(managerName, nameof(managerName));
        ReviewedAt = reviewedAt;
        Status = ApprovalTicketStatus.Approved;
    }

    public void Reject(Guid managerId, string managerName, string rejectionReason, DateTime reviewedAt)
    {
        if (Status != ApprovalTicketStatus.Pending)
        {
            throw new BusinessException("BookingHotel:TicketNotInPendingState");
        }

        ReviewedByManagerId = Check.NotNull(managerId, nameof(managerId));
        ReviewedByManagerName = Check.NotNullOrWhiteSpace(managerName, nameof(managerName));
        RejectionReason = Check.NotNullOrWhiteSpace(rejectionReason, nameof(rejectionReason));
        ReviewedAt = reviewedAt;
        Status = ApprovalTicketStatus.Rejected;
    }

    public void Cancel()
    {
        if (Status != ApprovalTicketStatus.Pending)
        {
            throw new BusinessException("BookingHotel:TicketNotInPendingState");
        }

        Status = ApprovalTicketStatus.Cancelled;
    }
}

