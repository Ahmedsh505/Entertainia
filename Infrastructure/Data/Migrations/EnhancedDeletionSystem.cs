using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class EnhancedDeletionSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add new columns to Users table
            migrationBuilder.AddColumn<int>(
                name: "DeletionType",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DeletedByUserId",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletionReason",
                table: "Users",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OriginalUsername",
                table: "Users",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            // Add new columns to Venues table
            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Venues",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Venues",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedByUserId",
                table: "Venues",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OriginalName",
                table: "Venues",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            // Create BannedEmails table
            migrationBuilder.CreateTable(
                name: "BannedEmails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    BannedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    BannedByUserId = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    OriginalUserId = table.Column<int>(type: "int", nullable: true),
                    OriginalUsername = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BannedEmails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BannedEmails_Users_BannedByUserId",
                        column: x => x.BannedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            // Create indexes
            migrationBuilder.CreateIndex(
                name: "IX_BannedEmails_Email",
                table: "BannedEmails",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BannedEmails_BannedAt",
                table: "BannedEmails",
                column: "BannedAt");

            migrationBuilder.CreateIndex(
                name: "IX_BannedEmails_BannedByUserId",
                table: "BannedEmails",
                column: "BannedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_DeletedByUserId",
                table: "Users",
                column: "DeletedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_DeletionType",
                table: "Users",
                column: "DeletionType");

            migrationBuilder.CreateIndex(
                name: "IX_Venues_DeletedByUserId",
                table: "Venues",
                column: "DeletedByUserId");

            // Add foreign keys
            migrationBuilder.AddForeignKey(
                name: "FK_Users_Users_DeletedByUserId",
                table: "Users",
                column: "DeletedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Venues_Users_DeletedByUserId",
                table: "Venues",
                column: "DeletedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop foreign keys
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Users_DeletedByUserId",
                table: "Users");

            migrationBuilder.DropForeignKey(
                name: "FK_Venues_Users_DeletedByUserId",
                table: "Venues");

            // Drop indexes
            migrationBuilder.DropIndex(
                name: "IX_Users_DeletedByUserId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_DeletionType",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Venues_DeletedByUserId",
                table: "Venues");

            // Drop BannedEmails table
            migrationBuilder.DropTable(
                name: "BannedEmails");

            // Drop columns from Users
            migrationBuilder.DropColumn(
                name: "DeletionType",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DeletionReason",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "OriginalUsername",
                table: "Users");

            // Drop columns from Venues
            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Venues");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Venues");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "Venues");

            migrationBuilder.DropColumn(
                name: "OriginalName",
                table: "Venues");
        }
    }
}