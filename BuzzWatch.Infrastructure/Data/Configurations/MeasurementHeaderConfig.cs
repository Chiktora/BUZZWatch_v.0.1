using BuzzWatch.Domain.Measurements;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuzzWatch.Infrastructure.Data.Configurations
{
    internal sealed class MeasurementHeaderConfig : IEntityTypeConfiguration<MeasurementHeader>
    {
        public void Configure(EntityTypeBuilder<MeasurementHeader> e)
        {
            e.HasKey(m => m.Id);
            e.Property(m => m.RecordedAt).IsRequired();
            e.HasIndex(m => m.RecordedAt);
            e.HasIndex(m => m.DeviceId);
            
            e.HasOne<Domain.Devices.Device>()
                .WithMany(d => d.Measurements)
                .HasForeignKey(m => m.DeviceId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
} 