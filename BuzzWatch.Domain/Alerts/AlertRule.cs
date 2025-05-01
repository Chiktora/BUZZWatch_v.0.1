using BuzzWatch.Domain.Common;
using BuzzWatch.Domain.Enums;

namespace BuzzWatch.Domain.Alerts
{
    public class AlertRule : BaseEntity<Guid>
    {
        private AlertRule() { } // EF Core constructor

        public Guid DeviceId { get; private set; }
        public string Metric { get; private set; } = default!;  // e.g. "TempInside"
        public ComparisonOperator Operator { get; private set; }
        public decimal Threshold { get; private set; }
        public int DurationSeconds { get; private set; }
        public bool Active { get; private set; } = true;
        public DateTimeOffset CreatedAt { get; private set; }

        public static AlertRule Create(Guid deviceId,
                                   string metric,
                                   ComparisonOperator op,
                                   decimal threshold,
                                   int durationS)
        {
            if (string.IsNullOrWhiteSpace(metric))
                throw new ArgumentException("Metric name is required", nameof(metric));
                
            if (durationS < 0)
                throw new ArgumentOutOfRangeException(nameof(durationS), "Duration cannot be negative");

            return new AlertRule
            {
                Id = Guid.NewGuid(),
                DeviceId = deviceId,
                Metric = metric,
                Operator = op,
                Threshold = threshold,
                DurationSeconds = durationS,
                CreatedAt = DateTimeOffset.UtcNow,
                Active = true
            };
        }

        public void Disable() => Active = false;
        
        public void Enable() => Active = true;
    }
} 