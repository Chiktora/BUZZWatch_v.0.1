using BuzzWatch.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuzzWatch.Infrastructure.Data.Configurations;

public class HourlyAggregateConfig : IEntityTypeConfiguration<HourlyAggregate>
{
    public void Configure(EntityTypeBuilder<HourlyAggregate> builder)
    {
        builder.ToTable("HourlyAggregates");
        
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.MetricType)
            .IsRequired()
            .HasMaxLength(50);
            
        builder.Property(x => x.AvgValue)
            .HasPrecision(10, 2);
            
        builder.Property(x => x.MinValue)
            .HasPrecision(10, 2);
            
        builder.Property(x => x.MaxValue)
            .HasPrecision(10, 2);
            
        builder.HasIndex(x => new { x.DeviceId, x.Period, x.MetricType }).IsUnique();
        
        builder.HasOne(x => x.Device)
            .WithMany()
            .HasForeignKey(x => x.DeviceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
} 