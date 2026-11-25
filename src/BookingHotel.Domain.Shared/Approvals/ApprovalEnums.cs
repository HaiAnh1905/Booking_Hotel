namespace BookingHotel.Approvals;

public enum ApprovalTicketType
{
    DiscountOverride = 1,
    PenaltyWaiver = 2,
    EarlyCheckIn = 3,
    LateCheckOut = 4,
    RoomUpgrade = 5
}

public enum ApprovalTicketStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    Cancelled = 3
}

