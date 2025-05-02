namespace BuzzWatch.Web.Models.Api
{
    public class AdminDashboardResponse
    {
        public int TotalUsers { get; set; }
        public int TotalDevices { get; set; }
        public int ActiveAlerts { get; set; }
        public List<DeviceResponse> RecentDevices { get; set; } = new();
        public List<AlertEventResponse> RecentAlerts { get; set; } = new();
        public SystemHealthResponse SystemHealth { get; set; } = new();
    }

    public class SystemHealthResponse
    {
        public bool DatabaseConnected { get; set; }
        public bool ApiServicesRunning { get; set; }
        public bool BackgroundTasksActive { get; set; }
        public double SystemLoad { get; set; }
        public double MemoryUsage { get; set; } // Percentage
        public double DiskUsage { get; set; } // Percentage
    }
} 