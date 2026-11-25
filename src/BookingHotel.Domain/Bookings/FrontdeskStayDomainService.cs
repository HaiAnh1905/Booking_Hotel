using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using BookingHotel.Rooms;
using Volo.Abp;
using Volo.Abp.Domain.Services;

namespace BookingHotel.Bookings;

public class FrontdeskStayDomainService : DomainService
{
    private static readonly Regex ValidMaskedPattern = new(@"^\*{2,}[A-Za-z0-9]{4}$", RegexOptions.Compiled);
    private static readonly Regex DigitsOnlyRegex = new(@"\D+", RegexOptions.Compiled);

    public string MaskIdentityNumber(string? rawIdentity)
    {
        if (string.IsNullOrWhiteSpace(rawIdentity))
        {
            throw new BusinessException("BookingHotel:FrontdeskIdentityRequired");
        }

        var trimmed = rawIdentity.Trim().Replace(" ", string.Empty);
        if (trimmed.Length < 6 || trimmed.Length > 32)
        {
            throw new BusinessException("BookingHotel:FrontdeskIdentityRequired");
        }

        if (ValidMaskedPattern.IsMatch(trimmed))
        {
            return trimmed.ToUpperInvariant();
        }

        var visibleTail = trimmed[^4..].ToUpperInvariant();
        var maskLength = Math.Max(4, trimmed.Length - 4);
        return new string('*', maskLength) + visibleTail;
    }

    public void ValidateCanConfirm(Booking booking)
    {
        if (booking.BookingStatus != BookingStatus.PendingConfirmation)
        {
            throw new BusinessException("BookingHotel:FrontdeskInvalidBookingTransition")
                .WithData("CurrentStatus", booking.BookingStatus.ToString())
                .WithData("Action", "Confirm");
        }
    }

    public string ValidateCanCancel(Booking booking, string? reason)
    {
        if (booking.BookingStatus != BookingStatus.PendingConfirmation &&
            booking.BookingStatus != BookingStatus.Confirmed)
        {
            throw new BusinessException("BookingHotel:FrontdeskInvalidBookingTransition")
                .WithData("CurrentStatus", booking.BookingStatus.ToString())
                .WithData("Action", "Cancel");
        }

        var trimmed = reason?.Trim() ?? string.Empty;
        if (trimmed.Length < 5)
        {
            throw new BusinessException("BookingHotel:FrontdeskCancelReasonRequired");
        }

        return trimmed.Length > 1000 ? trimmed[..1000] : trimmed;
    }

    public void ValidateCanCheckIn(Booking booking)
    {
        if (booking.BookingStatus != BookingStatus.Confirmed)
        {
            throw new BusinessException("BookingHotel:FrontdeskInvalidBookingTransition")
                .WithData("CurrentStatus", booking.BookingStatus.ToString())
                .WithData("Action", "CheckIn");
        }
    }

    public void ValidateCanCheckOut(Booking booking)
    {
        if (booking.BookingStatus != BookingStatus.CheckedIn)
        {
            throw new BusinessException("BookingHotel:FrontdeskInvalidBookingTransition")
                .WithData("CurrentStatus", booking.BookingStatus.ToString())
                .WithData("Action", "CheckOut");
        }
    }

    public void ValidateRoomEligibleForAssignment(
        Room room,
        Guid? requiredRoomTypeId,
        bool requireReadyHousekeeping = false,
        bool allowOccupied = false)
    {
        if (requiredRoomTypeId.HasValue && room.RoomTypeId != requiredRoomTypeId.Value)
        {
            throw new BusinessException("BookingHotel:FrontdeskRoomTypeMismatch")
                .WithData("RoomNumber", room.RoomNumber);
        }

        if (!room.IsActive ||
            room.OperationalStatus == RoomOperationalStatus.UnderMaintenance ||
            room.OperationalStatus == RoomOperationalStatus.OutOfService ||
            room.OccupancyStatus == RoomOccupancyStatus.Blocked)
        {
            throw new BusinessException("BookingHotel:FrontdeskRoomUnavailable")
                .WithData("RoomNumber", room.RoomNumber);
        }

        if (!allowOccupied && room.OccupancyStatus == RoomOccupancyStatus.Occupied)
        {
            throw new BusinessException("BookingHotel:FrontdeskRoomUnavailable")
                .WithData("RoomNumber", room.RoomNumber);
        }

        if (requireReadyHousekeeping &&
            (room.HousekeepingStatus == HousekeepingStatus.Dirty ||
             room.HousekeepingStatus == HousekeepingStatus.Cleaning))
        {
            throw new BusinessException("BookingHotel:FrontdeskRoomUnavailable")
                .WithData("RoomNumber", room.RoomNumber);
        }
    }

    public bool HasOverlapWithAssignments(
        DateTime fromDate,
        DateTime toDate,
        IEnumerable<RoomAssignment> activeAssignments,
        Guid excludeBookingId)
    {
        return activeAssignments.Any(a =>
            a.IsActive &&
            a.BookingId != excludeBookingId &&
            a.FromDate < toDate &&
            a.ToDate > fromDate);
    }

    public bool HasOverlapWithClosures(
        DateTime fromDate,
        DateTime toDate,
        IEnumerable<RoomClosure> closures)
    {
        return closures.Any(c =>
            (c.Status == RoomClosureStatus.Pending || c.Status == RoomClosureStatus.Active) &&
            c.StartDate < toDate &&
            (!c.EndDate.HasValue || c.EndDate.Value > fromDate));
    }

    public FrontdeskRoomRackStatus ComputeRoomRackStatus(Room room)
    {
        if (!room.IsActive ||
            room.OperationalStatus == RoomOperationalStatus.UnderMaintenance ||
            room.OperationalStatus == RoomOperationalStatus.OutOfService ||
            room.OccupancyStatus == RoomOccupancyStatus.Blocked)
        {
            return FrontdeskRoomRackStatus.Unavailable;
        }

        if (room.OccupancyStatus == RoomOccupancyStatus.Occupied)
        {
            return FrontdeskRoomRackStatus.Occupied;
        }

        if (room.OccupancyStatus == RoomOccupancyStatus.Reserved)
        {
            return FrontdeskRoomRackStatus.Reserved;
        }

        if (room.HousekeepingStatus == HousekeepingStatus.Dirty ||
            room.HousekeepingStatus == HousekeepingStatus.Cleaning)
        {
            return FrontdeskRoomRackStatus.NotReady;
        }

        return FrontdeskRoomRackStatus.Ready;
    }

    public string NormalizeGuestKey(Guid? guestId, string? phone, Guid fallbackBookingId)
    {
        if (guestId.HasValue && guestId.Value != Guid.Empty)
        {
            return $"id:{guestId.Value:N}";
        }

        var digits = DigitsOnlyRegex.Replace(phone ?? string.Empty, string.Empty);
        if (!string.IsNullOrWhiteSpace(digits))
        {
            return $"phone:{digits}";
        }

        return $"booking:{fallbackBookingId:N}";
    }
}
