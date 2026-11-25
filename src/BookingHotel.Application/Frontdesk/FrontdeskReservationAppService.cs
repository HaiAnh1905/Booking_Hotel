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

namespace BookingHotel.Frontdesk;

[RemoteService(IsEnabled = false)]
[Authorize(BookingHotelPermissions.Bookings.Default)]
public class FrontdeskReservationAppService : FrontdeskAppServiceBase, IFrontdeskReservationAppService
{
    private readonly IRepository<Booking, Guid> _bookingRepository;
    private readonly IRepository<RoomAssignment, Guid> _roomAssignmentRepository;
    private readonly IRepository<Room, Guid> _roomRepository;
    private readonly IRepository<RoomType, Guid> _roomTypeRepository;
    private readonly IRepository<RoomClosure, Guid> _roomClosureRepository;
    private readonly FrontdeskStayDomainService _stayDomainService;

    public FrontdeskReservationAppService(
        IRepository<UserHotelAssignment, Guid> userHotelAssignmentRepository,
        IRepository<Booking, Guid> bookingRepository,
        IRepository<RoomAssignment, Guid> roomAssignmentRepository,
        IRepository<Room, Guid> roomRepository,
        IRepository<RoomType, Guid> roomTypeRepository,
        IRepository<RoomClosure, Guid> roomClosureRepository,
        FrontdeskStayDomainService stayDomainService)
        : base(userHotelAssignmentRepository)
    {
        _bookingRepository = bookingRepository;
        _roomAssignmentRepository = roomAssignmentRepository;
        _roomRepository = roomRepository;
        _roomTypeRepository = roomTypeRepository;
        _roomClosureRepository = roomClosureRepository;
        _stayDomainService = stayDomainService;
    }

    public async Task<PagedResultDto<FrontdeskReservationListItemDto>> GetListAsync(GetFrontdeskReservationsInputDto input)
    {
        await CheckFrontdeskHotelAccessAsync(input.HotelId);

        var bookings = await _bookingRepository.GetListAsync(b => b.HotelId == input.HotelId);
        var today = Clock.Now.Date;

        var filtered = bookings.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(input.Keyword))
        {
            var kw = input.Keyword.Trim();
            filtered = filtered.Where(b =>
                b.BookingCode.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                b.GuestFullName.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                b.GuestPhone.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                b.GuestEmail.Contains(kw, StringComparison.OrdinalIgnoreCase));
        }

        if (input.BookingStatus.HasValue)
        {
            filtered = filtered.Where(b => b.BookingStatus == input.BookingStatus.Value);
        }

        if (input.RoomAssignmentStatus.HasValue)
        {
            filtered = filtered.Where(b => b.RoomAssignmentStatus == input.RoomAssignmentStatus.Value);
        }

        if (input.FromDate.HasValue)
        {
            var from = input.FromDate.Value.Date;
            filtered = filtered.Where(b => b.CheckOutDate.Date >= from);
        }

        if (input.ToDate.HasValue)
        {
            var to = input.ToDate.Value.Date;
            filtered = filtered.Where(b => b.CheckInDate.Date <= to);
        }

        if (input.ArrivalTodayOnly == true)
        {
            filtered = filtered.Where(b => b.CheckInDate.Date == today);
        }

        if (input.DepartureTodayOnly == true)
        {
            filtered = filtered.Where(b => b.CheckOutDate.Date <= today && b.BookingStatus == BookingStatus.CheckedIn);
        }

        var ordered = filtered
            .OrderByDescending(b => b.BookingStatus == BookingStatus.PendingConfirmation)
            .ThenByDescending(b => b.BookingStatus == BookingStatus.CheckedIn)
            .ThenBy(b => b.CheckInDate)
            .ThenByDescending(b => b.CreationTime)
            .ToList();

        var totalCount = ordered.Count;
        var maxCount = input.MaxResultCount > 0 ? input.MaxResultCount : 20;
        var pageItems = ordered.Skip(input.SkipCount).Take(maxCount).ToList();

        var ids = pageItems.Select(b => b.Id).ToList();
        var assignments = ids.Count > 0
            ? await _roomAssignmentRepository.GetListAsync(a => ids.Contains(a.BookingId))
            : new List<RoomAssignment>();
        var rooms = await _roomRepository.GetListAsync(r => r.HotelId == input.HotelId);
        var roomTypes = await _roomTypeRepository.GetListAsync(rt => rt.HotelId == input.HotelId);

        var roomDict = rooms.ToDictionary(r => r.Id);
        var roomTypeDict = roomTypes.ToDictionary(rt => rt.Id);
        var assignmentsByBooking = assignments.GroupBy(a => a.BookingId).ToDictionary(g => g.Key, g => g.ToList());

        var result = pageItems.Select(b =>
        {
            assignmentsByBooking.TryGetValue(b.Id, out var bAssigns);
            var activeAssign = bAssigns?.FirstOrDefault(a => a.IsActive);
            var latestAssign = activeAssign ?? bAssigns?.OrderByDescending(a => a.CreationTime).FirstOrDefault();
            Room? assignedRoom = activeAssign != null && roomDict.TryGetValue(activeAssign.RoomId, out var r) ? r : null;
            var resolvedRoomTypeId = b.RoomTypeId ?? latestAssign?.RoomTypeId ?? assignedRoom?.RoomTypeId;
            var roomTypeName = resolvedRoomTypeId.HasValue && roomTypeDict.TryGetValue(resolvedRoomTypeId.Value, out var rt)
                ? rt.Name
                : string.Empty;

            var isReady = assignedRoom != null &&
                (_stayDomainService.ComputeRoomRackStatus(assignedRoom) is FrontdeskRoomRackStatus.Ready or FrontdeskRoomRackStatus.Reserved);

            return new FrontdeskReservationListItemDto
            {
                Id = b.Id,
                HotelId = b.HotelId,
                BookingCode = b.BookingCode,
                GuestFullName = b.GuestFullName,
                GuestPhone = b.GuestPhone,
                GuestEmail = b.GuestEmail,
                GuestIdentityNumberMasked = b.GuestIdentityNumberMasked,
                IsIdentityVerified = b.IsIdentityVerified,
                RoomTypeId = resolvedRoomTypeId,
                RoomTypeName = roomTypeName,
                AssignedRoomId = assignedRoom?.Id,
                AssignedRoomNumber = assignedRoom?.RoomNumber,
                AssignedRoomFloor = assignedRoom?.Floor,
                AssignedRoomIsReady = isReady,
                CheckInDate = b.CheckInDate,
                CheckOutDate = b.CheckOutDate,
                ActualCheckInTime = b.ActualCheckInTime,
                ActualCheckOutTime = b.ActualCheckOutTime,
                BookingStatus = b.BookingStatus,
                PaymentStatus = b.PaymentStatus,
                RoomAssignmentStatus = b.RoomAssignmentStatus,
                TotalAmount = b.TotalAmount,
                DepositAmount = b.DepositAmount,
                CreationTime = b.CreationTime
            };
        }).ToList();

        return new PagedResultDto<FrontdeskReservationListItemDto>(totalCount, result);
    }

    public async Task<FrontdeskReservationDetailDto> GetAsync(Guid id, Guid hotelId)
    {
        await CheckFrontdeskHotelAccessAsync(hotelId);
        var booking = await GetVerifiedBookingAsync(id, hotelId);
        return await BuildDetailDtoAsync(booking);
    }

    public async Task<List<FrontdeskAvailableRoomDto>> GetAvailableRoomsAsync(Guid id, Guid hotelId)
    {
        await CheckFrontdeskHotelAccessAsync(hotelId);
        var booking = await GetVerifiedBookingAsync(id, hotelId);

        var requiredRoomTypeId = await ResolveBookingRoomTypeIdAsync(booking);
        var rooms = await _roomRepository.GetListAsync(r => r.HotelId == hotelId && r.IsActive);
        var roomTypes = await _roomTypeRepository.GetListAsync(rt => rt.HotelId == hotelId);
        var roomTypeDict = roomTypes.ToDictionary(rt => rt.Id);

        var candidateRooms = rooms.Where(r =>
            (!requiredRoomTypeId.HasValue || r.RoomTypeId == requiredRoomTypeId.Value) &&
            r.OperationalStatus == RoomOperationalStatus.Active &&
            r.OccupancyStatus != RoomOccupancyStatus.Blocked &&
            r.OccupancyStatus != RoomOccupancyStatus.Occupied &&
            r.HousekeepingStatus != HousekeepingStatus.Dirty &&
            r.HousekeepingStatus != HousekeepingStatus.Cleaning).ToList();

        if (candidateRooms.Count == 0)
        {
            return new List<FrontdeskAvailableRoomDto>();
        }

        var candidateRoomIds = candidateRooms.Select(r => r.Id).ToList();
        var activeAssignments = await _roomAssignmentRepository.GetListAsync(a =>
            candidateRoomIds.Contains(a.RoomId) && a.IsActive);
        var closures = await _roomClosureRepository.GetListAsync(c =>
            c.HotelId == hotelId &&
            candidateRoomIds.Contains(c.RoomId) &&
            (c.Status == RoomClosureStatus.Pending || c.Status == RoomClosureStatus.Active));

        var assignmentsByRoom = activeAssignments.GroupBy(a => a.RoomId).ToDictionary(g => g.Key, g => g.ToList());
        var closuresByRoom = closures.GroupBy(c => c.RoomId).ToDictionary(g => g.Key, g => g.ToList());

        var available = new List<FrontdeskAvailableRoomDto>();
        foreach (var room in candidateRooms.OrderBy(r => r.Floor).ThenBy(r => r.RoomNumber, StringComparer.OrdinalIgnoreCase))
        {
            if (assignmentsByRoom.TryGetValue(room.Id, out var rAssigns) &&
                _stayDomainService.HasOverlapWithAssignments(booking.CheckInDate, booking.CheckOutDate, rAssigns, booking.Id))
            {
                continue;
            }

            if (closuresByRoom.TryGetValue(room.Id, out var rClosures) &&
                _stayDomainService.HasOverlapWithClosures(booking.CheckInDate, booking.CheckOutDate, rClosures))
            {
                continue;
            }

            available.Add(new FrontdeskAvailableRoomDto
            {
                RoomId = room.Id,
                RoomNumber = room.RoomNumber,
                Floor = room.Floor,
                Building = room.Building,
                RoomTypeId = room.RoomTypeId,
                RoomTypeName = roomTypeDict.TryGetValue(room.RoomTypeId, out var rt) ? rt.Name : string.Empty
            });
        }

        return available;
    }

    public async Task<FrontdeskReservationDetailDto> ConfirmAsync(Guid id, ConfirmFrontdeskReservationDto input)
    {
        await CheckFrontdeskHotelAccessAsync(input.HotelId);
        var booking = await GetVerifiedBookingAsync(id, input.HotelId);

        _stayDomainService.ValidateCanConfirm(booking);

        // Kiểm tra quỹ phòng khả dụng nếu booking xác định hạng phòng
        var requiredRoomTypeId = await ResolveBookingRoomTypeIdAsync(booking);
        if (requiredRoomTypeId.HasValue)
        {
            var activeTypeRooms = await _roomRepository.GetListAsync(r =>
                r.HotelId == input.HotelId &&
                r.RoomTypeId == requiredRoomTypeId.Value &&
                r.IsActive &&
                r.OperationalStatus == RoomOperationalStatus.Active &&
                r.OccupancyStatus != RoomOccupancyStatus.Blocked);

            var roomIds = activeTypeRooms.Select(r => r.Id).ToList();
            var closures = roomIds.Count > 0
                ? await _roomClosureRepository.GetListAsync(c =>
                    c.HotelId == input.HotelId &&
                    roomIds.Contains(c.RoomId) &&
                    (c.Status == RoomClosureStatus.Pending || c.Status == RoomClosureStatus.Active))
                : new List<RoomClosure>();

            var closedRoomIds = closures
                .Where(c => _stayDomainService.HasOverlapWithClosures(booking.CheckInDate, booking.CheckOutDate, new[] { c }))
                .Select(c => c.RoomId)
                .ToHashSet();

            var sellableCount = activeTypeRooms.Count(r => !closedRoomIds.Contains(r.Id));

            var overlappingBookings = await _bookingRepository.GetListAsync(b =>
                b.HotelId == input.HotelId &&
                b.Id != booking.Id &&
                b.RoomTypeId == requiredRoomTypeId.Value &&
                (b.BookingStatus == BookingStatus.Confirmed || b.BookingStatus == BookingStatus.CheckedIn) &&
                b.CheckInDate < booking.CheckOutDate &&
                b.CheckOutDate > booking.CheckInDate);

            if (sellableCount <= overlappingBookings.Count)
            {
                throw new BusinessException("BookingHotel:FrontdeskNoAvailableInventory");
            }
        }

        booking.BookingStatus = BookingStatus.Confirmed;
        await _bookingRepository.UpdateAsync(booking, autoSave: true);

        return await BuildDetailDtoAsync(booking);
    }

    public async Task<FrontdeskReservationDetailDto> AssignRoomAsync(Guid id, AssignFrontdeskRoomDto input)
    {
        await CheckFrontdeskHotelAccessAsync(input.HotelId);
        var booking = await GetVerifiedBookingAsync(id, input.HotelId);

        if (booking.BookingStatus != BookingStatus.PendingConfirmation &&
            booking.BookingStatus != BookingStatus.Confirmed &&
            booking.BookingStatus != BookingStatus.CheckedIn)
        {
            throw new BusinessException("BookingHotel:FrontdeskInvalidBookingTransition");
        }

        await AssignRoomInternalAsync(booking, input.RoomId, requireReadyHousekeeping: booking.BookingStatus == BookingStatus.CheckedIn);
        await _bookingRepository.UpdateAsync(booking, autoSave: true);

        return await BuildDetailDtoAsync(booking);
    }

    public async Task<FrontdeskReservationDetailDto> UpdateAsync(Guid id, UpdateFrontdeskReservationDto input)
    {
        await CheckFrontdeskHotelAccessAsync(input.HotelId);
        var booking = await GetVerifiedBookingAsync(id, input.HotelId);

        if (booking.BookingStatus == BookingStatus.Cancelled ||
            booking.BookingStatus == BookingStatus.Rejected ||
            booking.BookingStatus == BookingStatus.Completed ||
            booking.BookingStatus == BookingStatus.NoShow)
        {
            throw new BusinessException("BookingHotel:FrontdeskInvalidBookingTransition");
        }

        booking.GuestFullName = input.GuestFullName.Trim();
        booking.GuestPhone = input.GuestPhone.Trim();
        booking.GuestEmail = input.GuestEmail?.Trim() ?? string.Empty;
        booking.SpecialRequests = input.SpecialRequests?.Trim();
        booking.FrontdeskNotes = input.FrontdeskNotes?.Trim();

        await _bookingRepository.UpdateAsync(booking, autoSave: true);
        return await BuildDetailDtoAsync(booking);
    }

    [Authorize(BookingHotelPermissions.Bookings.Cancel)]
    public async Task<FrontdeskReservationDetailDto> CancelAsync(Guid id, CancelFrontdeskReservationDto input)
    {
        await CheckFrontdeskHotelAccessAsync(input.HotelId);
        var booking = await GetVerifiedBookingAsync(id, input.HotelId);

        var validReason = _stayDomainService.ValidateCanCancel(booking, input.Reason);
        booking.BookingStatus = BookingStatus.Cancelled;
        booking.CancellationReason = validReason;
        booking.RoomAssignmentStatus = RoomAssignmentStatus.Unassigned;

        var activeAssignments = await _roomAssignmentRepository.GetListAsync(a => a.BookingId == booking.Id && a.IsActive);
        var affectedRoomIds = new List<Guid>();
        foreach (var assign in activeAssignments)
        {
            assign.IsActive = false;
            await _roomAssignmentRepository.UpdateAsync(assign);
            affectedRoomIds.Add(assign.RoomId);
        }

        await _bookingRepository.UpdateAsync(booking, autoSave: true);

        foreach (var roomId in affectedRoomIds.Distinct())
        {
            await RecalculateRoomOccupancyStatusAsync(roomId);
        }

        return await BuildDetailDtoAsync(booking);
    }

    [Authorize(BookingHotelPermissions.Bookings.CheckIn)]
    public async Task<FrontdeskReservationDetailDto> CheckInAsync(Guid id, FrontdeskCheckInDto input)
    {
        await CheckFrontdeskHotelAccessAsync(input.HotelId);
        var booking = await GetVerifiedBookingAsync(id, input.HotelId);

        _stayDomainService.ValidateCanCheckIn(booking);

        var maskedIdentity = _stayDomainService.MaskIdentityNumber(input.IdentityNumber);

        await AssignRoomInternalAsync(booking, input.RoomId, requireReadyHousekeeping: true);

        booking.GuestFullName = input.GuestFullName.Trim();
        booking.GuestPhone = input.GuestPhone.Trim();
        booking.GuestEmail = input.GuestEmail?.Trim() ?? string.Empty;
        booking.GuestIdentityNumberMasked = maskedIdentity;
        booking.IsIdentityVerified = true;
        if (!string.IsNullOrWhiteSpace(input.FrontdeskNotes))
        {
            booking.FrontdeskNotes = input.FrontdeskNotes.Trim();
        }
        booking.BookingStatus = BookingStatus.CheckedIn;
        booking.ActualCheckInTime = Clock.Now;

        var targetRoom = await _roomRepository.GetAsync(input.RoomId);
        targetRoom.OccupancyStatus = RoomOccupancyStatus.Occupied;
        await _roomRepository.UpdateAsync(targetRoom);
        await _bookingRepository.UpdateAsync(booking, autoSave: true);

        return await BuildDetailDtoAsync(booking);
    }

    [Authorize(BookingHotelPermissions.Bookings.CheckOut)]
    public async Task<FrontdeskReservationDetailDto> CheckOutAsync(Guid id, FrontdeskCheckOutDto input)
    {
        await CheckFrontdeskHotelAccessAsync(input.HotelId);
        var booking = await GetVerifiedBookingAsync(id, input.HotelId);

        _stayDomainService.ValidateCanCheckOut(booking);

        booking.BookingStatus = BookingStatus.Completed;
        booking.ActualCheckOutTime = Clock.Now;
        if (!string.IsNullOrWhiteSpace(input.FrontdeskNotes))
        {
            booking.FrontdeskNotes = input.FrontdeskNotes.Trim();
        }

        var activeAssignments = await _roomAssignmentRepository.GetListAsync(a => a.BookingId == booking.Id && a.IsActive);
        var releasedRoomIds = new List<Guid>();
        foreach (var assign in activeAssignments)
        {
            assign.IsActive = false;
            await _roomAssignmentRepository.UpdateAsync(assign);
            releasedRoomIds.Add(assign.RoomId);
        }

        await _bookingRepository.UpdateAsync(booking, autoSave: true);

        // Q2 = Option A: Sau Check-out, phòng chuyển về Trống (hoặc Reserved nếu đã có đơn kế tiếp) và Sẵn sàng đón khách ngay (Clean)
        foreach (var roomId in releasedRoomIds.Distinct())
        {
            await RecalculateRoomOccupancyStatusAsync(roomId, setReadyAfterCheckOut: true);
        }

        return await BuildDetailDtoAsync(booking);
    }

    private async Task<Booking> GetVerifiedBookingAsync(Guid id, Guid hotelId)
    {
        var booking = await _bookingRepository.GetAsync(id);
        if (booking.HotelId != hotelId)
        {
            throw new BusinessException("BookingHotel:CrossHotelOperationForbidden");
        }
        return booking;
    }

    private async Task<Guid?> ResolveBookingRoomTypeIdAsync(Booking booking)
    {
        if (booking.RoomTypeId.HasValue)
        {
            return booking.RoomTypeId.Value;
        }

        var existingAssigns = await _roomAssignmentRepository.GetListAsync(a => a.BookingId == booking.Id);
        var latest = existingAssigns.OrderByDescending(a => a.IsActive).ThenByDescending(a => a.CreationTime).FirstOrDefault();
        return latest?.RoomTypeId;
    }

    private async Task AssignRoomInternalAsync(Booking booking, Guid roomId, bool requireReadyHousekeeping)
    {
        var targetRoom = await _roomRepository.GetAsync(roomId);
        if (targetRoom.HotelId != booking.HotelId)
        {
            throw new BusinessException("BookingHotel:CrossHotelOperationForbidden");
        }

        var existingAssignments = await _roomAssignmentRepository.GetListAsync(a => a.BookingId == booking.Id && a.IsActive);
        var alreadyAssignedToSameRoom = existingAssignments.Any(a => a.RoomId == roomId);

        var requiredRoomTypeId = await ResolveBookingRoomTypeIdAsync(booking);
        _stayDomainService.ValidateRoomEligibleForAssignment(
            targetRoom,
            requiredRoomTypeId,
            requireReadyHousekeeping: requireReadyHousekeeping,
            allowOccupied: alreadyAssignedToSameRoom && booking.BookingStatus == BookingStatus.CheckedIn);

        var roomActiveAssignments = await _roomAssignmentRepository.GetListAsync(a => a.RoomId == roomId && a.IsActive);
        if (_stayDomainService.HasOverlapWithAssignments(booking.CheckInDate, booking.CheckOutDate, roomActiveAssignments, booking.Id))
        {
            throw new BusinessException("BookingHotel:FrontdeskRoomOverlapConflict")
                .WithData("RoomNumber", targetRoom.RoomNumber);
        }

        var closures = await _roomClosureRepository.GetListAsync(c =>
            c.HotelId == booking.HotelId &&
            c.RoomId == roomId &&
            (c.Status == RoomClosureStatus.Pending || c.Status == RoomClosureStatus.Active));
        if (_stayDomainService.HasOverlapWithClosures(booking.CheckInDate, booking.CheckOutDate, closures))
        {
            throw new BusinessException("BookingHotel:FrontdeskRoomOverlapConflict")
                .WithData("RoomNumber", targetRoom.RoomNumber);
        }

        var oldRoomIds = new List<Guid>();
        if (!alreadyAssignedToSameRoom)
        {
            foreach (var oldAssign in existingAssignments)
            {
                oldAssign.IsActive = false;
                await _roomAssignmentRepository.UpdateAsync(oldAssign);
                oldRoomIds.Add(oldAssign.RoomId);
            }

            var newAssignment = new RoomAssignment(
                GuidGenerator.Create(),
                booking.Id,
                targetRoom.Id,
                targetRoom.RoomTypeId,
                booking.CheckInDate,
                booking.CheckOutDate,
                isActive: true,
                tenantId: CurrentTenant.Id);
            await _roomAssignmentRepository.InsertAsync(newAssignment);
        }

        // Backfill Booking.RoomTypeId nếu booking cũ chưa có (I1)
        if (!booking.RoomTypeId.HasValue)
        {
            booking.RoomTypeId = targetRoom.RoomTypeId;
        }
        booking.RoomAssignmentStatus = RoomAssignmentStatus.Assigned;

        if (booking.BookingStatus == BookingStatus.CheckedIn)
        {
            targetRoom.OccupancyStatus = RoomOccupancyStatus.Occupied;
            await _roomRepository.UpdateAsync(targetRoom);
        }
        else if (targetRoom.OccupancyStatus == RoomOccupancyStatus.Vacant)
        {
            targetRoom.OccupancyStatus = RoomOccupancyStatus.Reserved;
            await _roomRepository.UpdateAsync(targetRoom);
        }

        foreach (var oldRoomId in oldRoomIds.Distinct())
        {
            await RecalculateRoomOccupancyStatusAsync(oldRoomId);
        }
    }

    private async Task RecalculateRoomOccupancyStatusAsync(Guid roomId, bool setReadyAfterCheckOut = false)
    {
        var room = await _roomRepository.FindAsync(roomId);
        if (room == null)
        {
            return;
        }

        if (setReadyAfterCheckOut)
        {
            room.HousekeepingStatus = HousekeepingStatus.Clean;
        }

        if (room.OccupancyStatus == RoomOccupancyStatus.Blocked)
        {
            await _roomRepository.UpdateAsync(room);
            return;
        }

        var activeAssigns = await _roomAssignmentRepository.GetListAsync(a => a.RoomId == roomId && a.IsActive);
        if (activeAssigns.Count == 0)
        {
            room.OccupancyStatus = RoomOccupancyStatus.Vacant;
            await _roomRepository.UpdateAsync(room);
            return;
        }

        var bIds = activeAssigns.Select(a => a.BookingId).Distinct().ToList();
        var activeBookings = await _bookingRepository.GetListAsync(b => bIds.Contains(b.Id));

        if (activeBookings.Any(b => b.BookingStatus == BookingStatus.CheckedIn))
        {
            room.OccupancyStatus = RoomOccupancyStatus.Occupied;
        }
        else if (activeBookings.Any(b => b.BookingStatus == BookingStatus.Confirmed || b.BookingStatus == BookingStatus.PendingConfirmation))
        {
            room.OccupancyStatus = RoomOccupancyStatus.Reserved;
        }
        else
        {
            room.OccupancyStatus = RoomOccupancyStatus.Vacant;
        }

        await _roomRepository.UpdateAsync(room);
    }

    private async Task<FrontdeskReservationDetailDto> BuildDetailDtoAsync(Booking booking)
    {
        var assigns = await _roomAssignmentRepository.GetListAsync(a => a.BookingId == booking.Id);
        var activeAssign = assigns.FirstOrDefault(a => a.IsActive);
        var latestAssign = activeAssign ?? assigns.OrderByDescending(a => a.CreationTime).FirstOrDefault();

        Room? assignedRoom = activeAssign != null ? await _roomRepository.FindAsync(activeAssign.RoomId) : null;
        var resolvedRoomTypeId = booking.RoomTypeId ?? latestAssign?.RoomTypeId ?? assignedRoom?.RoomTypeId;
        var roomType = resolvedRoomTypeId.HasValue ? await _roomTypeRepository.FindAsync(resolvedRoomTypeId.Value) : null;

        var isReady = assignedRoom != null &&
            (_stayDomainService.ComputeRoomRackStatus(assignedRoom) is FrontdeskRoomRackStatus.Ready or FrontdeskRoomRackStatus.Reserved);

        return new FrontdeskReservationDetailDto
        {
            Id = booking.Id,
            HotelId = booking.HotelId,
            BookingCode = booking.BookingCode,
            GuestId = booking.GuestId,
            GuestKey = _stayDomainService.NormalizeGuestKey(booking.GuestId, booking.GuestPhone, booking.Id),
            GuestFullName = booking.GuestFullName,
            GuestPhone = booking.GuestPhone,
            GuestEmail = booking.GuestEmail,
            GuestIdentityNumberMasked = booking.GuestIdentityNumberMasked,
            IsIdentityVerified = booking.IsIdentityVerified,
            RoomTypeId = resolvedRoomTypeId,
            RoomTypeName = roomType?.Name ?? string.Empty,
            AssignedRoomId = assignedRoom?.Id,
            AssignedRoomNumber = assignedRoom?.RoomNumber,
            AssignedRoomFloor = assignedRoom?.Floor,
            AssignedRoomIsReady = isReady,
            CheckInDate = booking.CheckInDate,
            CheckOutDate = booking.CheckOutDate,
            ActualCheckInTime = booking.ActualCheckInTime,
            ActualCheckOutTime = booking.ActualCheckOutTime,
            BookingStatus = booking.BookingStatus,
            PaymentStatus = booking.PaymentStatus,
            RoomAssignmentStatus = booking.RoomAssignmentStatus,
            TotalAmount = booking.TotalAmount,
            DepositAmount = booking.DepositAmount,
            SpecialRequests = booking.SpecialRequests,
            FrontdeskNotes = booking.FrontdeskNotes,
            CancellationReason = booking.CancellationReason,
            RejectionReason = booking.RejectionReason,
            CreationTime = booking.CreationTime,
            LastModificationTime = booking.LastModificationTime
        };
    }
}
