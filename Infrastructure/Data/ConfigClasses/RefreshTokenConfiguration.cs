using Core.Models.AuthModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.ConfigClasses
{
    // ===== REFRESH TOKEN CONFIGURATION =====
    public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            builder.HasKey(rt => rt.Id);

            builder.Property(rt => rt.Token)
                .IsRequired()
                .HasMaxLength(200);

            builder.HasIndex(rt => rt.Token)
                .IsUnique();

            builder.Property(rt => rt.CreatedByIp)
                .HasMaxLength(50);

            builder.Property(rt => rt.RevokedByIp)
                .HasMaxLength(50);

            builder.Property(rt => rt.DeviceInfo)
                .HasMaxLength(500);

            builder.Property(rt => rt.ReasonRevoked)
                .HasMaxLength(200);

            builder.Property(rt => rt.ReplacedByToken)
                .HasMaxLength(200);

            // Relationship with User
            builder.HasOne(rt => rt.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    // ===== EMAIL CONFIRMATION TOKEN CONFIGURATION =====
    public class EmailConfirmationTokenConfiguration : IEntityTypeConfiguration<EmailConfirmationToken>
    {
        public void Configure(EntityTypeBuilder<EmailConfirmationToken> builder)
        {
            builder.HasKey(t => t.Id);

            // ✅ CRITICAL FIX: Token is OPTIONAL and NULLABLE
            builder.Property(t => t.Token)
                .IsRequired(false)       // ✅ Not required
                .HasMaxLength(200)
                .IsUnicode(true);        // ✅ Allow NULL values

            // ❌ REMOVED: No longer unique index on Token
            // The old unique index was dropped in migration

            // ✅ NEW: Code is REQUIRED (6-digit verification)
            builder.Property(t => t.Code)
                .IsRequired()
                .HasMaxLength(6)
                .IsFixedLength(false);

            // ✅ NEW: Index on Code for fast lookups
            builder.HasIndex(t => t.Code);

            // ✅ NEW: Composite index on UserId + Code
            builder.HasIndex(t => new { t.UserId, t.Code });

            builder.Property(t => t.Email)
                .IsRequired()
                .HasMaxLength(256);

            // ✅ NEW: Failed attempts tracking
            builder.Property(t => t.FailedAttempts)
                .IsRequired()
                .HasDefaultValue(0);

            // ✅ NEW: Lockout tracking
            builder.Property(t => t.IsLocked)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(t => t.LockedUntil)
                .IsRequired(false);

            // ✅ NEW: Resend tracking
            builder.Property(t => t.ResendCount)
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(t => t.LastResendAt)
                .IsRequired(false);

            builder.Property(t => t.CreatedByIp)
                .IsRequired(false)
                .HasMaxLength(50);

            builder.Property(t => t.UsedByIp)
                .IsRequired(false)
                .HasMaxLength(50);

            // Relationship with User
            builder.HasOne(t => t.User)
                .WithMany(u => u.EmailConfirmationTokens)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    // ===== PASSWORD RESET TOKEN CONFIGURATION =====
    public class PasswordResetTokenConfiguration : IEntityTypeConfiguration<PasswordResetToken>
    {
        public void Configure(EntityTypeBuilder<PasswordResetToken> builder)
        {
            builder.HasKey(t => t.Id);

            builder.Property(t => t.Token)
                .IsRequired()
                .HasMaxLength(200);

            builder.HasIndex(t => t.Token)
                .IsUnique();

            builder.Property(t => t.Email)
                .IsRequired()
                .HasMaxLength(256);

            builder.Property(t => t.CreatedByIp)
                .HasMaxLength(50);

            builder.Property(t => t.UsedByIp)
                .HasMaxLength(50);

            // Relationship with User
            builder.HasOne(t => t.User)
                .WithMany(u => u.PasswordResetTokens)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    // ===== TWO FACTOR CODE CONFIGURATION =====
    public class TwoFactorCodeConfiguration : IEntityTypeConfiguration<TwoFactorCode>
    {
        public void Configure(EntityTypeBuilder<TwoFactorCode> builder)
        {
            builder.HasKey(c => c.Id);

            builder.Property(c => c.Code)
                .IsRequired()
                .HasMaxLength(10);

            builder.Property(c => c.CreatedByIp)
                .HasMaxLength(50);

            builder.Property(c => c.UsedByIp)
                .HasMaxLength(50);

            // Relationship with User
            builder.HasOne(c => c.User)
                .WithMany(u => u.TwoFactorCodes)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

}