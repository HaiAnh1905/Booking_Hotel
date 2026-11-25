using System;
using System.Threading.Tasks;
using BookingHotel.Controllers;
using BookingHotel.Hotels;
using BookingHotel.Hotels.Dtos;
using BookingHotel.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace BookingHotel.Controllers;

[RemoteService(Name = "Hotel")]
[Area("app")]
[Route("api/app/hotels")]
[Route("api/app/hotel")]
[Authorize]
public class HotelController : BookingHotelController, IHotelAppService
{
    private readonly IHotelAppService _hotelAppService;

    public HotelController(IHotelAppService hotelAppService)
    {
        _hotelAppService = hotelAppService;
    }

    [HttpGet]
    [Authorize(BookingHotelPermissions.Hotels.Default)]
    public Task<PagedResultDto<HotelDto>> GetListAsync(GetHotelsInput input)
    {
        return _hotelAppService.GetListAsync(input);
    }

    [HttpGet]
    [Route("{id}")]
    [Authorize(BookingHotelPermissions.Hotels.Default)]
    public Task<HotelDto> GetAsync(Guid id)
    {
        return _hotelAppService.GetAsync(id);
    }

    [HttpPost]
    [Authorize(BookingHotelPermissions.Hotels.Create)]
    public Task<HotelDto> CreateAsync(CreateUpdateHotelDto input)
    {
        return _hotelAppService.CreateAsync(input);
    }

    [HttpPut]
    [Route("{id}")]
    [Authorize(BookingHotelPermissions.Hotels.Edit)]
    public Task<HotelDto> UpdateAsync(Guid id, CreateUpdateHotelDto input)
    {
        return _hotelAppService.UpdateAsync(id, input);
    }

    [HttpDelete]
    [Route("{id}")]
    [Authorize(BookingHotelPermissions.Hotels.Delete)]
    public Task DeleteAsync(Guid id)
    {
        return _hotelAppService.DeleteAsync(id);
    }

    [HttpPost]
    [Route("{id}/suspend")]
    [Authorize(BookingHotelPermissions.Hotels.ManageBranches)]
    public Task<HotelSuspensionResultDto> SuspendAsync(Guid id, SuspendHotelInputDto input)
    {
        return _hotelAppService.SuspendAsync(id, input);
    }

    [HttpPost]
    [Route("{id}/activate")]
    [Authorize(BookingHotelPermissions.Hotels.ManageBranches)]
    public Task<HotelDto> ActivateAsync(Guid id)
    {
        return _hotelAppService.ActivateAsync(id);
    }

    [HttpPost]
    [Route("{id}/assign-manager")]
    [Authorize(BookingHotelPermissions.Hotels.AssignStaff)]
    public Task<HotelDto> AssignManagerAsync(Guid id, AssignHotelManagerDto input)
    {
        return _hotelAppService.AssignManagerAsync(id, input);
    }

    [HttpGet]
    [Route("check-name")]
    [Authorize(BookingHotelPermissions.Hotels.Default)]
    public Task<bool> CheckNameExistsAsync([FromQuery] string name, [FromQuery] Guid? excludeId = null)
    {
        return _hotelAppService.CheckNameExistsAsync(name, excludeId);
    }
}
