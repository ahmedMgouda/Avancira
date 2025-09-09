using Microsoft.EntityFrameworkCore.Migrations;
using Avancira.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Avancira.Migrations.Migrations
{
    [DbContext(typeof(AvanciraDbContext))]
    [Migration("20250903000000_RemoveDeviceFromSessions")]
    public class RemoveDeviceFromSessions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Sessions_Device",
                schema: "identity",
                table: "Sessions");

            migrationBuilder.DropIndex(
                name: "IX_Sessions_UserId_Device",
                schema: "identity",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "Device",
                schema: "identity",
                table: "Sessions");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_UserId",
                schema: "identity",
                table: "Sessions",
                column: "UserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Sessions_UserId",
                schema: "identity",
                table: "Sessions");

            migrationBuilder.AddColumn<string>(
                name: "Device",
                schema: "identity",
                table: "Sessions",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_Device",
                schema: "identity",
                table: "Sessions",
                column: "Device");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_UserId_Device",
                schema: "identity",
                table: "Sessions",
                columns: new[] { "UserId", "Device" },
                unique: true);
        }
    }
}
