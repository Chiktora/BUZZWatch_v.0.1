using BuzzWatch.Domain.Measurements;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuzzWatch.Infrastructure.Data.Configurations
{
    internal sealed class MeasurementTempInConfig : IEntityTypeConfiguration<MeasurementTempIn>
    {
        public void Configure(EntityTypeBuilder<MeasurementTempIn> e)
        {
            e.ToTable("MeasurementTempIn");
            e.HasKey(t => t.Id);
            e.Property(t => t.ValueC)
                 .HasPrecision(4, 1)
                 .IsRequired();
            e.HasOne<MeasurementHeader>()
                 .WithOne(h => h.TempIn)
                 .HasForeignKey<MeasurementTempIn>(t => t.Id)
                 .OnDelete(DeleteBehavior.Cascade);
        }
    }
} 