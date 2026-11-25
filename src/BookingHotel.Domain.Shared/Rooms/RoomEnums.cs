namespace BookingHotel.Rooms;

public enum RoomOperationalStatus
{
    Active = 1,
    Available = 1,
    UnderMaintenance = 2,
    Maintenance = 2,
    OutOfService = 3,
    Occupied = 4,
    Reserved = 5
}

public enum RoomOccupancyStatus
{
    Vacant = 1,
    Reserved = 2,
    Occupied = 3,
    Blocked = 4
}

public enum RoomClosureType
{
    Planned = 1,
    Emergency = 2
}

public enum RoomClosureStatus
{
    Pending = 1,
    Active = 2,
    Resolved = 3,
    Cancelled = 4
}

public enum AffectedBookingResolutionType
{
    RoomChange = 1,
    RoomUpgrade = 2,
    Refund = 3,
    OtherResolution = 4
}

public enum HousekeepingStatus
{
    Clean = 1,
    Dirty = 2,
    Cleaning = 3,
    Inspected = 4
}
