using Microsoft.EntityFrameworkCore.Migrations;
using Avancira.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Avancira.Migrations.Migrations
{
    [DbContext(typeof(AvanciraDbContext))]
    [Migration("20250901000001_AddUniqueSessionIdIndex")]
    public class AddUniqueSessionIdIndex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_SessionId",
                schema: "identity",
                table: "Sessions",
                column: "Id",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserSessions_SessionId",
                schema: "identity",
                table: "Sessions");
        }
    }
}
