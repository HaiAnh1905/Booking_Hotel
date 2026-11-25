using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingHotel.Migrations
{
    /// <inheritdoc />
    public partial class AddRoomManagementAndClosures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "RoomAmenities",
                table: "AppRooms",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1024)",
                oldMaxLength: 1024,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Capacity",
                table: "AppRooms",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 2);

            migrationBuilder.AlterColumn<string>(
                name: "BedType",
                table: "AppRooms",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "AppRooms",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "OccupancyStatus",
                table: "AppRooms",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "AppAffectedBookingResolutions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RoomClosureId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BookingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResolutionType = table.Column<int>(type: "int", nullable: true),
                    AlternativeRoomId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResolvedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResolvedTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsCustomerNotified = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_AppAffectedBookingResolutions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppRoomClosures",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    HotelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoomId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClosureType = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IncidentDescription = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EstimatedRecoveryTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 2),
                    RecoveryConfirmedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RecoveryConfirmedTime = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    table.PrimaryKey("PK_AppRoomClosures", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppRooms_TenantId_HotelId_Floor",
                table: "AppRooms",
                columns: new[] { "TenantId", "HotelId", "Floor" });

            migrationBuilder.CreateIndex(
                name: "IX_AppRooms_TenantId_HotelId_OccupancyStatus",
                table: "AppRooms",
                columns: new[] { "TenantId", "HotelId", "OccupancyStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_AppAffectedBookingResolutions_TenantId_BookingId",
                table: "AppAffectedBookingResolutions",
                columns: new[] { "TenantId", "BookingId" });

            migrationBuilder.CreateIndex(
                name: "IX_AppAffectedBookingResolutions_TenantId_RoomClosureId",
                table: "AppAffectedBookingResolutions",
                columns: new[] { "TenantId", "RoomClosureId" });

            migrationBuilder.CreateIndex(
                name: "IX_AppRoomClosures_TenantId_HotelId_Status",
                table: "AppRoomClosures",
                columns: new[] { "TenantId", "HotelId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_AppRoomClosures_TenantId_RoomId_Status_StartDate_EndDate",
                table: "AppRoomClosures",
                columns: new[] { "TenantId", "RoomId", "Status", "StartDate", "EndDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppAffectedBookingResolutions");

            migrationBuilder.DropTable(
                name: "AppRoomClosures");

            migrationBuilder.DropIndex(
                name: "IX_AppRooms_TenantId_HotelId_Floor",
                table: "AppRooms");

            migrationBuilder.DropIndex(
                name: "IX_AppRooms_TenantId_HotelId_OccupancyStatus",
                table: "AppRooms");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "AppRooms");

            migrationBuilder.DropColumn(
                name: "OccupancyStatus",
                table: "AppRooms");

            migrationBuilder.AlterColumn<string>(
                name: "RoomAmenities",
                table: "AppRooms",
                type: "nvarchar(1024)",
                maxLength: 1024,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Capacity",
                table: "AppRooms",
                type: "int",
                nullable: false,
                defaultValue: 2,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "BedType",
                table: "AppRooms",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }
    }
}
