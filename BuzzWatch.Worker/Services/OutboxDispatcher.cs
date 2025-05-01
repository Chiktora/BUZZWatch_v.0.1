using BuzzWatch.Application.Common.Interfaces;
using BuzzWatch.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BuzzWatch.Worker.Services;

public class OutboxDispatcher : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxDispatcher> _logger;
    private readonly IMessagePublisher _messagePublisher;
    private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(5);

    public OutboxDispatcher(
        IServiceProvider serviceProvider,
        ILogger<OutboxDispatcher> logger,
        IMessagePublisher messagePublisher)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _messagePublisher = messagePublisher;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxDispatcher service started at: {time}", DateTimeOffset.Now);
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxMessagesAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error processing outbox messages");
            }
            
            await Task.Delay(_pollingInterval, stoppingToken);
        }
    }

    private async Task ProcessOutboxMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var messages = await dbContext.OutboxMessages
            .Where(m => m.ProcessedAt == null)
            .OrderBy(m => m.CreatedAt)
            .Take(20)
            .ToListAsync(cancellationToken);
            
        if (messages.Count == 0)
        {
            return;
        }
        
        _logger.LogInformation("Processing {Count} outbox messages", messages.Count);
        
        foreach (var message in messages)
        {
            try
            {
                await _messagePublisher.PublishAsync(message.Type, message.Content, cancellationToken);
                
                message.ProcessedAt = DateTimeOffset.UtcNow;
                message.Status = "Processed";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to process outbox message {Id}", message.Id);
                message.Error = ex.Message;
                message.Status = "Failed";
                
                // Only retry a certain number of times
                if (message.RetryCount >= 3)
                {
                    message.ProcessedAt = DateTimeOffset.UtcNow;
                }
                else
                {
                    message.RetryCount++;
                }
            }
        }
        
        await dbContext.SaveChangesAsync(cancellationToken);
    }
} 