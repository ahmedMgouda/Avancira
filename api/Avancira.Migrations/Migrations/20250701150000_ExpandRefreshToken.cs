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
                table: "RefreshTokens");

            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_Latitude",
                table: "RefreshTokens");

            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_Longitude",
                table: "RefreshTokens");

            migrationBuilder.RenameColumn(
                name: "DeviceId",
                table: "RefreshTokens",
                newName: "Device");

            migrationBuilder.RenameColumn(
                name: "Expiry",
                table: "RefreshTokens",
                newName: "ExpiresAt");

            migrationBuilder.RenameIndex(
                name: "IX_RefreshTokens_UserId_DeviceId",
                table: "RefreshTokens",
                newName: "IX_RefreshTokens_UserId_Device");

            migrationBuilder.DropColumn(
                name: "UserAgent",
                table: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "RefreshTokens");

            migrationBuilder.AlterColumn<string>(
                name: "IpAddress",
                table: "RefreshTokens",
                type: "character varying(45)",
                maxLength: 45,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(45)",
                oldMaxLength: 45,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Browser",
                table: "RefreshTokens",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OperatingSystem",
                table: "RefreshTokens",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Revoked",
                table: "RefreshTokens",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "RevokedAt",
                table: "RefreshTokens",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_Device",
                table: "RefreshTokens",
                column: "Device");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_Browser",
                table: "RefreshTokens",
                column: "Browser");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_OperatingSystem",
                table: "RefreshTokens",
                column: "OperatingSystem");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_ExpiresAt",
                table: "RefreshTokens",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_Revoked",
                table: "RefreshTokens",
                column: "Revoked");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_Device",
                table: "RefreshTokens");

            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_Browser",
                table: "RefreshTokens");

            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_OperatingSystem",
                table: "RefreshTokens");

            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_ExpiresAt",
                table: "RefreshTokens");

            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_Revoked",
                table: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "Browser",
                table: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "OperatingSystem",
                table: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "Revoked",
                table: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "RevokedAt",
                table: "RefreshTokens");

            migrationBuilder.AlterColumn<string>(
                name: "IpAddress",
                table: "RefreshTokens",
                type: "character varying(45)",
                maxLength: 45,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(45)",
                oldMaxLength: 45);

            migrationBuilder.RenameColumn(
                name: "Device",
                table: "RefreshTokens",
                newName: "DeviceId");

            migrationBuilder.RenameColumn(
                name: "ExpiresAt",
                table: "RefreshTokens",
                newName: "Expiry");

            migrationBuilder.RenameIndex(
                name: "IX_RefreshTokens_UserId_Device",
                table: "RefreshTokens",
                newName: "IX_RefreshTokens_UserId_DeviceId");

            migrationBuilder.AddColumn<string>(
                name: "UserAgent",
                table: "RefreshTokens",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "RefreshTokens",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "RefreshTokens",
                type: "double precision",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserAgent",
                table: "RefreshTokens",
                column: "UserAgent");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_Latitude",
                table: "RefreshTokens",
                column: "Latitude");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_Longitude",
                table: "RefreshTokens",
                column: "Longitude");
        }
    }
}
