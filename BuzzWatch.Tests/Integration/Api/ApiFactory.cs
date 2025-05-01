using BuzzWatch.Infrastructure;
using BuzzWatch.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BuzzWatch.Tests.Integration.Api
{
    public class ApiFactory : WebApplicationFactory<BuzzWatch.Api.Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // Remove the app's ApplicationDbContext registration
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));

                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Register test services
                services.AddInfrastructure(
                    services.BuildServiceProvider().GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>(), 
                    useSqliteForTests: true);

                // Build the service provider
                var sp = services.BuildServiceProvider();

                // Create a scope to get the DbContext
                using (var scope = sp.CreateScope())
                {
                    var scopedServices = scope.ServiceProvider;
                    var db = scopedServices.GetRequiredService<ApplicationDbContext>();
                    var logger = scopedServices.GetRequiredService<ILogger<ApiFactory>>();

                    try
                    {
                        // Ensure the database is created and migrated
                        db.Database.EnsureCreated();

                        // Seed test data
                        SeedTestData(db);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "An error occurred setting up the test database.");
                    }
                }
            });
        }

        private void SeedTestData(ApplicationDbContext db)
        {
            // Create a test device
            var device = BuzzWatch.Domain.Devices.Device.Create("Test Device");
            db.Devices.Add(device);
            db.SaveChanges();
        }
    }
} 