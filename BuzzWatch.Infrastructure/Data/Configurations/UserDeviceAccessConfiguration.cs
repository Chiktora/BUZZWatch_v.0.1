using BuzzWatch.Domain.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuzzWatch.Infrastructure.Data.Configurations
{
    public class UserDeviceAccessConfiguration : IEntityTypeConfiguration<UserDeviceAccess>
    {
        public void Configure(EntityTypeBuilder<UserDeviceAccess> builder)
        {
            builder.ToTable("UserDeviceAccess");
            
            builder.HasKey(u => u.Id);
            
            // Create a unique index on the combination of UserId and DeviceId
            // to ensure a user can only have one access record per device
            builder.HasIndex(u => new { u.UserId, u.DeviceId })
                .IsUnique();
                
            // Add indexes for faster lookups
            builder.HasIndex(u => u.UserId);
            builder.HasIndex(u => u.DeviceId);
            
            // Configure properties
            builder.Property(u => u.UserId)
                .IsRequired();
                
            builder.Property(u => u.DeviceId)
                .IsRequired();
                
            builder.Property(u => u.CanManage)
                .IsRequired();
                
            builder.Property(u => u.GrantedAt)
                .IsRequired();
                
            builder.Property(u => u.LastUpdatedAt)
                .IsRequired(false);
        }
    }
} 