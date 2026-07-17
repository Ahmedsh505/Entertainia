using Core.Enums;
using Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            // Identity will already include Id, Email, PasswordHash, etc.
            // Keep extra fields for profile
            builder.Property(u => u.FirstName).HasMaxLength(100);
            builder.Property(u => u.LastName).HasMaxLength(100);
            builder.Property(u => u.Address).HasMaxLength(200);
            builder.Property(u => u.Role).HasMaxLength(50).HasDefaultValue("Customer");

            // ✅ NEW: Profile Picture Path
            builder.Property(u => u.ProfilePicturePath)
                   .HasMaxLength(500)
                   .IsRequired(false);

            // Account status enum (int stored)
            builder.Property(u => u.AccountStatus)
                   .HasConversion<int>()
                   .IsRequired()
                   .HasDefaultValue(AccountStatus.PendingApproval);

            // Verification document path
            builder.Property(u => u.VerificationDocumentPath)
                   .HasMaxLength(300);

            // Soft Delete
            builder.Property(u => u.IsDeleted)
                   .HasDefaultValue(false)
                   .IsRequired();

            builder.Property(u => u.DeletedAt)
                   .IsRequired(false);

            // Deletion tracking
            builder.Property(u => u.DeletionType)
                   .HasConversion<int>()
                   .IsRequired()
                   .HasDefaultValue(DeletionType.None);

            builder.Property(u => u.DeletionReason)
                   .HasMaxLength(1000);

            builder.Property(u => u.OriginalUsername)
                   .HasMaxLength(256);

            // Relationship to user who deleted this user (admin)
            builder.HasOne(u => u.DeletedBy)
                   .WithMany()
                   .HasForeignKey(u => u.DeletedByUserId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Relationships
            builder.HasMany(u => u.OrganizedEvents)
                   .WithOne(e => e.Organizer)
                   .HasForeignKey(e => e.OrganizerId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(u => u.Bookings)
                   .WithOne(b => b.User)
                   .HasForeignKey(b => b.UserId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(u => u.VerificationDocs)
                   .WithOne(v => v.User)
                   .HasForeignKey(v => v.UserId)
                   .OnDelete(DeleteBehavior.Cascade);

            // Table name (optional)
            builder.ToTable("Users");
        }
    }
}