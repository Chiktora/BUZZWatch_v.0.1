using BuzzWatch.Domain.Common;

namespace BuzzWatch.Domain.Alerts
{
    public class AlertEvent : BaseEntity<Guid>
    {
        private AlertEvent() { } // EF Core constructor
        
        public Guid AlertRuleId { get; private set; }
        public Guid DeviceId { get; private set; }
        public DateTimeOffset OpenedAt { get; private set; }
        public DateTimeOffset? ClosedAt { get; private set; }
        public decimal TriggerValue { get; private set; }
        public bool IsOpen => ClosedAt == null;
        
        public static AlertEvent Open(Guid alertRuleId, Guid deviceId, decimal triggerValue)
        {
            return new AlertEvent
            {
                Id = Guid.NewGuid(),
                AlertRuleId = alertRuleId,
                DeviceId = deviceId,
                OpenedAt = DateTimeOffset.UtcNow,
                TriggerValue = triggerValue
            };
        }
        
        public void Close()
        {
            if (IsOpen)
            {
                ClosedAt = DateTimeOffset.UtcNow;
            }
        }
    }
} 