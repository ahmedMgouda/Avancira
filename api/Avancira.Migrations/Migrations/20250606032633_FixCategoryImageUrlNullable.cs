using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Avancira.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class FixCategoryImageUrlNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // First, clean up any truly invalid URI strings by setting them to NULL
            // Keep relative paths (starting with /) and valid absolute URLs
            migrationBuilder.Sql(@"
                UPDATE ""Categories"" 
                SET ""ImageUrl"" = NULL 
                WHERE ""ImageUrl"" IS NOT NULL 
                AND ""ImageUrl"" != ''
                AND ""ImageUrl"" NOT LIKE 'http://%' 
                AND ""ImageUrl"" NOT LIKE 'https://%'
                AND ""ImageUrl"" NOT LIKE 'ftp://%'
                AND ""ImageUrl"" NOT LIKE 'file://%'
                AND ""ImageUrl"" NOT LIKE '/%'
                AND ""ImageUrl"" NOT LIKE './%'
                AND ""ImageUrl"" NOT LIKE '../%'
            ");

            migrationBuilder.AlterColumn<string>(
                name: "ImageUrl",
                table: "Categories",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ImageUrl",
                table: "Categories",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
