using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BookingHotel.Permissions;
using BookingHotel.Rooms;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace BookingHotel.TenantRates;

[RemoteService(IsEnabled = false)]
[Authorize]
public class TenantRatePlanAppService : ApplicationService, ITenantRatePlanAppService
{
    private readonly IRepository<RatePlan, Guid> _ratePlanRepository;
    private readonly IRepository<HotelRatePlan, Guid> _hotelRatePlanRepository;

    public TenantRatePlanAppService(
        IRepository<RatePlan, Guid> ratePlanRepository,
        IRepository<HotelRatePlan, Guid> hotelRatePlanRepository)
    {
        _ratePlanRepository = ratePlanRepository;
        _hotelRatePlanRepository = hotelRatePlanRepository;
    }

    public async Task<List<RatePlanDto>> GetListAsync()
    {
        var plans = await _ratePlanRepository.GetListAsync();
        return plans
            .OrderBy(p => p.CreationTime)
            .Select(MapToDto)
            .ToList();
    }

    public async Task<RatePlanDto> GetAsync(Guid id)
    {
        var plan = await _ratePlanRepository.GetAsync(id);
        return MapToDto(plan);
    }

    public async Task<RatePlanDto> CreateAsync(CreateUpdateRatePlanDto input)
    {
        var cleanCode = input.Code.Trim().ToUpperInvariant();

        var exists = await _ratePlanRepository.AnyAsync(p => p.Code == cleanCode);
        if (exists)
        {
            throw new BusinessException("BookingHotel:DuplicateRatePlanCode")
                .WithData("Code", cleanCode);
        }

        var cutoffHours = input.IsRefundable ? Math.Max(0, input.CancellationCutoffHours) : 0;

        var plan = new RatePlan(
            GuidGenerator.Create(),
            input.Name.Trim(),
            cleanCode,
            input.Description?.Trim() ?? string.Empty,
            input.IsRefundable,
            input.IncludesBreakfast,
            cutoffHours,
            input.DefaultPriceAdjustmentPercent,
            CurrentTenant.Id
        );

        await _ratePlanRepository.InsertAsync(plan);
        return MapToDto(plan);
    }

    public async Task<RatePlanDto> UpdateAsync(Guid id, CreateUpdateRatePlanDto input)
    {
        var plan = await _ratePlanRepository.GetAsync(id);
        var cleanCode = input.Code.Trim().ToUpperInvariant();

        var exists = await _ratePlanRepository.AnyAsync(p => p.Code == cleanCode && p.Id != id);
        if (exists)
        {
            throw new BusinessException("BookingHotel:DuplicateRatePlanCode")
                .WithData("Code", cleanCode);
        }

        plan.Name = input.Name.Trim();
        plan.Code = cleanCode;
        plan.Description = input.Description?.Trim() ?? string.Empty;
        plan.IsRefundable = input.IsRefundable;
        plan.IncludesBreakfast = input.IncludesBreakfast;
        plan.CancellationCutoffHours = input.IsRefundable ? Math.Max(0, input.CancellationCutoffHours) : 0;
        plan.DefaultPriceAdjustmentPercent = input.DefaultPriceAdjustmentPercent;

        await _ratePlanRepository.UpdateAsync(plan);
        return MapToDto(plan);
    }

    public async Task DeleteAsync(Guid id)
    {
        var plan = await _ratePlanRepository.GetAsync(id);

        // Tự động tắt mở bán tại tất cả chi nhánh để ngắt nhận booking mới
        var hotelPlans = await _hotelRatePlanRepository.GetListAsync(hp => hp.RatePlanId == id);
        foreach (var hp in hotelPlans)
        {
            hp.IsActive = false;
            await _hotelRatePlanRepository.UpdateAsync(hp);
        }

        await _ratePlanRepository.DeleteAsync(plan);
    }

    private static RatePlanDto MapToDto(RatePlan plan)
    {
        return new RatePlanDto
        {
            Id = plan.Id,
            Name = plan.Name,
            Code = plan.Code,
            Description = plan.Description,
            IsRefundable = plan.IsRefundable,
            IncludesBreakfast = plan.IncludesBreakfast,
            CancellationCutoffHours = plan.CancellationCutoffHours,
            DefaultPriceAdjustmentPercent = plan.DefaultPriceAdjustmentPercent,
            CreationTime = plan.CreationTime,
            CreatorId = plan.CreatorId,
            LastModificationTime = plan.LastModificationTime,
            LastModifierId = plan.LastModifierId,
            IsDeleted = plan.IsDeleted,
            DeleterId = plan.DeleterId,
            DeletionTime = plan.DeletionTime
        };
    }
}
