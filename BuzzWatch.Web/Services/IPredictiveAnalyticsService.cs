using BuzzWatch.Contracts.Devices;
using BuzzWatch.Contracts.Measurements;
using BuzzWatch.Web.Models;

namespace BuzzWatch.Web.Services
{
    public interface IPredictiveAnalyticsService
    {
        /// <summary>
        /// Generates predictive insights for a collection of devices based on their measurement history
        /// </summary>
        /// <param name="devices">The devices to analyze</param>
        /// <param name="measurements">Dictionary of device IDs mapped to their measurement history</param>
        /// <returns>A list of predictive insights for the devices</returns>
        Task<List<PredictiveInsightDto>> GenerateInsightsAsync(
            IEnumerable<DeviceDto> devices, 
            Dictionary<Guid, List<MeasurementDto>> measurements);
            
        /// <summary>
        /// Predicts future values for a specific metric on a device
        /// </summary>
        /// <param name="deviceId">The device ID</param>
        /// <param name="metric">The metric to predict (temperature, humidity, weight)</param>
        /// <param name="hoursAhead">How many hours in the future to predict</param>
        /// <returns>Dictionary of predicted timestamps to values</returns>
        Task<Dictionary<DateTime, double>> PredictMetricAsync(
            Guid deviceId, 
            string metric, 
            int hoursAhead = 24);
    }
} 