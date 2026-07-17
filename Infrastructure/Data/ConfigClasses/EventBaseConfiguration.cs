using Core.Enums;
using Core.Models.EventModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations
{
    public class EventBaseConfiguration : IEntityTypeConfiguration<EventBase>
    {
        public void Configure(EntityTypeBuilder<EventBase> builder)
        {
            builder.HasKey(e => e.EventId);

            builder.Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(e => e.Category)
                .IsRequired()
                .HasMaxLength(50);

            // ✅ Price configuration
            builder.Property(e => e.Price)
                .IsRequired()
                .HasColumnType("decimal(18,2)");

            // ✅ Currency enum conversion
            builder.Property(e => e.Currency)
                .HasConversion<int>()
                .IsRequired()
                .HasDefaultValue(Currency.EGP);

            // Enum conversion for ApprovalStatus
            builder.Property(e => e.ApprovalStatus)
                   .HasConversion<int>()
                   .IsRequired()
                   .HasDefaultValue(ApprovalStatus.Pending);

            // Optional note
            builder.Property(e => e.ApprovalNote)
                   .HasMaxLength(500);

            // Organizer (User)
            builder.HasOne(e => e.Organizer)
                   .WithMany(u => u.OrganizedEvents)
                   .HasForeignKey(e => e.OrganizerId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Approver (User)
            builder.HasOne(e => e.ApprovedBy)
                   .WithMany()
                   .HasForeignKey(e => e.ApprovedByUserId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Venue relationship
            builder.HasOne(e => e.Venue)
                   .WithMany(v => v.Events)
                   .HasForeignKey(e => e.VenueId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Bookings
            builder.HasMany(e => e.Bookings)
                   .WithOne(b => b.Event)
                   .HasForeignKey(b => b.EventId)
                   .OnDelete(DeleteBehavior.Cascade);

            // Table name (optional)
            builder.ToTable("Events");
        }
    }
}