using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BookingHotel.Bookings;
using BookingHotel.Guests;
using BookingHotel.Identity;
using BookingHotel.Permissions;
using BookingHotel.Rooms;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;

namespace BookingHotel.Manager.Guests;

[Authorize(BookingHotelPermissions.Guests.Default)]
public class ManagerGuestAppService : ManagerAppServiceBase, IManagerGuestAppService
{
    private readonly IRepository<Booking, Guid> _bookingRepository;
    private readonly IRepository<InternalGuestNote, Guid> _noteRepository;
    private readonly IRepository<RoomAssignment, Guid> _roomAssignmentRepository;
    private readonly IRepository<Room, Guid> _roomRepository;
    private readonly IRepository<RoomType, Guid> _roomTypeRepository;
    private readonly IdentityUserManager _userManager;

    public ManagerGuestAppService(
        IRepository<UserHotelAssignment, Guid> userHotelAssignmentRepository,
        IRepository<Booking, Guid> bookingRepository,
        IRepository<InternalGuestNote, Guid> noteRepository,
        IRepository<RoomAssignment, Guid> roomAssignmentRepository,
        IRepository<Room, Guid> roomRepository,
        IRepository<RoomType, Guid> roomTypeRepository,
        IdentityUserManager userManager)
        : base(userHotelAssignmentRepository)
    {
        _bookingRepository = bookingRepository;
        _noteRepository = noteRepository;
        _roomAssignmentRepository = roomAssignmentRepository;
        _roomRepository = roomRepository;
        _roomTypeRepository = roomTypeRepository;
        _userManager = userManager;
    }

    public async Task<PagedResultDto<ManagerGuestDto>> GetListAsync(Guid hotelId, string? keyword = null, int skipCount = 0, int maxResultCount = 20)
    {
        await CheckHotelManagerAccessAsync(hotelId);

        var query = await _bookingRepository.GetQueryableAsync();
        query = query.Where(b => b.HotelId == hotelId);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim();
            query = query.Where(b => b.GuestFullName.Contains(kw) || b.GuestPhone.Contains(kw) || b.GuestEmail.Contains(kw));
        }

        var allHotelBookings = query.ToList();

        // Nhóm khách hàng theo GuestId hoặc theo Phone (nếu chưa có User GuestId)
        var grouped = allHotelBookings
            .GroupBy(b => b.GuestId.HasValue && b.GuestId.Value != Guid.Empty ? b.GuestId.Value.ToString() : b.GuestPhone)
            .ToList();

        var totalCount = grouped.Count;

        var pagedGroups = grouped
            .OrderByDescending(g => g.Max(b => b.CheckInDate))
            .Skip(skipCount)
            .Take(maxResultCount)
            .ToList();

        var notes = await _noteRepository.GetListAsync(n => n.HotelId == hotelId);

        var items = new List<ManagerGuestDto>();
        foreach (var g in pagedGroups)
        {
            var latest = g.OrderByDescending(b => b.CheckInDate).First();
            var effectiveGuestId = latest.GuestId.HasValue && latest.GuestId.Value != Guid.Empty
                ? latest.GuestId.Value
                : latest.Id; // Fallback dùng ID booking đầu tiên làm key đại diện

            var hasNotes = notes.Any(n => n.GuestId == effectiveGuestId || (latest.GuestId.HasValue && n.GuestId == latest.GuestId.Value));
            var totalStays = g.Count(b => b.BookingStatus == BookingStatus.Completed || b.BookingStatus == BookingStatus.CheckedIn || b.BookingStatus == BookingStatus.Confirmed);
            var totalSpent = g.Where(b => b.BookingStatus == BookingStatus.Completed || b.BookingStatus == BookingStatus.CheckedIn).Sum(b => b.TotalAmount);

            items.Add(new ManagerGuestDto
            {
                GuestId = effectiveGuestId,
                FullName = latest.GuestFullName,
                PhoneNumber = latest.GuestPhone,
                Email = latest.GuestEmail,
                IdentityNumberMasked = !string.IsNullOrWhiteSpace(latest.GuestIdentityNumberMasked)
                    ? latest.GuestIdentityNumberMasked
                    : "***-***-****",
                TotalStaysAtThisHotel = totalStays,
                TotalSpentAtThisHotel = totalSpent,
                LastStayDate = latest.CheckInDate,
                HasInternalNotes = hasNotes
            });
        }

        return new PagedResultDto<ManagerGuestDto>(totalCount, items);
    }

    public async Task<List<GuestStayHistoryDto>> GetStayHistoryAsync(Guid guestId, Guid hotelId)
    {
        await CheckHotelManagerAccessAsync(hotelId);

        var bookings = await _bookingRepository.GetListAsync(b =>
            b.HotelId == hotelId &&
            (b.GuestId == guestId || b.Id == guestId));

        if (bookings.Count == 0)
        {
            // Thử tìm theo Phone nếu guestId là fallback
            var sample = await _bookingRepository.FindAsync(guestId);
            if (sample != null)
            {
                bookings = await _bookingRepository.GetListAsync(b => b.HotelId == hotelId && b.GuestPhone == sample.GuestPhone);
            }
        }

        var bookingIds = bookings.Select(b => b.Id).ToList();
        var assignments = await _roomAssignmentRepository.GetListAsync(a => bookingIds.Contains(a.BookingId) && a.IsActive);
        var roomIds = assignments.Select(a => a.RoomId).Distinct().ToList();
        var rooms = await _roomRepository.GetListAsync(r => roomIds.Contains(r.Id));

        var roomTypeIds = assignments.Select(a => a.RoomTypeId).Distinct().ToList();
        var roomTypes = await _roomTypeRepository.GetListAsync(rt => roomTypeIds.Contains(rt.Id));

        var result = new List<GuestStayHistoryDto>();
        foreach (var b in bookings.OrderByDescending(x => x.CheckInDate))
        {
            var assignment = assignments.FirstOrDefault(a => a.BookingId == b.Id);
            var room = assignment != null ? rooms.FirstOrDefault(r => r.Id == assignment.RoomId) : null;
            var roomType = assignment != null ? roomTypes.FirstOrDefault(rt => rt.Id == assignment.RoomTypeId) : null;

            result.Add(new GuestStayHistoryDto
            {
                BookingId = b.Id,
                BookingCode = b.BookingCode,
                RoomTypeName = roomType?.Name ?? "Standard",
                RoomNumber = room?.RoomNumber,
                CheckInDate = b.CheckInDate,
                CheckOutDate = b.CheckOutDate,
                TotalAmount = b.TotalAmount,
                BookingStatus = b.BookingStatus
            });
        }

        return result;
    }

    public async Task<List<InternalGuestNoteDto>> GetNotesAsync(Guid guestId, Guid hotelId)
    {
        await CheckHotelManagerAccessAsync(hotelId);

        var notes = await _noteRepository.GetListAsync(n => n.HotelId == hotelId && n.GuestId == guestId);

        var creatorIds = notes.Where(n => n.CreatorId.HasValue).Select(n => n.CreatorId!.Value).Distinct().ToList();
        var userDict = new Dictionary<Guid, string>();
        foreach (var uid in creatorIds)
        {
            var u = await _userManager.FindByIdAsync(uid.ToString());
            if (u != null)
            {
                var name = $"{u.Surname} {u.Name}".Trim();
                userDict[uid] = string.IsNullOrWhiteSpace(name) ? u.UserName : name;
            }
        }

        return notes.OrderByDescending(n => n.CreationTime).Select(n => new InternalGuestNoteDto
        {
            Id = n.Id,
            HotelId = n.HotelId,
            GuestId = n.GuestId,
            Category = n.Category,
            NoteText = n.NoteText,
            IsAlert = n.IsAlert,
            CreatorName = n.CreatorId.HasValue && userDict.ContainsKey(n.CreatorId.Value) ? userDict[n.CreatorId.Value] : "Staff",
            CreationTime = n.CreationTime,
            LastModificationTime = n.LastModificationTime
        }).ToList();
    }

    [Authorize(BookingHotelPermissions.Guests.ManageNotes)]
    public async Task<InternalGuestNoteDto> CreateNoteAsync(CreateInternalGuestNoteDto input)
    {
        await CheckHotelManagerAccessAsync(input.HotelId);

        var note = new InternalGuestNote(
            GuidGenerator.Create(),
            input.HotelId,
            input.GuestId,
            input.NoteText,
            input.Category,
            input.IsAlert,
            CurrentTenant.Id
        );

        await _noteRepository.InsertAsync(note);

        return new InternalGuestNoteDto
        {
            Id = note.Id,
            HotelId = note.HotelId,
            GuestId = note.GuestId,
            Category = note.Category,
            NoteText = note.NoteText,
            IsAlert = note.IsAlert,
            CreatorName = CurrentUser.UserName,
            CreationTime = Clock.Now
        };
    }

    [Authorize(BookingHotelPermissions.Guests.ManageNotes)]
    public async Task DeleteNoteAsync(Guid id, Guid hotelId)
    {
        await CheckHotelManagerAccessAsync(hotelId);

        var note = await _noteRepository.GetAsync(id);
        if (note.HotelId != hotelId)
        {
            throw new BusinessException("BookingHotel:CrossHotelOperationForbidden");
        }

        await _noteRepository.DeleteAsync(note);
    }
}
