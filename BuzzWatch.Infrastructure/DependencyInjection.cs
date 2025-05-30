using BuzzWatch.Application.Abstractions;
using BuzzWatch.Application.Alerts.Interfaces;
using BuzzWatch.Application.Common.Interfaces;
using BuzzWatch.Application.Measurements.Interfaces;
using BuzzWatch.Infrastructure.Data;
using BuzzWatch.Infrastructure.Identity;
using BuzzWatch.Infrastructure.Repositories;
using BuzzWatch.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
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
            services.AddScoped<IUserDeviceAccessRepository, UserDeviceAccessRepository>();
            services.AddScoped<IAlertRuleRepository, AlertRuleRepository>();
            services.AddScoped<IApiKeyRepository, ApiKeyRepository>();

            // Register services
            services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
            services.AddScoped<IAlertEvaluator, AlertEvaluator>();
            services.AddScoped<IMeasurementAggregator, MeasurementAggregator>();
            services.AddScoped<IEmailSender, EmailSender>();
            services.AddScoped<IMessagePublisher, EmailMessagePublisher>();

            // Register Identity
            services.AddIdentityCore<AppUser>(options =>
                {
                    options.Password.RequireDigit = true;
                    options.Password.RequireLowercase = true;
                    options.Password.RequireUppercase = true;
                    options.Password.RequireNonAlphanumeric = true;
                    options.Password.RequiredLength = 8;
                })
                .AddRoles<IdentityRole<Guid>>()
                .AddEntityFrameworkStores<ApplicationDbContext>();

            return services;
        }
    }
} 