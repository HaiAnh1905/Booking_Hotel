using System;
using System.Threading.Tasks;
using BookingHotel.Identity;
using BookingHotel.Permissions;
using Microsoft.AspNetCore.Identity;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Guids;
using Volo.Abp.Identity;
using Volo.Abp.MultiTenancy;
using Volo.Abp.PermissionManagement;
using Volo.Abp.Uow;

namespace BookingHotel.Data;

public class BookingHotelDataSeedContributor : IDataSeedContributor, ITransientDependency
{
    private readonly IdentityRoleManager _roleManager;
    private readonly IPermissionManager _permissionManager;
    private readonly IGuidGenerator _guidGenerator;
    private readonly ICurrentTenant _currentTenant;

    public BookingHotelDataSeedContributor(
        IdentityRoleManager roleManager,
        IPermissionManager permissionManager,
        IGuidGenerator guidGenerator,
        ICurrentTenant currentTenant)
    {
        _roleManager = roleManager;
        _permissionManager = permissionManager;
        _guidGenerator = guidGenerator;
        _currentTenant = currentTenant;
    }

    [UnitOfWork]
    public virtual async Task SeedAsync(DataSeedContext context)
    {
        if (context.TenantId == null)
        {
            await SeedHostRolesAndPermissionsAsync();
        }
        else
        {
            await SeedTenantRolesAndPermissionsAsync(context.TenantId.Value);
        }
    }

    private async Task SeedHostRolesAndPermissionsAsync()
    {
        using (_currentTenant.Change(null))
        {
            // 1. SystemAdmin Role
            await CreateRoleAndGrantPermissionsAsync(
                BookingHotelRoleConsts.SystemAdmin,
                null,
                isDefault: false,
                permissions: new[]
                {
                    BookingHotelPermissions.Tenants.Default,
                    BookingHotelPermissions.Tenants.Create,
                    BookingHotelPermissions.Tenants.Edit,
                    BookingHotelPermissions.Tenants.Delete,
                    BookingHotelPermissions.Tenants.Suspend,
                    BookingHotelPermissions.Identity.UnlockUser
                });

            // 2. Customer Role (Global Customer at Host level)
            await CreateRoleAndGrantPermissionsAsync(
                BookingHotelRoleConsts.Customer,
                null,
                isDefault: true,
                permissions: new[]
                {
                    BookingHotelPermissions.Bookings.Default,
                    BookingHotelPermissions.Bookings.Create,
                    BookingHotelPermissions.Bookings.Cancel,
                    BookingHotelPermissions.Folios.Default,
                    BookingHotelPermissions.Folios.View
                });
        }
    }

    private async Task SeedTenantRolesAndPermissionsAsync(Guid tenantId)
    {
        using (_currentTenant.Change(tenantId))
        {
            // 1. TenantAdmin Role
            await CreateRoleAndGrantPermissionsAsync(
                BookingHotelRoleConsts.TenantAdmin,
                tenantId,
                isDefault: false,
                permissions: new[]
                {
                    BookingHotelPermissions.Hotels.Default,
                    BookingHotelPermissions.Hotels.Create,
                    BookingHotelPermissions.Hotels.Edit,
                    BookingHotelPermissions.Hotels.ManageBranches,
                    BookingHotelPermissions.Hotels.AssignStaff,
                    BookingHotelPermissions.Rooms.Default,
                    BookingHotelPermissions.Rooms.Create,
                    BookingHotelPermissions.Rooms.Edit,
                    BookingHotelPermissions.Rooms.Delete,
                    BookingHotelPermissions.Rooms.SetRates,
                    BookingHotelPermissions.Rooms.UpdateStatus,
                    BookingHotelPermissions.Bookings.Default,
                    BookingHotelPermissions.Bookings.Create,
                    BookingHotelPermissions.Bookings.CheckIn,
                    BookingHotelPermissions.Bookings.CheckOut,
                    BookingHotelPermissions.Bookings.Cancel,
                    BookingHotelPermissions.Bookings.ApproveException,
                    BookingHotelPermissions.Folios.Default,
                    BookingHotelPermissions.Folios.View,
                    BookingHotelPermissions.Folios.AddCharge,
                    BookingHotelPermissions.Folios.Settle,
                    BookingHotelPermissions.Folios.Refund,
                    BookingHotelPermissions.Identity.UnlockUser,
                    BookingHotelPermissions.Reports.Default,
                    BookingHotelPermissions.Reports.Consolidated,
                    BookingHotelPermissions.Reports.HotelPerformance
                });

            // 2. HotelManager Role
            await CreateRoleAndGrantPermissionsAsync(
                BookingHotelRoleConsts.HotelManager,
                tenantId,
                isDefault: false,
                permissions: new[]
                {
                    BookingHotelPermissions.Hotels.Default,
                    BookingHotelPermissions.Rooms.Default,
                    BookingHotelPermissions.Rooms.Create,
                    BookingHotelPermissions.Rooms.Edit,
                    BookingHotelPermissions.Rooms.Delete,
                    BookingHotelPermissions.Rooms.SetRates,
                    BookingHotelPermissions.Rooms.UpdateStatus,
                    BookingHotelPermissions.Bookings.Default,
                    BookingHotelPermissions.Bookings.Create,
                    BookingHotelPermissions.Bookings.CheckIn,
                    BookingHotelPermissions.Bookings.CheckOut,
                    BookingHotelPermissions.Bookings.Cancel,
                    BookingHotelPermissions.Bookings.ApproveException,
                    BookingHotelPermissions.Folios.Default,
                    BookingHotelPermissions.Folios.View,
                    BookingHotelPermissions.Folios.AddCharge,
                    BookingHotelPermissions.Folios.Settle,
                    BookingHotelPermissions.Folios.Refund,
                    BookingHotelPermissions.Reports.Default,
                    BookingHotelPermissions.Reports.HotelPerformance
                });

            // 3. Receptionist Role
            await CreateRoleAndGrantPermissionsAsync(
                BookingHotelRoleConsts.Receptionist,
                tenantId,
                isDefault: false,
                permissions: new[]
                {
                    BookingHotelPermissions.Rooms.Default,
                    BookingHotelPermissions.Rooms.UpdateStatus,
                    BookingHotelPermissions.Bookings.Default,
                    BookingHotelPermissions.Bookings.Create,
                    BookingHotelPermissions.Bookings.CheckIn,
                    BookingHotelPermissions.Bookings.CheckOut,
                    BookingHotelPermissions.Bookings.Cancel,
                    BookingHotelPermissions.Folios.Default,
                    BookingHotelPermissions.Folios.View,
                    BookingHotelPermissions.Folios.AddCharge,
                    BookingHotelPermissions.Folios.Settle
                });
        }
    }

    private async Task CreateRoleAndGrantPermissionsAsync(
        string roleName,
        Guid? tenantId,
        bool isDefault,
        string[] permissions)
    {
        var role = await _roleManager.FindByNameAsync(roleName);
        if (role == null)
        {
            role = new IdentityRole(_guidGenerator.Create(), roleName, tenantId)
            {
                IsStatic = true,
                IsPublic = true,
                IsDefault = isDefault
            };
            (await _roleManager.CreateAsync(role)).CheckErrors();
        }

        foreach (var permission in permissions)
        {
            await _permissionManager.SetForRoleAsync(roleName, permission, isGranted: true);
        }
    }
}
