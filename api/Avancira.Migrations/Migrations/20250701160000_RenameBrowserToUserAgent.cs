using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Avancira.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class RenameBrowserToUserAgent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Browser",
                table: "RefreshTokens", schema: "identity",
                newName: "UserAgent");

            migrationBuilder.RenameIndex(
                name: "IX_RefreshTokens_Browser",
                table: "RefreshTokens", schema: "identity",
                newName: "IX_RefreshTokens_UserAgent");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "IX_RefreshTokens_UserAgent",
                table: "RefreshTokens", schema: "identity",
                newName: "IX_RefreshTokens_Browser");

            migrationBuilder.RenameColumn(
                name: "UserAgent",
                table: "RefreshTokens", schema: "identity",
                newName: "Browser");
        }
    }
}
