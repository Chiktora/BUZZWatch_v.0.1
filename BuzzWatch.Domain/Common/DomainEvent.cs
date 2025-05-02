using MediatR;

namespace BuzzWatch.Domain.Common
{
    public interface IDomainEvent : INotification
    {
        DateTimeOffset OccurredOn { get; }
    }

    public record MeasurementCreatedEvent(long MeasurementId, Guid DeviceId) : IDomainEvent
    {
        public DateTimeOffset OccurredOn => DateTimeOffset.UtcNow;
    }
} 