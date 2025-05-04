using System;

namespace BuzzWatch.Contracts.Admin
{
    /// <summary>
    /// Response model for alert rules
    /// </summary>
    public class AlertRuleResponse
    {
        /// <summary>
        /// Unique identifier for the alert rule
        /// </summary>
        public Guid Id { get; set; }
        
        /// <summary>
        /// The device ID this rule applies to
        /// </summary>
        public Guid DeviceId { get; set; }
        
        /// <summary>
        /// The name of the device (for display purposes)
        /// </summary>
        public string DeviceName { get; set; } = string.Empty;
        
        /// <summary>
        /// The metric being monitored (e.g., "temp_in", "hum_out")
        /// </summary>
        public string Metric { get; set; } = string.Empty;
        
        /// <summary>
        /// The comparison operator as a string (e.g., "<", ">=", "==")
        /// </summary>
        public string Operator { get; set; } = string.Empty;
        
        /// <summary>
        /// The threshold value for triggering the alert
        /// </summary>
        public decimal Threshold { get; set; }
        
        /// <summary>
        /// How long the condition must persist in seconds
        /// </summary>
        public int DurationSeconds { get; set; }
        
        /// <summary>
        /// Whether the rule is currently active
        /// </summary>
        public bool Active { get; set; }
        
        /// <summary>
        /// When this rule was created
        /// </summary>
        public DateTimeOffset CreatedAt { get; set; }
    }
} 