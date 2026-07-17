using Core.Models.TicketModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.ConfigClasses
{
    public class TicketConfiguration : IEntityTypeConfiguration<Ticket>
    {
        public void Configure(EntityTypeBuilder<Ticket> builder)
        {
            builder.ToTable("Tickets");

            builder.HasKey(t => t.TicketId);

            // Relationships
            builder.HasOne(t => t.Booking)
                .WithMany()
                .HasForeignKey(t => t.BookingId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(t => t.Event)
                .WithMany()
                .HasForeignKey(t => t.EventId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes for performance
            builder.HasIndex(t => t.BookingId);
            builder.HasIndex(t => t.EventId);
            builder.HasIndex(t => t.UserId);
            builder.HasIndex(t => t.VerificationHash).IsUnique();
            builder.HasIndex(t => t.Status);

            // Composite index for ticket lookup
            builder.HasIndex(t => new { t.BookingId, t.TicketNumber });

            // Properties
            builder.Property(t => t.QRCodeData).HasMaxLength(500);
            builder.Property(t => t.VerificationHash).HasMaxLength(100);
            builder.Property(t => t.Status).HasMaxLength(50).HasDefaultValue("Active");
            builder.Property(t => t.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            builder.Property(t => t.Notes).HasMaxLength(1000);
        }
    }
}