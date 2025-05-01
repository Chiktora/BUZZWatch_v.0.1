using BuzzWatch.Domain.Devices;
using BuzzWatch.Domain.Measurements;
using Microsoft.EntityFrameworkCore;

namespace BuzzWatch.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<Device> Devices => Set<Device>();
        public DbSet<MeasurementHeader> Headers => Set<MeasurementHeader>();

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> opt)
            : base(opt) { }

        protected override void OnModelCreating(ModelBuilder b)
        {
            b.ApplyConfigurationsFromAssembly(GetType().Assembly);
        }
    }
} 