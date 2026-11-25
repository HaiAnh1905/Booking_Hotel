using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingHotel.Migrations
{
    /// <inheritdoc />
    public partial class AddHotelManagerOperationsEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppBookings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    HotelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BookingCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    GuestId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    GuestFullName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    GuestPhone = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    GuestEmail = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    GuestIdentityNumberMasked = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CheckInDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CheckOutDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DepositAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    BookingStatus = table.Column<int>(type: "int", nullable: false),
                    PaymentStatus = table.Column<int>(type: "int", nullable: false),
                    RoomAssignmentStatus = table.Column<int>(type: "int", nullable: false),
                    IsDirectBooking = table.Column<bool>(type: "bit", nullable: false),
                    ContractNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    WaivePenaltyReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PenaltyAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SpecialRequests = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ExtraProperties = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeleterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppBookings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppComplaints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    HotelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GuestId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BookingId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Priority = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SlaDeadline = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EscalatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResolutionNotes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ResolvedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExtraProperties = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeleterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppComplaints", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppDailyRateOverrides",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    HotelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoomTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OverridePrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsClosedToArrival = table.Column<bool>(type: "bit", nullable: false),
                    IsClosedToDeparture = table.Column<bool>(type: "bit", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeleterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppDailyRateOverrides", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppHotelRatePlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    HotelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoomTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RatePlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CustomBasePrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    PriceMultiplier = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeleterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppHotelRatePlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppInternalGuestNotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    HotelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GuestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NoteText = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    IsAlert = table.Column<bool>(type: "bit", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeleterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppInternalGuestNotes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppRatePlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    IsRefundable = table.Column<bool>(type: "bit", nullable: false),
                    IncludesBreakfast = table.Column<bool>(type: "bit", nullable: false),
                    CancellationCutoffHours = table.Column<int>(type: "int", nullable: false),
                    DefaultPriceAdjustmentPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    ExtraProperties = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeleterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppRatePlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppReviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    HotelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GuestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BookingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CleanlinessScore = table.Column<int>(type: "int", nullable: false),
                    StaffScore = table.Column<int>(type: "int", nullable: false),
                    AmenitiesScore = table.Column<int>(type: "int", nullable: false),
                    LocationScore = table.Column<int>(type: "int", nullable: false),
                    OverallScore = table.Column<double>(type: "float", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    OfficialReply = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    RepliedById = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RepliedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExtraProperties = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeleterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppReviews", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppRoomAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BookingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoomId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoomTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ToDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeleterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppRoomAssignments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppRooms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    HotelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoomTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoomNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Floor = table.Column<int>(type: "int", nullable: false),
                    OperationalStatus = table.Column<int>(type: "int", nullable: false),
                    HousekeepingStatus = table.Column<int>(type: "int", nullable: false),
                    MaintenanceReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    MaintenanceEndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OutOfServiceReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    OutOfServiceEndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExtraProperties = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeleterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppRooms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppRoomTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    HotelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    StandardOccupancy = table.Column<int>(type: "int", nullable: false),
                    MaxOccupancy = table.Column<int>(type: "int", nullable: false),
                    BasePrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SizeSquareMeters = table.Column<double>(type: "float", nullable: false),
                    AmenitiesJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    ExtraProperties = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeleterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppRoomTypes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppBookings_TenantId_BookingCode",
                table: "AppBookings",
                columns: new[] { "TenantId", "BookingCode" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_AppBookings_TenantId_HotelId_BookingStatus",
                table: "AppBookings",
                columns: new[] { "TenantId", "HotelId", "BookingStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_AppBookings_TenantId_HotelId_CheckInDate_CheckOutDate",
                table: "AppBookings",
                columns: new[] { "TenantId", "HotelId", "CheckInDate", "CheckOutDate" });

            migrationBuilder.CreateIndex(
                name: "IX_AppComplaints_TenantId_HotelId_SlaDeadline",
                table: "AppComplaints",
                columns: new[] { "TenantId", "HotelId", "SlaDeadline" });

            migrationBuilder.CreateIndex(
                name: "IX_AppComplaints_TenantId_HotelId_Status",
                table: "AppComplaints",
                columns: new[] { "TenantId", "HotelId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_AppDailyRateOverrides_TenantId_HotelId_RoomTypeId_Date",
                table: "AppDailyRateOverrides",
                columns: new[] { "TenantId", "HotelId", "RoomTypeId", "Date" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_AppHotelRatePlans_TenantId_HotelId_RoomTypeId_RatePlanId",
                table: "AppHotelRatePlans",
                columns: new[] { "TenantId", "HotelId", "RoomTypeId", "RatePlanId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_AppInternalGuestNotes_TenantId_HotelId_GuestId",
                table: "AppInternalGuestNotes",
                columns: new[] { "TenantId", "HotelId", "GuestId" });

            migrationBuilder.CreateIndex(
                name: "IX_AppRatePlans_TenantId_Code",
                table: "AppRatePlans",
                columns: new[] { "TenantId", "Code" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_AppReviews_BookingId",
                table: "AppReviews",
                column: "BookingId",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_AppReviews_TenantId_HotelId_OverallScore",
                table: "AppReviews",
                columns: new[] { "TenantId", "HotelId", "OverallScore" });

            migrationBuilder.CreateIndex(
                name: "IX_AppRoomAssignments_BookingId",
                table: "AppRoomAssignments",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_AppRoomAssignments_TenantId_RoomId_FromDate_ToDate_IsActive",
                table: "AppRoomAssignments",
                columns: new[] { "TenantId", "RoomId", "FromDate", "ToDate", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_AppRooms_TenantId_HotelId_OperationalStatus",
                table: "AppRooms",
                columns: new[] { "TenantId", "HotelId", "OperationalStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_AppRooms_TenantId_HotelId_RoomNumber",
                table: "AppRooms",
                columns: new[] { "TenantId", "HotelId", "RoomNumber" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_AppRoomTypes_TenantId_HotelId_Name",
                table: "AppRoomTypes",
                columns: new[] { "TenantId", "HotelId", "Name" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppBookings");

            migrationBuilder.DropTable(
                name: "AppComplaints");

            migrationBuilder.DropTable(
                name: "AppDailyRateOverrides");

            migrationBuilder.DropTable(
                name: "AppHotelRatePlans");

            migrationBuilder.DropTable(
                name: "AppInternalGuestNotes");

            migrationBuilder.DropTable(
                name: "AppRatePlans");

            migrationBuilder.DropTable(
                name: "AppReviews");

            migrationBuilder.DropTable(
                name: "AppRoomAssignments");

            migrationBuilder.DropTable(
                name: "AppRooms");

            migrationBuilder.DropTable(
                name: "AppRoomTypes");
        }
    }
}
