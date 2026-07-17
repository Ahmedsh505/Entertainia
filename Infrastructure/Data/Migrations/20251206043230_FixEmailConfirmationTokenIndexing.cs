using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixEmailConfirmationTokenIndexing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Check if index exists before trying to drop it
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.indexes 
                          WHERE name='IX_EmailConfirmationTokens_Token' 
                          AND object_id = OBJECT_ID('EmailConfirmationTokens'))
                BEGIN
                    DROP INDEX [IX_EmailConfirmationTokens_Token] ON [EmailConfirmationTokens];
                END
            ");

            // Check if UserId index exists before trying to drop it
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.indexes 
                          WHERE name='IX_EmailConfirmationTokens_UserId' 
                          AND object_id = OBJECT_ID('EmailConfirmationTokens'))
                BEGIN
                    DROP INDEX [IX_EmailConfirmationTokens_UserId] ON [EmailConfirmationTokens];
                END
            ");

            // Ensure Code column has correct configuration
            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "EmailConfirmationTokens",
                type: "nvarchar(6)",
                maxLength: 6,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: false);

            // Create Code index if it doesn't exist
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.indexes 
                              WHERE name='IX_EmailConfirmationTokens_Code' 
                              AND object_id = OBJECT_ID('EmailConfirmationTokens'))
                BEGIN
                    CREATE INDEX [IX_EmailConfirmationTokens_Code] 
                    ON [EmailConfirmationTokens] ([Code]);
                END
            ");

            // Create composite index if it doesn't exist
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.indexes 
                              WHERE name='IX_EmailConfirmationTokens_UserId_Code' 
                              AND object_id = OBJECT_ID('EmailConfirmationTokens'))
                BEGIN
                    CREATE INDEX [IX_EmailConfirmationTokens_UserId_Code] 
                    ON [EmailConfirmationTokens] ([UserId], [Code]);
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EmailConfirmationTokens_Code",
                table: "EmailConfirmationTokens");

            migrationBuilder.DropIndex(
                name: "IX_EmailConfirmationTokens_UserId_Code",
                table: "EmailConfirmationTokens");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "EmailConfirmationTokens",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(6)",
                oldMaxLength: 6);

            migrationBuilder.CreateIndex(
                name: "IX_EmailConfirmationTokens_Token",
                table: "EmailConfirmationTokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmailConfirmationTokens_UserId",
                table: "EmailConfirmationTokens",
                column: "UserId");
        }
    }
}
