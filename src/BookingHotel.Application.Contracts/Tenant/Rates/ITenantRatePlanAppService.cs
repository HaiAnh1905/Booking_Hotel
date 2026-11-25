using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace BookingHotel.TenantRates;

public interface ITenantRatePlanAppService : IApplicationService
{
    Task<List<RatePlanDto>> GetListAsync();
    Task<RatePlanDto> GetAsync(Guid id);
    Task<RatePlanDto> CreateAsync(CreateUpdateRatePlanDto input);
    Task<RatePlanDto> UpdateAsync(Guid id, CreateUpdateRatePlanDto input);
    Task DeleteAsync(Guid id);
}
