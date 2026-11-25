using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BookingHotel.Identity;
using BookingHotel.Identity.Dtos;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Uow;
using Xunit;

namespace BookingHotel.Identity;

public abstract class IdentityUserManagementAppServiceTests<TStartupModule> : BookingHotelApplicationTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    protected IIdentityUserManagementAppService UserManagementAppService { get; }
    protected IdentityUserManager UserManager { get; }
    protected IdentityRoleManager RoleManager { get; }
    protected IRepository<UserHotelAssignment, Guid> AssignmentRepository { get; }
    protected ICurrentTenant CurrentTenant { get; }
    protected IUnitOfWorkManager UnitOfWorkManager { get; }

    protected IdentityUserManagementAppServiceTests()
    {
        UserManagementAppService = GetRequiredService<IIdentityUserManagementAppService>();
        UserManager = GetRequiredService<IdentityUserManager>();
        RoleManager = GetRequiredService<IdentityRoleManager>();
        AssignmentRepository = GetRequiredService<IRepository<UserHotelAssignment, Guid>>();
        CurrentTenant = GetRequiredService<ICurrentTenant>();
        UnitOfWorkManager = GetRequiredService<IUnitOfWorkManager>();
    }

    [Fact]
    public async Task Should_Filter_Host_Users_When_Tenant_Is_Null()
    {
        // Host context
        using (CurrentTenant.Change(null))
        {
            var hostEmail = $"host.user.{Guid.NewGuid():N}@bookinghotel.com";
            var hostUser = new IdentityUser(Guid.NewGuid(), hostEmail, hostEmail, tenantId: null);
            await UserManager.CreateAsync(hostUser, "Password123*#");

            var result = await UserManagementAppService.GetListAsync(new GetUsersWithAssignmentInput
            {
                Filter = hostEmail,
                MaxResultCount = 10
            });

            result.TotalCount.ShouldBeGreaterThanOrEqualTo(1);
            result.Items.ShouldContain(u => u.Email == hostEmail);
        }
    }

    [Fact]
    public async Task Should_Unlock_BruteForce_Locked_Host_User()
    {
        using (CurrentTenant.Change(null))
        {
            var email = $"locked.host.{Guid.NewGuid():N}@bookinghotel.com";
            var user = new IdentityUser(Guid.NewGuid(), email, email, tenantId: null);
            user.SetIsActive(false);
            user.SetProperty(BookingHotelIdentityConsts.DeactivatedReasonProperty, BookingHotelIdentityConsts.DeactivatedReasonBruteForce);
            user.SetProperty(BookingHotelIdentityConsts.FailedAttemptsInCurrentCycleProperty, 3);
            user.SetProperty(BookingHotelIdentityConsts.TotalFailedAttemptsProperty, 24);
            user.SetProperty(BookingHotelIdentityConsts.CurrentLockoutMinutesProperty, 30);
            await UserManager.CreateAsync(user, "Password123*#");

            var unlockResult = await UserManagementAppService.UnlockUserAsync(user.Id, new UnlockUserDto
            {
                Reason = "Identity test unlock"
            });

            unlockResult.Success.ShouldBeTrue();
            unlockResult.UserId.ShouldBe(user.Id);

            var refreshedUser = await UserManager.FindByIdAsync(user.Id.ToString());
            refreshedUser.ShouldNotBeNull();
            refreshedUser.IsActive.ShouldBeTrue();
            refreshedUser.GetProperty<int>(BookingHotelIdentityConsts.FailedAttemptsInCurrentCycleProperty).ShouldBe(0);
            refreshedUser.GetProperty<int>(BookingHotelIdentityConsts.TotalFailedAttemptsProperty).ShouldBe(0);
            refreshedUser.GetProperty<int>(BookingHotelIdentityConsts.CurrentLockoutMinutesProperty).ShouldBe(0);
            refreshedUser.GetProperty<string?>(BookingHotelIdentityConsts.DeactivatedReasonProperty).ShouldBeNull();
        }
    }

    [Fact]
    public async Task Should_Throw_When_Unlocking_Already_Active_User()
    {
        using (CurrentTenant.Change(null))
        {
            var email = $"active.user.{Guid.NewGuid():N}@bookinghotel.com";
            var user = new IdentityUser(Guid.NewGuid(), email, email, tenantId: null);
            await UserManager.CreateAsync(user, "Password123*#");

            var ex = await Should.ThrowAsync<BusinessException>(async () =>
            {
                await UserManagementAppService.UnlockUserAsync(user.Id);
            });

            ex.Code.ShouldBe("BookingHotel:UserAlreadyActive");
        }
    }

    [Fact]
    public async Task TenantAdmin_Should_Create_User_With_Role_And_HotelAssignment()
    {
        var tenantId = Guid.NewGuid();
        var hotel1 = Guid.NewGuid();
        var hotel2 = Guid.NewGuid();

        using (CurrentTenant.Change(tenantId))
        {
            using (var uow = UnitOfWorkManager.Begin())
            {
                var role = await RoleManager.FindByNameAsync(BookingHotelRoleConsts.HotelManager);
                if (role == null)
                {
                    await RoleManager.CreateAsync(new IdentityRole(Guid.NewGuid(), BookingHotelRoleConsts.HotelManager, tenantId));
                }
                await uow.CompleteAsync();
            }

            var input = new CreateUserWithAssignmentDto
            {
                UserName = $"manager_{Guid.NewGuid():N}",
                Email = $"manager.{Guid.NewGuid():N}@hotelchain.com",
                Name = "Manager",
                Surname = "Test",
                Password = "Password123*#",
                RoleNames = new List<string> { BookingHotelRoleConsts.HotelManager },
                HotelIds = new List<Guid> { hotel1, hotel2 },
                PrimaryHotelId = hotel1
            };

            var userDto = await UserManagementAppService.CreateAsync(input);

            userDto.ShouldNotBeNull();
            userDto.Email.ShouldBe(input.Email);
            userDto.ShouldChangePasswordOnNextLogin.ShouldBeTrue();
            userDto.RoleNames.ShouldContain(BookingHotelRoleConsts.HotelManager);
            userDto.AssignedHotels.Count.ShouldBe(2);
            userDto.AssignedHotels.ShouldContain(h => h.HotelId == hotel1 && h.IsPrimary);
            userDto.AssignedHotels.ShouldContain(h => h.HotelId == hotel2 && !h.IsPrimary);
        }
    }

    [Fact]
    public async Task Receptionist_Should_Enforce_Single_Hotel_On_Create()
    {
        var tenantId = Guid.NewGuid();
        var hotel1 = Guid.NewGuid();
        var hotel2 = Guid.NewGuid();

        using (CurrentTenant.Change(tenantId))
        {
            using (var uow = UnitOfWorkManager.Begin())
            {
                var role = await RoleManager.FindByNameAsync(BookingHotelRoleConsts.Receptionist);
                if (role == null)
                {
                    await RoleManager.CreateAsync(new IdentityRole(Guid.NewGuid(), BookingHotelRoleConsts.Receptionist, tenantId));
                }
                await uow.CompleteAsync();
            }

            var input = new CreateUserWithAssignmentDto
            {
                UserName = $"recep_{Guid.NewGuid():N}",
                Email = $"recep.{Guid.NewGuid():N}@hotelchain.com",
                Password = "Password123*#",
                RoleNames = new List<string> { BookingHotelRoleConsts.Receptionist },
                HotelIds = new List<Guid> { hotel1, hotel2 } // Invalid: 2 hotels
            };

            var ex = await Should.ThrowAsync<BusinessException>(async () =>
            {
                await UserManagementAppService.CreateAsync(input);
            });

            ex.Code.ShouldBe("BookingHotel:ReceptionistSingleHotelRequired");
        }
    }

    [Fact]
    public async Task TenantAdmin_Should_Update_User_And_Soft_Unlink_Removed_Hotels()
    {
        var tenantId = Guid.NewGuid();
        var hotel1 = Guid.NewGuid();
        var hotel2 = Guid.NewGuid();
        var hotel3 = Guid.NewGuid();

        using (CurrentTenant.Change(tenantId))
        {
            using (var uow = UnitOfWorkManager.Begin())
            {
                var role = await RoleManager.FindByNameAsync(BookingHotelRoleConsts.HotelManager);
                if (role == null)
                {
                    await RoleManager.CreateAsync(new IdentityRole(Guid.NewGuid(), BookingHotelRoleConsts.HotelManager, tenantId));
                }
                await uow.CompleteAsync();
            }

            // Create initial with hotel1 and hotel2
            var createDto = await UserManagementAppService.CreateAsync(new CreateUserWithAssignmentDto
            {
                UserName = $"manager_update_{Guid.NewGuid():N}",
                Email = $"manager.up.{Guid.NewGuid():N}@hotelchain.com",
                Password = "Password123*#",
                RoleNames = new List<string> { BookingHotelRoleConsts.HotelManager },
                HotelIds = new List<Guid> { hotel1, hotel2 },
                PrimaryHotelId = hotel1
            });

            // Update to keep hotel2 and add hotel3, removing hotel1
            var updateDto = await UserManagementAppService.UpdateAsync(createDto.Id, new UpdateUserWithAssignmentDto
            {
                Email = createDto.Email,
                RoleNames = new List<string> { BookingHotelRoleConsts.HotelManager },
                HotelIds = new List<Guid> { hotel2, hotel3 },
                PrimaryHotelId = hotel2
            });

            updateDto.AssignedHotels.Count.ShouldBe(2);
            updateDto.AssignedHotels.ShouldContain(h => h.HotelId == hotel2);
            updateDto.AssignedHotels.ShouldContain(h => h.HotelId == hotel3);
            updateDto.AssignedHotels.ShouldNotContain(h => h.HotelId == hotel1);

            // Verify hotel1 was soft unlinked in the database
            var allDbAssignments = await AssignmentRepository.GetListAsync(a => a.UserId == createDto.Id);
            var hotel1Db = allDbAssignments.First(a => a.HotelId == hotel1);
            hotel1Db.IsActive.ShouldBeFalse();
            hotel1Db.UnassignedAt.ShouldNotBeNull();
        }
    }

    [Fact]
    public async Task TenantAdmin_Should_Be_Forbidden_From_Assigning_SystemAdmin_Role()
    {
        var tenantId = Guid.NewGuid();

        using (CurrentTenant.Change(tenantId))
        {
            var input = new CreateUserWithAssignmentDto
            {
                UserName = $"hack_admin_{Guid.NewGuid():N}",
                Email = $"hack.{Guid.NewGuid():N}@hotelchain.com",
                Password = "Password123*#",
                RoleNames = new List<string> { BookingHotelRoleConsts.SystemAdmin }
            };

            var ex = await Should.ThrowAsync<BusinessException>(async () =>
            {
                await UserManagementAppService.CreateAsync(input);
            });

            ex.Code.ShouldBe("BookingHotel:CannotAssignUnauthorizedRole");
        }
    }

    [Fact]
    public async Task TenantAdmin_Should_Be_Forbidden_From_Modifying_CrossTenant_User()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        IdentityUser userB;
        using (CurrentTenant.Change(tenantB))
        {
            var emailB = $"userB.{Guid.NewGuid():N}@tenantb.com";
            userB = new IdentityUser(Guid.NewGuid(), emailB, emailB, tenantB);
            await UserManager.CreateAsync(userB, "Password123*#");
        }

        // Now operate as tenantA
        using (CurrentTenant.Change(tenantA))
        {
            var ex = await Should.ThrowAsync<BusinessException>(async () =>
            {
                await UserManagementAppService.UpdateAsync(userB.Id, new UpdateUserWithAssignmentDto
                {
                    Email = "hacked@tenanta.com",
                    RoleNames = new List<string> { BookingHotelRoleConsts.HotelManager }
                });
            });

            ex.Code.ShouldBe("BookingHotel:CrossTenantOperationForbidden");
        }
    }
}

