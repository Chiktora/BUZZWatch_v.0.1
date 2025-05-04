namespace BuzzWatch.Web.Models
{
    public class HomeOverviewViewModel
    {
        // Device statistics
        public int TotalDevices { get; set; }
        public int OnlineDevices { get; set; }
        public int OfflineDevices { get; set; }
        
        // Alert statistics
        public int TotalAlerts { get; set; }
        public int ActiveAlerts { get; set; }
        public int ResolvedAlerts { get; set; }
        
        // Device list for the status table
        public List<DeviceListItemDto> Devices { get; set; } = new List<DeviceListItemDto>();
        
        // Recent alerts for the alerts section
        public List<AlertListItemDto> RecentAlerts { get; set; } = new List<AlertListItemDto>();
        
        // Calculated properties
        public double DeviceOnlinePercentage => TotalDevices > 0 ? (double)OnlineDevices / TotalDevices * 100 : 0;
        public bool HasDevices => TotalDevices > 0;
    }
    
    public class DeviceStatisticsDto
    {
        public int Total { get; set; }
        public int Online { get; set; }
        public int Offline { get; set; }
    }
    
    public class AlertStatisticsDto
    {
        public int Total { get; set; }
        public int Active { get; set; }
        public int Resolved { get; set; }
    }
    
    public class DeviceListItemDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Status { get; set; } = "Offline";
        public DateTimeOffset LastSeen { get; set; }
        
        // Helper property to display time since last update
        public string LastSeenDisplay
        {
            get
            {
                var timeSince = DateTimeOffset.Now - LastSeen;
                
                if (timeSince.TotalMinutes < 60)
                    return $"{(int)timeSince.TotalMinutes} mins ago";
                if (timeSince.TotalHours < 24)
                    return $"{(int)timeSince.TotalHours} hours ago";
                return $"{(int)timeSince.TotalDays} days ago";
            }
        }
    }
    
    public class AlertListItemDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Severity { get; set; } = "Info"; // Info, Warning, Critical
        public string Status { get; set; } = "Active"; // Active, Resolved
        public DateTimeOffset CreatedAt { get; set; }
        public Guid DeviceId { get; set; }
        public string DeviceName { get; set; } = string.Empty;
        
        // Helper property to display time since alert was created
        public string TimeAgo
        {
            get
            {
                var timeSince = DateTimeOffset.Now - CreatedAt;
                
                if (timeSince.TotalMinutes < 60)
                    return $"{(int)timeSince.TotalMinutes} mins ago";
                if (timeSince.TotalHours < 24)
                    return $"{(int)timeSince.TotalHours} hours ago";
                return $"{(int)timeSince.TotalDays} days ago";
            }
        }
        
        // Helper property to get the appropriate CSS class for the alert severity
        public string SeverityClass
        {
            get
            {
                return Severity.ToLower() switch
                {
                    "critical" => "danger",
                    "warning" => "warning",
                    "info" => "info",
                    _ => "secondary"
                };
            }
        }
    }
}