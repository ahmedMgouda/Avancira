using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Avancira.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Avancira.Migrations.Migrations
{
    [DbContext(typeof(AvanciraDbContext))]
    [Migration("20250705000000_AddRefreshTokenSalt")]
    public class AddRefreshTokenSalt : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Salt",
                schema: "identity",
                table: "RefreshTokens",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Salt",
                schema: "identity",
                table: "RefreshTokens");
        }
    }
}
