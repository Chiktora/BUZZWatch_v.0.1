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
}