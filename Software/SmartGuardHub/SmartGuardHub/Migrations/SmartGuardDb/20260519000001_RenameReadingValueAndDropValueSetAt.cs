using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartGuardHub.Migrations.SmartGuardDb
{
    /// <inheritdoc />
    public partial class RenameReadingValueAndDropValueSetAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ReadingValue",
                table: "SensorReadings",
                newName: "Reading");

            migrationBuilder.DropColumn(
                name: "ValueSetAt",
                table: "SensorReadings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Reading",
                table: "SensorReadings",
                newName: "ReadingValue");

            migrationBuilder.AddColumn<DateTime>(
                name: "ValueSetAt",
                table: "SensorReadings",
                type: "TEXT",
                nullable: true);
        }
    }
}
