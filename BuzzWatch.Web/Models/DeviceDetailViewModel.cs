using BuzzWatch.Contracts.Devices;
using BuzzWatch.Contracts.Measurements;
using BuzzWatch.Contracts.Alerts;

namespace BuzzWatch.Web.Models
{
    public class DeviceDetailViewModel
    {
        public DeviceDto Device { get; set; } = default!;
        public List<MeasurementDto> Measurements { get; set; } = new List<MeasurementDto>();
        public List<AlertDto> Alerts { get; set; } = new List<AlertDto>();
        
        // Helper properties for the view
        public double? LatestTemperature 
        { 
            get
            {
                var latest = Measurements.OrderByDescending(m => m.Timestamp).FirstOrDefault();
                return latest?.Temperature.HasValue == true ? (double?)Convert.ToDouble(latest.Temperature) : null;
            }
        }
        
        public double? LatestHumidity
        {
            get
            {
                var latest = Measurements.OrderByDescending(m => m.Timestamp).FirstOrDefault();
                return latest?.Humidity.HasValue == true ? (double?)Convert.ToDouble(latest.Humidity) : null;
            }
        }
        
        public double? LatestWeight
        {
            get
            {
                var latest = Measurements.OrderByDescending(m => m.Timestamp).FirstOrDefault();
                return latest?.Weight.HasValue == true ? (double?)Convert.ToDouble(latest.Weight) : null;
            }
        }
        
        public double? LatestBatteryLevel
        {
            get
            {
                var latest = Measurements.OrderByDescending(m => m.Timestamp).FirstOrDefault();
                return latest?.BatteryLevel.HasValue == true ? (double?)Convert.ToDouble(latest.BatteryLevel) : null;
            }
        }
        
        public int ActiveAlertsCount => Alerts.Count(a => a.Status == "Active");
        
        // Data for charts
        public List<string> ChartLabels => Measurements.OrderBy(m => m.Timestamp).Select(m => m.Timestamp.ToString("MM/dd HH:mm")).ToList();
        public List<double?> TemperatureData => Measurements.OrderBy(m => m.Timestamp).Select(m => m.Temperature.HasValue ? (double?)Convert.ToDouble(m.Temperature) : null).ToList();
        public List<double?> HumidityData => Measurements.OrderBy(m => m.Timestamp).Select(m => m.Humidity.HasValue ? (double?)Convert.ToDouble(m.Humidity) : null).ToList();
        public List<double?> WeightData => Measurements.OrderBy(m => m.Timestamp).Select(m => m.Weight.HasValue ? (double?)Convert.ToDouble(m.Weight) : null).ToList();
    }
} 