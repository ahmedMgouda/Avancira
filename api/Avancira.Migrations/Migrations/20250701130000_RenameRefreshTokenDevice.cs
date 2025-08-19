using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Avancira.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class RenameRefreshTokenDevice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Device",
                table: "RefreshTokens",
                schema: "identity",
                newName: "DeviceId");

            migrationBuilder.RenameIndex(
                name: "IX_RefreshTokens_UserId_Device",
                table: "RefreshTokens",
                schema: "identity",
                newName: "IX_RefreshTokens_UserId_DeviceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "IX_RefreshTokens_UserId_DeviceId",
                table: "RefreshTokens",
                schema: "identity",
                newName: "IX_RefreshTokens_UserId_Device");

            migrationBuilder.RenameColumn(
                name: "DeviceId",
                table: "RefreshTokens",
                schema: "identity",
                newName: "Device");
        }
    }
}
