using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Avancira.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Avancira.Migrations.Migrations
{
    [DbContext(typeof(AvanciraDbContext))]
    [Migration("20250703000000_RemoveRefreshTokenAbsoluteExpiryUtc")]
    public class RemoveRefreshTokenAbsoluteExpiryUtc : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_AbsoluteExpiryUtc",
                schema: "identity",
                table: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "AbsoluteExpiryUtc",
                schema: "identity",
                table: "RefreshTokens");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AbsoluteExpiryUtc",
                schema: "identity",
                table: "RefreshTokens",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_AbsoluteExpiryUtc",
                schema: "identity",
                table: "RefreshTokens",
                column: "AbsoluteExpiryUtc");
        }
    }
}

