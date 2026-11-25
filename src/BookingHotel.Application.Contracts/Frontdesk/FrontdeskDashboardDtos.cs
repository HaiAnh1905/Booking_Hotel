using System;
using System.Collections.Generic;
using BookingHotel.Bookings;

namespace BookingHotel.Frontdesk;

public class FrontdeskQuickBookingItemDto
{
    public Guid BookingId { get; set; }
    public string BookingCode { get; set; } = string.Empty;
    public string GuestFullName { get; set; } = string.Empty;
    public string GuestPhone { get; set; } = string.Empty;
    public string RoomTypeName { get; set; } = string.Empty;
    public Guid? AssignedRoomId { get; set; }
    public string? AssignedRoomNumber { get; set; }
    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }
    public BookingStatus BookingStatus { get; set; }
    public RoomAssignmentStatus RoomAssignmentStatus { get; set; }
    public bool IsIdentityVerified { get; set; }
}

public class FrontdeskDashboardDto
{
    public Guid HotelId { get; set; }
    public string HotelName { get; set; } = string.Empty;
    public string HotelCode { get; set; } = string.Empty;
    public DateTime BusinessDate { get; set; }
    public int TotalActiveBookings { get; set; }
    public int PendingConfirmationCount { get; set; }
    public int ExpectedArrivalsTodayCount { get; set; }
    public int InHouseGuestsCount { get; set; }
    public int ExpectedDeparturesTodayCount { get; set; }
    public List<FrontdeskQuickBookingItemDto> TodayArrivals { get; set; } = new();
    public List<FrontdeskQuickBookingItemDto> TodayDepartures { get; set; } = new();
    public List<FrontdeskQuickBookingItemDto> PendingBookings { get; set; } = new();
}
