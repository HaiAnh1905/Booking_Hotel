using System;
using System.Linq;
using System.Threading.Tasks;
using BookingHotel.Bookings;
using BookingHotel.Frontdesk;
using BookingHotel.Hotels;
using BookingHotel.Identity;
using BookingHotel.Rooms;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Modularity;
using Volo.Abp.Timing;
using Volo.Abp.Users;
using Xunit;

namespace BookingHotel.Frontdesk;

public abstract class FrontdeskAppServiceTests<TStartupModule> : BookingHotelApplicationTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    protected IFrontdeskDashboardAppService DashboardAppService { get; }
    protected IFrontdeskRoomRackAppService RoomRackAppService { get; }
    protected IFrontdeskReservationAppService ReservationAppService { get; }
    protected IFrontdeskGuestAppService GuestAppService { get; }
    protected IRepository<Hotel, Guid> HotelRepository { get; }
    protected IRepository<RoomType, Guid> RoomTypeRepository { get; }
    protected IRepository<Room, Guid> RoomRepository { get; }
    protected IRepository<Booking, Guid> BookingRepository { get; }
    protected IRepository<UserHotelAssignment, Guid> AssignmentRepository { get; }
    protected ICurrentUser CurrentUser { get; }
    protected IClock Clock { get; }

    protected FrontdeskAppServiceTests()
    {
        DashboardAppService = GetRequiredService<IFrontdeskDashboardAppService>();
        RoomRackAppService = GetRequiredService<IFrontdeskRoomRackAppService>();
        ReservationAppService = GetRequiredService<IFrontdeskReservationAppService>();
        GuestAppService = GetRequiredService<IFrontdeskGuestAppService>();
        HotelRepository = GetRequiredService<IRepository<Hotel, Guid>>();
        RoomTypeRepository = GetRequiredService<IRepository<RoomType, Guid>>();
        RoomRepository = GetRequiredService<IRepository<Room, Guid>>();
        BookingRepository = GetRequiredService<IRepository<Booking, Guid>>();
        AssignmentRepository = GetRequiredService<IRepository<UserHotelAssignment, Guid>>();
        CurrentUser = GetRequiredService<ICurrentUser>();
        Clock = GetRequiredService<IClock>();
    }

    private async Task<(Hotel Hotel, RoomType RoomType, Room Room101, Room Room102)> SeedFrontdeskFixtureAsync(bool assignCurrentUser = true)
    {
        var hotelId = Guid.NewGuid();
        var suffix = hotelId.ToString("N")[..4].ToUpperInvariant();
        var hotel = new Hotel(
            hotelId,
            "Booking Hotel Frontdesk Test " + suffix,
            "BHFD-" + suffix,
            "100 Le Loi",
            "Ho Chi Minh",
            "02838220000",
            "fd@bookinghotel.vn",
            4,
            20,
            "Manager");

        await HotelRepository.InsertAsync(hotel, autoSave: true);

        if (assignCurrentUser && CurrentUser.Id.HasValue)
        {
            await AssignmentRepository.InsertAsync(
                new UserHotelAssignment(Guid.NewGuid(), CurrentUser.Id.Value, hotelId),
                autoSave: true);
        }

        var roomType = new RoomType(Guid.NewGuid(), hotelId, "Deluxe King", 1_200_000m);
        await RoomTypeRepository.InsertAsync(roomType, autoSave: true);

        var r101 = new Room(Guid.NewGuid(), hotelId, roomType.Id, "101", 1);
        var r102 = new Room(Guid.NewGuid(), hotelId, roomType.Id, "102", 1);
        await RoomRepository.InsertAsync(r101, autoSave: true);
        await RoomRepository.InsertAsync(r102, autoSave: true);

        return (hotel, roomType, r101, r102);
    }

    [Fact]
    public async Task Should_Complete_Full_Frontdesk_Stay_Lifecycle_Dashboard_RoomRack_Reservations_Guests_And_CheckInOut()
    {
        var (hotel, roomType, r101, r102) = await SeedFrontdeskFixtureAsync();
        var today = Clock.Now.Date;

        var booking = new Booking(
            Guid.NewGuid(),
            hotel.Id,
            "BK-FD-001",
            "Trần Thị Khách",
            "0909123456",
            "khach@example.com",
            today,
            today.AddDays(2),
            2_400_000m,
            depositAmount: 500_000m,
            bookingStatus: BookingStatus.PendingConfirmation,
            paymentStatus: PaymentStatus.PartiallyPaid,
            roomAssignmentStatus: RoomAssignmentStatus.Unassigned,
            roomTypeId: roomType.Id);

        await BookingRepository.InsertAsync(booking, autoSave: true);

        // 1. Dashboard shows 1 active booking, 1 pending, 1 expected arrival today
        var dash1 = await DashboardAppService.GetOverviewAsync(hotel.Id);
        dash1.TotalActiveBookings.ShouldBe(1);
        dash1.PendingConfirmationCount.ShouldBe(1);
        dash1.ExpectedArrivalsTodayCount.ShouldBe(1);
        dash1.InHouseGuestsCount.ShouldBe(0);

        // 2. Confirm booking
        var confirmed = await ReservationAppService.ConfirmAsync(booking.Id, new ConfirmFrontdeskReservationDto { HotelId = hotel.Id });
        confirmed.BookingStatus.ShouldBe(BookingStatus.Confirmed);

        // 3. Available rooms returns 101 and 102
        var availRooms = await ReservationAppService.GetAvailableRoomsAsync(booking.Id, hotel.Id);
        availRooms.Count.ShouldBe(2);

        // 4. Assign room 101 -> Room Rack shows 101 Reserved
        var assigned = await ReservationAppService.AssignRoomAsync(booking.Id, new AssignFrontdeskRoomDto
        {
            HotelId = hotel.Id,
            RoomId = r101.Id
        });
        assigned.AssignedRoomNumber.ShouldBe("101");

        var rackAfterAssign = await RoomRackAppService.GetAsync(new GetFrontdeskRoomRackInputDto { HotelId = hotel.Id });
        rackAfterAssign.ReservedCount.ShouldBe(1);
        rackAfterAssign.ReadyCount.ShouldBe(1);

        // 5. Update operational details (preserves TotalAmount)
        var updated = await ReservationAppService.UpdateAsync(booking.Id, new UpdateFrontdeskReservationDto
        {
            HotelId = hotel.Id,
            GuestFullName = "Trần Thị Khách",
            GuestPhone = "0909123456",
            GuestEmail = "khach.new@example.com",
            SpecialRequests = "Phòng yên tĩnh",
            FrontdeskNotes = "Khách đến lúc 14h"
        });
        updated.TotalAmount.ShouldBe(2_400_000m);
        updated.FrontdeskNotes.ShouldBe("Khách đến lúc 14h");

        // 6. Check-in with CCCD -> auto-masked, IsIdentityVerified = true, Room 101 Occupied
        var checkedIn = await ReservationAppService.CheckInAsync(booking.Id, new FrontdeskCheckInDto
        {
            HotelId = hotel.Id,
            RoomId = r101.Id,
            GuestFullName = "Trần Thị Khách",
            GuestPhone = "0909123456",
            GuestEmail = "khach.new@example.com",
            IdentityNumber = "079201001234",
            FrontdeskNotes = "Đã giao 2 thẻ từ"
        });
        checkedIn.BookingStatus.ShouldBe(BookingStatus.CheckedIn);
        checkedIn.IsIdentityVerified.ShouldBeTrue();
        checkedIn.GuestIdentityNumberMasked.ShouldBe("********1234");
        checkedIn.ActualCheckInTime.ShouldNotBeNull();

        var rackInHouse = await RoomRackAppService.GetAsync(new GetFrontdeskRoomRackInputDto { HotelId = hotel.Id });
        rackInHouse.OccupiedCount.ShouldBe(1);
        rackInHouse.Rooms.First(r => r.RoomId == r101.Id).CurrentBooking!.BookingCode.ShouldBe("BK-FD-001");

        // 7. Guest Management lookup & update sync
        var guests = await GuestAppService.GetListAsync(new GetFrontdeskGuestsInputDto { HotelId = hotel.Id, Keyword = "Khách" });
        guests.TotalCount.ShouldBe(1);
        var guestKey = guests.Items[0].GuestKey;

        var updatedGuest = await GuestAppService.UpdateAsync(guestKey, new UpdateFrontdeskGuestDto
        {
            HotelId = hotel.Id,
            FullName = "Trần Thị Khách VIP",
            PhoneNumber = "0909888777",
            Email = "vip@example.com",
            IdentityNumber = "079201009999"
        });
        updatedGuest.IdentityNumberMasked.ShouldBe("********9999");
        updatedGuest.GuestKey.ShouldBe("phone:0909888777");
        updatedGuest.Bookings.Count.ShouldBe(1);

        // 8. Check-out -> Booking Completed, Room 101 Vacant & Ready (Clean) per Q2 = Option A
        var checkedOut = await ReservationAppService.CheckOutAsync(booking.Id, new FrontdeskCheckOutDto
        {
            HotelId = hotel.Id,
            FrontdeskNotes = "Đã thu lại thẻ từ"
        });
        checkedOut.BookingStatus.ShouldBe(BookingStatus.Completed);
        checkedOut.ActualCheckOutTime.ShouldNotBeNull();

        var rackAfterCheckOut = await RoomRackAppService.GetAsync(new GetFrontdeskRoomRackInputDto { HotelId = hotel.Id });
        rackAfterCheckOut.OccupiedCount.ShouldBe(0);
        rackAfterCheckOut.ReadyCount.ShouldBe(2);
    }

    [Fact]
    public async Task Should_Cancel_Booking_With_Reason_And_Release_Assigned_Room()
    {
        var (hotel, roomType, r101, _) = await SeedFrontdeskFixtureAsync();
        var today = Clock.Now.Date;

        var booking = new Booking(
            Guid.NewGuid(),
            hotel.Id,
            "BK-FD-CANCEL",
            "Lê Văn Hủy",
            "0911222333",
            "cancel@example.com",
            today.AddDays(1),
            today.AddDays(3),
            2_400_000m,
            bookingStatus: BookingStatus.Confirmed,
            roomTypeId: roomType.Id);
        await BookingRepository.InsertAsync(booking, autoSave: true);

        await ReservationAppService.AssignRoomAsync(booking.Id, new AssignFrontdeskRoomDto
        {
            HotelId = hotel.Id,
            RoomId = r101.Id
        });

        var cancelled = await ReservationAppService.CancelAsync(booking.Id, new CancelFrontdeskReservationDto
        {
            HotelId = hotel.Id,
            Reason = "Khách thay đổi kế hoạch công tác"
        });

        cancelled.BookingStatus.ShouldBe(BookingStatus.Cancelled);
        cancelled.CancellationReason.ShouldBe("Khách thay đổi kế hoạch công tác");
        cancelled.RoomAssignmentStatus.ShouldBe(RoomAssignmentStatus.Unassigned);

        var rack = await RoomRackAppService.GetAsync(new GetFrontdeskRoomRackInputDto { HotelId = hotel.Id });
        rack.ReadyCount.ShouldBe(2);
        rack.ReservedCount.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Block_CrossHotel_Access_When_Not_Assigned()
    {
        var (unassignedHotel, _, _, _) = await SeedFrontdeskFixtureAsync(assignCurrentUser: false);

        var ex = await Should.ThrowAsync<BusinessException>(async () =>
            await DashboardAppService.GetOverviewAsync(unassignedHotel.Id));
        ex.Code.ShouldBe("BookingHotel:CrossHotelOperationForbidden");
    }
}
