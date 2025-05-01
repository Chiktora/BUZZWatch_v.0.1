using BuzzWatch.Domain.Measurements;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuzzWatch.Infrastructure.Data.Configurations
{
    internal sealed class MeasurementTempOutConfig : IEntityTypeConfiguration<MeasurementTempOut>
    {
        public void Configure(EntityTypeBuilder<MeasurementTempOut> e)
        {
            e.ToTable("MeasurementTempOut");
            e.HasKey(t => t.Id);
            e.Property(t => t.ValueC)
                 .HasPrecision(4, 1)
                 .IsRequired();
            e.HasOne<MeasurementHeader>()
                 .WithOne(h => h.TempOut)
                 .HasForeignKey<MeasurementTempOut>(t => t.Id)
                 .OnDelete(DeleteBehavior.Cascade);
        }
    }
} 