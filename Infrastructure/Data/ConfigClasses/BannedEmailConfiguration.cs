using Core.Models.AuthModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations
{
    public class BannedEmailConfiguration : IEntityTypeConfiguration<BannedEmail>
    {
        public void Configure(EntityTypeBuilder<BannedEmail> builder)
        {
            builder.ToTable("BannedEmails");

            builder.HasKey(b => b.Id);

            builder.Property(b => b.Email)
                .IsRequired()
                .HasMaxLength(256);

            builder.Property(b => b.BannedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            builder.Property(b => b.Reason)
                .HasMaxLength(1000);

            builder.Property(b => b.OriginalUsername)
                .HasMaxLength(256);

            // Relationship to Admin who banned
            builder.HasOne(b => b.BannedBy)
                .WithMany()
                .HasForeignKey(b => b.BannedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Unique index on email (case-insensitive)
            builder.HasIndex(b => b.Email)
                .IsUnique()
                .HasDatabaseName("IX_BannedEmails_Email");

            // Index for quick lookups
            builder.HasIndex(b => b.BannedAt);
        }
    }
}