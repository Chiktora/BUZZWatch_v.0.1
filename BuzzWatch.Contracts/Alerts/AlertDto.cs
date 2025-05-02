using System;

namespace BuzzWatch.Contracts.Alerts
{
    public class AlertDto
    {
        public Guid Id { get; set; }
        public Guid DeviceId { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Severity { get; set; } = "Medium"; // Low, Medium, High
        public string Status { get; set; } = "Active"; // Active, Acknowledged, Resolved
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
        public string? AlertType { get; set; }
        public double? ThresholdValue { get; set; }
        public double? ActualValue { get; set; }
    }

    public class AlertStatisticsDto
    {
        public int Total { get; set; }
        public int Active { get; set; }
        public int Acknowledged { get; set; }
        public int Resolved { get; set; }
        public int HighSeverity { get; set; }
        public int MediumSeverity { get; set; }
        public int LowSeverity { get; set; }
    }
} 