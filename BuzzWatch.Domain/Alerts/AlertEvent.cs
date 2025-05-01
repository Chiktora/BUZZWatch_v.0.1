using BuzzWatch.Domain.Common;

namespace BuzzWatch.Domain.Alerts
{
    public class AlertEvent : BaseEntity<Guid>
    {
        private AlertEvent() { } // EF Core constructor
        
        public Guid RuleId { get; private set; }
        public Guid DeviceId { get; private set; }
        public string Message { get; private set; } = string.Empty;
        public DateTimeOffset StartTime { get; private set; }
        public DateTimeOffset? EndTime { get; private set; }

        public static AlertEvent Create(
            Guid ruleId,
            Guid deviceId,
            string message)
        {
            return new AlertEvent
            {
                Id = Guid.NewGuid(),
                RuleId = ruleId,
                DeviceId = deviceId,
                Message = message,
                StartTime = DateTimeOffset.UtcNow,
                EndTime = null
            };
        }

        public void Close()
        {
            if (EndTime == null)
            {
                EndTime = DateTimeOffset.UtcNow;
            }
        }
    }
} 