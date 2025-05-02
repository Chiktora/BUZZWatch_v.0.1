using BuzzWatch.Worker.Jobs;
using BuzzWatch.Worker.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace BuzzWatch.Worker.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWorkerServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Add Quartz scheduler
        services.AddQuartz(configurator =>
        {
            // UseMicrosoftDependencyInjectionJobFactory is now the default and this call is obsolete
            
            // Alert Engine job - runs every 30 seconds
            configurator.ScheduleJob<AlertEngineJob>(trigger => trigger
                .WithIdentity("AlertEngineJob-trigger")
                .WithSimpleSchedule(schedule => schedule
                    .WithIntervalInSeconds(30)
                    .RepeatForever()));
                    
            // Aggregation job - runs at 5 minutes past every hour
            configurator.ScheduleJob<AggregateJob>(trigger => trigger
                .WithIdentity("AggregateJob-trigger")
                .WithCronSchedule("0 5 * * * ?"));
        });
        
        // Add the Quartz hosted service
        services.AddQuartzHostedService(options =>
        {
            options.WaitForJobsToComplete = true;
        });
        
        // Register OutboxDispatcher as a hosted service
        services.AddHostedService<OutboxDispatcher>();
        
        return services;
    }
} 