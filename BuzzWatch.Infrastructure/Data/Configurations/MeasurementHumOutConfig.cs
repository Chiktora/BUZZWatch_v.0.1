using BuzzWatch.Domain.Measurements;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuzzWatch.Infrastructure.Data.Configurations
{
    internal sealed class MeasurementHumOutConfig : IEntityTypeConfiguration<MeasurementHumOut>
    {
        public void Configure(EntityTypeBuilder<MeasurementHumOut> e)
        {
            e.ToTable("MeasurementHumOut");
            e.HasKey(h => h.Id);
            e.Property(h => h.ValuePct)
                 .HasPrecision(5, 2)
                 .IsRequired();
            e.HasOne<MeasurementHeader>()
                 .WithOne(h => h.HumOut)
                 .HasForeignKey<MeasurementHumOut>(h => h.Id)
                 .OnDelete(DeleteBehavior.Cascade);
        }
    }
} 