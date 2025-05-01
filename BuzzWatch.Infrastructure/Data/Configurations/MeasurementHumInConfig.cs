using BuzzWatch.Domain.Measurements;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuzzWatch.Infrastructure.Data.Configurations
{
    internal sealed class MeasurementHumInConfig : IEntityTypeConfiguration<MeasurementHumIn>
    {
        public void Configure(EntityTypeBuilder<MeasurementHumIn> e)
        {
            e.ToTable("MeasurementHumIn");
            e.HasKey(h => h.Id);
            e.Property(h => h.ValuePct)
                 .HasPrecision(5, 2)
                 .IsRequired();
            e.HasOne<MeasurementHeader>()
                 .WithOne(h => h.HumIn)
                 .HasForeignKey<MeasurementHumIn>(h => h.Id)
                 .OnDelete(DeleteBehavior.Cascade);
        }
    }
} 