using System;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace BookingHotel.Bookings;

public class Booking : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    public virtual Guid? TenantId { get; set; }

    public virtual Guid HotelId { get; set; }

    public virtual Guid? RoomTypeId { get; set; }

    public virtual string BookingCode { get; set; } = string.Empty;

    public virtual Guid? GuestId { get; set; }

    public virtual string GuestFullName { get; set; } = string.Empty;

    public virtual string GuestPhone { get; set; } = string.Empty;

    public virtual string GuestEmail { get; set; } = string.Empty;

    public virtual string GuestIdentityNumberMasked { get; set; } = string.Empty;

    public virtual bool IsIdentityVerified { get; set; }

    public virtual DateTime CheckInDate { get; set; }

    public virtual DateTime CheckOutDate { get; set; }

    public virtual DateTime? ActualCheckInTime { get; set; }

    public virtual DateTime? ActualCheckOutTime { get; set; }

    public virtual decimal TotalAmount { get; set; }

    public virtual decimal DepositAmount { get; set; }

    public virtual BookingStatus BookingStatus { get; set; } = BookingStatus.Confirmed;

    public virtual PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Unpaid;

    public virtual RoomAssignmentStatus RoomAssignmentStatus { get; set; } = RoomAssignmentStatus.Unassigned;

    public virtual bool IsDirectBooking { get; set; }

    public virtual string? ContractNotes { get; set; }

    public virtual string? WaivePenaltyReason { get; set; }

    public virtual decimal PenaltyAmount { get; set; }

    public virtual string? SpecialRequests { get; set; }

    public virtual string? FrontdeskNotes { get; set; }

    public virtual string? CancellationReason { get; set; }

    public virtual string? RejectionReason { get; set; }

    protected Booking()
    {
    }

    public Booking(
        Guid id,
        Guid hotelId,
        string bookingCode,
        string guestFullName,
        string guestPhone,
        string guestEmail,
        DateTime checkInDate,
        DateTime checkOutDate,
        decimal totalAmount,
        decimal depositAmount = 0m,
        BookingStatus bookingStatus = BookingStatus.Confirmed,
        PaymentStatus paymentStatus = PaymentStatus.Unpaid,
        RoomAssignmentStatus roomAssignmentStatus = RoomAssignmentStatus.Unassigned,
        bool isDirectBooking = false,
        string? guestIdentityNumberMasked = null,
        string? specialRequests = null,
        Guid? guestId = null,
        Guid? tenantId = null,
        Guid? roomTypeId = null)
        : base(id)
    {
        HotelId = hotelId;
        RoomTypeId = roomTypeId;
        BookingCode = bookingCode;
        GuestFullName = guestFullName;
        GuestPhone = guestPhone;
        GuestEmail = guestEmail;
        CheckInDate = checkInDate;
        CheckOutDate = checkOutDate;
        TotalAmount = totalAmount;
        DepositAmount = depositAmount;
        BookingStatus = bookingStatus;
        PaymentStatus = paymentStatus;
        RoomAssignmentStatus = roomAssignmentStatus;
        IsDirectBooking = isDirectBooking;
        GuestIdentityNumberMasked = guestIdentityNumberMasked ?? string.Empty;
        SpecialRequests = specialRequests;
        GuestId = guestId;
        TenantId = tenantId;
    }
}
