using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BookingHotel.Bookings;
using BookingHotel.Hotels;
using BookingHotel.Rooms;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Emailing;

namespace BookingHotel.Manager.Rooms.Jobs;

public class EmergencyClosureNotificationArgs
{
    public Guid HotelId { get; set; }
    public Guid RoomClosureId { get; set; }
    public List<Guid> AffectedBookingIds { get; set; } = new();
    public string Reason { get; set; } = string.Empty;
    public string IncidentDescription { get; set; } = string.Empty;
}

public class EmergencyClosureNotificationJob : AsyncBackgroundJob<EmergencyClosureNotificationArgs>, ITransientDependency
{
    private readonly IRepository<Booking, Guid> _bookingRepository;
    private readonly IRepository<Hotel, Guid> _hotelRepository;
    private readonly IRepository<AffectedBookingResolution, Guid> _resolutionRepository;
    private readonly IEmailSender _emailSender;

    public EmergencyClosureNotificationJob(
        IRepository<Booking, Guid> bookingRepository,
        IRepository<Hotel, Guid> hotelRepository,
        IRepository<AffectedBookingResolution, Guid> resolutionRepository,
        IEmailSender emailSender)
    {
        _bookingRepository = bookingRepository;
        _hotelRepository = hotelRepository;
        _resolutionRepository = resolutionRepository;
        _emailSender = emailSender;
    }

    public override async Task ExecuteAsync(EmergencyClosureNotificationArgs args)
    {
        var hotel = await _hotelRepository.FindAsync(args.HotelId);
        var hotelName = hotel?.Name ?? "Booking Hotel";

        var resolutions = await _resolutionRepository.GetListAsync(r => r.RoomClosureId == args.RoomClosureId);

        foreach (var bookingId in args.AffectedBookingIds)
        {
            var booking = await _bookingRepository.FindAsync(bookingId);
            if (booking == null) continue;

            var recipientEmail = !string.IsNullOrWhiteSpace(booking.GuestEmail)
                ? booking.GuestEmail
                : "guest@example.com";

            var subject = $"[Booking Hotel] Thông Báo Khẩn Cấp Về Sự Cố Phòng - Đơn Đặt #{booking.BookingCode}";
            var body = $@"
                <h3>Kính gửi Quý khách {booking.GuestFullName},</h3>
                <p>Khách sạn <strong>{hotelName}</strong> thành thật cáo lỗi thông báo rằng phòng Quý khách đã đặt hiện đang gặp sự cố đột xuất ngoài ý muốn.</p>
                <p><strong>Lý do sự cố:</strong> {args.Reason}</p>
                <p><strong>Chi tiết:</strong> {args.IncidentDescription}</p>
                <p>Đội ngũ Quản lý và Lễ tân của chúng tôi đang chủ động chuẩn bị các phương án hỗ trợ tốt nhất cho Quý khách (Đổi phòng tương đương, Nâng hạng phòng miễn phí hoặc Hoàn tiền 100%).</p>
                <p>Bộ phận Chăm sóc khách hàng sẽ liên hệ trực tiếp với Quý khách qua số điện thoại <strong>{booking.GuestPhone}</strong>.</p>
                <p>Trân trọng cảm ơn sự thông cảm của Quý khách,<br/>Ban Quản Lý Khách Sạn {hotelName}</p>
            ";

            try
            {
                await _emailSender.SendAsync(recipientEmail, subject, body, isBodyHtml: true);

                var resolution = resolutions.Find(r => r.BookingId == bookingId);
                if (resolution != null)
                {
                    resolution.IsCustomerNotified = true;
                    await _resolutionRepository.UpdateAsync(resolution);
                }
            }
            catch
            {
                // Background job catches email provider errors and maintains audit trail
            }
        }
    }
}
