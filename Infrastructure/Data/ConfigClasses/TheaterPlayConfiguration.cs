using Core.Models.EventModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations
{
    public class TheaterPlayConfiguration : IEntityTypeConfiguration<TheaterPlay>
    {
        public void Configure(EntityTypeBuilder<TheaterPlay> builder)
        {
            builder.HasKey(t => t.TheaterId);

            builder.HasOne(t => t.EventBase)
                   .WithOne()
                   .HasForeignKey<TheaterPlay>(t => t.TheaterId);
        }
    }
}
