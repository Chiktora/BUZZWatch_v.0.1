namespace BuzzWatch.Contracts.Admin
{
    public class AdminDashboardResponse
    {
        public int TotalUsers { get; set; }
        public int TotalDevices { get; set; }
        public int ActiveAlerts { get; set; }
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
    
    public class AlertEventResponse
    {
        public Guid Id { get; set; }
        public string DeviceName { get; set; } = string.Empty;
        public string AlertType { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTimeOffset Timestamp { get; set; }
        public bool IsResolved { get; set; }
    }
} 