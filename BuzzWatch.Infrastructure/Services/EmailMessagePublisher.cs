using BuzzWatch.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace BuzzWatch.Infrastructure.Services;

public class EmailMessagePublisher : IMessagePublisher
{
    private readonly IEmailSender _emailSender;
    private readonly ILogger<EmailMessagePublisher> _logger;

    public EmailMessagePublisher(
        IEmailSender emailSender,
        ILogger<EmailMessagePublisher> logger)
    {
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task PublishAsync(string type, string content, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Publishing message of type {Type}", type);
        
        try
        {
            switch (type)
            {
                case "AlertTriggered":
                    await HandleAlertTriggeredAsync(content, cancellationToken);
                    break;
                    
                case "AlertClosed":
                    await HandleAlertClosedAsync(content, cancellationToken);
                    break;
                    
                default:
                    _logger.LogWarning("Unknown message type: {Type}", type);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing message of type {Type}", type);
            throw;
        }
    }
    
    private async Task HandleAlertTriggeredAsync(string content, CancellationToken cancellationToken)
    {
        var alert = JsonSerializer.Deserialize<AlertTriggeredDto>(content);
        if (alert == null)
        {
            _logger.LogWarning("Failed to deserialize AlertTriggered message");
            return;
        }
        
        var subject = $"ALERT: {alert.Message}";
        var body = $@"
<h2>Alert Triggered</h2>
<p>An alert has been triggered for device: {alert.DeviceId}</p>
<p><strong>Message:</strong> {alert.Message}</p>
<p><strong>Time:</strong> {alert.StartTime}</p>
<p>Please check your BuzzWatch dashboard for more details.</p>
";
        
        await _emailSender.SendEmailAsync(
            "admin@buzzwatch.com", 
            subject, 
            body, 
            cancellationToken);
    }
    
    private async Task HandleAlertClosedAsync(string content, CancellationToken cancellationToken)
    {
        var alert = JsonSerializer.Deserialize<AlertClosedDto>(content);
        if (alert == null)
        {
            _logger.LogWarning("Failed to deserialize AlertClosed message");
            return;
        }
        
        var subject = $"RESOLVED: {alert.Message}";
        var body = $@"
<h2>Alert Resolved</h2>
<p>An alert has been resolved for device: {alert.DeviceId}</p>
<p><strong>Message:</strong> {alert.Message}</p>
<p><strong>Started:</strong> {alert.StartTime}</p>
<p><strong>Resolved:</strong> {alert.EndTime}</p>
<p>Please check your BuzzWatch dashboard for more details.</p>
";
        
        await _emailSender.SendEmailAsync(
            "admin@buzzwatch.com", 
            subject, 
            body, 
            cancellationToken);
    }
    
    // DTOs for message deserialization
    private class AlertTriggeredDto
    {
        public Guid AlertId { get; set; }
        public Guid DeviceId { get; set; }
        public Guid RuleId { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTimeOffset StartTime { get; set; }
    }
    
    private class AlertClosedDto
    {
        public Guid AlertId { get; set; }
        public Guid DeviceId { get; set; }
        public Guid RuleId { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTimeOffset StartTime { get; set; }
        public DateTimeOffset? EndTime { get; set; }
    }
} 