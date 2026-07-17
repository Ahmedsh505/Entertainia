using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGovernorateToVenue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add Governorate column with default value (Cairo = 6)
            migrationBuilder.AddColumn<int>(
                name: "Governorate",
                table: "Venues",
                type: "int",
                nullable: false,
                defaultValue: 6); // Cairo as default

            // Create index for faster filtering
            migrationBuilder.CreateIndex(
                name: "IX_Venues_Governorate",
                table: "Venues",
                column: "Governorate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Venues_Governorate",
                table: "Venues");

            migrationBuilder.DropColumn(
                name: "Governorate",
                table: "Venues");
        }
    }
}