using BuzzWatch.Contracts.Measurements;

namespace BuzzWatch.Infrastructure.Abstractions
{
    public interface IMeasurementNotificationService
    {
        Task NotifyMeasurementAdded(MeasurementDto measurement, CancellationToken cancellationToken = default);
    }
} 