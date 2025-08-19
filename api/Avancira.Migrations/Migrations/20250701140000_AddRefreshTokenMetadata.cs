using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Avancira.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddRefreshTokenMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "RefreshTokens", schema: "identity",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.AddColumn<string>(
                name: "IpAddress",
                table: "RefreshTokens", schema: "identity",
                type: "character varying(45)",
                maxLength: 45,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "RefreshTokens", schema: "identity",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "RefreshTokens", schema: "identity",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserAgent",
                table: "RefreshTokens", schema: "identity",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_IpAddress",
                table: "RefreshTokens", schema: "identity",
                column: "IpAddress");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserAgent",
                table: "RefreshTokens", schema: "identity",
                column: "UserAgent");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_Latitude",
                table: "RefreshTokens", schema: "identity",
                column: "Latitude");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_Longitude",
                table: "RefreshTokens", schema: "identity",
                column: "Longitude");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_CreatedAt",
                table: "RefreshTokens", schema: "identity",
                column: "CreatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_IpAddress",
                table: "RefreshTokens", schema: "identity");

            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_UserAgent",
                table: "RefreshTokens", schema: "identity");

            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_Latitude",
                table: "RefreshTokens", schema: "identity");

            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_Longitude",
                table: "RefreshTokens", schema: "identity");

            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_CreatedAt",
                table: "RefreshTokens", schema: "identity");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "RefreshTokens", schema: "identity");

            migrationBuilder.DropColumn(
                name: "IpAddress",
                table: "RefreshTokens", schema: "identity");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "RefreshTokens", schema: "identity");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "RefreshTokens", schema: "identity");

            migrationBuilder.DropColumn(
                name: "UserAgent",
                table: "RefreshTokens", schema: "identity");
        }
    }
}

