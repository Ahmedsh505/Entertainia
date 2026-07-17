using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixTicketVerificationHashIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop unique index
            migrationBuilder.DropIndex(
                name: "IX_Tickets_VerificationHash",
                table: "Tickets");

            // Recreate as non-unique index
            migrationBuilder.CreateIndex(
                name: "IX_Tickets_VerificationHash",
                table: "Tickets",
                column: "VerificationHash");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tickets_VerificationHash",
                table: "Tickets");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_VerificationHash",
                table: "Tickets",
                column: "VerificationHash",
                unique: true);
        }
    }
}
