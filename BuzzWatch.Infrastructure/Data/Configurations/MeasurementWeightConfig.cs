using BuzzWatch.Domain.Measurements;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuzzWatch.Infrastructure.Data.Configurations
{
    internal sealed class MeasurementWeightConfig : IEntityTypeConfiguration<MeasurementWeight>
    {
        public void Configure(EntityTypeBuilder<MeasurementWeight> e)
        {
            e.ToTable("MeasurementWeight");
            e.HasKey(w => w.Id);
            e.Property(w => w.ValueKg)
                 .HasPrecision(8, 3)
                 .IsRequired();
            e.HasOne<MeasurementHeader>()
                 .WithOne(h => h.Weight)
                 .HasForeignKey<MeasurementWeight>(w => w.Id)
                 .OnDelete(DeleteBehavior.Cascade);
        }
    }
} 