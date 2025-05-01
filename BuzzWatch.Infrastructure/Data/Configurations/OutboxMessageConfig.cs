using BuzzWatch.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuzzWatch.Infrastructure.Data.Configurations;

public class OutboxMessageConfig : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");
        
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Type)
            .IsRequired()
            .HasMaxLength(100);
            
        builder.Property(x => x.Content)
            .IsRequired();
            
        builder.Property(x => x.CreatedAt)
            .IsRequired();
            
        builder.Property(x => x.Status)
            .IsRequired()
            .HasMaxLength(20);
            
        builder.Property(x => x.Error)
            .HasMaxLength(500);
            
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.CreatedAt);
        builder.HasIndex(x => x.ProcessedAt);
    }
} 