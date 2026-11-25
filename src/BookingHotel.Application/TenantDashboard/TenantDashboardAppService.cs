using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BookingHotel.Hotels;
using BookingHotel.Identity;
using BookingHotel.Permissions;
using BookingHotel.TenantDashboard.Dtos;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;

namespace BookingHotel.TenantDashboard;

[RemoteService(IsEnabled = false)]
[Authorize]
public class TenantDashboardAppService : ApplicationService, ITenantDashboardAppService
{
    private readonly IRepository<IdentityUser, Guid> _userRepository;
    private readonly IRepository<Hotel, Guid> _hotelRepository;

    public TenantDashboardAppService(
        IRepository<IdentityUser, Guid> userRepository,
        IRepository<Hotel, Guid> hotelRepository)
    {
        _userRepository = userRepository;
        _hotelRepository = hotelRepository;
    }

    [Authorize(BookingHotelPermissions.Reports.Consolidated)]
    public async Task<TenantDashboardOverviewDto> GetOverviewAsync(GetTenantDashboardInput input)
    {
        if (!CurrentTenant.Id.HasValue)
        {
            throw new BusinessException("BookingHotel:TenantOnlyAccess");
        }

        var hotels = await _hotelRepository.GetListAsync();
        var operatingCount = hotels.Count(h => h.Status == HotelStatus.Operating);
        var suspendedCount = hotels.Count(h => h.Status == HotelStatus.ScheduledSuspended || h.Status == HotelStatus.EmergencySuspended);
        var staffCount = (int)await _userRepository.GetCountAsync();

        var branches = new List<TenantBranchMetricDto>();
        int rank = 1;
        foreach (var hotel in hotels)
        {
            branches.Add(new TenantBranchMetricDto
            {
                HotelId = hotel.Id,
                Name = hotel.Name,
                Code = hotel.Code,
                Rooms = hotel.RoomCount,
                OccupancyPercent = 0.0,
                MonthlyRevenue = 0m,
                Adr = 0m,
                RevPar = 0m,
                Rank = rank++
            });
        }

        return new TenantDashboardOverviewDto
        {
            ChainName = CurrentTenant.Name ?? "Booking Hotel Chain",
            TotalHotelCount = hotels.Count,
            OperatingBranchCount = operatingCount,
            SuspendedBranchCount = suspendedCount,
            TotalStaffCount = staffCount,
            TotalBookingCount = 0,
            MonthlyCashRevenue = 0m,
            AverageOccupancyPercent = 0.0,
            AverageRevPar = 0m,
            AverageAdr = 0m,
            Branches = branches
        };
    }

    [Authorize(BookingHotelPermissions.Bookings.Default)]
    public Task<PagedResultDto<TenantBookingSurveillanceDto>> GetSurveillanceBookingsAsync(GetBookingSurveillanceInput input)
    {
        // Tuân thủ Hiến pháp X (Zero Mock Data): Khi chưa có giao dịch đặt phòng thực tế trong CSDL, trả về danh sách rỗng
        var emptyList = new List<TenantBookingSurveillanceDto>();
        return Task.FromResult(new PagedResultDto<TenantBookingSurveillanceDto>(0, emptyList));
    }

    [Authorize(BookingHotelPermissions.Bookings.Default)]
    public Task<TenantBookingSurveillanceDto> GetSurveillanceBookingDetailAsync(Guid bookingId)
    {
        throw new EntityNotFoundException(typeof(TenantBookingSurveillanceDto), bookingId);
    }
}
