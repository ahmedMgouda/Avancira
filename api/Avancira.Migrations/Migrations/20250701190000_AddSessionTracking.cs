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
                table: "Sessions", schema: "identity",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.AddColumn<DateTime>(
                name: "LastRefreshUtc",
                table: "Sessions", schema: "identity",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.AddColumn<DateTime>(
                name: "LastActivityUtc",
                table: "Sessions", schema: "identity",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.Sql(@"
                UPDATE \"identity\".\"Sessions\"
                SET \"AbsoluteExpiryUtc\" = \"CreatedAt\" + interval '7 days',
                    \"LastRefreshUtc\" = \"CreatedAt\",
                    \"LastActivityUtc\" = \"CreatedAt\";
            ");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_AbsoluteExpiryUtc",
                table: "Sessions", schema: "identity",
                column: "AbsoluteExpiryUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_LastRefreshUtc",
                table: "Sessions", schema: "identity",
                column: "LastRefreshUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_LastActivityUtc",
                table: "Sessions", schema: "identity",
                column: "LastActivityUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Sessions_AbsoluteExpiryUtc",
                table: "Sessions", schema: "identity");

            migrationBuilder.DropIndex(
                name: "IX_Sessions_LastRefreshUtc",
                table: "Sessions", schema: "identity");

            migrationBuilder.DropIndex(
                name: "IX_Sessions_LastActivityUtc",
                table: "Sessions", schema: "identity");

            migrationBuilder.DropColumn(
                name: "AbsoluteExpiryUtc",
                table: "Sessions", schema: "identity");

            migrationBuilder.DropColumn(
                name: "LastRefreshUtc",
                table: "Sessions", schema: "identity");

            migrationBuilder.DropColumn(
                name: "LastActivityUtc",
                table: "Sessions", schema: "identity");
        }
    }
}
