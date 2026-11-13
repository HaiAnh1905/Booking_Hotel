using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingHotel.Migrations
{
    /// <inheritdoc />
    public partial class AddIdentityUserBruteForceFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CurrentLockoutMinutes",
                table: "AbpUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "DeactivatedReason",
                table: "AbpUsers",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FailedAttemptsInCurrentCycle",
                table: "AbpUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "GoogleId",
                table: "AbpUsers",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TotalFailedAttempts",
                table: "AbpUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentLockoutMinutes",
                table: "AbpUsers");

            migrationBuilder.DropColumn(
                name: "DeactivatedReason",
                table: "AbpUsers");

            migrationBuilder.DropColumn(
                name: "FailedAttemptsInCurrentCycle",
                table: "AbpUsers");

            migrationBuilder.DropColumn(
                name: "GoogleId",
                table: "AbpUsers");

            migrationBuilder.DropColumn(
                name: "TotalFailedAttempts",
                table: "AbpUsers");
        }
    }
}
