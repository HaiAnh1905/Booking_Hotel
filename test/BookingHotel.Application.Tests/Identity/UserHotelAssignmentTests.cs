using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BookingHotel.Identity;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Uow;
using Xunit;

namespace BookingHotel.Identity;

public abstract class UserHotelAssignmentTests<TStartupModule> : BookingHotelApplicationTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    private readonly IUserHotelAssignmentAppService _assignmentAppService;
    private readonly IdentityUserManager _userManager;
    private readonly IdentityRoleManager _roleManager;
    private readonly ICurrentTenant _currentTenant;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    protected UserHotelAssignmentTests()
    {
        _assignmentAppService = GetRequiredService<IUserHotelAssignmentAppService>();
        _userManager = GetRequiredService<IdentityUserManager>();
        _roleManager = GetRequiredService<IdentityRoleManager>();
        _currentTenant = GetRequiredService<ICurrentTenant>();
        _unitOfWorkManager = GetRequiredService<IUnitOfWorkManager>();
    }

    [Fact]
    public async Task HotelManager_Should_Be_Assignable_To_Multiple_Hotels()
    {
        var tenantId = Guid.NewGuid();
        var hotel1 = Guid.NewGuid();
        var hotel2 = Guid.NewGuid();

        using (_currentTenant.Change(tenantId))
        {
            IdentityUser manager;
            using (var uow = _unitOfWorkManager.Begin())
            {
                // Ensure HotelManager role exists
                var managerRole = await _roleManager.FindByNameAsync(BookingHotelRoleConsts.HotelManager);
                if (managerRole == null)
                {
                    managerRole = new IdentityRole(Guid.NewGuid(), BookingHotelRoleConsts.HotelManager, tenantId);
                    await _roleManager.CreateAsync(managerRole);
                }

                manager = new IdentityUser(Guid.NewGuid(), "hotel.manager@chain.com", "hotel.manager@chain.com", tenantId);
                await _userManager.CreateAsync(manager, "Password123!");
                await _userManager.AddToRoleAsync(manager, BookingHotelRoleConsts.HotelManager);

                await uow.CompleteAsync();
            }

            // Assign 2 hotels (1-n)
            await _assignmentAppService.AssignUserToHotelsAsync(new AssignUserHotelDto
            {
                UserId = manager.Id,
                HotelIds = new List<Guid> { hotel1, hotel2 },
                PrimaryHotelId = hotel1
            });

            var assigned = await _assignmentAppService.GetAssignedHotelIdsAsync(manager.Id);
            assigned.Count.ShouldBe(2);
            assigned.ShouldContain(hotel1);
            assigned.ShouldContain(hotel2);

            (await _assignmentAppService.HasAccessToHotelAsync(manager.Id, hotel1)).ShouldBeTrue();
            (await _assignmentAppService.HasAccessToHotelAsync(manager.Id, hotel2)).ShouldBeTrue();
            (await _assignmentAppService.HasAccessToHotelAsync(manager.Id, Guid.NewGuid())).ShouldBeFalse();
        }
    }

    [Fact]
    public async Task Receptionist_Should_Enforce_Single_Hotel_Constraint()
    {
        var tenantId = Guid.NewGuid();
        var hotel1 = Guid.NewGuid();
        var hotel2 = Guid.NewGuid();

        using (_currentTenant.Change(tenantId))
        {
            IdentityUser receptionist;
            using (var uow = _unitOfWorkManager.Begin())
            {
                // Ensure Receptionist role exists
                var recepRole = await _roleManager.FindByNameAsync(BookingHotelRoleConsts.Receptionist);
                if (recepRole == null)
                {
                    recepRole = new IdentityRole(Guid.NewGuid(), BookingHotelRoleConsts.Receptionist, tenantId);
                    await _roleManager.CreateAsync(recepRole);
                }

                receptionist = new IdentityUser(Guid.NewGuid(), "receptionist@chain.com", "receptionist@chain.com", tenantId);
                await _userManager.CreateAsync(receptionist, "Password123!");
                await _userManager.AddToRoleAsync(receptionist, BookingHotelRoleConsts.Receptionist);

                await uow.CompleteAsync();
            }

            // 1. Assign single hotel (1-1) -> Success
            await _assignmentAppService.AssignUserToHotelsAsync(new AssignUserHotelDto
            {
                UserId = receptionist.Id,
                HotelIds = new List<Guid> { hotel1 }
            });

            var assigned = await _assignmentAppService.GetAssignedHotelIdsAsync(receptionist.Id);
            assigned.Count.ShouldBe(1);
            assigned.ShouldContain(hotel1);

            // 2. Attempt to assign multiple hotels (1-n) -> Must throw UserFriendlyException
            await Should.ThrowAsync<UserFriendlyException>(async () =>
            {
                await _assignmentAppService.AssignUserToHotelsAsync(new AssignUserHotelDto
                {
                    UserId = receptionist.Id,
                    HotelIds = new List<Guid> { hotel1, hotel2 }
                });
            });
        }
    }
}
