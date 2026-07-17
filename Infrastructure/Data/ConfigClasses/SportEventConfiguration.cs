using Core.Models.EventModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations
{
    public class SportEventConfiguration : IEntityTypeConfiguration<SportEvent>
    {
        public void Configure(EntityTypeBuilder<SportEvent> builder)
        {
            builder.HasKey(s => s.SportId);

            builder.HasOne(s => s.EventBase)
                   .WithOne()
                   .HasForeignKey<SportEvent>(s => s.SportId);
        }
    }
}
