using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using BookingHotel.Bookings;
using BookingHotel.Identity;
using BookingHotel.Manager.Rooms.Jobs;
using BookingHotel.Permissions;
using BookingHotel.Rooms;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;

namespace BookingHotel.Manager.Rooms;

[Authorize(BookingHotelPermissions.Rooms.Default)]
public class ManagerRoomAppService : ManagerAppServiceBase, IManagerRoomAppService
{
    private readonly IRepository<RoomType, Guid> _roomTypeRepository;
    private readonly IRepository<Room, Guid> _roomRepository;
    private readonly IRepository<RoomAssignment, Guid> _roomAssignmentRepository;
    private readonly IRepository<Booking, Guid> _bookingRepository;
    private readonly IRepository<RoomClosure, Guid> _closureRepository;
    private readonly IRepository<AffectedBookingResolution, Guid> _resolutionRepository;
    private readonly IRepository<IdentityUser, Guid> _userRepository;
    private readonly IBackgroundJobManager _backgroundJobManager;

    public ManagerRoomAppService(
        IRepository<UserHotelAssignment, Guid> userHotelAssignmentRepository,
        IRepository<RoomType, Guid> roomTypeRepository,
        IRepository<Room, Guid> roomRepository,
        IRepository<RoomAssignment, Guid> roomAssignmentRepository,
        IRepository<Booking, Guid> bookingRepository,
        IRepository<RoomClosure, Guid> closureRepository,
        IRepository<AffectedBookingResolution, Guid> resolutionRepository,
        IRepository<IdentityUser, Guid> userRepository,
        IBackgroundJobManager backgroundJobManager)
        : base(userHotelAssignmentRepository)
    {
        _roomTypeRepository = roomTypeRepository;
        _roomRepository = roomRepository;
        _roomAssignmentRepository = roomAssignmentRepository;
        _bookingRepository = bookingRepository;
        _closureRepository = closureRepository;
        _resolutionRepository = resolutionRepository;
        _userRepository = userRepository;
        _backgroundJobManager = backgroundJobManager;
    }

    #region Room Types (Loại Phòng)

    public async Task<List<RoomTypeDto>> GetRoomTypesAsync(Guid hotelId)
    {
        await CheckHotelManagerAccessAsync(hotelId);

        var roomTypes = await _roomTypeRepository.GetListAsync(rt => rt.HotelId == hotelId);
        var rooms = await _roomRepository.GetListAsync(r => r.HotelId == hotelId);

        var result = new List<RoomTypeDto>();
        foreach (var rt in roomTypes)
        {
            List<string> amenities = new();
            if (!string.IsNullOrWhiteSpace(rt.AmenitiesJson))
            {
                try { amenities = JsonSerializer.Deserialize<List<string>>(rt.AmenitiesJson) ?? new(); }
                catch { }
            }

            result.Add(new RoomTypeDto
            {
                Id = rt.Id,
                HotelId = rt.HotelId,
                Name = rt.Name,
                Description = rt.Description,
                StandardOccupancy = rt.StandardOccupancy,
                MaxOccupancy = rt.MaxOccupancy,
                BasePrice = rt.BasePrice,
                SizeSquareMeters = rt.SizeSquareMeters,
                Amenities = amenities,
                TotalRoomCount = rooms.Count(r => r.RoomTypeId == rt.Id)
            });
        }

        return result;
    }

    [Authorize(BookingHotelPermissions.Rooms.Create)]
    public async Task<RoomTypeDto> CreateRoomTypeAsync(CreateUpdateRoomTypeDto input)
    {
        await CheckHotelManagerAccessAsync(input.HotelId);

        var amenitiesJson = input.Amenities != null ? JsonSerializer.Serialize(input.Amenities) : "[]";
        var roomType = new RoomType(
            GuidGenerator.Create(),
            input.HotelId,
            input.Name,
            input.BasePrice,
            input.StandardOccupancy,
            input.MaxOccupancy,
            input.SizeSquareMeters,
            input.Description,
            amenitiesJson,
            CurrentTenant.Id
        );

        await _roomTypeRepository.InsertAsync(roomType);

        return new RoomTypeDto
        {
            Id = roomType.Id,
            HotelId = roomType.HotelId,
            Name = roomType.Name,
            Description = roomType.Description,
            StandardOccupancy = roomType.StandardOccupancy,
            MaxOccupancy = roomType.MaxOccupancy,
            BasePrice = roomType.BasePrice,
            SizeSquareMeters = roomType.SizeSquareMeters,
            Amenities = input.Amenities ?? new(),
            TotalRoomCount = 0
        };
    }

    [Authorize(BookingHotelPermissions.Rooms.Edit)]
    public async Task<RoomTypeDto> UpdateRoomTypeAsync(Guid id, CreateUpdateRoomTypeDto input)
    {
        await CheckHotelManagerAccessAsync(input.HotelId);

        var roomType = await _roomTypeRepository.GetAsync(id);
        if (roomType.HotelId != input.HotelId)
        {
            throw new BusinessException("BookingHotel:CrossHotelOperationForbidden");
        }

        roomType.Name = input.Name;
        roomType.Description = input.Description;
        roomType.StandardOccupancy = input.StandardOccupancy;
        roomType.MaxOccupancy = input.MaxOccupancy;
        roomType.BasePrice = input.BasePrice;
        roomType.SizeSquareMeters = input.SizeSquareMeters;
        roomType.AmenitiesJson = input.Amenities != null ? JsonSerializer.Serialize(input.Amenities) : "[]";

        await _roomTypeRepository.UpdateAsync(roomType);

        var roomCount = await _roomRepository.CountAsync(r => r.RoomTypeId == id);

        return new RoomTypeDto
        {
            Id = roomType.Id,
            HotelId = roomType.HotelId,
            Name = roomType.Name,
            Description = roomType.Description,
            StandardOccupancy = roomType.StandardOccupancy,
            MaxOccupancy = roomType.MaxOccupancy,
            BasePrice = roomType.BasePrice,
            SizeSquareMeters = roomType.SizeSquareMeters,
            Amenities = input.Amenities ?? new(),
            TotalRoomCount = roomCount
        };
    }

    [Authorize(BookingHotelPermissions.Rooms.Delete)]
    public async Task DeleteRoomTypeAsync(Guid id, Guid hotelId)
    {
        await CheckHotelManagerAccessAsync(hotelId);

        var hasRooms = await _roomRepository.AnyAsync(r => r.RoomTypeId == id);
        if (hasRooms)
        {
            throw new BusinessException("BookingHotel:CannotDeleteRoomTypeWithExistingRooms");
        }

        var roomType = await _roomTypeRepository.GetAsync(id);
        if (roomType.HotelId != hotelId)
        {
            throw new BusinessException("BookingHotel:CrossHotelOperationForbidden");
        }

        await _roomTypeRepository.DeleteAsync(roomType);
    }

    #endregion

    #region Dashboard Overview (RM-010 US1)

    public async Task<RoomDashboardOverviewDto> GetDashboardOverviewAsync(Guid hotelId)
    {
        await CheckHotelManagerAccessAsync(hotelId);

        var rooms = await _roomRepository.GetListAsync(r => r.HotelId == hotelId);
        var totalRooms = rooms.Count;

        var occupiedRooms = rooms.Count(r => r.OccupancyStatus == RoomOccupancyStatus.Occupied);
        var vacantRooms = rooms.Count(r => r.OccupancyStatus == RoomOccupancyStatus.Vacant && r.OperationalStatus == RoomOperationalStatus.Active && r.IsActive);
        var reservedRooms = rooms.Count(r => r.OccupancyStatus == RoomOccupancyStatus.Reserved);
        var unavailableRooms = rooms.Count(r => !r.IsActive || r.OperationalStatus == RoomOperationalStatus.UnderMaintenance || r.OperationalStatus == RoomOperationalStatus.OutOfService || r.OccupancyStatus == RoomOccupancyStatus.Blocked);

        var availableInventory = Math.Max(0, totalRooms - rooms.Count(r => r.OperationalStatus == RoomOperationalStatus.OutOfService));
        var occupancyRate = availableInventory > 0
            ? Math.Round((double)occupiedRooms / availableInventory * 100, 1)
            : 0.0;

        var occupancyBreakdown = new Dictionary<string, int>
        {
            ["Vacant"] = rooms.Count(r => r.OccupancyStatus == RoomOccupancyStatus.Vacant),
            ["Reserved"] = reservedRooms,
            ["Occupied"] = occupiedRooms,
            ["Blocked"] = rooms.Count(r => r.OccupancyStatus == RoomOccupancyStatus.Blocked)
        };

        var operationalBreakdown = new Dictionary<string, int>
        {
            ["Active"] = rooms.Count(r => r.OperationalStatus == RoomOperationalStatus.Active),
            ["UnderMaintenance"] = rooms.Count(r => r.OperationalStatus == RoomOperationalStatus.UnderMaintenance),
            ["OutOfService"] = rooms.Count(r => r.OperationalStatus == RoomOperationalStatus.OutOfService)
        };

        return new RoomDashboardOverviewDto
        {
            HotelId = hotelId,
            TotalRooms = totalRooms,
            OccupiedRooms = occupiedRooms,
            VacantRooms = vacantRooms,
            ReservedRooms = reservedRooms,
            UnavailableRooms = unavailableRooms,
            OccupancyRate = occupancyRate,
            OccupancyBreakdown = occupancyBreakdown,
            OperationalBreakdown = operationalBreakdown
        };
    }

    #endregion

    #region Physical Rooms (RM-010 US2)

    public async Task<List<RoomDto>> GetPhysicalRoomsAsync(
        Guid hotelId,
        int? floor = null,
        Guid? roomTypeId = null,
        RoomOccupancyStatus? occupancyStatus = null,
        RoomOperationalStatus? operationalStatus = null,
        string? search = null)
    {
        await CheckHotelManagerAccessAsync(hotelId);

        var rooms = await _roomRepository.GetListAsync(r =>
            r.HotelId == hotelId &&
            (!floor.HasValue || r.Floor == floor.Value) &&
            (!roomTypeId.HasValue || r.RoomTypeId == roomTypeId.Value) &&
            (!occupancyStatus.HasValue || r.OccupancyStatus == occupancyStatus.Value) &&
            (!operationalStatus.HasValue || r.OperationalStatus == operationalStatus.Value) &&
            (string.IsNullOrWhiteSpace(search) || r.RoomNumber.Contains(search) || (r.Building != null && r.Building.Contains(search))));

        var roomTypeIds = rooms.Select(r => r.RoomTypeId).Distinct().ToList();
        var roomTypes = await _roomTypeRepository.GetListAsync(rt => roomTypeIds.Contains(rt.Id));

        var roomIds = rooms.Select(r => r.Id).ToList();
        var activeAssignments = await _roomAssignmentRepository.GetListAsync(a => roomIds.Contains(a.RoomId) && a.IsActive);
        var bookingIds = activeAssignments.Select(a => a.BookingId).Distinct().ToList();
        var bookings = await _bookingRepository.GetListAsync(b => bookingIds.Contains(b.Id));

        var result = new List<RoomDto>();
        foreach (var r in rooms.OrderBy(x => x.Floor).ThenBy(x => x.RoomNumber))
        {
            var roomType = roomTypes.FirstOrDefault(rt => rt.Id == r.RoomTypeId);
            var isSellable = r.IsActive &&
                             r.OperationalStatus == RoomOperationalStatus.Active &&
                             r.OccupancyStatus != RoomOccupancyStatus.Blocked &&
                             (r.HousekeepingStatus == HousekeepingStatus.Clean || r.HousekeepingStatus == HousekeepingStatus.Inspected);

            string? occupantName = null;
            if (r.OccupancyStatus == RoomOccupancyStatus.Occupied)
            {
                var assignment = activeAssignments.FirstOrDefault(a => a.RoomId == r.Id);
                if (assignment != null)
                {
                    var booking = bookings.FirstOrDefault(b => b.Id == assignment.BookingId);
                    occupantName = booking?.GuestFullName;
                }
            }

            result.Add(new RoomDto
            {
                Id = r.Id,
                HotelId = r.HotelId,
                RoomTypeId = r.RoomTypeId,
                RoomTypeName = roomType?.Name ?? string.Empty,
                RoomNumber = r.RoomNumber,
                Floor = r.Floor,
                OperationalStatus = r.OperationalStatus,
                OccupancyStatus = r.OccupancyStatus,
                HousekeepingStatus = r.HousekeepingStatus,
                IsActive = r.IsActive,
                Building = r.Building,
                Description = r.Description,
                IsSellable = isSellable,
                CurrentOccupantName = occupantName,
                MaintenanceReason = r.MaintenanceReason,
                MaintenanceEndDate = r.MaintenanceEndDate,
                OutOfServiceReason = r.OutOfServiceReason,
                OutOfServiceEndDate = r.OutOfServiceEndDate
            });
        }

        return result;
    }

    [Authorize(BookingHotelPermissions.Rooms.Create)]
    public async Task<RoomDto> CreatePhysicalRoomAsync(CreateUpdateRoomDto input)
    {
        await CheckHotelManagerAccessAsync(input.HotelId);

        var exists = await _roomRepository.AnyAsync(r => r.HotelId == input.HotelId && r.RoomNumber == input.RoomNumber);
        if (exists)
        {
            throw new BusinessException("BookingHotel:DuplicateRoomNumber")
                .WithData("RoomNumber", input.RoomNumber);
        }

        var roomType = await _roomTypeRepository.GetAsync(input.RoomTypeId);
        if (roomType.HotelId != input.HotelId)
        {
            throw new BusinessException("BookingHotel:CrossHotelOperationForbidden");
        }

        var room = new Room(
            GuidGenerator.Create(),
            input.HotelId,
            input.RoomTypeId,
            input.RoomNumber,
            input.Floor,
            RoomOperationalStatus.Active,
            HousekeepingStatus.Clean,
            CurrentTenant.Id,
            RoomOccupancyStatus.Vacant,
            true,
            input.Building,
            input.Description
        );

        await _roomRepository.InsertAsync(room);

        return new RoomDto
        {
            Id = room.Id,
            HotelId = room.HotelId,
            RoomTypeId = room.RoomTypeId,
            RoomTypeName = roomType.Name,
            RoomNumber = room.RoomNumber,
            Floor = room.Floor,
            OperationalStatus = room.OperationalStatus,
            OccupancyStatus = room.OccupancyStatus,
            HousekeepingStatus = room.HousekeepingStatus,
            IsActive = room.IsActive,
            Building = room.Building,
            Description = room.Description,
            IsSellable = true
        };
    }

    [Authorize(BookingHotelPermissions.Rooms.Edit)]
    public async Task<RoomDto> UpdatePhysicalRoomAsync(Guid id, CreateUpdateRoomDto input)
    {
        await CheckHotelManagerAccessAsync(input.HotelId);

        var room = await _roomRepository.GetAsync(id);
        if (room.HotelId != input.HotelId)
        {
            throw new BusinessException("BookingHotel:CrossHotelOperationForbidden");
        }

        var exists = await _roomRepository.AnyAsync(r => r.HotelId == input.HotelId && r.RoomNumber == input.RoomNumber && r.Id != id);
        if (exists)
        {
            throw new BusinessException("BookingHotel:DuplicateRoomNumber")
                .WithData("RoomNumber", input.RoomNumber);
        }

        var roomType = await _roomTypeRepository.GetAsync(input.RoomTypeId);
        if (roomType.HotelId != input.HotelId)
        {
            throw new BusinessException("BookingHotel:CrossHotelOperationForbidden");
        }

        room.RoomTypeId = input.RoomTypeId;
        room.RoomNumber = input.RoomNumber;
        room.Floor = input.Floor;
        room.Building = input.Building;
        room.Description = input.Description;

        await _roomRepository.UpdateAsync(room);

        return new RoomDto
        {
            Id = room.Id,
            HotelId = room.HotelId,
            RoomTypeId = room.RoomTypeId,
            RoomTypeName = roomType.Name,
            RoomNumber = room.RoomNumber,
            Floor = room.Floor,
            OperationalStatus = room.OperationalStatus,
            OccupancyStatus = room.OccupancyStatus,
            HousekeepingStatus = room.HousekeepingStatus,
            IsActive = room.IsActive,
            Building = room.Building,
            Description = room.Description,
            IsSellable = room.IsActive && room.OperationalStatus == RoomOperationalStatus.Active &&
                         (room.HousekeepingStatus == HousekeepingStatus.Clean || room.HousekeepingStatus == HousekeepingStatus.Inspected)
        };
    }

    [Authorize(BookingHotelPermissions.Rooms.Edit)]
    public async Task DeactivateRoomAsync(Guid id, DeactivateRoomDto input)
    {
        await CheckHotelManagerAccessAsync(input.HotelId);

        var room = await _roomRepository.GetAsync(id);
        if (room.HotelId != input.HotelId)
        {
            throw new BusinessException("BookingHotel:CrossHotelOperationForbidden");
        }

        if (room.OccupancyStatus == RoomOccupancyStatus.Occupied)
        {
            throw new BusinessException("RoomManagement:CannotDeactivateOccupied");
        }

        var hasFutureBookings = await _roomAssignmentRepository.AnyAsync(a =>
            a.RoomId == id && a.IsActive && a.ToDate > Clock.Now);

        if (hasFutureBookings)
        {
            throw new BusinessException("RoomManagement:CannotDeactivateOccupied");
        }

        room.IsActive = false;
        await _roomRepository.UpdateAsync(room, autoSave: true);
    }

    [Authorize(BookingHotelPermissions.Rooms.Edit)]
    public async Task ActivateRoomAsync(Guid id, Guid hotelId)
    {
        await CheckHotelManagerAccessAsync(hotelId);

        var room = await _roomRepository.GetAsync(id);
        if (room.HotelId != hotelId)
        {
            throw new BusinessException("BookingHotel:CrossHotelOperationForbidden");
        }

        room.IsActive = true;
        await _roomRepository.UpdateAsync(room, autoSave: true);
    }

    [Authorize(BookingHotelPermissions.Rooms.Delete)]
    public async Task DeletePhysicalRoomAsync(Guid id, Guid hotelId)
    {
        await CheckHotelManagerAccessAsync(hotelId);

        var room = await _roomRepository.GetAsync(id);
        if (room.HotelId != hotelId)
        {
            throw new BusinessException("BookingHotel:CrossHotelOperationForbidden");
        }

        // BR-05: Chặn xóa nếu phòng đã từng phát sinh booking lịch sử
        var hasHistory = await _roomAssignmentRepository.AnyAsync(a => a.RoomId == id);
        if (hasHistory)
        {
            throw new BusinessException("RoomManagement:CannotDeleteWithBookings");
        }

        await _roomRepository.DeleteAsync(room);
    }

    [Authorize(BookingHotelPermissions.Rooms.Edit)]
    public async Task<RoomDto> ChangeOperationalStatusAsync(Guid id, ChangeOperationalStatusDto input)
    {
        await CheckHotelManagerAccessAsync(input.HotelId);

        var room = await _roomRepository.GetAsync(id);
        if (room.HotelId != input.HotelId)
        {
            throw new BusinessException("BookingHotel:CrossHotelOperationForbidden");
        }

        if (input.TargetStatus == RoomOperationalStatus.UnderMaintenance || input.TargetStatus == RoomOperationalStatus.OutOfService)
        {
            if (string.IsNullOrWhiteSpace(input.Reason))
            {
                throw new BusinessException("BookingHotel:StatusChangeReasonRequired");
            }

            if (room.OccupancyStatus == RoomOccupancyStatus.Occupied)
            {
                throw new BusinessException("BookingHotel:RoomCurrentlyOccupied")
                    .WithData("RoomNumber", room.RoomNumber);
            }

            var start = input.StartDate ?? Clock.Now;
            var end = input.ExpectedEndDate ?? start.AddDays(1);

            var hasConflict = await _roomAssignmentRepository.AnyAsync(a =>
                a.RoomId == id &&
                a.IsActive &&
                a.FromDate < end &&
                a.ToDate > start);

            if (hasConflict)
            {
                throw new BusinessException("BookingHotel:ConflictingBookingExists")
                    .WithData("RoomNumber", room.RoomNumber);
            }

            if (input.TargetStatus == RoomOperationalStatus.UnderMaintenance)
            {
                room.MaintenanceReason = input.Reason;
                room.MaintenanceEndDate = input.ExpectedEndDate;
                room.OutOfServiceReason = null;
                room.OutOfServiceEndDate = null;
            }
            else
            {
                room.OutOfServiceReason = input.Reason;
                room.OutOfServiceEndDate = input.ExpectedEndDate;
                room.MaintenanceReason = null;
                room.MaintenanceEndDate = null;
            }
        }
        else if (input.TargetStatus == RoomOperationalStatus.Active)
        {
            room.MaintenanceReason = null;
            room.MaintenanceEndDate = null;
            room.OutOfServiceReason = null;
            room.OutOfServiceEndDate = null;
        }

        room.OperationalStatus = input.TargetStatus;
        await _roomRepository.UpdateAsync(room);

        var roomType = await _roomTypeRepository.FindAsync(room.RoomTypeId);

        return new RoomDto
        {
            Id = room.Id,
            HotelId = room.HotelId,
            RoomTypeId = room.RoomTypeId,
            RoomTypeName = roomType?.Name ?? string.Empty,
            RoomNumber = room.RoomNumber,
            Floor = room.Floor,
            OperationalStatus = room.OperationalStatus,
            OccupancyStatus = room.OccupancyStatus,
            HousekeepingStatus = room.HousekeepingStatus,
            IsActive = room.IsActive,
            Building = room.Building,
            Description = room.Description,
            IsSellable = room.IsActive && room.OperationalStatus == RoomOperationalStatus.Active &&
                         (room.HousekeepingStatus == HousekeepingStatus.Clean || room.HousekeepingStatus == HousekeepingStatus.Inspected),
            MaintenanceReason = room.MaintenanceReason,
            MaintenanceEndDate = room.MaintenanceEndDate,
            OutOfServiceReason = room.OutOfServiceReason,
            OutOfServiceEndDate = room.OutOfServiceEndDate
        };
    }

    [Authorize(BookingHotelPermissions.Rooms.UpdateStatus)]
    public async Task<RoomDto> UpdateHousekeepingStatusAsync(Guid id, UpdateHousekeepingStatusDto input)
    {
        await CheckHotelManagerAccessAsync(input.HotelId);

        var room = await _roomRepository.GetAsync(id);
        if (room.HotelId != input.HotelId)
        {
            throw new BusinessException("BookingHotel:CrossHotelOperationForbidden");
        }

        room.HousekeepingStatus = input.TargetStatus;
        await _roomRepository.UpdateAsync(room);

        var roomType = await _roomTypeRepository.FindAsync(room.RoomTypeId);

        return new RoomDto
        {
            Id = room.Id,
            HotelId = room.HotelId,
            RoomTypeId = room.RoomTypeId,
            RoomTypeName = roomType?.Name ?? string.Empty,
            RoomNumber = room.RoomNumber,
            Floor = room.Floor,
            OperationalStatus = room.OperationalStatus,
            OccupancyStatus = room.OccupancyStatus,
            HousekeepingStatus = room.HousekeepingStatus,
            IsActive = room.IsActive,
            Building = room.Building,
            Description = room.Description,
            IsSellable = room.IsActive && room.OperationalStatus == RoomOperationalStatus.Active &&
                         (room.HousekeepingStatus == HousekeepingStatus.Clean || room.HousekeepingStatus == HousekeepingStatus.Inspected)
        };
    }

    #endregion

    #region Room Closures & Incident Resolution (RM-010 US3, US4, US5)

    [Authorize(BookingHotelPermissions.Rooms.Edit)]
    public async Task<List<AffectedBookingDto>> CheckAffectedBookingsAsync(CheckAffectedBookingsInputDto input)
    {
        await CheckHotelManagerAccessAsync(input.HotelId);

        var assignments = await _roomAssignmentRepository.GetListAsync(a =>
            input.RoomIds.Contains(a.RoomId) &&
            a.IsActive &&
            a.FromDate < input.EndDate &&
            a.ToDate > input.StartDate);

        var bookingIds = assignments.Select(a => a.BookingId).Distinct().ToList();
        var bookings = await _bookingRepository.GetListAsync(b =>
            bookingIds.Contains(b.Id) &&
            b.BookingStatus != BookingStatus.Cancelled &&
            b.BookingStatus != BookingStatus.Rejected);

        var rooms = await _roomRepository.GetListAsync(r => input.RoomIds.Contains(r.Id));
        var roomTypeIds = rooms.Select(r => r.RoomTypeId).Distinct().ToList();
        var roomTypes = await _roomTypeRepository.GetListAsync(rt => roomTypeIds.Contains(rt.Id));

        var result = new List<AffectedBookingDto>();
        foreach (var b in bookings)
        {
            var assignment = assignments.FirstOrDefault(a => a.BookingId == b.Id);
            var room = rooms.FirstOrDefault(r => r.Id == assignment?.RoomId);
            var roomType = roomTypes.FirstOrDefault(rt => rt.Id == room?.RoomTypeId);

            result.Add(new AffectedBookingDto
            {
                BookingId = b.Id,
                BookingCode = b.BookingCode,
                CustomerName = b.GuestFullName,
                CustomerPhone = b.GuestPhone,
                CustomerEmail = b.GuestEmail,
                CheckInDate = b.CheckInDate,
                CheckOutDate = b.CheckOutDate,
                RoomId = room?.Id ?? Guid.Empty,
                RoomNumber = room?.RoomNumber ?? string.Empty,
                RoomTypeId = roomType?.Id ?? Guid.Empty,
                RoomTypeName = roomType?.Name ?? string.Empty,
                BasePrice = roomType?.BasePrice ?? 0
            });
        }

        return result;
    }

    [Authorize(BookingHotelPermissions.Rooms.Edit)]
    public async Task<List<RoomClosureDto>> CreatePlannedClosureAsync(CreatePlannedClosureDto input)
    {
        await CheckHotelManagerAccessAsync(input.HotelId);

        if (input.StartDate >= input.EndDate)
        {
            throw new BusinessException("BookingHotel:InvalidDateRange");
        }

        var result = new List<RoomClosureDto>();
        var rooms = await _roomRepository.GetListAsync(r => input.RoomIds.Contains(r.Id));

        foreach (var room in rooms)
        {
            var closure = new RoomClosure(
                GuidGenerator.Create(),
                input.HotelId,
                room.Id,
                RoomClosureType.Planned,
                input.Reason,
                input.StartDate,
                input.EndDate,
                null,
                input.EndDate,
                CurrentTenant.Id
            );

            await _closureRepository.InsertAsync(closure);

            // Tìm các booking bị ảnh hưởng để tạo bản ghi Resolution chờ điều phối
            var affectedAssignments = await _roomAssignmentRepository.GetListAsync(a =>
                a.RoomId == room.Id &&
                a.IsActive &&
                a.FromDate < input.EndDate &&
                a.ToDate > input.StartDate);

            foreach (var a in affectedAssignments)
            {
                var resolution = new AffectedBookingResolution(
                    GuidGenerator.Create(),
                    closure.Id,
                    a.BookingId,
                    CurrentTenant.Id
                );
                await _resolutionRepository.InsertAsync(resolution);
            }

            result.Add(new RoomClosureDto
            {
                Id = closure.Id,
                HotelId = closure.HotelId,
                RoomId = closure.RoomId,
                RoomNumber = room.RoomNumber,
                ClosureType = closure.ClosureType,
                Reason = closure.Reason,
                StartDate = closure.StartDate,
                EndDate = closure.EndDate,
                Status = closure.Status,
                CreationTime = Clock.Now
            });
        }

        return result;
    }

    [Authorize(BookingHotelPermissions.Rooms.Edit)]
    public async Task<List<RoomClosureDto>> CreateEmergencyClosureAsync(CreateEmergencyClosureDto input)
    {
        await CheckHotelManagerAccessAsync(input.HotelId);

        if (string.IsNullOrWhiteSpace(input.Reason))
        {
            throw new BusinessException("RoomManagement:ReasonRequired");
        }

        var result = new List<RoomClosureDto>();
        var rooms = await _roomRepository.GetListAsync(r => input.RoomIds.Contains(r.Id));
        var allAffectedBookingIds = new List<Guid>();

        foreach (var room in rooms)
        {
            // BR-08: Tự động chuyển trạng thái phòng sang OutOfService ngay lập tức
            room.OperationalStatus = RoomOperationalStatus.OutOfService;
            room.OutOfServiceReason = input.Reason;
            room.OutOfServiceEndDate = input.EstimatedRecoveryTime;
            await _roomRepository.UpdateAsync(room);

            var closure = new RoomClosure(
                GuidGenerator.Create(),
                input.HotelId,
                room.Id,
                RoomClosureType.Emergency,
                input.Reason,
                input.StartDate,
                null,
                input.IncidentDescription,
                input.EstimatedRecoveryTime,
                CurrentTenant.Id
            );

            await _closureRepository.InsertAsync(closure);

            // Quét các booking trùng thời gian
            var endThreshold = input.EstimatedRecoveryTime ?? input.StartDate.AddDays(7);
            var affectedAssignments = await _roomAssignmentRepository.GetListAsync(a =>
                a.RoomId == room.Id &&
                a.IsActive &&
                a.FromDate < endThreshold &&
                a.ToDate > input.StartDate);

            foreach (var a in affectedAssignments)
            {
                var resolution = new AffectedBookingResolution(
                    GuidGenerator.Create(),
                    closure.Id,
                    a.BookingId,
                    CurrentTenant.Id
                );
                await _resolutionRepository.InsertAsync(resolution);
                allAffectedBookingIds.Add(a.BookingId);
            }

            // Kích hoạt gửi email cảnh báo khách hàng qua Background Job bất đồng bộ
            if (allAffectedBookingIds.Count > 0)
            {
                await _backgroundJobManager.EnqueueAsync(new EmergencyClosureNotificationArgs
                {
                    HotelId = input.HotelId,
                    RoomClosureId = closure.Id,
                    AffectedBookingIds = allAffectedBookingIds.Distinct().ToList(),
                    Reason = input.Reason,
                    IncidentDescription = input.IncidentDescription
                });
            }

            result.Add(new RoomClosureDto
            {
                Id = closure.Id,
                HotelId = closure.HotelId,
                RoomId = closure.RoomId,
                RoomNumber = room.RoomNumber,
                ClosureType = closure.ClosureType,
                Reason = closure.Reason,
                IncidentDescription = closure.IncidentDescription,
                StartDate = closure.StartDate,
                EstimatedRecoveryTime = closure.EstimatedRecoveryTime,
                Status = closure.Status,
                CreationTime = Clock.Now
            });
        }

        return result;
    }

    [Authorize(BookingHotelPermissions.Rooms.Edit)]
    public async Task<List<AlternativeRoomSuggestionDto>> GetAlternativeRoomSuggestionsAsync(
        Guid hotelId,
        Guid bookingId,
        AffectedBookingResolutionType resolutionType)
    {
        await CheckHotelManagerAccessAsync(hotelId);

        var booking = await _bookingRepository.GetAsync(bookingId);
        var activeAssignment = await _roomAssignmentRepository.FirstOrDefaultAsync(a => a.BookingId == bookingId && a.IsActive);
        var currentRoom = activeAssignment != null ? await _roomRepository.FindAsync(activeAssignment.RoomId) : null;
        var currentRoomType = currentRoom != null ? await _roomTypeRepository.FindAsync(currentRoom.RoomTypeId) : null;
        var currentBasePrice = currentRoomType?.BasePrice ?? 0;

        var allRooms = await _roomRepository.GetListAsync(r =>
            r.HotelId == hotelId &&
            r.IsActive &&
            r.OperationalStatus == RoomOperationalStatus.Active &&
            r.OccupancyStatus != RoomOccupancyStatus.Blocked);

        // Lọc các phòng bị trùng lịch booking trong khoảng [booking.CheckInDate, booking.CheckOutDate]
        var occupiedRoomIds = (await _roomAssignmentRepository.GetListAsync(a =>
            a.IsActive &&
            a.BookingId != bookingId &&
            a.FromDate < booking.CheckOutDate &&
            a.ToDate > booking.CheckInDate))
            .Select(a => a.RoomId)
            .Distinct()
            .ToHashSet();

        var availableRooms = allRooms.Where(r => !occupiedRoomIds.Contains(r.Id)).ToList();
        var roomTypes = await _roomTypeRepository.GetListAsync(rt => rt.HotelId == hotelId);

        var result = new List<AlternativeRoomSuggestionDto>();
        foreach (var r in availableRooms)
        {
            var rt = roomTypes.FirstOrDefault(t => t.Id == r.RoomTypeId);
            if (rt == null) continue;

            if (resolutionType == AffectedBookingResolutionType.RoomChange)
            {
                // Cùng hạng phòng
                if (currentRoom != null && r.RoomTypeId == currentRoom.RoomTypeId && r.Id != currentRoom.Id)
                {
                    result.Add(new AlternativeRoomSuggestionDto
                    {
                        RoomId = r.Id,
                        RoomNumber = r.RoomNumber,
                        Floor = r.Floor,
                        RoomTypeId = rt.Id,
                        RoomTypeName = rt.Name,
                        BasePrice = rt.BasePrice,
                        IsUpgrade = false
                    });
                }
            }
            else if (resolutionType == AffectedBookingResolutionType.RoomUpgrade)
            {
                // Hạng phòng cao hơn
                if (rt.BasePrice > currentBasePrice)
                {
                    result.Add(new AlternativeRoomSuggestionDto
                    {
                        RoomId = r.Id,
                        RoomNumber = r.RoomNumber,
                        Floor = r.Floor,
                        RoomTypeId = rt.Id,
                        RoomTypeName = rt.Name,
                        BasePrice = rt.BasePrice,
                        IsUpgrade = true
                    });
                }
            }
        }

        return result.OrderBy(x => x.BasePrice).ThenBy(x => x.RoomNumber).ToList();
    }

    [Authorize(BookingHotelPermissions.Rooms.Edit)]
    public async Task ResolveAffectedBookingAsync(ResolveAffectedBookingDto input)
    {
        await CheckHotelManagerAccessAsync(input.HotelId);

        var resolution = await _resolutionRepository.GetAsync(input.ResolutionId);
        resolution.ResolutionType = input.ResolutionType;
        resolution.AlternativeRoomId = input.AlternativeRoomId;
        resolution.Notes = input.Notes;
        resolution.ResolvedBy = CurrentUser.Id;
        resolution.ResolvedTime = Clock.Now;

        // Nếu là Đổi phòng hoặc Nâng hạng, cập nhật RoomAssignment của booking
        if ((input.ResolutionType == AffectedBookingResolutionType.RoomChange || input.ResolutionType == AffectedBookingResolutionType.RoomUpgrade)
            && input.AlternativeRoomId.HasValue)
        {
            var activeAssignment = await _roomAssignmentRepository.FirstOrDefaultAsync(a => a.BookingId == resolution.BookingId && a.IsActive);
            if (activeAssignment != null)
            {
                activeAssignment.RoomId = input.AlternativeRoomId.Value;
                await _roomAssignmentRepository.UpdateAsync(activeAssignment);
            }
        }

        await _resolutionRepository.UpdateAsync(resolution);
    }

    [Authorize(BookingHotelPermissions.Rooms.Edit)]
    public async Task ConfirmRoomRecoveryAsync(Guid closureId, ConfirmRoomRecoveryDto input)
    {
        await CheckHotelManagerAccessAsync(input.HotelId);

        var closure = await _closureRepository.GetAsync(closureId);
        closure.Status = RoomClosureStatus.Resolved;
        closure.EndDate = Clock.Now;
        closure.RecoveryConfirmedBy = CurrentUser.Id;
        closure.RecoveryConfirmedTime = Clock.Now;
        await _closureRepository.UpdateAsync(closure);

        var room = await _roomRepository.GetAsync(closure.RoomId);

        // BR-18: Kiểm tra chéo nếu còn Closure khác còn hiệu lực thì giữ nguyên
        var hasActiveOtherClosure = await _closureRepository.AnyAsync(c =>
            c.RoomId == closure.RoomId &&
            c.Id != closureId &&
            c.Status == RoomClosureStatus.Active &&
            (c.EndDate == null || c.EndDate > Clock.Now));

        if (!hasActiveOtherClosure)
        {
            room.OperationalStatus = RoomOperationalStatus.Active;
            room.OutOfServiceReason = null;
            room.OutOfServiceEndDate = null;
            room.MaintenanceReason = null;
            room.MaintenanceEndDate = null;
            await _roomRepository.UpdateAsync(room);
        }
    }

    [Authorize(BookingHotelPermissions.Rooms.Edit)]
    public async Task<List<RoomClosureDto>> GetClosureHistoryAsync(
        Guid hotelId,
        RoomClosureType? closureType = null,
        DateTime? fromDate = null,
        DateTime? toDate = null)
    {
        await CheckHotelManagerAccessAsync(hotelId);

        var closures = await _closureRepository.GetListAsync(c =>
            c.HotelId == hotelId &&
            (!closureType.HasValue || c.ClosureType == closureType.Value) &&
            (!fromDate.HasValue || c.StartDate >= fromDate.Value) &&
            (!toDate.HasValue || c.StartDate <= toDate.Value));

        var roomIds = closures.Select(c => c.RoomId).Distinct().ToList();
        var rooms = await _roomRepository.GetListAsync(r => roomIds.Contains(r.Id));

        var closureIds = closures.Select(c => c.Id).ToList();
        var resolutions = await _resolutionRepository.GetListAsync(r => closureIds.Contains(r.RoomClosureId));

        var bookingIds = resolutions.Select(r => r.BookingId).Distinct().ToList();
        var bookings = await _bookingRepository.GetListAsync(b => bookingIds.Contains(b.Id));

        var creatorIds = closures.Where(c => c.CreatorId.HasValue).Select(c => c.CreatorId!.Value).Distinct().ToList();
        var users = await _userRepository.GetListAsync(u => creatorIds.Contains(u.Id));

        var result = new List<RoomClosureDto>();
        foreach (var c in closures.OrderByDescending(x => x.CreationTime))
        {
            var room = rooms.FirstOrDefault(r => r.Id == c.RoomId);
            var creator = users.FirstOrDefault(u => u.Id == c.CreatorId);
            var closureResolutions = resolutions.Where(r => r.RoomClosureId == c.Id).ToList();

            var resDtos = new List<AffectedBookingResolutionDto>();
            foreach (var r in closureResolutions)
            {
                var booking = bookings.FirstOrDefault(b => b.Id == r.BookingId);
                var altRoom = r.AlternativeRoomId.HasValue ? rooms.FirstOrDefault(rm => rm.Id == r.AlternativeRoomId.Value) : null;

                resDtos.Add(new AffectedBookingResolutionDto
                {
                    Id = r.Id,
                    RoomClosureId = r.RoomClosureId,
                    BookingId = r.BookingId,
                    BookingCode = booking?.BookingCode ?? string.Empty,
                    CustomerName = booking?.GuestFullName ?? string.Empty,
                    CustomerEmail = booking?.GuestEmail ?? string.Empty,
                    RoomNumber = room?.RoomNumber ?? string.Empty,
                    ResolutionType = r.ResolutionType,
                    AlternativeRoomId = r.AlternativeRoomId,
                    AlternativeRoomNumber = altRoom?.RoomNumber,
                    ResolvedBy = r.ResolvedBy,
                    ResolvedTime = r.ResolvedTime,
                    Notes = r.Notes,
                    IsCustomerNotified = r.IsCustomerNotified
                });
            }

            result.Add(new RoomClosureDto
            {
                Id = c.Id,
                HotelId = c.HotelId,
                RoomId = c.RoomId,
                RoomNumber = room?.RoomNumber ?? string.Empty,
                ClosureType = c.ClosureType,
                Reason = c.Reason,
                IncidentDescription = c.IncidentDescription,
                StartDate = c.StartDate,
                EndDate = c.EndDate,
                EstimatedRecoveryTime = c.EstimatedRecoveryTime,
                Status = c.Status,
                RecoveryConfirmedBy = c.RecoveryConfirmedBy,
                RecoveryConfirmedTime = c.RecoveryConfirmedTime,
                CreationTime = c.CreationTime,
                CreatorName = creator?.UserName,
                Resolutions = resDtos
            });
        }

        return result;
    }

    #endregion
}
