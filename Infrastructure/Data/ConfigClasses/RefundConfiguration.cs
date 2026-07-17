using Core.Models.PaymentModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.ConfigClasses
{
    public class RefundConfiguration : IEntityTypeConfiguration<Refund>
    {
        public void Configure(EntityTypeBuilder<Refund> builder)
        {
            builder.ToTable("Refunds");

            builder.HasKey(r => r.RefundId);

            builder.Property(r => r.OriginalAmount)
                .IsRequired()
                .HasColumnType("decimal(18,2)");

            builder.Property(r => r.RefundPercentage)
                .IsRequired()
                .HasColumnType("decimal(5,2)")
                .HasDefaultValue(80m);

            builder.Property(r => r.RefundAmount)
                .IsRequired()
                .HasColumnType("decimal(18,2)");

            builder.Property(r => r.Currency)
                .IsRequired();

            builder.Property(r => r.Status)
                .IsRequired();

            builder.Property(r => r.RefundTransactionId)
                .HasMaxLength(200);

            // ✅ FIXED: Changed from RefundGatewayReference to PaymentGatewayReference
            builder.Property(r => r.PaymentGatewayReference)
                .HasMaxLength(500);

            builder.Property(r => r.Reason)
                .HasMaxLength(1000);

            builder.Property(r => r.AdminNotes)
                .HasMaxLength(1000);

            builder.Property(r => r.FailureReason)
                .HasMaxLength(1000);

            builder.Property(r => r.RequestedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            // Relationships
            builder.HasOne(r => r.Payment)
                .WithMany()
                .HasForeignKey(r => r.PaymentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(r => r.Booking)
                .WithOne(b => b.Refund)
                .HasForeignKey<Refund>(r => r.BookingId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(r => r.RequestedBy)
                .WithMany()
                .HasForeignKey(r => r.RequestedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(r => r.ProcessedBy)
                .WithMany()
                .HasForeignKey(r => r.ProcessedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            builder.HasIndex(r => r.BookingId).IsUnique();
            builder.HasIndex(r => r.PaymentId);
            builder.HasIndex(r => r.Status);
        }
    }
}