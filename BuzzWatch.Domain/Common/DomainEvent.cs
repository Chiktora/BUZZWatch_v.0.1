namespace BuzzWatch.Domain.Common
{
    public interface IDomainEvent
    {
        DateTimeOffset OccurredOn { get; }
    }

    public record MeasurementCreatedEvent(long MeasurementId, Guid DeviceId) : IDomainEvent
    {
        public DateTimeOffset OccurredOn => DateTimeOffset.UtcNow;
    }
} 