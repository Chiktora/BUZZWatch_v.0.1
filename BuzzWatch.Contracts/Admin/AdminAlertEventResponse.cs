namespace BuzzWatch.Contracts.Admin
{
    /// <summary>
    /// Response model for alert events (for admin controller)
    /// </summary>
    public class AdminAlertEventResponse
    {
        /// <summary>
        /// Unique identifier for the alert event
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// The name of the device that triggered the alert
        /// </summary>
        public string DeviceName { get; set; } = string.Empty;

        /// <summary>
        /// The type of alert (e.g., "Temperature", "Humidity")
        /// </summary>
        public string AlertType { get; set; } = string.Empty;

        /// <summary>
        /// The alert message
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// When the alert was triggered
        /// </summary>
        public DateTimeOffset Timestamp { get; set; }

        /// <summary>
        /// Whether the alert has been resolved
        /// </summary>
        public bool IsResolved { get; set; }
    }
} 