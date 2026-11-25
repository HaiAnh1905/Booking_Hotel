using System;
using BookingHotel.Bookings;
using BookingHotel.Rooms;
using Shouldly;
using Volo.Abp;
using Xunit;

namespace BookingHotel.Bookings;

public class FrontdeskStayDomainServiceTests
{
    private readonly FrontdeskStayDomainService _service = new();

    [Theory]
    [InlineData("079201001234", "********1234")]
    [InlineData("B1234567", "****4567")]
    [InlineData("079 201 001 234", "********1234")]
    [InlineData("********1234", "********1234")]
    public void MaskIdentityNumber_Should_Mask_And_Preserve_Last_Four_Chars(string input, string expected)
    {
        var masked = _service.MaskIdentityNumber(input);
        masked.ShouldBe(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("12345")]
    public void MaskIdentityNumber_Should_Throw_When_Missing_Or_Too_Short(string? input)
    {
        var ex = Should.Throw<BusinessException>(() => _service.MaskIdentityNumber(input));
        ex.Code.ShouldBe("BookingHotel:FrontdeskIdentityRequired");
    }

    [Fact]
    public void ValidateCanConfirm_Should_Allow_Only_PendingConfirmation()
    {
        var pending = CreateBooking(BookingStatus.PendingConfirmation);
        Should.NotThrow(() => _service.ValidateCanConfirm(pending));

        var confirmed = CreateBooking(BookingStatus.Confirmed);
        var ex = Should.Throw<BusinessException>(() => _service.ValidateCanConfirm(confirmed));
        ex.Code.ShouldBe("BookingHotel:FrontdeskInvalidBookingTransition");
    }

    [Fact]
    public void ValidateCanCancel_Should_Require_Valid_Status_And_Reason()
    {
        var confirmed = CreateBooking(BookingStatus.Confirmed);
        _service.ValidateCanCancel(confirmed, "  Khách đổi lịch trình  ").ShouldBe("Khách đổi lịch trình");

        var exShort = Should.Throw<BusinessException>(() => _service.ValidateCanCancel(confirmed, "Hủy"));
        exShort.Code.ShouldBe("BookingHotel:FrontdeskCancelReasonRequired");

        var checkedIn = CreateBooking(BookingStatus.CheckedIn);
        var exStatus = Should.Throw<BusinessException>(() => _service.ValidateCanCancel(checkedIn, "Lý do hủy hợp lệ"));
        exStatus.Code.ShouldBe("BookingHotel:FrontdeskInvalidBookingTransition");
    }

    [Fact]
    public void ValidateCanCheckIn_And_CheckOut_Should_Enforce_State_Machine()
    {
        var confirmed = CreateBooking(BookingStatus.Confirmed);
        Should.NotThrow(() => _service.ValidateCanCheckIn(confirmed));
        Should.Throw<BusinessException>(() => _service.ValidateCanCheckOut(confirmed))
            .Code.ShouldBe("BookingHotel:FrontdeskInvalidBookingTransition");

        var checkedIn = CreateBooking(BookingStatus.CheckedIn);
        Should.NotThrow(() => _service.ValidateCanCheckOut(checkedIn));
        Should.Throw<BusinessException>(() => _service.ValidateCanCheckIn(checkedIn))
            .Code.ShouldBe("BookingHotel:FrontdeskInvalidBookingTransition");
    }

    [Fact]
    public void ValidateRoomEligibleForAssignment_Should_Reject_Mismatch_And_Unavailable_Rooms()
    {
        var roomTypeId = Guid.NewGuid();
        var otherRoomTypeId = Guid.NewGuid();

        var readyRoom = new Room(Guid.NewGuid(), Guid.NewGuid(), roomTypeId, "101", 1);
        Should.NotThrow(() => _service.ValidateRoomEligibleForAssignment(readyRoom, roomTypeId, requireReadyHousekeeping: true));

        Should.Throw<BusinessException>(() => _service.ValidateRoomEligibleForAssignment(readyRoom, otherRoomTypeId))
            .Code.ShouldBe("BookingHotel:FrontdeskRoomTypeMismatch");

        var maintRoom = new Room(Guid.NewGuid(), Guid.NewGuid(), roomTypeId, "102", 1,
            operationalStatus: RoomOperationalStatus.UnderMaintenance);
        Should.Throw<BusinessException>(() => _service.ValidateRoomEligibleForAssignment(maintRoom, roomTypeId))
            .Code.ShouldBe("BookingHotel:FrontdeskRoomUnavailable");

        var occupiedRoom = new Room(Guid.NewGuid(), Guid.NewGuid(), roomTypeId, "103", 1,
            occupancyStatus: RoomOccupancyStatus.Occupied);
        Should.Throw<BusinessException>(() => _service.ValidateRoomEligibleForAssignment(occupiedRoom, roomTypeId))
            .Code.ShouldBe("BookingHotel:FrontdeskRoomUnavailable");
    }

    [Fact]
    public void OverlapDetection_Should_Catch_Assignment_And_Closure_Conflicts()
    {
        var bookingA = Guid.NewGuid();
        var bookingB = Guid.NewGuid();
        var roomId = Guid.NewGuid();
        var roomTypeId = Guid.NewGuid();

        var existing = new[]
        {
            new RoomAssignment(Guid.NewGuid(), bookingA, roomId, roomTypeId, new DateTime(2026, 11, 25), new DateTime(2026, 11, 27))
        };

        _service.HasOverlapWithAssignments(new DateTime(2026, 11, 26), new DateTime(2026, 11, 28), existing, bookingB).ShouldBeTrue();
        _service.HasOverlapWithAssignments(new DateTime(2026, 11, 27), new DateTime(2026, 11, 29), existing, bookingB).ShouldBeFalse();
        _service.HasOverlapWithAssignments(new DateTime(2026, 11, 26), new DateTime(2026, 11, 28), existing, bookingA).ShouldBeFalse();

        var closures = new[]
        {
            new RoomClosure(Guid.NewGuid(), Guid.NewGuid(), roomId, RoomClosureType.Planned, "Bảo trì",
                new DateTime(2026, 11, 26), new DateTime(2026, 11, 28))
        };
        _service.HasOverlapWithClosures(new DateTime(2026, 11, 25), new DateTime(2026, 11, 27), closures).ShouldBeTrue();
        _service.HasOverlapWithClosures(new DateTime(2026, 11, 28), new DateTime(2026, 11, 30), closures).ShouldBeFalse();
    }

    [Fact]
    public void ComputeRoomRackStatus_And_NormalizeGuestKey_Should_Be_Deterministic()
    {
        var readyRoom = new Room(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "201", 2);
        _service.ComputeRoomRackStatus(readyRoom).ShouldBe(FrontdeskRoomRackStatus.Ready);

        readyRoom.HousekeepingStatus = HousekeepingStatus.Dirty;
        _service.ComputeRoomRackStatus(readyRoom).ShouldBe(FrontdeskRoomRackStatus.NotReady);

        readyRoom.OccupancyStatus = RoomOccupancyStatus.Occupied;
        _service.ComputeRoomRackStatus(readyRoom).ShouldBe(FrontdeskRoomRackStatus.Occupied);

        readyRoom.OperationalStatus = RoomOperationalStatus.OutOfService;
        _service.ComputeRoomRackStatus(readyRoom).ShouldBe(FrontdeskRoomRackStatus.Unavailable);

        var guestGuid = Guid.NewGuid();
        _service.NormalizeGuestKey(guestGuid, "+84 901-234-567", Guid.NewGuid()).ShouldBe($"id:{guestGuid:N}");
        _service.NormalizeGuestKey(null, "+84 901-234-567", Guid.NewGuid()).ShouldBe("phone:84901234567");
    }

    private static Booking CreateBooking(BookingStatus status) =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "BK-TEST-001",
            "Nguyễn Văn Khách",
            "0901234567",
            "guest@example.com",
            DateTime.Today,
            DateTime.Today.AddDays(2),
            1_000_000m,
            bookingStatus: status);
}
