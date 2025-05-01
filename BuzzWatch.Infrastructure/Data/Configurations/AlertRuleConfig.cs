using BuzzWatch.Domain.Alerts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuzzWatch.Infrastructure.Data.Configurations
{
    public class AlertRuleConfig : IEntityTypeConfiguration<AlertRule>
    {
        public void Configure(EntityTypeBuilder<AlertRule> builder)
        {
            builder.ToTable("AlertRules");
            
            builder.HasKey(x => x.Id);
            
            builder.Property(x => x.Metric)
                .IsRequired()
                .HasMaxLength(50);
                
            builder.Property(x => x.Threshold)
                .IsRequired()
                .HasPrecision(10, 2);
                
            builder.Property(x => x.CreatedAt)
                .IsRequired();
                
            builder.HasIndex(x => new { x.DeviceId, x.Active });
        }
    }
} 