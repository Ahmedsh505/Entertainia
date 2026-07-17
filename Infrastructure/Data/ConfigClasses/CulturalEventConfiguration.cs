using Core.Models.EventModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations
{
    public class CulturalEventConfiguration : IEntityTypeConfiguration<CulturalEvent>
    {
        public void Configure(EntityTypeBuilder<CulturalEvent> builder)
        {
            builder.HasKey(c => c.CulturalId);

            builder.HasOne(c => c.EventBase)
                   .WithOne()
                   .HasForeignKey<CulturalEvent>(c => c.CulturalId);
        }
    }
}
