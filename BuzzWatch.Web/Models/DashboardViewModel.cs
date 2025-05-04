using BuzzWatch.Contracts.Devices;
using BuzzWatch.Contracts.Measurements;
using BuzzWatch.Contracts.Alerts;
using System.Text.Json.Serialization;

namespace BuzzWatch.Web.Models
{
    public class DashboardViewModel
    {
        public List<DeviceDto> Devices { get; set; } = new List<DeviceDto>();
        public Dictionary<Guid, List<MeasurementDto>> RecentMeasurements { get; set; } = new Dictionary<Guid, List<MeasurementDto>>();
        public List<AlertDto> RecentAlerts { get; set; } = new List<AlertDto>();
        public List<DashboardWidgetDto> Widgets { get; set; } = new List<DashboardWidgetDto>();
        public UserPreferencesDto UserPreferences { get; set; } = new UserPreferencesDto();
        
        // Statistics properties
        public bool HasDevices => Devices.Any();
        public int TotalDevices => Devices.Count;
        public int OnlineDevices => Devices.Count(d => d.Status == "Online");
        public int ActiveAlertCount => RecentAlerts.Count(a => a.Status == "Active");
        
        // Helper methods for widgets
        public IEnumerable<DeviceDto> GetTopDevices(int count = 5) => 
            Devices.OrderByDescending(d => RecentMeasurements.TryGetValue(d.Id, out var m) && m.Any() ? m.Max(x => x.Timestamp) : new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero))
                  .Take(count);
                  
        public IEnumerable<string> GetActiveLocations() => 
            Devices.Where(d => d.Status == "Online").Select(d => d.Location).Distinct();
            
        // Predictive analytics summary
        public List<PredictiveInsightDto> PredictiveInsights { get; set; } = new List<PredictiveInsightDto>();
    }
    
    public class UserPreferencesDto
    {
        public List<string> EnabledWidgets { get; set; } = new List<string>();
        public string DashboardLayout { get; set; } = "grid"; // grid, list, compact
        public string MetricPreference { get; set; } = "celsius"; // celsius, fahrenheit
        public int DefaultTimeRange { get; set; } = 24; // hours
        public string Theme { get; set; } = "light"; // light, dark
    }
    
    public class DashboardWidgetDto
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Type { get; set; } = string.Empty; // device-summary, alert-feed, weather, system-health, etc.
        public string Title { get; set; } = string.Empty;
        public int Order { get; set; }
        public Dictionary<string, object> Settings { get; set; } = new Dictionary<string, object>();
        public bool IsEnabled { get; set; } = true;
        public string Size { get; set; } = "medium"; // small, medium, large
    }
    
    public class PredictiveInsightDto
    {
        public Guid DeviceId { get; set; }
        public string DeviceName { get; set; } = string.Empty;
        public string InsightType { get; set; } = string.Empty; // temperature-anomaly, humidity-trend, weight-forecast
        public string Message { get; set; } = string.Empty;
        public string Severity { get; set; } = "info"; // info, warning, alert
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, double> DataPoints { get; set; } = new Dictionary<string, double>();
        public double? Confidence { get; set; }
    }
} 