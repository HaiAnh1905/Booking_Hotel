using System;
using System.Collections.Generic;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace BookingHotel.Hotels;

public class Hotel : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    public virtual Guid? TenantId { get; set; }

    public virtual string Name { get; set; } = string.Empty;

    public virtual string Code { get; set; } = string.Empty;

    public virtual string Address { get; set; } = string.Empty;

    public virtual string City { get; set; } = string.Empty;

    public virtual string Phone { get; set; } = string.Empty;

    public virtual string Email { get; set; } = string.Empty;

    public virtual int StarRating { get; set; } = 5;

    public virtual int RoomCount { get; set; } = 50;

    public virtual string ManagerName { get; set; } = string.Empty;

    public virtual HotelStatus Status { get; set; } = HotelStatus.Operating;

    public virtual TimeSpan StandardCheckInTime { get; set; } = new(14, 0, 0);

    public virtual TimeSpan StandardCheckOutTime { get; set; } = new(12, 0, 0);

    public virtual int BookingCutoffDays { get; set; } = 14;

    public virtual ICollection<HotelSuspensionRecord> SuspensionRecords { get; set; } = new List<HotelSuspensionRecord>();

    protected Hotel()
    {
    }

    public Hotel(
        Guid id,
        string name,
        string code,
        string address,
        string city,
        string phone,
        string email,
        int starRating,
        int roomCount,
        string managerName,
        HotelStatus status = HotelStatus.Operating,
        Guid? tenantId = null,
        TimeSpan? standardCheckInTime = null,
        TimeSpan? standardCheckOutTime = null,
        int bookingCutoffDays = 14)
        : base(id)
    {
        Name = name;
        Code = code;
        Address = address;
        City = city;
        Phone = phone;
        Email = email;
        StarRating = starRating;
        RoomCount = roomCount;
        ManagerName = managerName;
        Status = status;
        TenantId = tenantId;
        StandardCheckInTime = standardCheckInTime ?? new TimeSpan(14, 0, 0);
        StandardCheckOutTime = standardCheckOutTime ?? new TimeSpan(12, 0, 0);
        BookingCutoffDays = bookingCutoffDays;
    }
}
