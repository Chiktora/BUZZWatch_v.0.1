using BuzzWatch.Domain.Alerts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuzzWatch.Infrastructure.Data.Configurations
{
    public class AlertEventConfig : IEntityTypeConfiguration<AlertEvent>
    {
        public void Configure(EntityTypeBuilder<AlertEvent> builder)
        {
            builder.ToTable("AlertEvents");
            
            builder.HasKey(x => x.Id);
            
            builder.Property(x => x.Message)
                .IsRequired()
                .HasMaxLength(200);
                
            builder.Property(x => x.StartTime)
                .IsRequired();
                
            builder.HasIndex(x => new { x.DeviceId, x.EndTime });
            builder.HasIndex(x => x.RuleId);
        }
    }
} 