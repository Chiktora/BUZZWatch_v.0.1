using BuzzWatch.Application.Measurements.Interfaces;
using BuzzWatch.Domain.Devices;
using BuzzWatch.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BuzzWatch.Infrastructure.Services;

public class MeasurementAggregator : IMeasurementAggregator
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<MeasurementAggregator> _logger;

    public MeasurementAggregator(
        ApplicationDbContext dbContext,
        ILogger<MeasurementAggregator> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<int> AggregateHourlyDataAsync(
        DateTimeOffset startTime,
        DateTimeOffset endTime,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Aggregating hourly data from {Start} to {End}", startTime, endTime);
        
        try
        {
            // Get all devices with measurements in the time range
            var deviceIds = await _dbContext.Devices
                .Where(d => d.Measurements.Any(m => m.RecordedAt >= startTime && m.RecordedAt < endTime))
                .Select(d => d.Id)
                .ToListAsync(cancellationToken);
                
            if (!deviceIds.Any())
            {
                _logger.LogInformation("No devices with measurements in the specified time range");
                return 0;
            }
            
            var aggregateCount = 0;
            foreach (var deviceId in deviceIds)
            {
                // Get aggregated temperature data
                var tempData = await _dbContext.Headers
                    .Where(m => m.DeviceId == deviceId && 
                           m.RecordedAt >= startTime && 
                           m.RecordedAt < endTime &&
                           m.TempIn != null)
                    .GroupBy(x => x.DeviceId)
                    .Select(g => new 
                    {
                        DeviceId = g.Key,
                        AvgTemp = g.Average(m => m.TempIn!.ValueC),
                        MinTemp = g.Min(m => m.TempIn!.ValueC),
                        MaxTemp = g.Max(m => m.TempIn!.ValueC),
                        Count = g.Count()
                    })
                    .FirstOrDefaultAsync(cancellationToken);
                
                if (tempData != null)
                {
                    // Create hourly aggregate record
                    var tempAggregate = new HourlyAggregate
                    {
                        Id = Guid.NewGuid(),
                        DeviceId = deviceId,
                        Period = startTime.DateTime, // Store as DateTime in SQL
                        MetricType = "Temperature",
                        AvgValue = tempData.AvgTemp,
                        MinValue = tempData.MinTemp,
                        MaxValue = tempData.MaxTemp,
                        SampleCount = tempData.Count
                    };
                    
                    _dbContext.Set<HourlyAggregate>().Add(tempAggregate);
                    aggregateCount++;
                    
                    _logger.LogInformation("Created hourly temperature aggregate for device {DeviceId} with {Count} samples",
                        deviceId, tempData.Count);
                }
                
                // Similarly, we could aggregate humidity, weight, etc.
                // Code omitted for brevity
            }
            
            await _dbContext.SaveChangesAsync(cancellationToken);
            return aggregateCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during hourly data aggregation");
            throw;
        }
    }
}

// Entity for storing aggregated measurements
public class HourlyAggregate
{
    public Guid Id { get; set; }
    public Guid DeviceId { get; set; }
    public DateTime Period { get; set; }
    public string MetricType { get; set; } = string.Empty;
    public decimal AvgValue { get; set; }
    public decimal MinValue { get; set; }
    public decimal MaxValue { get; set; }
    public int SampleCount { get; set; }

    // Navigation property
    public Device? Device { get; set; }
} 