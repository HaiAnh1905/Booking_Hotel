using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingHotel.Migrations
{
    /// <inheritdoc />
    public partial class AddFrontdeskStayTrackingToBookings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ActualCheckInTime",
                table: "AppBookings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ActualCheckOutTime",
                table: "AppBookings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancellationReason",
                table: "AppBookings",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FrontdeskNotes",
                table: "AppBookings",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsIdentityVerified",
                table: "AppBookings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "RoomTypeId",
                table: "AppBookings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppBookings_Tenant_Hotel_RoomType",
                table: "AppBookings",
                columns: new[] { "TenantId", "HotelId", "RoomTypeId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AppBookings_Tenant_Hotel_RoomType",
                table: "AppBookings");

            migrationBuilder.DropColumn(
                name: "ActualCheckInTime",
                table: "AppBookings");

            migrationBuilder.DropColumn(
                name: "ActualCheckOutTime",
                table: "AppBookings");

            migrationBuilder.DropColumn(
                name: "CancellationReason",
                table: "AppBookings");

            migrationBuilder.DropColumn(
                name: "FrontdeskNotes",
                table: "AppBookings");

            migrationBuilder.DropColumn(
                name: "IsIdentityVerified",
                table: "AppBookings");

            migrationBuilder.DropColumn(
                name: "RoomTypeId",
                table: "AppBookings");
        }
    }
}
