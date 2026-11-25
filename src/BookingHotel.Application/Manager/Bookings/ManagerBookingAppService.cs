using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BookingHotel.Bookings;
using BookingHotel.Identity;
using BookingHotel.Permissions;
using BookingHotel.Rooms;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Repositories;

namespace BookingHotel.Manager.Bookings;

[Authorize(BookingHotelPermissions.Bookings.Default)]
public class ManagerBookingAppService : ManagerAppServiceBase, IManagerBookingAppService
{
    private readonly IRepository<Booking, Guid> _bookingRepository;
    private readonly IRepository<RoomAssignment, Guid> _roomAssignmentRepository;
    private readonly IRepository<Room, Guid> _roomRepository;
    private readonly IRepository<RoomType, Guid> _roomTypeRepository;

    public ManagerBookingAppService(
        IRepository<UserHotelAssignment, Guid> userHotelAssignmentRepository,
        IRepository<Booking, Guid> bookingRepository,
        IRepository<RoomAssignment, Guid> roomAssignmentRepository,
        IRepository<Room, Guid> roomRepository,
        IRepository<RoomType, Guid> roomTypeRepository)
        : base(userHotelAssignmentRepository)
    {
        _bookingRepository = bookingRepository;
        _roomAssignmentRepository = roomAssignmentRepository;
        _roomRepository = roomRepository;
        _roomTypeRepository = roomTypeRepository;
    }

    public async Task<PagedResultDto<ManagerBookingDto>> GetListAsync(GetManagerBookingsInputDto input)
    {
        await CheckHotelManagerAccessAsync(input.HotelId);

        var query = await _bookingRepository.GetQueryableAsync();
        query = query.Where(b => b.HotelId == input.HotelId);

        if (!string.IsNullOrWhiteSpace(input.BookingCode))
        {
            query = query.Where(b => b.BookingCode.Contains(input.BookingCode));
        }

        if (!string.IsNullOrWhiteSpace(input.GuestKeyword))
        {
            query = query.Where(b => b.GuestFullName.Contains(input.GuestKeyword) || b.GuestPhone.Contains(input.GuestKeyword));
        }

        if (input.BookingStatus.HasValue)
        {
            query = query.Where(b => b.BookingStatus == input.BookingStatus.Value);
        }

        if (input.PaymentStatus.HasValue)
        {
            query = query.Where(b => b.PaymentStatus == input.PaymentStatus.Value);
        }

        if (input.FromDate.HasValue)
        {
            query = query.Where(b => b.CheckInDate >= input.FromDate.Value);
        }

        if (input.ToDate.HasValue)
        {
            query = query.Where(b => b.CheckOutDate <= input.ToDate.Value);
        }

        var totalCount = query.Count();

        var bookings = query.OrderByDescending(b => b.CreationTime)
            .Skip(input.SkipCount)
            .Take(input.MaxResultCount)
            .ToList();

        var bookingIds = bookings.Select(b => b.Id).ToList();
        var assignments = await _roomAssignmentRepository.GetListAsync(a => bookingIds.Contains(a.BookingId) && a.IsActive);
        var roomIds = assignments.Select(a => a.RoomId).Distinct().ToList();
        var rooms = await _roomRepository.GetListAsync(r => roomIds.Contains(r.Id));

        var roomTypeIds = assignments.Select(a => a.RoomTypeId).Distinct().ToList();
        var roomTypes = await _roomTypeRepository.GetListAsync(rt => roomTypeIds.Contains(rt.Id));

        var items = new List<ManagerBookingDto>();
        foreach (var b in bookings)
        {
            var assignment = assignments.FirstOrDefault(a => a.BookingId == b.Id);
            var room = assignment != null ? rooms.FirstOrDefault(r => r.Id == assignment.RoomId) : null;
            var roomType = assignment != null ? roomTypes.FirstOrDefault(rt => rt.Id == assignment.RoomTypeId) : null;

            items.Add(new ManagerBookingDto
            {
                Id = b.Id,
                HotelId = b.HotelId,
                BookingCode = b.BookingCode,
                GuestFullName = b.GuestFullName,
                GuestPhone = b.GuestPhone,
                GuestEmail = b.GuestEmail,
                GuestIdentityNumberMasked = b.GuestIdentityNumberMasked,
                RoomTypeName = roomType?.Name ?? "Standard",
                AssignedRoomNumber = room?.RoomNumber,
                CheckInDate = b.CheckInDate,
                CheckOutDate = b.CheckOutDate,
                TotalAmount = b.TotalAmount,
                DepositAmount = b.DepositAmount,
                BookingStatus = b.BookingStatus,
                PaymentStatus = b.PaymentStatus,
                RoomAssignmentStatus = b.RoomAssignmentStatus,
                IsDirectBooking = b.IsDirectBooking,
                ContractNotes = b.ContractNotes,
                SpecialRequests = b.SpecialRequests,
                RejectionReason = b.RejectionReason,
                WaivePenaltyReason = b.WaivePenaltyReason,
                PenaltyAmount = b.PenaltyAmount,
                CreationTime = b.CreationTime
            });
        }

        return new PagedResultDto<ManagerBookingDto>(totalCount, items);
    }

    public async Task<ManagerBookingDto> GetAsync(Guid id, Guid hotelId)
    {
        await CheckHotelManagerAccessAsync(hotelId);

        var b = await _bookingRepository.GetAsync(id);
        if (b.HotelId != hotelId)
        {
            throw new BusinessException("BookingHotel:CrossHotelOperationForbidden");
        }

        var assignment = await _roomAssignmentRepository.FirstOrDefaultAsync(a => a.BookingId == b.Id && a.IsActive);
        var room = assignment != null ? await _roomRepository.FindAsync(assignment.RoomId) : null;
        var roomType = assignment != null ? await _roomTypeRepository.FindAsync(assignment.RoomTypeId) : null;

        return new ManagerBookingDto
        {
            Id = b.Id,
            HotelId = b.HotelId,
            BookingCode = b.BookingCode,
            GuestFullName = b.GuestFullName,
            GuestPhone = b.GuestPhone,
            GuestEmail = b.GuestEmail,
            GuestIdentityNumberMasked = b.GuestIdentityNumberMasked,
            RoomTypeName = roomType?.Name ?? "Standard",
            AssignedRoomNumber = room?.RoomNumber,
            CheckInDate = b.CheckInDate,
            CheckOutDate = b.CheckOutDate,
            TotalAmount = b.TotalAmount,
            DepositAmount = b.DepositAmount,
            BookingStatus = b.BookingStatus,
            PaymentStatus = b.PaymentStatus,
            RoomAssignmentStatus = b.RoomAssignmentStatus,
            IsDirectBooking = b.IsDirectBooking,
            ContractNotes = b.ContractNotes,
            SpecialRequests = b.SpecialRequests,
            RejectionReason = b.RejectionReason,
            WaivePenaltyReason = b.WaivePenaltyReason,
            PenaltyAmount = b.PenaltyAmount,
            CreationTime = b.CreationTime
        };
    }

    [Authorize(BookingHotelPermissions.Bookings.Create)]
    public async Task<ManagerBookingDto> CreateDirectBookingAsync(CreateDirectBookingDto input)
    {
        await CheckHotelManagerAccessAsync(input.HotelId);

        var nights = Math.Max(1, (input.CheckOutDate.Date - input.CheckInDate.Date).Days);
        var totalAmount = input.NumberOfRooms * input.AgreedRatePerNight * nights;

        var paymentStatus = PaymentStatus.Unpaid;
        if (input.DepositAmountReceived >= totalAmount && totalAmount > 0)
        {
            paymentStatus = PaymentStatus.Paid;
        }
        else if (input.DepositAmountReceived > 0)
        {
            paymentStatus = PaymentStatus.PartiallyPaid;
        }

        var maskedIdentity = string.Empty;
        if (!string.IsNullOrWhiteSpace(input.GuestIdentityNumber))
        {
            var raw = input.GuestIdentityNumber.Trim();
            maskedIdentity = raw.Length > 4 ? "***-***-" + raw[^4..] : raw;
        }

        var bookingCode = $"BK-{Clock.Now:yyyyMMdd}-DIR-{Random.Shared.Next(100, 999)}";

        var booking = new Booking(
            GuidGenerator.Create(),
            input.HotelId,
            bookingCode,
            input.GuestFullName,
            input.GuestPhone,
            input.GuestEmail ?? string.Empty,
            input.CheckInDate,
            input.CheckOutDate,
            totalAmount,
            input.DepositAmountReceived,
            BookingStatus.Confirmed,
            paymentStatus,
            RoomAssignmentStatus.Unassigned,
            isDirectBooking: true,
            guestIdentityNumberMasked: maskedIdentity,
            specialRequests: input.SpecialRequests,
            tenantId: CurrentTenant.Id,
            roomTypeId: input.RoomTypeId
        )
        {
            ContractNotes = input.ContractNotes
        };

        await _bookingRepository.InsertAsync(booking);

        // Tự động tìm phòng trống sẵn sàng bán để gán nếu có
        var candidateRooms = await _roomRepository.GetListAsync(r =>
            r.HotelId == input.HotelId &&
            r.RoomTypeId == input.RoomTypeId &&
            r.OperationalStatus == RoomOperationalStatus.Available);

        var assignedRoomNumber = (string?)null;
        var start = input.CheckInDate;
        var end = input.CheckOutDate;

        foreach (var r in candidateRooms)
        {
            var hasConflict = await _roomAssignmentRepository.AnyAsync(a =>
                a.RoomId == r.Id &&
                a.IsActive &&
                a.FromDate < end &&
                a.ToDate > start);

            if (!hasConflict)
            {
                var assignment = new RoomAssignment(
                    GuidGenerator.Create(),
                    booking.Id,
                    r.Id,
                    input.RoomTypeId,
                    start,
                    end,
                    isActive: true,
                    tenantId: CurrentTenant.Id
                );
                await _roomAssignmentRepository.InsertAsync(assignment);
                booking.RoomAssignmentStatus = RoomAssignmentStatus.Assigned;
                assignedRoomNumber = r.RoomNumber;
                await _bookingRepository.UpdateAsync(booking);
                break;
            }
        }

        var roomType = await _roomTypeRepository.FindAsync(input.RoomTypeId);

        return new ManagerBookingDto
        {
            Id = booking.Id,
            HotelId = booking.HotelId,
            BookingCode = booking.BookingCode,
            GuestFullName = booking.GuestFullName,
            GuestPhone = booking.GuestPhone,
            GuestEmail = booking.GuestEmail,
            GuestIdentityNumberMasked = booking.GuestIdentityNumberMasked,
            RoomTypeName = roomType?.Name ?? "Standard",
            AssignedRoomNumber = assignedRoomNumber,
            CheckInDate = booking.CheckInDate,
            CheckOutDate = booking.CheckOutDate,
            TotalAmount = booking.TotalAmount,
            DepositAmount = booking.DepositAmount,
            BookingStatus = booking.BookingStatus,
            PaymentStatus = booking.PaymentStatus,
            RoomAssignmentStatus = booking.RoomAssignmentStatus,
            IsDirectBooking = booking.IsDirectBooking,
            ContractNotes = booking.ContractNotes,
            SpecialRequests = booking.SpecialRequests,
            CreationTime = booking.CreationTime
        };
    }

    [Authorize(BookingHotelPermissions.Bookings.ApproveException)]
    public async Task<ManagerBookingDto> ConfirmAsync(Guid id, Guid hotelId)
    {
        await CheckHotelManagerAccessAsync(hotelId);

        var booking = await _bookingRepository.GetAsync(id);
        if (booking.HotelId != hotelId)
        {
            throw new BusinessException("BookingHotel:CrossHotelOperationForbidden");
        }

        if (booking.BookingStatus != BookingStatus.PendingConfirmation)
        {
            throw new BusinessException("BookingHotel:CannotConfirmNonPendingBooking");
        }

        booking.BookingStatus = BookingStatus.Confirmed;
        await _bookingRepository.UpdateAsync(booking);

        return await GetAsync(id, hotelId);
    }

    [Authorize(BookingHotelPermissions.Bookings.ApproveException)]
    public async Task<ManagerBookingDto> RejectAsync(Guid id, RejectBookingDto input)
    {
        await CheckHotelManagerAccessAsync(input.HotelId);

        var booking = await _bookingRepository.GetAsync(id);
        if (booking.HotelId != input.HotelId)
        {
            throw new BusinessException("BookingHotel:CrossHotelOperationForbidden");
        }

        if (booking.BookingStatus != BookingStatus.PendingConfirmation)
        {
            throw new BusinessException("BookingHotel:CannotRejectNonPendingBooking");
        }

        booking.BookingStatus = BookingStatus.Rejected;
        booking.RejectionReason = input.Reason;
        await _bookingRepository.UpdateAsync(booking);

        return await GetAsync(id, input.HotelId);
    }

    [Authorize(BookingHotelPermissions.Bookings.ApproveException)]
    public async Task<NoShowResultDto> MarkNoShowAsync(Guid id, MarkNoShowDto input)
    {
        await CheckHotelManagerAccessAsync(input.HotelId);

        var booking = await _bookingRepository.GetAsync(id);
        if (booking.HotelId != input.HotelId)
        {
            throw new BusinessException("BookingHotel:CrossHotelOperationForbidden");
        }

        if (booking.BookingStatus != BookingStatus.Confirmed)
        {
            throw new BusinessException("BookingHotel:CanOnlyMarkNoShowForConfirmedBooking");
        }

        // 1. Giải phóng các phân phòng (Release Room Assignments)
        var assignments = await _roomAssignmentRepository.GetListAsync(a => a.BookingId == id && a.IsActive);
        var releasedRooms = new List<string>();
        foreach (var a in assignments)
        {
            a.IsActive = false;
            await _roomAssignmentRepository.UpdateAsync(a);
            var room = await _roomRepository.FindAsync(a.RoomId);
            if (room != null)
            {
                releasedRooms.Add(room.RoomNumber);
            }
        }

        // 2. Quyết toán tiền phạt No-Show
        decimal penaltyApplied;
        if (input.WaivePenalty)
        {
            if (string.IsNullOrWhiteSpace(input.WaivePenaltyReason))
            {
                throw new BusinessException("BookingHotel:WaivePenaltyReasonRequired");
            }

            penaltyApplied = input.PenaltyAmountOverride ?? 0m;
            booking.WaivePenaltyReason = input.WaivePenaltyReason;
        }
        else
        {
            penaltyApplied = input.PenaltyAmountOverride ?? booking.DepositAmount;
        }

        var refundInitiated = Math.Max(0m, booking.DepositAmount - penaltyApplied);

        booking.PenaltyAmount = penaltyApplied;
        booking.BookingStatus = BookingStatus.NoShow;
        if (refundInitiated > 0)
        {
            booking.PaymentStatus = PaymentStatus.Refunded;
        }

        await _bookingRepository.UpdateAsync(booking);

        var auditMsg = $"[AUDIT] Manager {CurrentUser.UserName} ({CurrentUser.Id}) processed No-Show for booking {booking.BookingCode}. WaivePenalty={input.WaivePenalty}, Reason={input.WaivePenaltyReason}, PenaltyApplied={penaltyApplied:N0} VNĐ, Refund={refundInitiated:N0} VNĐ.";

        return new NoShowResultDto
        {
            BookingId = booking.Id,
            BookingStatus = booking.BookingStatus,
            PaymentStatus = booking.PaymentStatus,
            PenaltyAmountApplied = penaltyApplied,
            RefundAmountInitiated = refundInitiated,
            ReleasedRoomNumbers = releasedRooms,
            AuditMessage = auditMsg
        };
    }
}
