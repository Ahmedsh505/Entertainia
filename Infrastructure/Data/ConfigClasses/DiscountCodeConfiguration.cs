using Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.ConfigClasses
{
    public class DiscountCodeConfiguration : IEntityTypeConfiguration<DiscountCode>
    {
        public void Configure(EntityTypeBuilder<DiscountCode> builder)
        {
            builder.ToTable("DiscountCodes");

            builder.HasKey(d => d.DiscountCodeId);

            builder.Property(d => d.Code)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(d => d.DiscountType)
                .IsRequired();

            builder.Property(d => d.DiscountValue)
                .IsRequired()
                .HasColumnType("decimal(18,2)");

            builder.Property(d => d.CurrentUsageCount)
                .HasDefaultValue(0);

            builder.Property(d => d.IsActive)
                .HasDefaultValue(true);

            builder.Property(d => d.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            // Relationships
            builder.HasOne(d => d.Event)
                .WithMany(e => e.DiscountCodes)
                .HasForeignKey(d => d.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(d => d.CreatedBy)
                .WithMany()
                .HasForeignKey(d => d.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            builder.HasIndex(d => d.Code);
            builder.HasIndex(d => d.EventId);
            builder.HasIndex(d => new { d.Code, d.EventId }).IsUnique();
        }
    }
}