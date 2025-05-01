using BuzzWatch.Domain.Devices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuzzWatch.Infrastructure.Data.Configurations
{
    internal sealed class DeviceConfig : IEntityTypeConfiguration<Device>
    {
        public void Configure(EntityTypeBuilder<Device> e)
        {
            e.HasKey(d => d.Id);
            e.Property(d => d.Name)
              .HasMaxLength(50)
              .IsRequired();
            e.OwnsOne(d => d.Location);
            e.HasIndex(d => d.CreatedAt);
        }
    }
} 