using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingHotel.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantOperationsAndSuspensionSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AppHotels_TenantId_Code",
                table: "AppHotels");

            migrationBuilder.AlterColumn<int>(
                name: "TotalRooms",
                table: "AppHotels",
                type: "int",
                nullable: false,
                defaultValue: 50,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "AppHotels",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "ManagerName",
                table: "AppHotels",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "AppHotels",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "City",
                table: "AppHotels",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "Address",
                table: "AppHotels",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256);

            migrationBuilder.AddColumn<int>(
                name: "BookingCutoffDays",
                table: "AppHotels",
                type: "int",
                nullable: false,
                defaultValue: 14);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "StandardCheckInTime",
                table: "AppHotels",
                type: "time",
                nullable: false,
                defaultValue: new TimeSpan(0, 14, 0, 0, 0));

            migrationBuilder.AddColumn<TimeSpan>(
                name: "StandardCheckOutTime",
                table: "AppHotels",
                type: "time",
                nullable: false,
                defaultValue: new TimeSpan(0, 12, 0, 0, 0));

            migrationBuilder.CreateTable(
                name: "AppHotelSuspensionRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    HotelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SuspensionType = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    AffectedBookingsCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppHotelSuspensionRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppHotelSuspensionRecords_AppHotels_HotelId",
                        column: x => x.HotelId,
                        principalTable: "AppHotels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppTenantOrganizationProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OrganizationName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    LogoUrl = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    HeadquarterAddress = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    Hotline = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ContactEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    WebsiteUrl = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    DefaultCheckInTime = table.Column<TimeSpan>(type: "time", nullable: false, defaultValue: new TimeSpan(0, 14, 0, 0, 0)),
                    DefaultCheckOutTime = table.Column<TimeSpan>(type: "time", nullable: false, defaultValue: new TimeSpan(0, 12, 0, 0, 0)),
                    CancellationPolicyJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
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
                    table.PrimaryKey("PK_AppTenantOrganizationProfiles", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppHotels_TenantId_Code",
                table: "AppHotels",
                columns: new[] { "TenantId", "Code" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_AppHotelSuspensionRecords_HotelId",
                table: "AppHotelSuspensionRecords",
                column: "HotelId");

            migrationBuilder.CreateIndex(
                name: "IX_AppHotelSuspensionRecords_TenantId_HotelId",
                table: "AppHotelSuspensionRecords",
                columns: new[] { "TenantId", "HotelId" });

            migrationBuilder.CreateIndex(
                name: "IX_AppHotelSuspensionRecords_TenantId_SuspensionType",
                table: "AppHotelSuspensionRecords",
                columns: new[] { "TenantId", "SuspensionType" });

            migrationBuilder.CreateIndex(
                name: "IX_AppTenantOrganizationProfiles_TenantId",
                table: "AppTenantOrganizationProfiles",
                column: "TenantId",
                unique: true,
                filter: "[TenantId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppHotelSuspensionRecords");

            migrationBuilder.DropTable(
                name: "AppTenantOrganizationProfiles");

            migrationBuilder.DropIndex(
                name: "IX_AppHotels_TenantId_Code",
                table: "AppHotels");

            migrationBuilder.DropColumn(
                name: "BookingCutoffDays",
                table: "AppHotels");

            migrationBuilder.DropColumn(
                name: "StandardCheckInTime",
                table: "AppHotels");

            migrationBuilder.DropColumn(
                name: "StandardCheckOutTime",
                table: "AppHotels");

            migrationBuilder.AlterColumn<int>(
                name: "TotalRooms",
                table: "AppHotels",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "AppHotels",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256);

            migrationBuilder.AlterColumn<string>(
                name: "ManagerName",
                table: "AppHotels",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "AppHotels",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256);

            migrationBuilder.AlterColumn<string>(
                name: "City",
                table: "AppHotels",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "Address",
                table: "AppHotels",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(512)",
                oldMaxLength: 512);

            migrationBuilder.CreateIndex(
                name: "IX_AppHotels_TenantId_Code",
                table: "AppHotels",
                columns: new[] { "TenantId", "Code" });
        }
    }
}
