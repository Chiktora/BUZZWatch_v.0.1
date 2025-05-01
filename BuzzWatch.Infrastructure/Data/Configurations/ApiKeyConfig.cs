using BuzzWatch.Domain.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuzzWatch.Infrastructure.Data.Configurations
{
    public class ApiKeyConfig : IEntityTypeConfiguration<ApiKey>
    {
        public void Configure(EntityTypeBuilder<ApiKey> builder)
        {
            builder.HasKey(a => a.Id);
            
            builder.Property(a => a.Key)
                .IsRequired()
                .HasMaxLength(32);
                
            builder.HasIndex(a => a.Key)
                .IsUnique();
                
            builder.HasIndex(a => a.DeviceId);
            
            builder.HasIndex(a => a.ExpiresAt);
        }
    }
} 