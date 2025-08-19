using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Avancira.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AbsoluteExpiryUtc",
                table: "Sessions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.AddColumn<DateTime>(
                name: "LastRefreshUtc",
                table: "Sessions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.AddColumn<DateTime>(
                name: "LastActivityUtc",
                table: "Sessions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.Sql(@"
                UPDATE \"Sessions\" 
                SET \"AbsoluteExpiryUtc\" = \"CreatedAt\" + interval '7 days',
                    \"LastRefreshUtc\" = \"CreatedAt\",
                    \"LastActivityUtc\" = \"CreatedAt\";
            ");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_AbsoluteExpiryUtc",
                table: "Sessions",
                column: "AbsoluteExpiryUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_LastRefreshUtc",
                table: "Sessions",
                column: "LastRefreshUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_LastActivityUtc",
                table: "Sessions",
                column: "LastActivityUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Sessions_AbsoluteExpiryUtc",
                table: "Sessions");

            migrationBuilder.DropIndex(
                name: "IX_Sessions_LastRefreshUtc",
                table: "Sessions");

            migrationBuilder.DropIndex(
                name: "IX_Sessions_LastActivityUtc",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "AbsoluteExpiryUtc",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "LastRefreshUtc",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "LastActivityUtc",
                table: "Sessions");
        }
    }
}
