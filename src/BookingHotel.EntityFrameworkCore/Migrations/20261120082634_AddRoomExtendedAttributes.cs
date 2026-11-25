using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingHotel.Migrations
{
    /// <inheritdoc />
    public partial class AddRoomExtendedAttributes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BedType",
                table: "AppRooms",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Building",
                table: "AppRooms",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Capacity",
                table: "AppRooms",
                type: "int",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "AppRooms",
                type: "nvarchar(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RoomAmenities",
                table: "AppRooms",
                type: "nvarchar(1024)",
                maxLength: 1024,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BedType",
                table: "AppRooms");

            migrationBuilder.DropColumn(
                name: "Building",
                table: "AppRooms");

            migrationBuilder.DropColumn(
                name: "Capacity",
                table: "AppRooms");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "AppRooms");

            migrationBuilder.DropColumn(
                name: "RoomAmenities",
                table: "AppRooms");
        }
    }
}
