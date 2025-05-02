using BuzzWatch.Contracts.Devices;
using BuzzWatch.Contracts.Measurements;

namespace BuzzWatch.Web.Models
{
    public class AnalyticsViewModel
    {
        public List<DeviceDto> Devices { get; set; } = new List<DeviceDto>();
        
        public bool HasDevices => Devices.Any();
        public List<DeviceDto> ActiveDevices => Devices.Where(d => d.Status == "Online").ToList();
    }
    
    public class DeviceComparisonViewModel
    {
        public DeviceDto Device1 { get; set; }
        public DeviceDto Device2 { get; set; }
        public List<MeasurementDto> Device1Measurements { get; set; } = new List<MeasurementDto>();
        public List<MeasurementDto> Device2Measurements { get; set; } = new List<MeasurementDto>();
        public string SelectedMetric { get; set; } = "temperature";
        public string SelectedTimeRange { get; set; } = "7d";
        
        public string MetricDisplayName => SelectedMetric switch
        {
            "temperature" => "Temperature (°C)",
            "humidity" => "Humidity (%)",
            "weight" => "Weight (kg)",
            _ => "Temperature (°C)"
        };
        
        public List<string> ChartLabels
        {
            get
            {
                // Combine timestamps from both devices and sort
                var allTimestamps = new List<DateTimeOffset>();
                allTimestamps.AddRange(Device1Measurements.Select(m => m.Timestamp));
                allTimestamps.AddRange(Device2Measurements.Select(m => m.Timestamp));
                return allTimestamps.Distinct().OrderBy(t => t).Select(t => t.ToString("MM/dd HH:mm")).ToList();
            }
        }
        
        public List<double?> Device1MetricData
        {
            get
            {
                return SelectedMetric switch
                {
                    "temperature" => Device1Measurements.OrderBy(m => m.Timestamp).Select(m => m.Temperature.HasValue ? (double?)Convert.ToDouble(m.Temperature) : null).ToList(),
                    "humidity" => Device1Measurements.OrderBy(m => m.Timestamp).Select(m => m.Humidity.HasValue ? (double?)Convert.ToDouble(m.Humidity) : null).ToList(),
                    "weight" => Device1Measurements.OrderBy(m => m.Timestamp).Select(m => m.Weight.HasValue ? (double?)Convert.ToDouble(m.Weight) : null).ToList(),
                    _ => Device1Measurements.OrderBy(m => m.Timestamp).Select(m => m.Temperature.HasValue ? (double?)Convert.ToDouble(m.Temperature) : null).ToList()
                };
            }
        }
        
        public List<double?> Device2MetricData
        {
            get
            {
                return SelectedMetric switch
                {
                    "temperature" => Device2Measurements.OrderBy(m => m.Timestamp).Select(m => m.Temperature.HasValue ? (double?)Convert.ToDouble(m.Temperature) : null).ToList(),
                    "humidity" => Device2Measurements.OrderBy(m => m.Timestamp).Select(m => m.Humidity.HasValue ? (double?)Convert.ToDouble(m.Humidity) : null).ToList(),
                    "weight" => Device2Measurements.OrderBy(m => m.Timestamp).Select(m => m.Weight.HasValue ? (double?)Convert.ToDouble(m.Weight) : null).ToList(),
                    _ => Device2Measurements.OrderBy(m => m.Timestamp).Select(m => m.Temperature.HasValue ? (double?)Convert.ToDouble(m.Temperature) : null).ToList()
                };
            }
        }
        
        // Statistical comparison
        public double? Device1Average => Device1MetricData.Where(v => v.HasValue).Select(v => v.Value).DefaultIfEmpty(0).Average();
        public double? Device2Average => Device2MetricData.Where(v => v.HasValue).Select(v => v.Value).DefaultIfEmpty(0).Average();
        public double? Device1Min => Device1MetricData.Where(v => v.HasValue).Select(v => v.Value).DefaultIfEmpty(0).Min();
        public double? Device2Min => Device2MetricData.Where(v => v.HasValue).Select(v => v.Value).DefaultIfEmpty(0).Min();
        public double? Device1Max => Device1MetricData.Where(v => v.HasValue).Select(v => v.Value).DefaultIfEmpty(0).Max();
        public double? Device2Max => Device2MetricData.Where(v => v.HasValue).Select(v => v.Value).DefaultIfEmpty(0).Max();
        
        public double Difference => Math.Abs((Device1Average ?? 0) - (Device2Average ?? 0));
        public double PercentageDifference
        {
            get
            {
                if ((Device1Average ?? 0) == 0 && (Device2Average ?? 0) == 0)
                    return 0;
                
                if ((Device1Average ?? 0) == 0)
                    return 100;
                
                return Math.Abs(((Device2Average ?? 0) - (Device1Average ?? 0)) / (Device1Average ?? 1) * 100);
            }
        }
    }
    
    public class TrendAnalysisViewModel
    {
        public DeviceDto Device { get; set; }
        public List<MeasurementDto> Measurements { get; set; } = new List<MeasurementDto>();
        public string SelectedMetric { get; set; } = "temperature";
        public string SelectedTimeRange { get; set; } = "30d";
        
        public string MetricDisplayName => SelectedMetric switch
        {
            "temperature" => "Temperature (°C)",
            "humidity" => "Humidity (%)",
            "weight" => "Weight (kg)",
            _ => "Temperature (°C)"
        };
        
        public List<string> ChartLabels => Measurements.OrderBy(m => m.Timestamp).Select(m => m.Timestamp.ToString("MM/dd HH:mm")).ToList();
        
        public List<double?> MetricData
        {
            get
            {
                return SelectedMetric switch
                {
                    "temperature" => Measurements.OrderBy(m => m.Timestamp).Select(m => m.Temperature.HasValue ? (double?)Convert.ToDouble(m.Temperature) : null).ToList(),
                    "humidity" => Measurements.OrderBy(m => m.Timestamp).Select(m => m.Humidity.HasValue ? (double?)Convert.ToDouble(m.Humidity) : null).ToList(),
                    "weight" => Measurements.OrderBy(m => m.Timestamp).Select(m => m.Weight.HasValue ? (double?)Convert.ToDouble(m.Weight) : null).ToList(),
                    _ => Measurements.OrderBy(m => m.Timestamp).Select(m => m.Temperature.HasValue ? (double?)Convert.ToDouble(m.Temperature) : null).ToList()
                };
            }
        }
        
        // Statistical analysis
        public double? Average => MetricData.Where(v => v.HasValue).Select(v => v.Value).DefaultIfEmpty(0).Average();
        public double? Minimum => MetricData.Where(v => v.HasValue).Select(v => v.Value).DefaultIfEmpty(0).Min();
        public double? Maximum => MetricData.Where(v => v.HasValue).Select(v => v.Value).DefaultIfEmpty(0).Max();
        
        // Trend calculation - simplified for this example
        public string TrendDirection
        {
            get
            {
                if (Measurements.Count < 2)
                    return "Stable";
                
                var validData = MetricData.Where(v => v.HasValue).Select(v => v.Value).ToList();
                if (validData.Count < 2)
                    return "Stable";
                
                // Simple linear regression
                double sum_x = 0, sum_y = 0, sum_xy = 0, sum_xx = 0;
                
                for (int i = 0; i < validData.Count; i++)
                {
                    sum_x += i;
                    sum_y += validData[i];
                    sum_xy += i * validData[i];
                    sum_xx += i * i;
                }
                
                double slope = (validData.Count * sum_xy - sum_x * sum_y) / (validData.Count * sum_xx - sum_x * sum_x);
                
                if (Math.Abs(slope) < 0.01)
                    return "Stable";
                
                return slope > 0 ? "Increasing" : "Decreasing";
            }
        }
        
        public string TrendClass => TrendDirection switch
        {
            "Increasing" => "text-success",
            "Decreasing" => "text-danger",
            _ => "text-secondary"
        };
        
        public string TrendIcon => TrendDirection switch
        {
            "Increasing" => "bi-arrow-up",
            "Decreasing" => "bi-arrow-down",
            _ => "bi-arrow-right"
        };
    }
} 