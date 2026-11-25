using System;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.AuditLogging.EntityFrameworkCore;
using Volo.Abp.BackgroundJobs.EntityFrameworkCore;
using Volo.Abp.BlobStoring.Database.EntityFrameworkCore;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore.Modeling;
using Volo.Abp.FeatureManagement.EntityFrameworkCore;
using Volo.Abp.Identity;
using Volo.Abp.Identity.EntityFrameworkCore;
using Volo.Abp.PermissionManagement.EntityFrameworkCore;
using Volo.Abp.SettingManagement.EntityFrameworkCore;
using Volo.Abp.OpenIddict.EntityFrameworkCore;
using Volo.Abp.TenantManagement;
using Volo.Abp.TenantManagement.EntityFrameworkCore;
using BookingHotel.Identity;
using BookingHotel.Approvals;
using BookingHotel.Hotels;
using BookingHotel.Organization;
using BookingHotel.Rooms;
using BookingHotel.Bookings;
using BookingHotel.Guests;
using BookingHotel.Complaints;
using BookingHotel.Reviews;

namespace BookingHotel.EntityFrameworkCore;

[ReplaceDbContext(typeof(IIdentityDbContext))]
[ReplaceDbContext(typeof(ITenantManagementDbContext))]
[ConnectionStringName("Default")]
public class BookingHotelDbContext :
    AbpDbContext<BookingHotelDbContext>,
    ITenantManagementDbContext,
    IIdentityDbContext
{
    /* Add DbSet properties for your Aggregate Roots / Entities here. */
    public DbSet<Hotel> Hotels { get; set; }
    public DbSet<HotelSuspensionRecord> HotelSuspensionRecords { get; set; }
    public DbSet<TenantOrganizationProfile> TenantOrganizationProfiles { get; set; }
    public DbSet<UserHotelAssignment> UserHotelAssignments { get; set; }
    public DbSet<OperationalApprovalTicket> OperationalApprovalTickets { get; set; }

    // Phân hệ Quản lý cơ sở buồng phòng & Đặt phòng (RM-008 & RM-010)
    public DbSet<RoomType> RoomTypes { get; set; }
    public DbSet<Room> Rooms { get; set; }
    public DbSet<RoomClosure> RoomClosures { get; set; }
    public DbSet<AffectedBookingResolution> AffectedBookingResolutions { get; set; }
    public DbSet<RatePlan> RatePlans { get; set; }
    public DbSet<HotelRatePlan> HotelRatePlans { get; set; }
    public DbSet<DailyRateOverride> DailyRateOverrides { get; set; }
    public DbSet<RoomPriceOverride> RoomPriceOverrides { get; set; }
    public DbSet<PricingAuditLog> PricingAuditLogs { get; set; }
    public DbSet<Booking> Bookings { get; set; }
    public DbSet<RoomAssignment> RoomAssignments { get; set; }
    public DbSet<InternalGuestNote> InternalGuestNotes { get; set; }
    public DbSet<Complaint> Complaints { get; set; }
    public DbSet<Review> Reviews { get; set; }


    #region Entities from the modules

    /* Notice: We only implemented IIdentityProDbContext and ISaasDbContext
     * and replaced them for this DbContext. This allows you to perform JOIN
     * queries for the entities of these modules over the repositories easily. You
     * typically don't need that for other modules. But, if you need, you can
     * implement the DbContext interface of the needed module and use ReplaceDbContext
     * attribute just like IIdentityProDbContext and ISaasDbContext.
     *
     * More info: Replacing a DbContext of a module ensures that the related module
     * uses this DbContext on runtime. Otherwise, it will use its own DbContext class.
     */

    // Identity
    public DbSet<IdentityUser> Users { get; set; }
    public DbSet<IdentityRole> Roles { get; set; }
    public DbSet<IdentityClaimType> ClaimTypes { get; set; }
    public DbSet<OrganizationUnit> OrganizationUnits { get; set; }
    public DbSet<IdentitySecurityLog> SecurityLogs { get; set; }
    public DbSet<IdentityLinkUser> LinkUsers { get; set; }
    public DbSet<IdentityUserDelegation> UserDelegations { get; set; }
    public DbSet<IdentitySession> Sessions { get; set; }

    // Tenant Management
    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<TenantConnectionString> TenantConnectionStrings { get; set; }

    #endregion

    public BookingHotelDbContext(DbContextOptions<BookingHotelDbContext> options)
        : base(options)
    {

    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        /* Include modules to your migration db context */

        builder.ConfigurePermissionManagement();
        builder.ConfigureSettingManagement();
        builder.ConfigureBackgroundJobs();
        builder.ConfigureAuditLogging();
        builder.ConfigureFeatureManagement();
        builder.ConfigureIdentity();
        builder.ConfigureOpenIddict();
        builder.ConfigureTenantManagement();
        builder.ConfigureBlobStoring();

        /* Configure your own tables/entities inside here */

        builder.Entity<UserHotelAssignment>(b =>
        {
            b.ToTable(BookingHotelConsts.DbTablePrefix + "UserHotelAssignments", BookingHotelConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.UserId).IsRequired();
            b.Property(x => x.HotelId).IsRequired();
            b.Property(x => x.AssignedDate).IsRequired();
            b.Property(x => x.IsPrimary).IsRequired().HasDefaultValue(true);
            b.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);
            b.Property(x => x.UnassignedAt);

            b.Ignore(x => x.AssignedAt);

            b.HasIndex(x => new { x.TenantId, x.UserId, x.HotelId, x.IsActive });
            b.HasIndex(x => new { x.TenantId, x.UserId, x.IsActive });
            b.HasIndex(x => new { x.TenantId, x.HotelId, x.IsActive });
        });

        builder.Entity<OperationalApprovalTicket>(b =>
        {
            b.ToTable(BookingHotelConsts.DbTablePrefix + "OperationalApprovalTickets", BookingHotelConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.HotelId).IsRequired();
            b.Property(x => x.TicketType).IsRequired();
            b.Property(x => x.Status).IsRequired().HasDefaultValue(ApprovalTicketStatus.Pending);
            b.Property(x => x.RequestedByStaffId).IsRequired();
            b.Property(x => x.RequestedByStaffName).IsRequired().HasMaxLength(256);
            b.Property(x => x.Reason).IsRequired().HasMaxLength(1000);
            b.Property(x => x.RoomNumber).HasMaxLength(32);
            b.Property(x => x.Amount).HasPrecision(18, 2);
            b.Property(x => x.DiscountPercent).HasPrecision(5, 2);
            b.Property(x => x.ReviewedByManagerName).HasMaxLength(256);
            b.Property(x => x.RejectionReason).HasMaxLength(1000);

            b.HasIndex(x => new { x.TenantId, x.HotelId, x.Status });
            b.HasIndex(x => new { x.TenantId, x.RequestedByStaffId, x.Status });
            b.HasIndex(x => x.BookingId);
        });

        builder.Entity<Hotel>(b =>
        {
            b.ToTable(BookingHotelConsts.DbTablePrefix + "Hotels", BookingHotelConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.Name).IsRequired().HasMaxLength(256);
            b.Property(x => x.Code).IsRequired().HasMaxLength(32);
            b.Property(x => x.Address).IsRequired().HasMaxLength(512);
            b.Property(x => x.City).IsRequired().HasMaxLength(128);
            b.Property(x => x.Phone).HasColumnName("PhoneNumber").IsRequired().HasMaxLength(32);
            b.Property(x => x.Email).IsRequired().HasMaxLength(256);
            b.Property(x => x.StarRating).IsRequired().HasDefaultValue(5);
            b.Property(x => x.RoomCount).HasColumnName("TotalRooms").IsRequired().HasDefaultValue(50);
            b.Property(x => x.ManagerName).HasMaxLength(256);
            b.Property(x => x.Status).IsRequired().HasDefaultValue(HotelStatus.Operating);
            b.Property(x => x.StandardCheckInTime).IsRequired().HasDefaultValue(new TimeSpan(14, 0, 0));
            b.Property(x => x.StandardCheckOutTime).IsRequired().HasDefaultValue(new TimeSpan(12, 0, 0));
            b.Property(x => x.BookingCutoffDays).IsRequired().HasDefaultValue(14);

            b.HasMany(x => x.SuspensionRecords)
                .WithOne()
                .HasForeignKey(x => x.HotelId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(x => new { x.TenantId, x.Code }).IsUnique().HasFilter("[IsDeleted] = 0");
            b.HasIndex(x => new { x.TenantId, x.Status });
        });

        builder.Entity<HotelSuspensionRecord>(b =>
        {
            b.ToTable(BookingHotelConsts.DbTablePrefix + "HotelSuspensionRecords", BookingHotelConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.HotelId).IsRequired();
            b.Property(x => x.SuspensionType).IsRequired();
            b.Property(x => x.StartDate).IsRequired();
            b.Property(x => x.EndDate);
            b.Property(x => x.Reason).HasMaxLength(1000);
            b.Property(x => x.AffectedBookingsCount).IsRequired().HasDefaultValue(0);

            b.HasIndex(x => new { x.TenantId, x.HotelId });
            b.HasIndex(x => new { x.TenantId, x.SuspensionType });
        });

        builder.Entity<TenantOrganizationProfile>(b =>
        {
            b.ToTable(BookingHotelConsts.DbTablePrefix + "TenantOrganizationProfiles", BookingHotelConsts.DbSchema);
            b.ConfigureByConvention();

            b.Property(x => x.OrganizationName).IsRequired().HasMaxLength(256);
            b.Property(x => x.LogoUrl).HasMaxLength(1024);
            b.Property(x => x.HeadquarterAddress).HasMaxLength(512);
            b.Property(x => x.Hotline).HasMaxLength(32);
            b.Property(x => x.ContactEmail).HasMaxLength(256);
            b.Property(x => x.WebsiteUrl).HasMaxLength(512);
            b.Property(x => x.DefaultCheckInTime).IsRequired().HasDefaultValue(new TimeSpan(14, 0, 0));
            b.Property(x => x.DefaultCheckOutTime).IsRequired().HasDefaultValue(new TimeSpan(12, 0, 0));
            b.Property(x => x.CancellationPolicyJson).HasMaxLength(4000);

            b.HasIndex(x => x.TenantId).IsUnique();
        });

        /* Cấu hình các bảng Phân hệ Quản lý buồng phòng & Đặt phòng (RM-008) */

        builder.Entity<RoomType>(b =>
        {
            b.ToTable(BookingHotelConsts.DbTablePrefix + "RoomTypes", BookingHotelConsts.DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.HotelId).IsRequired();
            b.Property(x => x.Name).IsRequired().HasMaxLength(128);
            b.Property(x => x.Description).HasMaxLength(2048);
            b.Property(x => x.BasePrice).HasPrecision(18, 2);
            b.Property(x => x.AmenitiesJson).HasMaxLength(4000);
            b.HasIndex(x => new { x.TenantId, x.HotelId, x.Name });
        });

        builder.Entity<Room>(b =>
        {
            b.ToTable(BookingHotelConsts.DbTablePrefix + "Rooms", BookingHotelConsts.DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.HotelId).IsRequired();
            b.Property(x => x.RoomTypeId).IsRequired();
            b.Property(x => x.RoomNumber).IsRequired().HasMaxLength(32);
            b.Property(x => x.Building).HasMaxLength(64);
            b.Property(x => x.Description).HasMaxLength(2048);
            b.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);
            b.Property(x => x.OccupancyStatus).IsRequired().HasDefaultValue(RoomOccupancyStatus.Vacant);
            b.Property(x => x.MaintenanceReason).HasMaxLength(1000);
            b.Property(x => x.OutOfServiceReason).HasMaxLength(1000);
            b.HasIndex(x => new { x.TenantId, x.HotelId, x.RoomNumber }).IsUnique().HasFilter("[IsDeleted] = 0");
            b.HasIndex(x => new { x.TenantId, x.HotelId, x.OperationalStatus });
            b.HasIndex(x => new { x.TenantId, x.HotelId, x.OccupancyStatus });
            b.HasIndex(x => new { x.TenantId, x.HotelId, x.Floor });
        });

        builder.Entity<RoomClosure>(b =>
        {
            b.ToTable(BookingHotelConsts.DbTablePrefix + "RoomClosures", BookingHotelConsts.DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.HotelId).IsRequired();
            b.Property(x => x.RoomId).IsRequired();
            b.Property(x => x.ClosureType).IsRequired();
            b.Property(x => x.Reason).IsRequired().HasMaxLength(500);
            b.Property(x => x.IncidentDescription).HasMaxLength(2000);
            b.Property(x => x.StartDate).IsRequired();
            b.Property(x => x.Status).IsRequired().HasDefaultValue(RoomClosureStatus.Active);
            b.HasIndex(x => new { x.TenantId, x.HotelId, x.Status });
            b.HasIndex(x => new { x.TenantId, x.RoomId, x.Status, x.StartDate, x.EndDate });
        });

        builder.Entity<AffectedBookingResolution>(b =>
        {
            b.ToTable(BookingHotelConsts.DbTablePrefix + "AffectedBookingResolutions", BookingHotelConsts.DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.RoomClosureId).IsRequired();
            b.Property(x => x.BookingId).IsRequired();
            b.Property(x => x.Notes).HasMaxLength(1000);
            b.HasIndex(x => new { x.TenantId, x.RoomClosureId });
            b.HasIndex(x => new { x.TenantId, x.BookingId });
        });

        builder.Entity<RatePlan>(b =>
        {
            b.ToTable(BookingHotelConsts.DbTablePrefix + "RatePlans", BookingHotelConsts.DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.Name).IsRequired().HasMaxLength(128);
            b.Property(x => x.Code).IsRequired().HasMaxLength(32);
            b.Property(x => x.Description).HasMaxLength(2048);
            b.Property(x => x.DefaultPriceAdjustmentPercent).HasPrecision(5, 2);
            b.HasIndex(x => new { x.TenantId, x.Code }).IsUnique().HasFilter("[IsDeleted] = 0");
        });

        builder.Entity<HotelRatePlan>(b =>
        {
            b.ToTable(BookingHotelConsts.DbTablePrefix + "HotelRatePlans", BookingHotelConsts.DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.HotelId).IsRequired();
            b.Property(x => x.RoomTypeId).IsRequired();
            b.Property(x => x.RatePlanId).IsRequired();
            b.Property(x => x.CustomBasePrice).HasPrecision(18, 2);
            b.Property(x => x.PriceMultiplier).HasPrecision(5, 2);
            b.HasIndex(x => new { x.TenantId, x.HotelId, x.RoomTypeId, x.RatePlanId }).IsUnique().HasFilter("[IsDeleted] = 0");
        });

        builder.Entity<DailyRateOverride>(b =>
        {
            b.ToTable(BookingHotelConsts.DbTablePrefix + "DailyRateOverrides", BookingHotelConsts.DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.HotelId).IsRequired();
            b.Property(x => x.RoomTypeId).IsRequired();
            b.Property(x => x.Date).IsRequired();
            b.Property(x => x.OverridePrice).HasPrecision(18, 2);
            b.HasIndex(x => new { x.TenantId, x.HotelId, x.RoomTypeId, x.Date }).IsUnique().HasFilter("[IsDeleted] = 0");
        });

        builder.Entity<RoomPriceOverride>(b =>
        {
            b.ToTable(BookingHotelConsts.DbTablePrefix + "RoomPriceOverrides", BookingHotelConsts.DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.HotelId).IsRequired();
            b.Property(x => x.RoomTypeId).IsRequired();
            b.Property(x => x.RatePlanId).IsRequired();
            b.Property(x => x.StartDate).IsRequired();
            b.Property(x => x.EndDate).IsRequired();
            b.Property(x => x.AdjustmentType).IsRequired();
            b.Property(x => x.AdjustmentValue).HasPrecision(18, 2);
            b.Property(x => x.CalculatedEffectivePrice).HasPrecision(18, 2);
            b.Property(x => x.IsClosedToArrival).IsRequired().HasDefaultValue(false);
            b.Property(x => x.IsClosedToDeparture).IsRequired().HasDefaultValue(false);
            b.Property(x => x.CreatorName).IsRequired().HasMaxLength(256);

            b.HasIndex(x => new { x.TenantId, x.HotelId, x.RoomTypeId, x.RatePlanId, x.StartDate, x.EndDate })
                .HasDatabaseName("IX_RoomPriceOverrides_Scope_Dates")
                .HasFilter("[IsDeleted] = 0");
            b.HasIndex(x => new { x.TenantId, x.HotelId, x.RoomTypeId })
                .HasDatabaseName("IX_RoomPriceOverrides_Hotel_RoomType")
                .HasFilter("[IsDeleted] = 0");
        });

        builder.Entity<PricingAuditLog>(b =>
        {
            b.ToTable(BookingHotelConsts.DbTablePrefix + "PricingAuditLogs", BookingHotelConsts.DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.HotelId).IsRequired();
            b.Property(x => x.RoomTypeId).IsRequired();
            b.Property(x => x.RoomTypeName).IsRequired().HasMaxLength(128);
            b.Property(x => x.RatePlanId).IsRequired();
            b.Property(x => x.RatePlanCode).IsRequired().HasMaxLength(32);
            b.Property(x => x.RatePlanName).IsRequired().HasMaxLength(128);
            b.Property(x => x.ActionType).IsRequired();
            b.Property(x => x.ActorId).IsRequired();
            b.Property(x => x.ActorName).IsRequired().HasMaxLength(256);
            b.Property(x => x.ActionTime).IsRequired();
            b.Property(x => x.BeforeValueJson).HasMaxLength(4000);
            b.Property(x => x.AfterValueJson).HasMaxLength(4000);
            b.Property(x => x.SummaryNote).HasMaxLength(500);

            b.HasIndex(x => new { x.TenantId, x.HotelId, x.ActionTime })
                .HasDatabaseName("IX_PricingAuditLogs_Hotel_Time");
            b.HasIndex(x => new { x.TenantId, x.HotelId, x.RoomTypeId, x.RatePlanId, x.ActionTime })
                .HasDatabaseName("IX_PricingAuditLogs_Scope_Time");
            b.HasIndex(x => new { x.TenantId, x.HotelId, x.ActionType, x.ActionTime })
                .HasDatabaseName("IX_PricingAuditLogs_Hotel_Action_Time");
        });

        builder.Entity<Booking>(b =>
        {
            b.ToTable(BookingHotelConsts.DbTablePrefix + "Bookings", BookingHotelConsts.DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.HotelId).IsRequired();
            b.Property(x => x.BookingCode).IsRequired().HasMaxLength(32);
            b.Property(x => x.GuestFullName).IsRequired().HasMaxLength(128);
            b.Property(x => x.GuestPhone).IsRequired().HasMaxLength(32);
            b.Property(x => x.GuestEmail).HasMaxLength(128);
            b.Property(x => x.GuestIdentityNumberMasked).HasMaxLength(32);
            b.Property(x => x.IsIdentityVerified).IsRequired().HasDefaultValue(false);
            b.Property(x => x.TotalAmount).HasPrecision(18, 2);
            b.Property(x => x.DepositAmount).HasPrecision(18, 2);
            b.Property(x => x.PenaltyAmount).HasPrecision(18, 2);
            b.Property(x => x.ContractNotes).HasMaxLength(2000);
            b.Property(x => x.WaivePenaltyReason).HasMaxLength(1000);
            b.Property(x => x.SpecialRequests).HasMaxLength(2000);
            b.Property(x => x.FrontdeskNotes).HasMaxLength(2000);
            b.Property(x => x.CancellationReason).HasMaxLength(1000);
            b.Property(x => x.RejectionReason).HasMaxLength(1000);
            b.HasIndex(x => new { x.TenantId, x.BookingCode }).IsUnique().HasFilter("[IsDeleted] = 0");
            b.HasIndex(x => new { x.TenantId, x.HotelId, x.BookingStatus });
            b.HasIndex(x => new { x.TenantId, x.HotelId, x.CheckInDate, x.CheckOutDate });
            b.HasIndex(x => new { x.TenantId, x.HotelId, x.RoomTypeId })
                .HasDatabaseName("IX_AppBookings_Tenant_Hotel_RoomType");
        });

        builder.Entity<RoomAssignment>(b =>
        {
            b.ToTable(BookingHotelConsts.DbTablePrefix + "RoomAssignments", BookingHotelConsts.DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.BookingId).IsRequired();
            b.Property(x => x.RoomId).IsRequired();
            b.Property(x => x.RoomTypeId).IsRequired();
            b.HasIndex(x => new { x.TenantId, x.RoomId, x.FromDate, x.ToDate, x.IsActive });
            b.HasIndex(x => x.BookingId);
        });

        builder.Entity<InternalGuestNote>(b =>
        {
            b.ToTable(BookingHotelConsts.DbTablePrefix + "InternalGuestNotes", BookingHotelConsts.DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.HotelId).IsRequired();
            b.Property(x => x.GuestId).IsRequired();
            b.Property(x => x.NoteText).IsRequired().HasMaxLength(2000);
            b.Property(x => x.Category).IsRequired().HasMaxLength(64);
            b.HasIndex(x => new { x.TenantId, x.HotelId, x.GuestId });
        });

        builder.Entity<Complaint>(b =>
        {
            b.ToTable(BookingHotelConsts.DbTablePrefix + "Complaints", BookingHotelConsts.DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.HotelId).IsRequired();
            b.Property(x => x.Title).IsRequired().HasMaxLength(256);
            b.Property(x => x.Description).IsRequired().HasMaxLength(4000);
            b.Property(x => x.Priority).IsRequired().HasMaxLength(32);
            b.Property(x => x.ResolutionNotes).HasMaxLength(4000);
            b.HasIndex(x => new { x.TenantId, x.HotelId, x.Status });
            b.HasIndex(x => new { x.TenantId, x.HotelId, x.SlaDeadline });
        });

        builder.Entity<Review>(b =>
        {
            b.ToTable(BookingHotelConsts.DbTablePrefix + "Reviews", BookingHotelConsts.DbSchema);
            b.ConfigureByConvention();
            b.Property(x => x.HotelId).IsRequired();
            b.Property(x => x.GuestId).IsRequired();
            b.Property(x => x.BookingId).IsRequired();
            b.Property(x => x.Comment).IsRequired().HasMaxLength(4000);
            b.Property(x => x.OfficialReply).HasMaxLength(4000);
            b.HasIndex(x => new { x.TenantId, x.HotelId, x.OverallScore });
            b.HasIndex(x => x.BookingId).IsUnique().HasFilter("[IsDeleted] = 0");
        });
    }
}
