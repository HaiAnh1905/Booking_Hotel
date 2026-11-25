using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BookingHotel.Hotels;
using BookingHotel.Permissions;
using BookingHotel.TenantReports.Dtos;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Content;
using Volo.Abp.Domain.Repositories;

namespace BookingHotel.TenantReports;

[RemoteService(IsEnabled = false)]
[Authorize]
public class TenantReportAppService : ApplicationService, ITenantReportAppService
{
    private readonly IRepository<Hotel, Guid> _hotelRepository;

    public TenantReportAppService(IRepository<Hotel, Guid> hotelRepository)
    {
        _hotelRepository = hotelRepository;
    }

    [Authorize(BookingHotelPermissions.Reports.Consolidated)]
    public Task<RevenueReportDto> GetRevenueReportAsync(GetTenantReportInput input)
    {
        // Tuân thủ Hiến pháp X (Zero Mock Data): Khi chưa có đơn đặt phòng & Folio thực tế, trả về kết quả rỗng
        return Task.FromResult(new RevenueReportDto
        {
            TotalNetRevenue = 0m,
            TotalRoomRevenue = 0m,
            TotalServiceRevenue = 0m,
            TotalRefundAmount = 0m,
            TotalBookingsCount = 0,
            Items = new List<RevenueReportItemDto>()
        });
    }

    [Authorize(BookingHotelPermissions.Reports.Consolidated)]
    public async Task<HotelPerformanceReportDto> GetHotelPerformanceReportAsync(GetTenantReportInput input)
    {
        var hotels = await _hotelRepository.GetListAsync();
        var items = new List<HotelPerformanceItemDto>();

        int rank = 1;
        foreach (var h in hotels.OrderBy(h => h.Name))
        {
            items.Add(new HotelPerformanceItemDto
            {
                HotelId = h.Id,
                HotelName = h.Name,
                HotelCode = h.Code,
                RoomCount = h.RoomCount,
                RoomRevenue = 0m,
                AvailableRoomNights = 0,
                SoldRoomNights = 0,
                OccupancyPercent = 0.0,
                Adr = 0m,
                RevPar = 0m,
                Rank = rank++
            });
        }

        return new HotelPerformanceReportDto
        {
            StartDate = input.StartDate ?? Clock.Now.Date.AddDays(-30),
            EndDate = input.EndDate ?? Clock.Now.Date,
            ChainAverageOccupancy = 0,
            ChainAverageRevPar = 0,
            ChainAverageAdr = 0,
            Items = items
        };
    }

    [Authorize(BookingHotelPermissions.Reports.Consolidated)]
    public async Task<IRemoteStreamContent> ExportReportAsync(string reportType, string format, GetTenantReportInput input)
    {
        var hotels = await _hotelRepository.GetListAsync();
        var sb = new StringBuilder();

        sb.AppendLine("Mã cơ sở,Tên khách sạn,Quy mô phòng,Trạng thái");
        foreach (var h in hotels)
        {
            sb.AppendLine($"{h.Code},\"{h.Name}\",{h.RoomCount},{h.Status}");
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        var stream = new MemoryStream(bytes);

        return new RemoteStreamContent(
            stream,
            fileName: $"Report_{reportType}_{Clock.Now:yyyyMMdd_HHmm}.csv",
            contentType: "text/csv; charset=utf-8"
        );
    }
}
