using BuzzWatch.Contracts.Devices;
using BuzzWatch.Contracts.Measurements;
using BuzzWatch.Contracts.Alerts;

namespace BuzzWatch.Web.Models
{
    public class DashboardViewModel
    {
        public List<DeviceDto> Devices { get; set; } = new List<DeviceDto>();
        public Dictionary<Guid, List<MeasurementDto>> RecentMeasurements { get; set; } = new Dictionary<Guid, List<MeasurementDto>>();
        public List<AlertDto> RecentAlerts { get; set; } = new List<AlertDto>();
        public bool HasDevices => Devices.Any();
        public int TotalDevices => Devices.Count;
        public int OnlineDevices => Devices.Count(d => d.Status == "Online");
    }
} 