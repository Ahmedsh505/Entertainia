using Core.Enums;
using Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations
{
    public class VenueConfiguration : IEntityTypeConfiguration<Venue>
    {
        public void Configure(EntityTypeBuilder<Venue> builder)
        {
            builder.HasKey(v => v.VenueId);

            builder.Property(v => v.Name)
                   .IsRequired()
                   .HasMaxLength(150);

            builder.Property(v => v.Location)
                   .IsRequired()
                   .HasMaxLength(250);

            builder.Property(v => v.Capacity)
                   .IsRequired();

            // Enum conversion for ApprovalStatus
            builder.Property(v => v.ApprovalStatus)
                   .HasConversion<int>()
                   .IsRequired()
                   .HasDefaultValue(ApprovalStatus.Pending);

            // Approval note
            builder.Property(v => v.ApprovalNote)
                   .HasMaxLength(500);

            // ✅ Soft Delete Properties
            builder.Property(v => v.IsDeleted)
                   .HasDefaultValue(false)
                   .IsRequired();

            builder.Property(v => v.DeletedAt)
                   .IsRequired(false);

            builder.Property(v => v.OriginalName)
                   .HasMaxLength(150);

            // ✅ CRITICAL: Query filter to exclude deleted venues by default
            // Use IgnoreQueryFilters() in repositories when you need deleted venues
            builder.HasQueryFilter(v => !v.IsDeleted);

            // CreatedByUser relationship (Organizer)
            builder.HasOne(v => v.CreatedByUser)
                   .WithMany()
                   .HasForeignKey(v => v.CreatedByUserId)
                   .OnDelete(DeleteBehavior.Restrict);

            // ApprovedByUser relationship (Admin)
            builder.HasOne(v => v.ApprovedBy)
                   .WithMany()
                   .HasForeignKey(v => v.ApprovedByUserId)
                   .OnDelete(DeleteBehavior.Restrict);

            // ✅ DeletedBy relationship
            builder.HasOne(v => v.DeletedBy)
                   .WithMany()
                   .HasForeignKey(v => v.DeletedByUserId)
                   .OnDelete(DeleteBehavior.Restrict);

            // ✅ CRITICAL: Events relationship - Use Restrict to preserve events when venue is soft-deleted
            builder.HasMany(v => v.Events)
                   .WithOne(e => e.Venue)
                   .HasForeignKey(e => e.VenueId)
                   .OnDelete(DeleteBehavior.Restrict); // ✅ Prevents cascade delete

            builder.ToTable("Venues");
        }
    }
}