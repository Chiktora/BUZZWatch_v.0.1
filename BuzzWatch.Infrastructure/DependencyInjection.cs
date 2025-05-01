using BuzzWatch.Application.Abstractions;
using BuzzWatch.Infrastructure.Data;
using BuzzWatch.Infrastructure.Repositories;
using BuzzWatch.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BuzzWatch.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services, 
            IConfiguration configuration, 
            bool useSqliteForTests = false)
        {
            // Configure DbContext
            if (useSqliteForTests)
            {
                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseSqlite("Filename=:memory:"));
            }
            else
            {
                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseSqlServer(configuration.GetConnectionString("Default")));
            }

            // Register repositories
            services.AddScoped<IDeviceRepository, DeviceRepository>();
            services.AddScoped<IMeasurementRepository, MeasurementRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // Register services
            services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

            return services;
        }
    }
} 