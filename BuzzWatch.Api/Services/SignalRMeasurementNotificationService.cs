using BuzzWatch.Api.Hubs;
using BuzzWatch.Contracts.Measurements;
using BuzzWatch.Infrastructure.Abstractions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace BuzzWatch.Api.Services
{
    public class SignalRMeasurementNotificationService : IMeasurementNotificationService
    {
        private readonly IHubContext<MeasurementHub> _hubContext;
        private readonly ILogger<SignalRMeasurementNotificationService> _logger;

        public SignalRMeasurementNotificationService(
            IHubContext<MeasurementHub> hubContext,
            ILogger<SignalRMeasurementNotificationService> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task NotifyMeasurementAdded(MeasurementDto measurement, CancellationToken cancellationToken = default)
        {
            try
            {
                await _hubContext.Clients.Group(measurement.DeviceId.ToString())
                    .SendAsync("MeasurementAdded", measurement, cancellationToken);
                
                _logger.LogInformation("SignalR notification sent for measurement {Id} to device {DeviceId}",
                    measurement.Id, measurement.DeviceId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending SignalR notification for measurement {Id}", measurement.Id);
            }
        }
    }
} 