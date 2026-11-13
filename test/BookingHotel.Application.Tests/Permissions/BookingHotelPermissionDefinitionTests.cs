using System.Threading.Tasks;
using BookingHotel.Permissions;
using Shouldly;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Modularity;
using Volo.Abp.MultiTenancy;
using Xunit;

namespace BookingHotel.Permissions;

public abstract class BookingHotelPermissionDefinitionTests<TStartupModule> : BookingHotelApplicationTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    private readonly IPermissionDefinitionManager _permissionDefinitionManager;

    protected BookingHotelPermissionDefinitionTests()
    {
        _permissionDefinitionManager = GetRequiredService<IPermissionDefinitionManager>();
    }

    [Fact]
    public async Task Tenants_Permissions_Should_Be_Configured_As_Host_Only()
    {
        var tenantsGroup = await _permissionDefinitionManager.GetAsync(BookingHotelPermissions.Tenants.Default);
        tenantsGroup.ShouldNotBeNull();
        tenantsGroup.MultiTenancySide.ShouldBe(MultiTenancySides.Host);

        var tenantsCreate = await _permissionDefinitionManager.GetAsync(BookingHotelPermissions.Tenants.Create);
        tenantsCreate.ShouldNotBeNull();
        tenantsCreate.MultiTenancySide.ShouldBe(MultiTenancySides.Host);

        var tenantsEdit = await _permissionDefinitionManager.GetAsync(BookingHotelPermissions.Tenants.Edit);
        tenantsEdit.ShouldNotBeNull();
        tenantsEdit.MultiTenancySide.ShouldBe(MultiTenancySides.Host);

        var tenantsDelete = await _permissionDefinitionManager.GetAsync(BookingHotelPermissions.Tenants.Delete);
        tenantsDelete.ShouldNotBeNull();
        tenantsDelete.MultiTenancySide.ShouldBe(MultiTenancySides.Host);

        var tenantsSuspend = await _permissionDefinitionManager.GetAsync(BookingHotelPermissions.Tenants.Suspend);
        tenantsSuspend.ShouldNotBeNull();
        tenantsSuspend.MultiTenancySide.ShouldBe(MultiTenancySides.Host);
    }

    [Fact]
    public async Task UnlockUser_And_Bookings_Shared_Permissions_Should_Be_Both()
    {
        var unlockUser = await _permissionDefinitionManager.GetAsync(BookingHotelPermissions.Identity.UnlockUser);
        unlockUser.ShouldNotBeNull();
        unlockUser.MultiTenancySide.ShouldBe(MultiTenancySides.Both);

        var bookingCreate = await _permissionDefinitionManager.GetAsync(BookingHotelPermissions.Bookings.Create);
        bookingCreate.ShouldNotBeNull();
        bookingCreate.MultiTenancySide.ShouldBe(MultiTenancySides.Both);

        var bookingCancel = await _permissionDefinitionManager.GetAsync(BookingHotelPermissions.Bookings.Cancel);
        bookingCancel.ShouldNotBeNull();
        bookingCancel.MultiTenancySide.ShouldBe(MultiTenancySides.Both);

        var foliosView = await _permissionDefinitionManager.GetAsync(BookingHotelPermissions.Folios.View);
        foliosView.ShouldNotBeNull();
        foliosView.MultiTenancySide.ShouldBe(MultiTenancySides.Both);
    }

    [Fact]
    public async Task Tenant_Operational_Permissions_Should_Be_Tenant_Only()
    {
        var hotelManage = await _permissionDefinitionManager.GetAsync(BookingHotelPermissions.Hotels.ManageBranches);
        hotelManage.ShouldNotBeNull();
        hotelManage.MultiTenancySide.ShouldBe(MultiTenancySides.Tenant);

        var roomCreate = await _permissionDefinitionManager.GetAsync(BookingHotelPermissions.Rooms.Create);
        roomCreate.ShouldNotBeNull();
        roomCreate.MultiTenancySide.ShouldBe(MultiTenancySides.Tenant);

        var checkIn = await _permissionDefinitionManager.GetAsync(BookingHotelPermissions.Bookings.CheckIn);
        checkIn.ShouldNotBeNull();
        checkIn.MultiTenancySide.ShouldBe(MultiTenancySides.Tenant);

        var reportsConsolidated = await _permissionDefinitionManager.GetAsync(BookingHotelPermissions.Reports.Consolidated);
        reportsConsolidated.ShouldNotBeNull();
        reportsConsolidated.MultiTenancySide.ShouldBe(MultiTenancySides.Tenant);
    }

    [Fact]
    public void Localization_Keys_Should_Be_Synchronized_Between_Vi_And_En()
    {
        var viPath = System.IO.Path.Combine(System.AppContext.BaseDirectory, "../../../../../src/BookingHotel.Domain.Shared/Localization/BookingHotel/vi.json");
        var enPath = System.IO.Path.Combine(System.AppContext.BaseDirectory, "../../../../../src/BookingHotel.Domain.Shared/Localization/BookingHotel/en.json");

        if (System.IO.File.Exists(viPath) && System.IO.File.Exists(enPath))
        {
            var viJson = System.Text.Json.JsonDocument.Parse(System.IO.File.ReadAllText(viPath));
            var enJson = System.Text.Json.JsonDocument.Parse(System.IO.File.ReadAllText(enPath));

            var viKeys = new System.Collections.Generic.HashSet<string>();
            var viTexts = viJson.RootElement.TryGetProperty("Texts", out var viT) ? viT : viJson.RootElement.GetProperty("texts");
            foreach (var prop in viTexts.EnumerateObject())
            {
                viKeys.Add(prop.Name);
            }

            var enKeys = new System.Collections.Generic.HashSet<string>();
            var enTexts = enJson.RootElement.TryGetProperty("Texts", out var enT) ? enT : enJson.RootElement.GetProperty("texts");
            foreach (var prop in enTexts.EnumerateObject())
            {
                enKeys.Add(prop.Name);
            }

            var missingInEn = new System.Collections.Generic.List<string>();
            foreach (var k in viKeys)
            {
                if (!enKeys.Contains(k)) missingInEn.Add(k);
            }

            var missingInVi = new System.Collections.Generic.List<string>();
            foreach (var k in enKeys)
            {
                if (!viKeys.Contains(k)) missingInVi.Add(k);
            }

            missingInEn.ShouldBeEmpty("These keys exist in vi.json but missing in en.json");
            missingInVi.ShouldBeEmpty("These keys exist in en.json but missing in vi.json");
        }
    }
}
