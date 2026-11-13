using System.Threading.Tasks;
using BookingHotel.Data;
using BookingHotel.Identity;
using BookingHotel.Permissions;
using Shouldly;
using Volo.Abp.Data;
using Volo.Abp.Modularity;
using Volo.Abp.MultiTenancy;
using Volo.Abp.PermissionManagement;
using Xunit;

namespace BookingHotel.Permissions;

public abstract class BookingHotelPermissionSecurityTests<TStartupModule> : BookingHotelApplicationTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    private readonly IPermissionManager _permissionManager;
    private readonly BookingHotelDataSeedContributor _dataSeedContributor;
    private readonly ICurrentTenant _currentTenant;

    protected BookingHotelPermissionSecurityTests()
    {
        _permissionManager = GetRequiredService<IPermissionManager>();
        _dataSeedContributor = GetRequiredService<BookingHotelDataSeedContributor>();
        _currentTenant = GetRequiredService<ICurrentTenant>();
    }

    [Fact]
    public async Task Customer_Role_Should_Only_Have_SelfService_Permissions_And_Be_Forbidden_From_Staff_Actions()
    {
        // Seed host data to ensure Customer role and its default permissions are configured
        await _dataSeedContributor.SeedAsync(new DataSeedContext(tenantId: null));

        using (_currentTenant.Change(null))
        {
            // 1. Customer self-service permissions MUST be granted
            (await _permissionManager.GetForRoleAsync(BookingHotelRoleConsts.Customer, BookingHotelPermissions.Bookings.Create)).IsGranted.ShouldBeTrue();
            (await _permissionManager.GetForRoleAsync(BookingHotelRoleConsts.Customer, BookingHotelPermissions.Bookings.Cancel)).IsGranted.ShouldBeTrue();
            (await _permissionManager.GetForRoleAsync(BookingHotelRoleConsts.Customer, BookingHotelPermissions.Folios.View)).IsGranted.ShouldBeTrue();

            // 2. Staff & Admin internal permissions MUST NOT be granted to Customer (Forbidden)
            (await _permissionManager.GetForRoleAsync(BookingHotelRoleConsts.Customer, BookingHotelPermissions.Tenants.Create)).IsGranted.ShouldBeFalse();
            (await _permissionManager.GetForRoleAsync(BookingHotelRoleConsts.Customer, BookingHotelPermissions.Hotels.Create)).IsGranted.ShouldBeFalse();
            (await _permissionManager.GetForRoleAsync(BookingHotelRoleConsts.Customer, BookingHotelPermissions.Hotels.AssignStaff)).IsGranted.ShouldBeFalse();
            (await _permissionManager.GetForRoleAsync(BookingHotelRoleConsts.Customer, BookingHotelPermissions.Rooms.Create)).IsGranted.ShouldBeFalse();
            (await _permissionManager.GetForRoleAsync(BookingHotelRoleConsts.Customer, BookingHotelPermissions.Rooms.SetRates)).IsGranted.ShouldBeFalse();
            (await _permissionManager.GetForRoleAsync(BookingHotelRoleConsts.Customer, BookingHotelPermissions.Bookings.CheckIn)).IsGranted.ShouldBeFalse();
            (await _permissionManager.GetForRoleAsync(BookingHotelRoleConsts.Customer, BookingHotelPermissions.Folios.Settle)).IsGranted.ShouldBeFalse();
            (await _permissionManager.GetForRoleAsync(BookingHotelRoleConsts.Customer, BookingHotelPermissions.Reports.Consolidated)).IsGranted.ShouldBeFalse();
            (await _permissionManager.GetForRoleAsync(BookingHotelRoleConsts.Customer, BookingHotelPermissions.Identity.UnlockUser)).IsGranted.ShouldBeFalse();
        }
    }
}
