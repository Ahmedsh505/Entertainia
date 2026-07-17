using Core.Models.PaymentModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.ConfigClasses
{
    public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
    {
        public void Configure(EntityTypeBuilder<Transaction> builder)
        {
            builder.HasKey(t => t.TransactionId);

            builder.Property(t => t.TransactionType)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(t => t.Amount)
                .IsRequired()
                .HasColumnType("decimal(18,2)");

            builder.Property(t => t.Currency)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(10);

            builder.Property(t => t.Status)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(50);

            builder.Property(t => t.PaymentMethod)
                .HasMaxLength(200);

            builder.Property(t => t.PaymentGateway)
                .HasMaxLength(200);

            builder.Property(t => t.TransactionReference)
                .HasMaxLength(200);

            builder.Property(t => t.GatewayTransactionId)
                .HasMaxLength(200);

            builder.Property(t => t.GatewayReference)
                .HasMaxLength(500);

            builder.Property(t => t.Description)
                .HasMaxLength(1000);

            builder.Property(t => t.ErrorMessage)
                .HasMaxLength(1000);

            builder.Property(t => t.IpAddress)
                .HasMaxLength(100);

            builder.Property(t => t.UserAgent)
                .HasMaxLength(500);

            builder.Property(t => t.CreatedAt)
                .IsRequired();

            // Relationships
            builder.HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(t => t.Booking)
                .WithMany()
                .HasForeignKey(t => t.BookingId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(t => t.Payment)
                .WithMany()
                .HasForeignKey(t => t.PaymentId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(t => t.Refund)
                .WithMany()
                .HasForeignKey(t => t.RefundId)
                .OnDelete(DeleteBehavior.NoAction);

            // Indexes for performance
            builder.HasIndex(t => t.UserId);
            builder.HasIndex(t => t.BookingId);
            builder.HasIndex(t => t.PaymentId);
            builder.HasIndex(t => t.RefundId);
            builder.HasIndex(t => t.TransactionReference);
            builder.HasIndex(t => t.GatewayTransactionId);
            builder.HasIndex(t => t.Status);
            builder.HasIndex(t => t.CreatedAt);
        }
    }
}
