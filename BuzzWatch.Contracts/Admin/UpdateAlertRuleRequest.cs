using System;
using System.ComponentModel.DataAnnotations;

namespace BuzzWatch.Contracts.Admin
{
    /// <summary>
    /// Request model for updating an existing alert rule
    /// </summary>
    public class UpdateAlertRuleRequest
    {
        /// <summary>
        /// The device ID this rule applies to
        /// </summary>
        [Required]
        public Guid DeviceId { get; set; }
        
        /// <summary>
        /// The metric being monitored (e.g., "temp_in", "hum_out")
        /// </summary>
        [Required]
        [StringLength(50)]
        public string Metric { get; set; } = string.Empty;
        
        /// <summary>
        /// The comparison operator as a string (e.g., "<", ">=", "==")
        /// </summary>
        [Required]
        [StringLength(2)]
        public string Operator { get; set; } = string.Empty;
        
        /// <summary>
        /// The threshold value for triggering the alert
        /// </summary>
        [Required]
        [Range(-100, 1000)]
        public decimal Threshold { get; set; }
        
        /// <summary>
        /// How long the condition must persist in seconds
        /// </summary>
        [Required]
        [Range(0, 86400)] // Maximum of 24 hours
        public int DurationSeconds { get; set; }
        
        /// <summary>
        /// Whether the rule is active
        /// </summary>
        public bool Active { get; set; }
    }
} 