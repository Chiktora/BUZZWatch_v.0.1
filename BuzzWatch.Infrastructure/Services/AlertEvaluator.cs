using BuzzWatch.Application.Alerts.Interfaces;
using BuzzWatch.Domain.Alerts;
using BuzzWatch.Domain.Measurements;
using BuzzWatch.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace BuzzWatch.Infrastructure.Services;

public class AlertEvaluator : IAlertEvaluator
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<AlertEvaluator> _logger;

    public AlertEvaluator(
        ApplicationDbContext dbContext,
        ILogger<AlertEvaluator> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<int> ProcessAsync(CancellationToken cancellationToken)
    {
        // Get all active alert rules
        var activeRules = await _dbContext.Set<AlertRule>()
            .Where(r => r.Active)
            .ToListAsync(cancellationToken);

        if (!activeRules.Any())
        {
            _logger.LogInformation("No active alert rules found");
            return 0;
        }

        _logger.LogInformation("Processing {Count} active alert rules", activeRules.Count);
        var processedCount = 0;

        // Group rules by device
        var rulesByDevice = activeRules.GroupBy(r => r.DeviceId);
        
        foreach (var deviceRules in rulesByDevice)
        {
            var deviceId = deviceRules.Key;
            
            // Get recent measurements for this device
            var recentMeasurements = await _dbContext.Headers
                .Where(m => m.DeviceId == deviceId)
                .OrderByDescending(m => m.RecordedAt)
                .Take(100) // Limit to recent measurements
                .ToListAsync(cancellationToken);
                
            if (!recentMeasurements.Any())
            {
                continue;
            }
            
            processedCount += recentMeasurements.Count;
            
            // For each rule, check if conditions are met
            foreach (var rule in deviceRules)
            {
                await EvaluateRuleAsync(rule, recentMeasurements, cancellationToken);
            }
        }
        
        return processedCount;
    }
    
    private async Task EvaluateRuleAsync(AlertRule rule, ICollection<MeasurementHeader> measurements, CancellationToken cancellationToken)
    {
        // Find existing open alert for this rule
        var existingAlert = await _dbContext.Set<AlertEvent>()
            .FirstOrDefaultAsync(e => e.RuleId == rule.Id && e.EndTime == null, cancellationToken);
            
        // Check if conditions are met for the rule
        var conditionsMet = CheckRuleConditions(rule, measurements);
        
        if (conditionsMet)
        {
            // If no open alert exists, create one
            if (existingAlert == null)
            {
                var message = $"Alert triggered for {rule.Metric} {rule.Operator} {rule.Threshold}";
                var newAlert = AlertEvent.Create(rule.Id, rule.DeviceId, message);
                
                _dbContext.Add(newAlert);
                await _dbContext.SaveChangesAsync(cancellationToken);
                
                _logger.LogWarning("New alert created: {Message} for Device {DeviceId}", 
                    message, rule.DeviceId);
                
                // Create outbox message for notification
                var outboxMessage = new Domain.Common.OutboxMessage
                {
                    Id = Guid.NewGuid(),
                    Type = "AlertTriggered",
                    Content = JsonSerializer.Serialize(new 
                    { 
                        AlertId = newAlert.Id,
                        DeviceId = newAlert.DeviceId,
                        RuleId = newAlert.RuleId,
                        Message = message,
                        StartTime = DateTimeOffset.UtcNow
                    }),
                    CreatedAt = DateTimeOffset.UtcNow,
                    Status = "Pending"
                };
                
                _dbContext.OutboxMessages.Add(outboxMessage);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
        }
        else if (existingAlert != null)
        {
            // If conditions are no longer met, close the alert
            existingAlert.Close();
            await _dbContext.SaveChangesAsync(cancellationToken);
            
            _logger.LogInformation("Alert closed: {AlertId} for Device {DeviceId}", 
                existingAlert.Id, existingAlert.DeviceId);
                
            // Create outbox message for notification
            var outboxMessage = new Domain.Common.OutboxMessage
            {
                Id = Guid.NewGuid(),
                Type = "AlertClosed",
                Content = JsonSerializer.Serialize(new 
                { 
                    AlertId = existingAlert.Id,
                    DeviceId = existingAlert.DeviceId,
                    RuleId = existingAlert.RuleId,
                    Message = existingAlert.Message,
                    StartTime = existingAlert.StartTime,
                    EndTime = existingAlert.EndTime
                }),
                CreatedAt = DateTimeOffset.UtcNow,
                Status = "Pending"
            };
            
            _dbContext.OutboxMessages.Add(outboxMessage);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
    
    private bool CheckRuleConditions(AlertRule rule, ICollection<MeasurementHeader> measurements)
    {
        // Find consecutive measurements that exceed the threshold for the required duration
        
        // This is a simplified implementation - in a real system you'd need to handle
        // different metrics (temp, humidity, etc.) and perform proper time-based analysis
        
        // For now, we'll just check if the most recent measurement exceeds the threshold
        var latestMeasurement = measurements.OrderByDescending(m => m.RecordedAt).FirstOrDefault();
        if (latestMeasurement == null)
        {
            return false;
        }
        
        decimal? value = null;
        
        // Get the value based on the metric type
        switch (rule.Metric.ToLowerInvariant())
        {
            case "tempinside":
            case "tempinsidec":
                value = latestMeasurement.TempIn?.ValueC;
                break;
            case "huminside":
            case "huminsidepct":
                value = latestMeasurement.HumIn?.ValuePct;
                break;
            case "tempoutside":
            case "tempoutsidec":
                value = latestMeasurement.TempOut?.ValueC;
                break;
            case "humoutside":
            case "humoutsidepct":
                value = latestMeasurement.HumOut?.ValuePct;
                break;
            case "weight":
            case "weightkg":
                value = latestMeasurement.Weight?.ValueKg;
                break;
        }
        
        if (value == null)
        {
            return false;
        }
        
        // Check the condition based on the operator
        switch (rule.Operator)
        {
            case ComparisonOperator.GreaterThan:
                return value > rule.Threshold;
            case ComparisonOperator.LessThan:
                return value < rule.Threshold;
            case ComparisonOperator.Equals:
                return value == rule.Threshold;
            default:
                return false;
        }
    }
} 