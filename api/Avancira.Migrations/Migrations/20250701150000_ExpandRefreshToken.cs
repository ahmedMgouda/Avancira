using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Avancira.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class ExpandRefreshToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_UserAgent",
                table: "RefreshTokens", schema: "identity");

            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_Latitude",
                table: "RefreshTokens", schema: "identity");

            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_Longitude",
                table: "RefreshTokens", schema: "identity");

            migrationBuilder.RenameColumn(
                name: "DeviceId",
                table: "RefreshTokens", schema: "identity",
                newName: "Device");

            migrationBuilder.RenameColumn(
                name: "Expiry",
                table: "RefreshTokens", schema: "identity",
                newName: "ExpiresAt");

            migrationBuilder.RenameIndex(
                name: "IX_RefreshTokens_UserId_DeviceId",
                table: "RefreshTokens", schema: "identity",
                newName: "IX_RefreshTokens_UserId_Device");

            migrationBuilder.DropColumn(
                name: "UserAgent",
                table: "RefreshTokens", schema: "identity");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "RefreshTokens", schema: "identity");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "RefreshTokens", schema: "identity");

            migrationBuilder.AlterColumn<string>(
                name: "IpAddress",
                table: "RefreshTokens", schema: "identity",
                type: "character varying(45)",
                maxLength: 45,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(45)",
                oldMaxLength: 45,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Browser",
                table: "RefreshTokens", schema: "identity",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OperatingSystem",
                table: "RefreshTokens", schema: "identity",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Revoked",
                table: "RefreshTokens", schema: "identity",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "RevokedAt",
                table: "RefreshTokens", schema: "identity",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_Device",
                table: "RefreshTokens", schema: "identity",
                column: "Device");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_Browser",
                table: "RefreshTokens", schema: "identity",
                column: "Browser");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_OperatingSystem",
                table: "RefreshTokens", schema: "identity",
                column: "OperatingSystem");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_ExpiresAt",
                table: "RefreshTokens", schema: "identity",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_Revoked",
                table: "RefreshTokens", schema: "identity",
                column: "Revoked");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_Device",
                table: "RefreshTokens", schema: "identity");

            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_Browser",
                table: "RefreshTokens", schema: "identity");

            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_OperatingSystem",
                table: "RefreshTokens", schema: "identity");

            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_ExpiresAt",
                table: "RefreshTokens", schema: "identity");

            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_Revoked",
                table: "RefreshTokens", schema: "identity");

            migrationBuilder.DropColumn(
                name: "Browser",
                table: "RefreshTokens", schema: "identity");

            migrationBuilder.DropColumn(
                name: "OperatingSystem",
                table: "RefreshTokens", schema: "identity");

            migrationBuilder.DropColumn(
                name: "Revoked",
                table: "RefreshTokens", schema: "identity");

            migrationBuilder.DropColumn(
                name: "RevokedAt",
                table: "RefreshTokens", schema: "identity");

            migrationBuilder.AlterColumn<string>(
                name: "IpAddress",
                table: "RefreshTokens", schema: "identity",
                type: "character varying(45)",
                maxLength: 45,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(45)",
                oldMaxLength: 45);

            migrationBuilder.RenameColumn(
                name: "Device",
                table: "RefreshTokens", schema: "identity",
                newName: "DeviceId");

            migrationBuilder.RenameColumn(
                name: "ExpiresAt",
                table: "RefreshTokens", schema: "identity",
                newName: "Expiry");

            migrationBuilder.RenameIndex(
                name: "IX_RefreshTokens_UserId_Device",
                table: "RefreshTokens", schema: "identity",
                newName: "IX_RefreshTokens_UserId_DeviceId");

            migrationBuilder.AddColumn<string>(
                name: "UserAgent",
                table: "RefreshTokens", schema: "identity",
                type: "character varying(512)",
                maxLength: 512,
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
        }
    }
}
