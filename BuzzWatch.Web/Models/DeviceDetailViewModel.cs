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
        public List<PredictiveInsightDto> PredictiveInsights { get; set; } = new List<PredictiveInsightDto>();
        
        // Prediction data for charts
        public Dictionary<string, double> TemperaturePredictions { get; set; } = new Dictionary<string, double>();
        public Dictionary<string, double> HumidityPredictions { get; set; } = new Dictionary<string, double>();
        public Dictionary<string, double> WeightPredictions { get; set; } = new Dictionary<string, double>();
        
        // Export options
        public List<string> AvailableExportFormats => new List<string> { "CSV", "JSON", "Excel" };
        public List<int> AvailableExportTimeframes => new List<int> { 7, 14, 30, 90 };
        
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
        
        // Analytics properties
        public double? TemperatureAvg => GetMetricStatistics(m => m.Temperature, out _);
        public double? TemperatureMin => GetMetricMinimum(m => m.Temperature);
        public double? TemperatureMax => GetMetricMaximum(m => m.Temperature);
        
        public double? HumidityAvg => GetMetricStatistics(m => m.Humidity, out _);
        public double? HumidityMin => GetMetricMinimum(m => m.Humidity);
        public double? HumidityMax => GetMetricMaximum(m => m.Humidity);
        
        public double? WeightAvg => GetMetricStatistics(m => m.Weight, out _);
        public double? WeightMin => GetMetricMinimum(m => m.Weight);
        public double? WeightMax => GetMetricMaximum(m => m.Weight);
        
        public double? WeightChange24h => CalculateRecentWeightChange(24);
        public double? WeightChange7d => CalculateRecentWeightChange(168); // 7 days in hours
        
        // Data for charts
        public List<string> ChartLabels => Measurements.OrderBy(m => m.Timestamp).Select(m => m.Timestamp.ToString("MM/dd HH:mm")).ToList();
        public List<double?> TemperatureData => Measurements.OrderBy(m => m.Timestamp).Select(m => m.Temperature.HasValue ? (double?)Convert.ToDouble(m.Temperature) : null).ToList();
        public List<double?> HumidityData => Measurements.OrderBy(m => m.Timestamp).Select(m => m.Humidity.HasValue ? (double?)Convert.ToDouble(m.Humidity) : null).ToList();
        public List<double?> WeightData => Measurements.OrderBy(m => m.Timestamp).Select(m => m.Weight.HasValue ? (double?)Convert.ToDouble(m.Weight) : null).ToList();
        
        // Helper methods
        private double? GetMetricStatistics(Func<MeasurementDto, decimal?> selector, out double? stdDev)
        {
            var values = Measurements
                .Where(m => selector(m).HasValue)
                .Select(m => Convert.ToDouble(selector(m).Value))
                .ToList();
                
            if (!values.Any())
            {
                stdDev = null;
                return null;
            }
            
            double avg = values.Average();
            stdDev = Math.Sqrt(values.Select(v => Math.Pow(v - avg, 2)).Average());
            
            return Math.Round(avg, 2);
        }
        
        private double? GetMetricMinimum(Func<MeasurementDto, decimal?> selector)
        {
            var values = Measurements
                .Where(m => selector(m).HasValue)
                .Select(m => Convert.ToDouble(selector(m).Value));
                
            return values.Any() ? Math.Round(values.Min(), 2) : null;
        }
        
        private double? GetMetricMaximum(Func<MeasurementDto, decimal?> selector)
        {
            var values = Measurements
                .Where(m => selector(m).HasValue)
                .Select(m => Convert.ToDouble(selector(m).Value));
                
            return values.Any() ? Math.Round(values.Max(), 2) : null;
        }
        
        private double? CalculateRecentWeightChange(int hours)
        {
            if (Measurements.Count < 2)
            {
                return null;
            }
            
            var orderedMeasurements = Measurements.OrderBy(m => m.Timestamp).ToList();
            var currentTime = orderedMeasurements.Last().Timestamp;
            var startTime = currentTime.AddHours(-hours);
            
            var startMeasurement = orderedMeasurements
                .Where(m => m.Timestamp >= startTime)
                .OrderBy(m => m.Timestamp)
                .FirstOrDefault();
                
            var endMeasurement = orderedMeasurements.Last();
            
            if (startMeasurement == null || !startMeasurement.Weight.HasValue || !endMeasurement.Weight.HasValue)
            {
                return null;
            }
            
            return Math.Round(Convert.ToDouble(endMeasurement.Weight.Value - startMeasurement.Weight.Value), 2);
        }
    }
} 