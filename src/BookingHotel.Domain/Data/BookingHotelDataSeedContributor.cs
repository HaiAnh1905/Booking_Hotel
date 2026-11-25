using System;
using System.Threading.Tasks;
using BookingHotel.Identity;
using BookingHotel.Permissions;
using BookingHotel.Approvals;
using BookingHotel.Rooms;
using Microsoft.AspNetCore.Identity;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
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
    private readonly IRepository<OperationalApprovalTicket, Guid> _approvalTicketRepository;
    private readonly IRepository<RatePlan, Guid> _ratePlanRepository;

    public BookingHotelDataSeedContributor(
        IdentityRoleManager roleManager,
        IPermissionManager permissionManager,
        IGuidGenerator guidGenerator,
        ICurrentTenant currentTenant,
        IRepository<OperationalApprovalTicket, Guid> approvalTicketRepository,
        IRepository<RatePlan, Guid> ratePlanRepository)
    {
        _roleManager = roleManager;
        _permissionManager = permissionManager;
        _guidGenerator = guidGenerator;
        _currentTenant = currentTenant;
        _approvalTicketRepository = approvalTicketRepository;
        _ratePlanRepository = ratePlanRepository;
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
                    BookingHotelPermissions.Identity.UnlockUser,
                    BookingHotelPermissions.Hotels.Default,
                    "AbpIdentity.Users",
                    "AbpIdentity.Users.Create",
                    "AbpIdentity.Users.Update",
                    "AbpIdentity.Users.Delete",
                    "AbpIdentity.Users.ManagePermissions",
                    "AbpIdentity.Roles",
                    "AbpIdentity.Roles.Create",
                    "AbpIdentity.Roles.Update",
                    "AbpIdentity.Roles.Delete",
                    "AbpIdentity.Roles.ManagePermissions"
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
                    BookingHotelPermissions.Guests.Default,
                    BookingHotelPermissions.Guests.ManageNotes,
                    BookingHotelPermissions.Reviews.Default,
                    BookingHotelPermissions.Reviews.Reply,
                    BookingHotelPermissions.Reviews.ManageComplaints,
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
                    BookingHotelPermissions.Guests.Default,
                    BookingHotelPermissions.Guests.ManageNotes,
                    BookingHotelPermissions.Reviews.Default,
                    BookingHotelPermissions.Reviews.Reply,
                    BookingHotelPermissions.Reviews.ManageComplaints,
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

            // 4. Sample Operational Approval Ticket
            if (await _approvalTicketRepository.GetCountAsync() == 0)
            {
                var sampleHotelId = Guid.Parse("4d5e8241-1111-2222-3333-444455556666");
                var staffId = _guidGenerator.Create();
                var ticket = new OperationalApprovalTicket(
                    _guidGenerator.Create(),
                    sampleHotelId,
                    ApprovalTicketType.DiscountOverride,
                    staffId,
                    "Nguyễn Văn Lễ Tân",
                    "Khách VIP kỷ niệm ngày cưới muốn xin chiết khấu phòng 15%",
                    tenantId,
                    roomNumber: "P.302",
                    amount: 250000,
                    discountPercent: 15);

                await _approvalTicketRepository.InsertAsync(ticket, autoSave: true);
            }

            // 5. Default Rate Plans (Gói giá phòng chuỗi)
            if (await _ratePlanRepository.GetCountAsync() == 0)
            {
                await _ratePlanRepository.InsertAsync(new RatePlan(
                    _guidGenerator.Create(),
                    "Gói Tiêu Chuẩn Linh Hoạt",
                    "STANDARD",
                    "Cho phép hủy miễn phí trước 24 giờ. Không bao gồm bữa sáng.",
                    isRefundable: true,
                    includesBreakfast: false,
                    cancellationCutoffHours: 24,
                    defaultPriceAdjustmentPercent: 0m,
                    tenantId), autoSave: true);

                await _ratePlanRepository.InsertAsync(new RatePlan(
                    _guidGenerator.Create(),
                    "Gói Kèm Buffet Sáng",
                    "BED_AND_BREAKFAST",
                    "Bao gồm buffet ăn sáng hàng ngày cho số khách tiêu chuẩn. Hủy linh hoạt trước 24 giờ.",
                    isRefundable: true,
                    includesBreakfast: true,
                    cancellationCutoffHours: 24,
                    defaultPriceAdjustmentPercent: 10m,
                    tenantId), autoSave: true);

                await _ratePlanRepository.InsertAsync(new RatePlan(
                    _guidGenerator.Create(),
                    "Gói Tiết Kiệm Không Hoàn Hủy",
                    "NON_REFUNDABLE",
                    "Ưu đãi giảm 10% giá phòng. Thanh toán ngay, không áp dụng hoàn tiền hoặc hủy đổi.",
                    isRefundable: false,
                    includesBreakfast: false,
                    cancellationCutoffHours: 0,
                    defaultPriceAdjustmentPercent: -10m,
                    tenantId), autoSave: true);
            }
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
