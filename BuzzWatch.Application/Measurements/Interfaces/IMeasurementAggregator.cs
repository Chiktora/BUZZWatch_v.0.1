namespace BuzzWatch.Application.Measurements.Interfaces;

/// <summary>
/// Interface for aggregating measurement data
/// </summary>
public interface IMeasurementAggregator
{
    /// <summary>
    /// Aggregates measurement data on an hourly basis
    /// </summary>
    /// <param name="startTime">Start time of aggregation period</param>
    /// <param name="endTime">End time of aggregation period</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of device measurements aggregated</returns>
    Task<int> AggregateHourlyDataAsync(
        DateTimeOffset startTime, 
        DateTimeOffset endTime,
        CancellationToken cancellationToken);
} 