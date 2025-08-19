using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Avancira.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddRevokedFilterToRefreshTokenIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_UserId_Device",
                table: "RefreshTokens", schema: "identity");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId_Device",
                table: "RefreshTokens", schema: "identity",
                columns: new[] { "UserId", "Device" },
                unique: true,
                filter: "\"Revoked\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_UserId_Device",
                table: "RefreshTokens", schema: "identity");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId_Device",
                table: "RefreshTokens", schema: "identity",
                columns: new[] { "UserId", "Device" },
                unique: true);
        }
    }
}

