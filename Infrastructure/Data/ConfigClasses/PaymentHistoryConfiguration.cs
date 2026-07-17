using Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Data.ConfigClasses
{
    public class PaymentHistoryConfiguration : IEntityTypeConfiguration<PaymentHistory>
    {
        public void Configure(EntityTypeBuilder<PaymentHistory> builder)
        {
            builder.HasKey(p => p.Id);

            builder.Property(p => p.Amount)
                   .HasColumnType("decimal(18,2)")
                   .IsRequired();

            builder.Property(p => p.Status)
                   .IsRequired();

            builder.Property(p => p.PaymentMethod)
                   .HasMaxLength(50);

            builder.Property(p => p.TransactionId)
                   .HasMaxLength(100);

            builder.Property(p => p.GatewayResponse)
                   .HasColumnType("nvarchar(max)");

            builder.Property(p => p.CreatedAt)
                   .HasDefaultValueSql("GETUTCDATE()");

            // العلاقات
            builder.HasOne(p => p.User)
                   .WithMany() // أو .WithMany(u => u.PaymentHistories) لو ضايف Navigation في User
                   .HasForeignKey(p => p.UserId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(p => p.Booking)
                   .WithMany() // أو .WithMany(b => b.PaymentHistories)
                   .HasForeignKey(p => p.BookingId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.ToTable("PaymentHistories");
        }
    }
}
