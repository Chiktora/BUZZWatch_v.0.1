using BuzzWatch.Domain.Alerts;
using BuzzWatch.Domain.Common;
using BuzzWatch.Domain.Devices;
using BuzzWatch.Domain.Measurements;
using BuzzWatch.Domain.Security;
using BuzzWatch.Infrastructure.Identity;
using BuzzWatch.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BuzzWatch.Infrastructure.Data
{
    public class ApplicationDbContext : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>
    {
        public DbSet<Device> Devices => Set<Device>();
        public DbSet<MeasurementHeader> Headers => Set<MeasurementHeader>();
        public DbSet<ApiKey> ApiKeys => Set<ApiKey>();
        public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
        public DbSet<HourlyAggregate> HourlyAggregates => Set<HourlyAggregate>();
        public DbSet<AlertRule> AlertRules => Set<AlertRule>();
        public DbSet<AlertEvent> AlertEvents => Set<AlertEvent>();

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> opt)
            : base(opt) { }

        protected override void OnModelCreating(ModelBuilder b)
        {
            base.OnModelCreating(b); // Call base for Identity tables
            b.ApplyConfigurationsFromAssembly(GetType().Assembly);
        }
    }
} 