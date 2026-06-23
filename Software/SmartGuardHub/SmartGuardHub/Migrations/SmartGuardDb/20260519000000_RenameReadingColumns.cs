using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartGuardHub.Migrations.SmartGuardDb
{
    /// <inheritdoc />
    public partial class RenameReadingColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Time",
                table: "SensorReadings",
                newName: "LogTime");

            migrationBuilder.RenameColumn(
                name: "LastSeen",
                table: "SensorReadings",
                newName: "ReadingTime");

            migrationBuilder.RenameIndex(
                name: "IX_SensorReadings_SensorId_Time",
                table: "SensorReadings",
                newName: "IX_SensorReadings_SensorId_LogTime");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LogTime",
                table: "SensorReadings",
                newName: "Time");

            migrationBuilder.RenameColumn(
                name: "ReadingTime",
                table: "SensorReadings",
                newName: "LastSeen");

            migrationBuilder.RenameIndex(
                name: "IX_SensorReadings_SensorId_LogTime",
                table: "SensorReadings",
                newName: "IX_SensorReadings_SensorId_Time");
        }
    }
}
