using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartGuardHub.Migrations.SmartGuardDb
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DeviceConfigs",
                columns: table => new
                {
                    Id            = table.Column<int>(type: "INTEGER", nullable: false)
                                        .Annotation("Sqlite:Autoincrement", true),
                    ConfigType    = table.Column<int>(type: "INTEGER", nullable: false),
                    UpdateTime    = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Config        = table.Column<string>(type: "TEXT", nullable: false),
                    UpdatedFrom   = table.Column<int>(type: "INTEGER", nullable: false),
                    SyncedToCloud = table.Column<bool>(type: "INTEGER", nullable: false),
                    TimeToSyncedToCloud = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SensorReadings",
                columns: table => new
                {
                    Id            = table.Column<int>(type: "INTEGER", nullable: false)
                                        .Annotation("Sqlite:Autoincrement", true),
                    UnitId        = table.Column<string>(type: "TEXT", nullable: false),
                    SensorId      = table.Column<string>(type: "TEXT", nullable: false),
                    LogTime       = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Reading       = table.Column<string>(type: "TEXT", nullable: true),
                    IsOnline      = table.Column<bool>(type: "INTEGER", nullable: false),
                    ReadingTime   = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SyncedToCloud = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SensorReadings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceConfigs_ConfigType",
                table: "DeviceConfigs",
                column: "ConfigType",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeviceConfigs_SyncedToCloud",
                table: "DeviceConfigs",
                column: "SyncedToCloud");

            migrationBuilder.CreateIndex(
                name: "IX_SensorReadings_SensorId",
                table: "SensorReadings",
                column: "SensorId");

            migrationBuilder.CreateIndex(
                name: "IX_SensorReadings_SensorId_Time",
                table: "SensorReadings",
                columns: new[] { "SensorId", "Time" });

            migrationBuilder.CreateIndex(
                name: "IX_SensorReadings_SyncedToCloud",
                table: "SensorReadings",
                column: "SyncedToCloud");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "DeviceConfigs");
            migrationBuilder.DropTable(name: "SensorReadings");
        }
    }
}
