using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingHotel.Migrations
{
    /// <inheritdoc />
    public partial class UpdateUserHotelAssignmentAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AppUserHotelAssignments_TenantId_HotelId",
                table: "AppUserHotelAssignments");

            migrationBuilder.DropIndex(
                name: "IX_AppUserHotelAssignments_TenantId_UserId",
                table: "AppUserHotelAssignments");

            migrationBuilder.DropIndex(
                name: "IX_AppUserHotelAssignments_TenantId_UserId_HotelId",
                table: "AppUserHotelAssignments");

            migrationBuilder.AddColumn<Guid>(
                name: "DeleterId",
                table: "AppUserHotelAssignments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletionTime",
                table: "AppUserHotelAssignments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "AppUserHotelAssignments",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "AppUserHotelAssignments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastModificationTime",
                table: "AppUserHotelAssignments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LastModifierId",
                table: "AppUserHotelAssignments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UnassignedAt",
                table: "AppUserHotelAssignments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppUserHotelAssignments_TenantId_HotelId_IsActive",
                table: "AppUserHotelAssignments",
                columns: new[] { "TenantId", "HotelId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_AppUserHotelAssignments_TenantId_UserId_HotelId_IsActive",
                table: "AppUserHotelAssignments",
                columns: new[] { "TenantId", "UserId", "HotelId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_AppUserHotelAssignments_TenantId_UserId_IsActive",
                table: "AppUserHotelAssignments",
                columns: new[] { "TenantId", "UserId", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AppUserHotelAssignments_TenantId_HotelId_IsActive",
                table: "AppUserHotelAssignments");

            migrationBuilder.DropIndex(
                name: "IX_AppUserHotelAssignments_TenantId_UserId_HotelId_IsActive",
                table: "AppUserHotelAssignments");

            migrationBuilder.DropIndex(
                name: "IX_AppUserHotelAssignments_TenantId_UserId_IsActive",
                table: "AppUserHotelAssignments");

            migrationBuilder.DropColumn(
                name: "DeleterId",
                table: "AppUserHotelAssignments");

            migrationBuilder.DropColumn(
                name: "DeletionTime",
                table: "AppUserHotelAssignments");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "AppUserHotelAssignments");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "AppUserHotelAssignments");

            migrationBuilder.DropColumn(
                name: "LastModificationTime",
                table: "AppUserHotelAssignments");

            migrationBuilder.DropColumn(
                name: "LastModifierId",
                table: "AppUserHotelAssignments");

            migrationBuilder.DropColumn(
                name: "UnassignedAt",
                table: "AppUserHotelAssignments");

            migrationBuilder.CreateIndex(
                name: "IX_AppUserHotelAssignments_TenantId_HotelId",
                table: "AppUserHotelAssignments",
                columns: new[] { "TenantId", "HotelId" });

            migrationBuilder.CreateIndex(
                name: "IX_AppUserHotelAssignments_TenantId_UserId",
                table: "AppUserHotelAssignments",
                columns: new[] { "TenantId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_AppUserHotelAssignments_TenantId_UserId_HotelId",
                table: "AppUserHotelAssignments",
                columns: new[] { "TenantId", "UserId", "HotelId" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");
        }
    }
}
