using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using BookingHotel.Bookings;
using BookingHotel.Rooms;

namespace BookingHotel.Frontdesk;

public class GetFrontdeskRoomRackInputDto
{
    [Required]
    public Guid HotelId { get; set; }
    public int? Floor { get; set; }
    public Guid? RoomTypeId { get; set; }
    public FrontdeskRoomRackStatus? RackStatus { get; set; }
    [StringLength(128)]
    public string? Search { get; set; }
}

public class FrontdeskRoomBookingSummaryDto
{
    public Guid BookingId { get; set; }
    public string BookingCode { get; set; } = string.Empty;
    public string GuestFullName { get; set; } = string.Empty;
    public string GuestPhone { get; set; } = string.Empty;
    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }
    public BookingStatus BookingStatus { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal DepositAmount { get; set; }
    public int GuestCount { get; set; }
    public bool IsIdentityVerified { get; set; }
}

public class FrontdeskRoomRackItemDto
{
    public Guid RoomId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public int Floor { get; set; }
    public string? Building { get; set; }
    public Guid RoomTypeId { get; set; }
    public string RoomTypeName { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public int MaxOccupancy { get; set; }
    public double SizeSquareMeters { get; set; }
    public string? BedType { get; set; }
    public string? Description { get; set; }
    public FrontdeskRoomRackStatus RackStatus { get; set; }
    public RoomOccupancyStatus OccupancyStatus { get; set; }
    public RoomOperationalStatus OperationalStatus { get; set; }
    public bool IsReadyForCheckIn { get; set; }
    public string? UnavailableReason { get; set; }
    public FrontdeskRoomBookingSummaryDto? CurrentBooking { get; set; }
}

public class FrontdeskRoomRackDto
{
    public Guid HotelId { get; set; }
    public int TotalRooms { get; set; }
    public int ReadyCount { get; set; }
    public int ReservedCount { get; set; }
    public int OccupiedCount { get; set; }
    public int NotReadyCount { get; set; }
    public int UnavailableCount { get; set; }
    public List<int> Floors { get; set; } = new();
    public List<FrontdeskRoomRackItemDto> Rooms { get; set; } = new();
}
