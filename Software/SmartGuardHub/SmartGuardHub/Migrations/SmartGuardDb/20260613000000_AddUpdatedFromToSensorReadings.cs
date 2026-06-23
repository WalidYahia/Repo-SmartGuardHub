using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartGuardHub.Migrations.SmartGuardDb
{
    /// <inheritdoc />
    public partial class AddUpdatedFromToSensorReadings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UpdatedFrom",
                table: "SensorReadings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UpdatedFrom",
                table: "SensorReadings");
        }
    }
}
