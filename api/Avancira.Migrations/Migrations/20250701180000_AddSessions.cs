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
                table: "RefreshTokens", schema: "identity");

            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_Device",
                table: "RefreshTokens", schema: "identity");

            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_UserAgent",
                table: "RefreshTokens", schema: "identity");

            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_OperatingSystem",
                table: "RefreshTokens", schema: "identity");

            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_IpAddress",
                table: "RefreshTokens", schema: "identity");

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
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sessions", x => x.Id);
                });

            migrationBuilder.AddColumn<Guid>(
                name: "SessionId",
                table: "RefreshTokens", schema: "identity",
                type: "uuid",
                nullable: false,
                defaultValue: Guid.Empty);

            migrationBuilder.AddColumn<Guid>(
                name: "RotatedFromId",
                table: "RefreshTokens", schema: "identity",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql(@"
                INSERT INTO \"identity\".\"Sessions\" (\"Id\", \"UserId\", \"Device\", \"UserAgent\", \"OperatingSystem\", \"IpAddress\", \"Country\", \"City\", \"CreatedAt\")
                SELECT \"Id\", \"UserId\", \"Device\", \"UserAgent\", \"OperatingSystem\", \"IpAddress\", \"Country\", \"City\", \"CreatedAt\"
                FROM \"identity\".\"RefreshTokens\";

                UPDATE \"identity\".\"RefreshTokens\" SET \"SessionId\" = \"Id\";
            ");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "RefreshTokens", schema: "identity");

            migrationBuilder.DropColumn(
                name: "Device",
                table: "RefreshTokens", schema: "identity");

            migrationBuilder.DropColumn(
                name: "UserAgent",
                table: "RefreshTokens", schema: "identity");

            migrationBuilder.DropColumn(
                name: "OperatingSystem",
                table: "RefreshTokens", schema: "identity");

            migrationBuilder.DropColumn(
                name: "IpAddress",
                table: "RefreshTokens", schema: "identity");

            migrationBuilder.DropColumn(
                name: "Country",
                table: "RefreshTokens", schema: "identity");

            migrationBuilder.DropColumn(
                name: "City",
                table: "RefreshTokens", schema: "identity");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_UserId_Device",
                table: "Sessions", schema: "identity",
                columns: new[] { "UserId", "Device" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_Device",
                table: "Sessions", schema: "identity",
                column: "Device");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_UserAgent",
                table: "Sessions", schema: "identity",
                column: "UserAgent");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_OperatingSystem",
                table: "Sessions", schema: "identity",
                column: "OperatingSystem");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_IpAddress",
                table: "Sessions", schema: "identity",
                column: "IpAddress");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_CreatedAt",
                table: "Sessions", schema: "identity",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_SessionId",
                table: "RefreshTokens", schema: "identity",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_RotatedFromId",
                table: "RefreshTokens", schema: "identity",
                column: "RotatedFromId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_RefreshTokens_Sessions_SessionId",
                table: "RefreshTokens", schema: "identity",
                column: "SessionId",
                principalTable: "Sessions", principalSchema: "identity",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RefreshTokens_RefreshTokens_RotatedFromId",
                table: "RefreshTokens", schema: "identity",
                column: "RotatedFromId",
                principalTable: "RefreshTokens", principalSchema: "identity",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RefreshTokens_Sessions_SessionId",
                table: "RefreshTokens", schema: "identity");

            migrationBuilder.DropForeignKey(
                name: "FK_RefreshTokens_RefreshTokens_RotatedFromId",
                table: "RefreshTokens", schema: "identity");

            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_SessionId",
                table: "RefreshTokens", schema: "identity");

            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_RotatedFromId",
                table: "RefreshTokens", schema: "identity");

            migrationBuilder.DropTable(
                name: "Sessions",
                schema: "identity");

            migrationBuilder.DropColumn(
                name: "SessionId",
                table: "RefreshTokens", schema: "identity");

            migrationBuilder.DropColumn(
                name: "RotatedFromId",
                table: "RefreshTokens", schema: "identity");

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "RefreshTokens", schema: "identity",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Device",
                table: "RefreshTokens", schema: "identity",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UserAgent",
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

            migrationBuilder.AddColumn<string>(
                name: "IpAddress",
                table: "RefreshTokens", schema: "identity",
                type: "character varying(45)",
                maxLength: 45,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "RefreshTokens", schema: "identity",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "RefreshTokens", schema: "identity",
                type: "character varying(100)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId_Device",
                table: "RefreshTokens", schema: "identity",
                columns: new[] { "UserId", "Device" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_Device",
                table: "RefreshTokens", schema: "identity",
                column: "Device");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserAgent",
                table: "RefreshTokens", schema: "identity",
                column: "UserAgent");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_OperatingSystem",
                table: "RefreshTokens", schema: "identity",
                column: "OperatingSystem");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_IpAddress",
                table: "RefreshTokens", schema: "identity",
                column: "IpAddress");
        }
    }
}
