using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailConfirmationCodeSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ✅ STEP 1: Drop the UNIQUE INDEX on Token (prevents ALTER COLUMN)
            migrationBuilder.DropIndex(
                name: "IX_EmailConfirmationTokens_Token",
                table: "EmailConfirmationTokens");

            // ✅ STEP 2: Make Token column NULLABLE
            migrationBuilder.AlterColumn<string>(
                name: "Token",
                table: "EmailConfirmationTokens",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,  // ✅ Now nullable (backward compatibility)
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: false);

            // ✅ STEP 3: Add Code column (6-digit verification)
            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "EmailConfirmationTokens",
                type: "nvarchar(6)",
                maxLength: 6,
                nullable: false,
                defaultValue: "000000");

            // ✅ STEP 4: Add FailedAttempts tracking
            migrationBuilder.AddColumn<int>(
                name: "FailedAttempts",
                table: "EmailConfirmationTokens",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // ✅ STEP 5: Add IsLocked flag
            migrationBuilder.AddColumn<bool>(
                name: "IsLocked",
                table: "EmailConfirmationTokens",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // ✅ STEP 6: Add LockedUntil timestamp
            migrationBuilder.AddColumn<DateTime>(
                name: "LockedUntil",
                table: "EmailConfirmationTokens",
                type: "datetime2",
                nullable: true);

            // ✅ STEP 7: Add ResendCount tracking
            migrationBuilder.AddColumn<int>(
                name: "ResendCount",
                table: "EmailConfirmationTokens",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // ✅ STEP 8: Add LastResendAt timestamp
            migrationBuilder.AddColumn<DateTime>(
                name: "LastResendAt",
                table: "EmailConfirmationTokens",
                type: "datetime2",
                nullable: true);

            // ✅ STEP 9: Create index on Code (fast lookups)
            migrationBuilder.CreateIndex(
                name: "IX_EmailConfirmationTokens_Code",
                table: "EmailConfirmationTokens",
                column: "Code");

            // ✅ STEP 10: Create composite index (UserId + Code)
            migrationBuilder.CreateIndex(
                name: "IX_EmailConfirmationTokens_UserId_Code",
                table: "EmailConfirmationTokens",
                columns: new[] { "UserId", "Code" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop new indexes
            migrationBuilder.DropIndex(
                name: "IX_EmailConfirmationTokens_Code",
                table: "EmailConfirmationTokens");

            migrationBuilder.DropIndex(
                name: "IX_EmailConfirmationTokens_UserId_Code",
                table: "EmailConfirmationTokens");

            // Remove new columns
            migrationBuilder.DropColumn(
                name: "Code",
                table: "EmailConfirmationTokens");

            migrationBuilder.DropColumn(
                name: "FailedAttempts",
                table: "EmailConfirmationTokens");

            migrationBuilder.DropColumn(
                name: "IsLocked",
                table: "EmailConfirmationTokens");

            migrationBuilder.DropColumn(
                name: "LockedUntil",
                table: "EmailConfirmationTokens");

            migrationBuilder.DropColumn(
                name: "ResendCount",
                table: "EmailConfirmationTokens");

            migrationBuilder.DropColumn(
                name: "LastResendAt",
                table: "EmailConfirmationTokens");

            // Revert Token to NOT NULL
            migrationBuilder.AlterColumn<string>(
                name: "Token",
                table: "EmailConfirmationTokens",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            // Recreate the unique index on Token
            migrationBuilder.CreateIndex(
                name: "IX_EmailConfirmationTokens_Token",
                table: "EmailConfirmationTokens",
                column: "Token",
                unique: true);
        }
    }
}
