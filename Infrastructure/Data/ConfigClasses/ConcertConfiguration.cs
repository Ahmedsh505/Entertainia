using Core.Models.EventModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations
{
    public class ConcertConfiguration : IEntityTypeConfiguration<Concert>
    {
        public void Configure(EntityTypeBuilder<Concert> builder)
        {
            builder.HasKey(c => c.ConcertId);

            builder.HasOne(c => c.EventBase)
                   .WithOne()
                   .HasForeignKey<Concert>(c => c.ConcertId);
        }
    }
}
