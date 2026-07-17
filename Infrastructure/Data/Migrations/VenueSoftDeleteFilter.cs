using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Data.Migrations
{
    /// <summary>
    /// Migration to add query filter for soft-deleted venues
    /// Run: dotnet ef migrations add VenueSoftDeleteFilter --project Infrastructure --startup-project Entertania1
    /// Run: dotnet ef database update --project Infrastructure --startup-project Entertania1
    /// </summary>
    public partial class VenueSoftDeleteFilter : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Query filter is applied at the EF Core configuration level
            // No database schema changes needed

            // Ensure IsDeleted column exists with proper default
            migrationBuilder.AlterColumn<bool>(
                name: "IsDeleted",
                table: "Venues",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldNullable: false,
                oldDefaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No changes needed - query filter is in code only
        }
    }
}