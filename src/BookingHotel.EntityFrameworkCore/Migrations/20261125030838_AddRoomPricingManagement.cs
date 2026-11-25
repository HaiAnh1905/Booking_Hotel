using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingHotel.Migrations
{
    /// <inheritdoc />
    public partial class AddRoomPricingManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppPricingAuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    HotelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoomTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoomTypeName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    RatePlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RatePlanCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    RatePlanName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ActionType = table.Column<int>(type: "int", nullable: false),
                    ActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ActionTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BeforeValueJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    AfterValueJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    AffectedStartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AffectedEndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SummaryNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppPricingAuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppRoomPriceOverrides",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    HotelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoomTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RatePlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AdjustmentType = table.Column<int>(type: "int", nullable: false),
                    AdjustmentValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    CalculatedEffectivePrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    IsClosedToArrival = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsClosedToDeparture = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatorName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
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
                    table.PrimaryKey("PK_AppRoomPriceOverrides", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PricingAuditLogs_Hotel_Action_Time",
                table: "AppPricingAuditLogs",
                columns: new[] { "TenantId", "HotelId", "ActionType", "ActionTime" });

            migrationBuilder.CreateIndex(
                name: "IX_PricingAuditLogs_Hotel_Time",
                table: "AppPricingAuditLogs",
                columns: new[] { "TenantId", "HotelId", "ActionTime" });

            migrationBuilder.CreateIndex(
                name: "IX_PricingAuditLogs_Scope_Time",
                table: "AppPricingAuditLogs",
                columns: new[] { "TenantId", "HotelId", "RoomTypeId", "RatePlanId", "ActionTime" });

            migrationBuilder.CreateIndex(
                name: "IX_RoomPriceOverrides_Hotel_RoomType",
                table: "AppRoomPriceOverrides",
                columns: new[] { "TenantId", "HotelId", "RoomTypeId" },
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_RoomPriceOverrides_Scope_Dates",
                table: "AppRoomPriceOverrides",
                columns: new[] { "TenantId", "HotelId", "RoomTypeId", "RatePlanId", "StartDate", "EndDate" },
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppPricingAuditLogs");

            migrationBuilder.DropTable(
                name: "AppRoomPriceOverrides");
        }
    }
}
