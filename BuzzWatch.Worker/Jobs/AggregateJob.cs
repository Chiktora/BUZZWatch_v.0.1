using BuzzWatch.Application.Measurements.Interfaces;
using Microsoft.Extensions.Logging;
using Quartz;

namespace BuzzWatch.Worker.Jobs;

[DisallowConcurrentExecution]
public class AggregateJob : IJob
{
    private readonly ILogger<AggregateJob> _logger;
    private readonly IMeasurementAggregator _aggregator;

    public AggregateJob(
        ILogger<AggregateJob> logger,
        IMeasurementAggregator aggregator)
    {
        _logger = logger;
        _aggregator = aggregator;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("Aggregate job started at: {time}", DateTimeOffset.Now);
        
        try
        {
            // Calculate the time range for aggregation (previous hour)
            var endTime = DateTimeOffset.UtcNow;
            var startTime = new DateTimeOffset(
                endTime.Year, 
                endTime.Month, 
                endTime.Day, 
                endTime.Hour, 
                0, 0, 
                endTime.Offset).AddHours(-1);
            
            // Perform the hourly aggregation
            var aggregatedCount = await _aggregator.AggregateHourlyDataAsync(
                startTime, 
                endTime, 
                context.CancellationToken);
            
            _logger.LogInformation("Aggregated {Count} device measurements for period {Start} to {End}", 
                aggregatedCount, startTime, endTime);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during data aggregation");
            throw;
        }
    }
} 