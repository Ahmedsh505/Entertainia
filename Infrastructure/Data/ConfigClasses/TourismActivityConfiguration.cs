using Core.Models.EventModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Configurations
{
    public class TourismActivityConfiguration : IEntityTypeConfiguration<TourismActivity>
    {
        public void Configure(EntityTypeBuilder<TourismActivity> builder)
        {
            builder.HasKey(t => t.TourismId);

            builder.HasOne(t => t.EventBase)
                   .WithOne()
                   .HasForeignKey<TourismActivity>(t => t.TourismId);
        }
    }
}
