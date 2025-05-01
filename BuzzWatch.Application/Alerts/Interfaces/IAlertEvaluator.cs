namespace BuzzWatch.Application.Alerts.Interfaces;

/// <summary>
/// Interface for evaluating alert rules against recent measurements
/// </summary>
public interface IAlertEvaluator
{
    /// <summary>
    /// Processes all active alert rules against recent measurements
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of measurements processed</returns>
    Task<int> ProcessAsync(CancellationToken cancellationToken);
} 