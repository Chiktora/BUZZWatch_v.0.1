using BuzzWatch.Application.Alerts.Interfaces;
using Microsoft.Extensions.Logging;
using Quartz;

namespace BuzzWatch.Worker.Jobs;

[DisallowConcurrentExecution]
public class AlertEngineJob : IJob
{
    private readonly ILogger<AlertEngineJob> _logger;
    private readonly IAlertEvaluator _alertEvaluator;

    public AlertEngineJob(
        ILogger<AlertEngineJob> logger,
        IAlertEvaluator alertEvaluator)
    {
        _logger = logger;
        _alertEvaluator = alertEvaluator;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("AlertEngine job started at: {time}", DateTimeOffset.Now);
        
        try
        {
            // Process all active alert rules against recent measurements
            var processedCount = await _alertEvaluator.ProcessAsync(context.CancellationToken);
            
            _logger.LogInformation("AlertEngine processed {Count} measurements", processedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while processing alerts");
            throw;
        }
    }
} 