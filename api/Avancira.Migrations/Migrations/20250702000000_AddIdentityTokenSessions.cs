using Microsoft.EntityFrameworkCore.Migrations;
using Avancira.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Avancira.Migrations.Migrations
{
    [DbContext(typeof(AvanciraDbContext))]
    [Migration("20250702000000_AddIdentityTokenSessions")]
    public class AddIdentityTokenSessions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
            name: "Sessions",
            schema: "identity",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<string>(type: "text", nullable: false),
                Device = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                UserAgent = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                OperatingSystem = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: false),
                Country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                CreatedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                AbsoluteExpiryUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                LastRefreshUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                LastActivityUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                RevokedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Sessions", x => x.Id);
            });

            migrationBuilder.CreateIndex(
            name: "IX_Sessions_Device",
            schema: "identity",
            table: "Sessions",
            column: "Device");

        migrationBuilder.CreateIndex(
            name: "IX_Sessions_UserAgent",
            schema: "identity",
            table: "Sessions",
            column: "UserAgent");

        migrationBuilder.CreateIndex(
            name: "IX_Sessions_OperatingSystem",
            schema: "identity",
            table: "Sessions",
            column: "OperatingSystem");

        migrationBuilder.CreateIndex(
            name: "IX_Sessions_IpAddress",
            schema: "identity",
            table: "Sessions",
            column: "IpAddress");

        migrationBuilder.CreateIndex(
            name: "IX_Sessions_CreatedUtc",
            schema: "identity",
            table: "Sessions",
            column: "CreatedUtc");

        migrationBuilder.CreateIndex(
            name: "IX_Sessions_AbsoluteExpiryUtc",
            schema: "identity",
            table: "Sessions",
            column: "AbsoluteExpiryUtc");

        migrationBuilder.CreateIndex(
            name: "IX_Sessions_LastRefreshUtc",
            schema: "identity",
            table: "Sessions",
            column: "LastRefreshUtc");

        migrationBuilder.CreateIndex(
            name: "IX_Sessions_LastActivityUtc",
            schema: "identity",
            table: "Sessions",
            column: "LastActivityUtc");

        migrationBuilder.CreateIndex(
            name: "IX_Sessions_RevokedUtc",
            schema: "identity",
            table: "Sessions",
            column: "RevokedUtc");

        migrationBuilder.CreateIndex(
            name: "IX_Sessions_UserId_Device",
            schema: "identity",
            table: "Sessions",
            columns: new[] { "UserId", "Device" },
            unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
            name: "Sessions",
            schema: "identity");
        }
    }
}

