using System;
using System.Threading.Tasks;
using BookingHotel.Hotels;
using BookingHotel.Identity;
using BookingHotel.Permissions;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;

namespace BookingHotel.Manager.Hotels;

[Authorize]
public class ManagerHotelProfileAppService : ManagerAppServiceBase, IManagerHotelProfileAppService
{
    private readonly IRepository<Hotel, Guid> _hotelRepository;

    public ManagerHotelProfileAppService(
        IRepository<UserHotelAssignment, Guid> userHotelAssignmentRepository,
        IRepository<Hotel, Guid> hotelRepository)
        : base(userHotelAssignmentRepository)
    {
        _hotelRepository = hotelRepository;
    }

    public async Task<ManagerHotelProfileDto> GetAsync(Guid hotelId)
    {
        await CheckHotelManagerAccessAsync(hotelId);

        var hotel = await _hotelRepository.GetAsync(hotelId);

        return new ManagerHotelProfileDto
        {
            Id = hotel.Id,
            Name = hotel.Name,
            Code = hotel.Code,
            Address = hotel.Address,
            City = hotel.City,
            Phone = hotel.Phone,
            Email = hotel.Email,
            StarRating = hotel.StarRating,
            RoomCount = hotel.RoomCount,
            ManagerName = hotel.ManagerName,
            StandardCheckInTime = hotel.StandardCheckInTime,
            StandardCheckOutTime = hotel.StandardCheckOutTime,
            Description = hotel.GetProperty<string>("Description"),
            AmenitiesJson = hotel.GetProperty<string>("AmenitiesJson"),
            HouseRules = hotel.GetProperty<string>("HouseRules")
        };
    }

    [Authorize(BookingHotelPermissions.Hotels.Default)]
    public async Task<ManagerHotelProfileDto> UpdateAsync(UpdateManagerHotelProfileDto input)
    {
        await CheckHotelManagerAccessAsync(input.HotelId);

        var hotel = await _hotelRepository.GetAsync(input.HotelId);

        // Bảo vệ bất biến: HotelCode, TenantId và Tên khách sạn không được phép thay đổi bởi Hotel Manager
        hotel.Address = input.Address;
        hotel.City = input.City;
        hotel.Phone = input.Phone;
        hotel.Email = input.Email;
        hotel.StandardCheckInTime = input.StandardCheckInTime;
        hotel.StandardCheckOutTime = input.StandardCheckOutTime;

        hotel.SetProperty("Description", input.Description);
        hotel.SetProperty("AmenitiesJson", input.AmenitiesJson);
        hotel.SetProperty("HouseRules", input.HouseRules);

        await _hotelRepository.UpdateAsync(hotel);

        return new ManagerHotelProfileDto
        {
            Id = hotel.Id,
            Name = hotel.Name,
            Code = hotel.Code,
            Address = hotel.Address,
            City = hotel.City,
            Phone = hotel.Phone,
            Email = hotel.Email,
            StarRating = hotel.StarRating,
            RoomCount = hotel.RoomCount,
            ManagerName = hotel.ManagerName,
            StandardCheckInTime = hotel.StandardCheckInTime,
            StandardCheckOutTime = hotel.StandardCheckOutTime,
            Description = input.Description,
            AmenitiesJson = input.AmenitiesJson,
            HouseRules = input.HouseRules
        };
    }
}
