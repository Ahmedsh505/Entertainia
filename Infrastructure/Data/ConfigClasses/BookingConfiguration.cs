using Core.Enums;
using Core.Models.BookingModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations
{
    public class BookingConfiguration : IEntityTypeConfiguration<Booking>
    {
        public void Configure(EntityTypeBuilder<Booking> builder)
        {
            builder.HasKey(b => b.BookingId);

            builder.Property(b => b.Status)
                   .HasMaxLength(20)
                   .IsRequired()
                   .HasDefaultValue("Pending");

            builder.Property(b => b.NumberOfTickets)
                   .IsRequired();

            // ✅ Price snapshot configuration
            builder.Property(b => b.PricePerTicket)
                .IsRequired()
                .HasColumnType("decimal(18,2)");

            builder.Property(b => b.TotalPrice)
                .IsRequired()
                .HasColumnType("decimal(18,2)");

            builder.Property(b => b.Currency)
                .HasConversion<int>()
                .IsRequired();

            builder.HasOne(b => b.User)
                   .WithMany(u => u.Bookings)
                   .HasForeignKey(b => b.UserId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(b => b.Event)
                   .WithMany(e => e.Bookings)
                   .HasForeignKey(b => b.EventId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.ToTable("Bookings");
        }
    }
}