using System;

namespace BuzzWatch.Contracts.Alerts
{
    public class AlertRuleDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsEnabled { get; set; } = true;
        public Guid? DeviceId { get; set; } // null means applies to all devices
        public string MetricType { get; set; } = string.Empty; // temperature, humidity, weight, battery, etc.
        public string ConditionType { get; set; } = "GreaterThan"; // GreaterThan, LessThan, Equal, Between, etc.
        public double ThresholdValue { get; set; }
        public double? SecondaryThresholdValue { get; set; } // for Between condition
        public string Severity { get; set; } = "Medium"; // Low, Medium, High
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? UpdatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
    }
    
    public class AlertRuleCreateDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsEnabled { get; set; } = true;
        public Guid? DeviceId { get; set; }
        public string MetricType { get; set; } = string.Empty;
        public string ConditionType { get; set; } = "GreaterThan";
        public double ThresholdValue { get; set; }
        public double? SecondaryThresholdValue { get; set; }
        public string Severity { get; set; } = "Medium";
    }
    
    public class AlertRuleUpdateDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsEnabled { get; set; } = true;
        public Guid? DeviceId { get; set; }
        public string MetricType { get; set; } = string.Empty;
        public string ConditionType { get; set; } = "GreaterThan";
        public double ThresholdValue { get; set; }
        public double? SecondaryThresholdValue { get; set; }
        public string Severity { get; set; } = "Medium";
    }
} 