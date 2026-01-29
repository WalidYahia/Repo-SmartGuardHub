using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartGuardHub.Migrations
{
    /// <inheritdoc />
    public partial class Add_LastTimeValueSet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastTimeValueSet",
                table: "Sensors",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastTimeValueSet",
                table: "Sensors");
        }
    }
}
