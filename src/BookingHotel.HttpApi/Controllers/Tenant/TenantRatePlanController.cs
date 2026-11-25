using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BookingHotel.Controllers;
using BookingHotel.TenantRates;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace BookingHotel.Controllers.Tenant;

[RemoteService(Name = "TenantRatePlan")]
[Area("app")]
[Route("api/app/tenant/rate-plans")]
[Authorize]
public class TenantRatePlanController : BookingHotelController, ITenantRatePlanAppService
{
    private readonly ITenantRatePlanAppService _service;

    public TenantRatePlanController(ITenantRatePlanAppService service)
    {
        _service = service;
    }

    [HttpGet]
    public Task<List<RatePlanDto>> GetListAsync()
    {
        return _service.GetListAsync();
    }

    [HttpGet]
    [Route("{id}")]
    public Task<RatePlanDto> GetAsync(Guid id)
    {
        return _service.GetAsync(id);
    }

    [HttpPost]
    public Task<RatePlanDto> CreateAsync([FromBody] CreateUpdateRatePlanDto input)
    {
        return _service.CreateAsync(input);
    }

    [HttpPut]
    [Route("{id}")]
    public Task<RatePlanDto> UpdateAsync(Guid id, [FromBody] CreateUpdateRatePlanDto input)
    {
        return _service.UpdateAsync(id, input);
    }

    [HttpDelete]
    [Route("{id}")]
    public Task DeleteAsync(Guid id)
    {
        return _service.DeleteAsync(id);
    }
}
