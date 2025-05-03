using BuzzWatch.Contracts.Devices;
using BuzzWatch.Contracts.Measurements;
using BuzzWatch.Web.Models;
using System.Linq;

namespace BuzzWatch.Web.Services
{
    public class PredictiveAnalyticsService : IPredictiveAnalyticsService
    {
        private readonly ILogger<PredictiveAnalyticsService> _logger;
        private readonly ApiClient _apiClient;
        
        public PredictiveAnalyticsService(ILogger<PredictiveAnalyticsService> logger, ApiClient apiClient)
        {
            _logger = logger;
            _apiClient = apiClient;
        }
        
        public Task<List<PredictiveInsightDto>> GenerateInsightsAsync(
            IEnumerable<DeviceDto> devices, 
            Dictionary<Guid, List<MeasurementDto>> measurements)
        {
            var insights = new List<PredictiveInsightDto>();
            
            foreach (var device in devices)
            {
                if (!measurements.TryGetValue(device.Id, out var deviceMeasurements) || deviceMeasurements.Count < 5)
                {
                    continue; // Not enough data for prediction
                }
                
                try
                {
                    // Temperature trend analysis
                    var tempInsight = AnalyzeTemperatureTrend(device, deviceMeasurements);
                    if (tempInsight != null)
                    {
                        insights.Add(tempInsight);
                    }
                    
                    // Humidity analysis
                    var humidityInsight = AnalyzeHumidityLevels(device, deviceMeasurements);
                    if (humidityInsight != null)
                    {
                        insights.Add(humidityInsight);
                    }
                    
                    // Weight analysis
                    var weightInsight = AnalyzeWeightTrend(device, deviceMeasurements);
                    if (weightInsight != null)
                    {
                        insights.Add(weightInsight);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error generating insights for device {DeviceId}", device.Id);
                }
            }
            
            return Task.FromResult(insights);
        }
        
        public async Task<Dictionary<DateTime, double>> PredictMetricAsync(
            Guid deviceId, 
            string metric, 
            int hoursAhead = 24)
        {
            try
            {
                // Get historical data
                var measurements = await _apiClient.GetAsync<List<MeasurementDto>>($"/api/v1/devices/{deviceId}/measurements?limit=168"); // 7 days
                if (measurements == null || measurements.Count < 24)
                {
                    return new Dictionary<DateTime, double>();
                }
                
                // Sort by timestamp
                measurements = measurements.OrderBy(m => m.Timestamp).ToList();
                
                // Get the relevant metric values
                var values = GetMetricValues(measurements, metric);
                if (values.Count == 0)
                {
                    return new Dictionary<DateTime, double>();
                }
                
                // Create prediction
                return GenerateSimplePrediction(measurements, values, hoursAhead);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error predicting {Metric} for device {DeviceId}", metric, deviceId);
                return new Dictionary<DateTime, double>();
            }
        }
        
        #region Helper Methods
        
        private List<double> GetMetricValues(List<MeasurementDto> measurements, string metric)
        {
            var values = new List<double>();
            
            foreach (var measurement in measurements)
            {
                double? value = metric.ToLower() switch
                {
                    "temperature" => measurement.Temperature.HasValue ? Convert.ToDouble(measurement.Temperature.Value) : null,
                    "humidity" => measurement.Humidity.HasValue ? Convert.ToDouble(measurement.Humidity.Value) : null,
                    "weight" => measurement.Weight.HasValue ? Convert.ToDouble(measurement.Weight.Value) : null,
                    _ => null
                };
                
                if (value.HasValue)
                {
                    values.Add(value.Value);
                }
            }
            
            return values;
        }
        
        private Dictionary<DateTime, double> GenerateSimplePrediction(
            List<MeasurementDto> measurements, 
            List<double> values, 
            int hoursAhead)
        {
            // This is a very simplified prediction model using moving average
            // In a real implementation, you would use a more sophisticated model
            
            var result = new Dictionary<DateTime, double>();
            double movingAvg = values.TakeLast(12).Average(); // 12-hour moving average
            double stdDev = CalculateStdDev(values.TakeLast(12));
            
            DateTime lastTimestamp = measurements.Last().Timestamp.DateTime;
            
            // Add hourly predictions
            for (int i = 1; i <= hoursAhead; i++)
            {
                DateTime predictedTime = lastTimestamp.AddHours(i);
                
                // Add some randomness to simulate prediction
                double randomFactor = ((new Random()).NextDouble() * 2 - 1) * stdDev * 0.5;
                double predictedValue = movingAvg + randomFactor;
                
                // Add daily patterns (simplified)
                double hourFactor = Math.Sin((predictedTime.Hour - 6) * Math.PI / 12) * stdDev * 0.3;
                predictedValue += hourFactor;
                
                result.Add(predictedTime, Math.Round(predictedValue, 2));
            }
            
            return result;
        }
        
        private double CalculateStdDev(IEnumerable<double> values)
        {
            double avg = values.Average();
            return Math.Sqrt(values.Select(v => Math.Pow(v - avg, 2)).Average());
        }
        
        private PredictiveInsightDto AnalyzeTemperatureTrend(DeviceDto device, List<MeasurementDto> measurements)
        {
            var tempValues = measurements
                .Where(m => m.Temperature.HasValue)
                .OrderBy(m => m.Timestamp)
                .Select(m => new { Timestamp = m.Timestamp, Value = m.Temperature.Value })
                .ToList();
                
            if (tempValues.Count < 5)
            {
                return null;
            }
            
            // Calculate simple linear regression
            var xValues = Enumerable.Range(0, tempValues.Count).Select(i => (double)i).ToList();
            var yValues = tempValues.Select(t => Convert.ToDouble(t.Value)).ToList();
            
            double slope = CalculateSlope(xValues, yValues);
            double currentTemp = Convert.ToDouble(tempValues.Last().Value);
            
            // Create insight based on temperature trend
            string message;
            string severity;
            
            if (Math.Abs(slope) < 0.01)
            {
                message = $"Temperature is stable around {currentTemp:F1}°C";
                severity = "info";
            }
            else if (slope > 0.05)
            {
                message = $"Temperature is rising rapidly ({slope:F2}°C/hour). Current: {currentTemp:F1}°C";
                severity = "warning";
            }
            else if (slope > 0)
            {
                message = $"Temperature is gradually increasing. Current: {currentTemp:F1}°C";
                severity = "info";
            }
            else if (slope < -0.05)
            {
                message = $"Temperature is dropping rapidly ({Math.Abs(slope):F2}°C/hour). Current: {currentTemp:F1}°C";
                severity = "warning";
            }
            else
            {
                message = $"Temperature is gradually decreasing. Current: {currentTemp:F1}°C";
                severity = "info";
            }
            
            // High or low temperature warnings
            if (currentTemp > 35)
            {
                message = $"Temperature is critically high at {currentTemp:F1}°C!";
                severity = "alert";
            }
            else if (currentTemp > 32)
            {
                message = $"Temperature is high at {currentTemp:F1}°C";
                severity = "warning";
            }
            else if (currentTemp < 10)
            {
                message = $"Temperature is critically low at {currentTemp:F1}°C!";
                severity = "alert";
            }
            else if (currentTemp < 15)
            {
                message = $"Temperature is low at {currentTemp:F1}°C";
                severity = "warning";
            }
            
            // Create a datapoint dictionary for visualization
            var dataPoints = new Dictionary<string, double>();
            var recentPoints = tempValues.TakeLast(8).ToList();
            
            for (int i = 0; i < recentPoints.Count; i++)
            {
                dataPoints.Add(recentPoints[i].Timestamp.ToString("HH:mm"), Convert.ToDouble(recentPoints[i].Value));
            }
            
            return new PredictiveInsightDto
            {
                DeviceId = device.Id,
                DeviceName = device.Name,
                InsightType = "temperature-trend",
                Message = message,
                Severity = severity,
                CreatedAt = DateTime.UtcNow,
                DataPoints = dataPoints,
                Confidence = 0.85
            };
        }
        
        private PredictiveInsightDto AnalyzeHumidityLevels(DeviceDto device, List<MeasurementDto> measurements)
        {
            var humidityValues = measurements
                .Where(m => m.Humidity.HasValue)
                .OrderBy(m => m.Timestamp)
                .Select(m => new { Timestamp = m.Timestamp, Value = m.Humidity.Value })
                .ToList();
                
            if (humidityValues.Count < 5)
            {
                return null;
            }
            
            // Get current humidity
            double currentHumidity = Convert.ToDouble(humidityValues.Last().Value);
            
            // Determine optimal humidity range for bees (example range)
            const double MIN_OPTIMAL = 50;
            const double MAX_OPTIMAL = 65;
            
            string message;
            string severity;
            
            if (currentHumidity < 30)
            {
                message = $"Humidity is critically low at {currentHumidity:F1}%. Bee health may be at risk!";
                severity = "alert";
            }
            else if (currentHumidity < MIN_OPTIMAL)
            {
                message = $"Humidity is below optimal range at {currentHumidity:F1}%";
                severity = "warning";
            }
            else if (currentHumidity > 80)
            {
                message = $"Humidity is critically high at {currentHumidity:F1}%. Risk of mold and disease!";
                severity = "alert";
            }
            else if (currentHumidity > MAX_OPTIMAL)
            {
                message = $"Humidity is above optimal range at {currentHumidity:F1}%";
                severity = "warning";
            }
            else
            {
                message = $"Humidity is within optimal range at {currentHumidity:F1}%";
                severity = "info";
            }
            
            // Create a datapoint dictionary for visualization
            var dataPoints = new Dictionary<string, double>();
            var recentPoints = humidityValues.TakeLast(8).ToList();
            
            for (int i = 0; i < recentPoints.Count; i++)
            {
                dataPoints.Add(recentPoints[i].Timestamp.ToString("HH:mm"), Convert.ToDouble(recentPoints[i].Value));
            }
            
            return new PredictiveInsightDto
            {
                DeviceId = device.Id,
                DeviceName = device.Name,
                InsightType = "humidity-levels",
                Message = message,
                Severity = severity,
                CreatedAt = DateTime.UtcNow,
                DataPoints = dataPoints,
                Confidence = 0.9
            };
        }
        
        private PredictiveInsightDto AnalyzeWeightTrend(DeviceDto device, List<MeasurementDto> measurements)
        {
            var weightValues = measurements
                .Where(m => m.Weight.HasValue)
                .OrderBy(m => m.Timestamp)
                .Select(m => new { Timestamp = m.Timestamp, Value = m.Weight.Value })
                .ToList();
                
            if (weightValues.Count < 5)
            {
                return null;
            }
            
            // Calculate weight change
            double firstWeight = Convert.ToDouble(weightValues.First().Value);
            double currentWeight = Convert.ToDouble(weightValues.Last().Value);
            double weightChange = currentWeight - firstWeight;
            double percentChange = (weightChange / firstWeight) * 100;
            
            string message;
            string severity;
            
            if (Math.Abs(percentChange) < 1)
            {
                message = $"Hive weight is stable around {currentWeight:F1}kg";
                severity = "info";
            }
            else if (percentChange > 5)
            {
                message = $"Hive weight has increased significantly by {percentChange:F1}%. Current: {currentWeight:F1}kg";
                severity = "info";
            }
            else if (percentChange > 0)
            {
                message = $"Hive weight is gradually increasing. Current: {currentWeight:F1}kg";
                severity = "info";
            }
            else if (percentChange < -5)
            {
                message = $"Hive weight has decreased significantly by {Math.Abs(percentChange):F1}%. Current: {currentWeight:F1}kg";
                severity = "warning";
            }
            else
            {
                message = $"Hive weight is gradually decreasing. Current: {currentWeight:F1}kg";
                severity = "info";
            }
            
            // Create a datapoint dictionary for visualization
            var dataPoints = new Dictionary<string, double>();
            var recentPoints = weightValues.TakeLast(8).ToList();
            
            for (int i = 0; i < recentPoints.Count; i++)
            {
                dataPoints.Add(recentPoints[i].Timestamp.ToString("HH:mm"), Convert.ToDouble(recentPoints[i].Value));
            }
            
            return new PredictiveInsightDto
            {
                DeviceId = device.Id,
                DeviceName = device.Name,
                InsightType = "weight-trend",
                Message = message,
                Severity = severity,
                CreatedAt = DateTime.UtcNow,
                DataPoints = dataPoints,
                Confidence = 0.8
            };
        }
        
        private double CalculateSlope(List<double> xValues, List<double> yValues)
        {
            // Simple linear regression to calculate slope
            double n = xValues.Count;
            double sumX = xValues.Sum();
            double sumY = yValues.Sum();
            double sumXY = xValues.Zip(yValues, (x, y) => x * y).Sum();
            double sumX2 = xValues.Select(x => x * x).Sum();
            
            return (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
        }
        
        #endregion
    }
} 