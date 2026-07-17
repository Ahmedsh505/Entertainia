using Core.Models.EventModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.ConfigClasses
{
    public class EventTicketTierConfiguration : IEntityTypeConfiguration<EventTicketTier>
    {
        public void Configure(EntityTypeBuilder<EventTicketTier> builder)
        {
            builder.ToTable("EventTicketTiers");

            builder.HasKey(t => t.EventTicketTierId);

            builder.Property(t => t.Tier)
                .IsRequired();

            builder.Property(t => t.PriceMultiplier)
                .IsRequired()
                .HasColumnType("decimal(18,2)");

            builder.Property(t => t.CapacityPercentage)
                .IsRequired()
                .HasColumnType("decimal(5,2)");

            builder.Property(t => t.AvailableSeats)
                .IsRequired();

            builder.Property(t => t.IsActive)
                .HasDefaultValue(true);

            // Relationships
            builder.HasOne(t => t.Event)
                .WithMany(e => e.TicketTiers)
                .HasForeignKey(t => t.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes
            builder.HasIndex(t => t.EventId);
            builder.HasIndex(t => new { t.EventId, t.Tier }).IsUnique();
        }
    }
}