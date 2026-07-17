using Core.Models.PaymentModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.ConfigClasses
{
    public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
    {
        public void Configure(EntityTypeBuilder<Payment> builder)
        {
            builder.ToTable("Payments");

            builder.HasKey(p => p.PaymentId);

            builder.Property(p => p.Amount)
                .IsRequired()
                .HasColumnType("decimal(18,2)");

            builder.Property(p => p.Currency)
                .IsRequired();

            builder.Property(p => p.Status)
                .IsRequired();

            builder.Property(p => p.PaymentGateway)
                .HasMaxLength(200);

            builder.Property(p => p.TransactionId)
                .HasMaxLength(200);

            builder.Property(p => p.PaymentGatewayReference)
                .HasMaxLength(500);

            builder.Property(p => p.PaymentMethod)
                .HasMaxLength(100);

            builder.Property(p => p.FailureReason)
                .HasMaxLength(1000);

            builder.Property(p => p.TaxAmount)
                .HasColumnType("decimal(18,2)");

            builder.Property(p => p.TotalAmountWithTax)
                .HasColumnType("decimal(18,2)");

            builder.Property(p => p.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            // Relationships
            builder.HasOne(p => p.Booking)
                .WithOne(b => b.Payment)
                .HasForeignKey<Payment>(p => p.BookingId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes
            builder.HasIndex(p => p.BookingId).IsUnique();
            builder.HasIndex(p => p.TransactionId);
            builder.HasIndex(p => p.Status);
        }
    }
}