using System;
using System.Threading.Tasks;
using BookingHotel.Controllers;
using BookingHotel.Manager.Hotels;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace BookingHotel.HttpApi.Controllers.Manager;

[RemoteService]
[Route("api/app/manager/hotel-profile")]
public class ManagerHotelProfileController : BookingHotelController, IManagerHotelProfileAppService
{
    private readonly IManagerHotelProfileAppService _profileAppService;

    public ManagerHotelProfileController(IManagerHotelProfileAppService profileAppService)
    {
        _profileAppService = profileAppService;
    }

    [HttpGet]
    public Task<ManagerHotelProfileDto> GetAsync([FromQuery] Guid hotelId)
    {
        return _profileAppService.GetAsync(hotelId);
    }

    [HttpPut]
    public Task<ManagerHotelProfileDto> UpdateAsync([FromBody] UpdateManagerHotelProfileDto input)
    {
        return _profileAppService.UpdateAsync(input);
    }
}
