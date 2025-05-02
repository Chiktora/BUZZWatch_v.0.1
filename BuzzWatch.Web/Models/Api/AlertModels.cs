namespace BuzzWatch.Web.Models.Api
{
    // API Request Models
    public class CreateAlertRuleRequest
    {
        public Guid DeviceId { get; set; }
        public string ReadingType { get; set; } = string.Empty;
        public string Condition { get; set; } = string.Empty; // "Greater", "Less", "Equal"
        public double Threshold { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool IsEnabled { get; set; } = true;
    }

    public class UpdateAlertRuleRequest
    {
        public string Condition { get; set; } = string.Empty;
        public double Threshold { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool IsEnabled { get; set; }
    }

    public class ResolveAlertRequest
    {
        public string ResolutionNotes { get; set; } = string.Empty;
    }

    // API Response Models
    public class AlertRuleResponse
    {
        public Guid Id { get; set; }
        public Guid DeviceId { get; set; }
        public string DeviceName { get; set; } = string.Empty;
        public string ReadingType { get; set; } = string.Empty;
        public string Condition { get; set; } = string.Empty;
        public double Threshold { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool IsEnabled { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }

    public class AlertEventResponse
    {
        public Guid Id { get; set; }
        public Guid DeviceId { get; set; }
        public string DeviceName { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTimeOffset Timestamp { get; set; }
        public bool IsResolved { get; set; }
        public string? ResolutionNotes { get; set; }
        public DateTimeOffset? ResolvedAt { get; set; }
        public ReadingResponse TriggeringReading { get; set; } = new();
    }
} 