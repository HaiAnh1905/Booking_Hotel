namespace BookingHotel.Bookings;

public enum BookingStatus
{
    PendingConfirmation = 1,
    Confirmed = 2,
    CheckedIn = 3,
    Completed = 4,
    Rejected = 5,
    Cancelled = 6,
    NoShow = 7
}

public enum PaymentStatus
{
    Unpaid = 1,
    PartiallyPaid = 2,
    Paid = 3,
    Refunded = 4
}

public enum RoomAssignmentStatus
{
    Unassigned = 1,
    Assigned = 2
}
