using System;
using System.Threading.Tasks;
using BookingHotel.Identity;
using BookingHotel.Permissions;
using Shouldly;
using Volo.Abp.Data;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Volo.Abp.MultiTenancy;
using Volo.Abp.PermissionManagement;
using Xunit;

namespace BookingHotel.Data;

public abstract class BookingHotelDataSeedContributorTests<TStartupModule> : BookingHotelDomainTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    private readonly BookingHotelDataSeedContributor _dataSeedContributor;
    private readonly IdentityRoleManager _roleManager;
    private readonly IPermissionManager _permissionManager;
    private readonly ICurrentTenant _currentTenant;

    protected BookingHotelDataSeedContributorTests()
    {
        _dataSeedContributor = GetRequiredService<BookingHotelDataSeedContributor>();
        _roleManager = GetRequiredService<IdentityRoleManager>();
        _permissionManager = GetRequiredService<IPermissionManager>();
        _currentTenant = GetRequiredService<ICurrentTenant>();
    }

    [Fact]
    public async Task Should_Seed_SystemAdmin_And_Customer_At_Host_Level()
    {
        // Act
        await _dataSeedContributor.SeedAsync(new DataSeedContext(tenantId: null));

        // Assert SystemAdmin
        using (_currentTenant.Change(null))
        {
            var systemAdminRole = await _roleManager.FindByNameAsync(BookingHotelRoleConsts.SystemAdmin);
            systemAdminRole.ShouldNotBeNull();
            systemAdminRole.TenantId.ShouldBeNull();
            systemAdminRole.IsStatic.ShouldBeTrue();

            var sysAdminTenantsPerm = await _permissionManager.GetForRoleAsync(BookingHotelRoleConsts.SystemAdmin, BookingHotelPermissions.Tenants.Create);
            sysAdminTenantsPerm.IsGranted.ShouldBeTrue();

            var sysAdminUnlockPerm = await _permissionManager.GetForRoleAsync(BookingHotelRoleConsts.SystemAdmin, BookingHotelPermissions.Identity.UnlockUser);
            sysAdminUnlockPerm.IsGranted.ShouldBeTrue();

            // Assert Customer
            var customerRole = await _roleManager.FindByNameAsync(BookingHotelRoleConsts.Customer);
            customerRole.ShouldNotBeNull();
            customerRole.TenantId.ShouldBeNull();
            customerRole.IsDefault.ShouldBeTrue();

            var customerBookingPerm = await _permissionManager.GetForRoleAsync(BookingHotelRoleConsts.Customer, BookingHotelPermissions.Bookings.Create);
            customerBookingPerm.IsGranted.ShouldBeTrue();

            var customerFolioPerm = await _permissionManager.GetForRoleAsync(BookingHotelRoleConsts.Customer, BookingHotelPermissions.Folios.View);
            customerFolioPerm.IsGranted.ShouldBeTrue();
        }
    }

    [Fact]
    public async Task Should_Seed_TenantAdmin_HotelManager_And_Receptionist_At_Tenant_Level()
    {
        var tenantId = Guid.NewGuid();

        // Act
        await _dataSeedContributor.SeedAsync(new DataSeedContext(tenantId));

        using (_currentTenant.Change(tenantId))
        {
            // 1. TenantAdmin
            var tenantAdmin = await _roleManager.FindByNameAsync(BookingHotelRoleConsts.TenantAdmin);
            tenantAdmin.ShouldNotBeNull();
            tenantAdmin.TenantId.ShouldBe(tenantId);

            var tenantAdminReportPerm = await _permissionManager.GetForRoleAsync(BookingHotelRoleConsts.TenantAdmin, BookingHotelPermissions.Reports.Consolidated);
            tenantAdminReportPerm.IsGranted.ShouldBeTrue();

            var tenantAdminBranchPerm = await _permissionManager.GetForRoleAsync(BookingHotelRoleConsts.TenantAdmin, BookingHotelPermissions.Hotels.ManageBranches);
            tenantAdminBranchPerm.IsGranted.ShouldBeTrue();

            // 2. HotelManager
            var hotelManager = await _roleManager.FindByNameAsync(BookingHotelRoleConsts.HotelManager);
            hotelManager.ShouldNotBeNull();
            hotelManager.TenantId.ShouldBe(tenantId);

            var mgrRatePerm = await _permissionManager.GetForRoleAsync(BookingHotelRoleConsts.HotelManager, BookingHotelPermissions.Rooms.SetRates);
            mgrRatePerm.IsGranted.ShouldBeTrue();

            var mgrPerfReportPerm = await _permissionManager.GetForRoleAsync(BookingHotelRoleConsts.HotelManager, BookingHotelPermissions.Reports.HotelPerformance);
            mgrPerfReportPerm.IsGranted.ShouldBeTrue();

            var mgrConsolidatedReportPerm = await _permissionManager.GetForRoleAsync(BookingHotelRoleConsts.HotelManager, BookingHotelPermissions.Reports.Consolidated);
            mgrConsolidatedReportPerm.IsGranted.ShouldBeFalse();

            // 3. Receptionist
            var receptionist = await _roleManager.FindByNameAsync(BookingHotelRoleConsts.Receptionist);
            receptionist.ShouldNotBeNull();
            receptionist.TenantId.ShouldBe(tenantId);

            var recepCheckInPerm = await _permissionManager.GetForRoleAsync(BookingHotelRoleConsts.Receptionist, BookingHotelPermissions.Bookings.CheckIn);
            recepCheckInPerm.IsGranted.ShouldBeTrue();

            var recepSettlePerm = await _permissionManager.GetForRoleAsync(BookingHotelRoleConsts.Receptionist, BookingHotelPermissions.Folios.Settle);
            recepSettlePerm.IsGranted.ShouldBeTrue();

            var recepRatesPerm = await _permissionManager.GetForRoleAsync(BookingHotelRoleConsts.Receptionist, BookingHotelPermissions.Rooms.SetRates);
            recepRatesPerm.IsGranted.ShouldBeFalse();
        }
    }
}
