using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Avancira.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_UserId_Device",
                table: "RefreshTokens");

            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_Device",
                table: "RefreshTokens");

            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_UserAgent",
                table: "RefreshTokens");

            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_OperatingSystem",
                table: "RefreshTokens");

            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_IpAddress",
                table: "RefreshTokens");

            migrationBuilder.CreateTable(
                name: "Sessions",
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
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sessions", x => x.Id);
                });

            migrationBuilder.AddColumn<Guid>(
                name: "SessionId",
                table: "RefreshTokens",
                type: "uuid",
                nullable: false,
                defaultValue: Guid.Empty);

            migrationBuilder.AddColumn<Guid>(
                name: "RotatedFromId",
                table: "RefreshTokens",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql(@"
                INSERT INTO \"Sessions\" (\"Id\", \"UserId\", \"Device\", \"UserAgent\", \"OperatingSystem\", \"IpAddress\", \"Country\", \"City\", \"CreatedAt\")
                SELECT \"Id\", \"UserId\", \"Device\", \"UserAgent\", \"OperatingSystem\", \"IpAddress\", \"Country\", \"City\", \"CreatedAt\"
                FROM \"RefreshTokens\";

                UPDATE \"RefreshTokens\" SET \"SessionId\" = \"Id\";
            ");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "Device",
                table: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "UserAgent",
                table: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "OperatingSystem",
                table: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "IpAddress",
                table: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "Country",
                table: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "City",
                table: "RefreshTokens");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_UserId_Device",
                table: "Sessions",
                columns: new[] { "UserId", "Device" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_Device",
                table: "Sessions",
                column: "Device");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_UserAgent",
                table: "Sessions",
                column: "UserAgent");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_OperatingSystem",
                table: "Sessions",
                column: "OperatingSystem");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_IpAddress",
                table: "Sessions",
                column: "IpAddress");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_CreatedAt",
                table: "Sessions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_SessionId",
                table: "RefreshTokens",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_RotatedFromId",
                table: "RefreshTokens",
                column: "RotatedFromId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_RefreshTokens_Sessions_SessionId",
                table: "RefreshTokens",
                column: "SessionId",
                principalTable: "Sessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RefreshTokens_RefreshTokens_RotatedFromId",
                table: "RefreshTokens",
                column: "RotatedFromId",
                principalTable: "RefreshTokens",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RefreshTokens_Sessions_SessionId",
                table: "RefreshTokens");

            migrationBuilder.DropForeignKey(
                name: "FK_RefreshTokens_RefreshTokens_RotatedFromId",
                table: "RefreshTokens");

            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_SessionId",
                table: "RefreshTokens");

            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_RotatedFromId",
                table: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "Sessions");

            migrationBuilder.DropColumn(
                name: "SessionId",
                table: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "RotatedFromId",
                table: "RefreshTokens");

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "RefreshTokens",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Device",
                table: "RefreshTokens",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UserAgent",
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

            migrationBuilder.AddColumn<string>(
                name: "IpAddress",
                table: "RefreshTokens",
                type: "character varying(45)",
                maxLength: 45,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "RefreshTokens",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "RefreshTokens",
                type: "character varying(100)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId_Device",
                table: "RefreshTokens",
                columns: new[] { "UserId", "Device" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_Device",
                table: "RefreshTokens",
                column: "Device");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserAgent",
                table: "RefreshTokens",
                column: "UserAgent");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_OperatingSystem",
                table: "RefreshTokens",
                column: "OperatingSystem");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_IpAddress",
                table: "RefreshTokens",
                column: "IpAddress");
        }
    }
}
