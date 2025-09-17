using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartGuardHub.Migrations
{
    /// <inheritdoc />
    public partial class AddedLatestDeviceValue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LatestValue",
                table: "Devices",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LatestValue",
                table: "Devices");
        }
    }
}
