using System.ComponentModel.DataAnnotations;

namespace BuzzWatch.Web.Models
{
    public class UserPreferencesViewModel
    {
        [Display(Name = "Theme")]
        public string Theme { get; set; } = "light";
        
        [Display(Name = "Dashboard Layout")]
        public string DashboardLayout { get; set; } = "grid";
        
        [Display(Name = "Temperature Units")]
        public string MetricPreference { get; set; } = "celsius";
        
        [Display(Name = "Default Time Range (hours)")]
        [Range(1, 168, ErrorMessage = "Time range must be between 1 and 168 hours")]
        public int DefaultTimeRange { get; set; } = 24;
        
        [Display(Name = "Enable Real-time Updates")]
        public bool EnableRealTimeUpdates { get; set; } = true;
        
        [Display(Name = "Enable Notifications")]
        public bool EnableNotifications { get; set; } = true;
        
        [Display(Name = "Show Predictive Insights")]
        public bool ShowPredictiveInsights { get; set; } = true;
        
        // Notification settings
        [Display(Name = "Email Notifications")]
        public bool EmailNotifications { get; set; } = false;
        
        [Display(Name = "Email Address")]
        [EmailAddress]
        public string? NotificationEmail { get; set; }
        
        // Additional customization options
        [Display(Name = "Default Graph View")]
        public string DefaultGraphView { get; set; } = "line";
    }
} 