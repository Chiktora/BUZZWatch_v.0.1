using BuzzWatch.Contracts.Devices;
using BuzzWatch.Contracts.Measurements;
using BuzzWatch.Web.Models;
using BuzzWatch.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BuzzWatch.Web.Controllers
{
    [Authorize]
    public class AnalyticsController : Controller
    {
        private readonly ApiClient _apiClient;
        private readonly ILogger<AnalyticsController> _logger;

        public AnalyticsController(ApiClient apiClient, ILogger<AnalyticsController> logger)
        {
            _apiClient = apiClient;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var devices = await _apiClient.GetAsync<List<DeviceDto>>("/api/v1/devices");
                
                var viewModel = new AnalyticsViewModel
                {
                    Devices = devices ?? new List<DeviceDto>()
                };
                
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading analytics dashboard");
                return View(new AnalyticsViewModel());
            }
        }
        
        public async Task<IActionResult> DeviceComparison(Guid device1Id, Guid device2Id, string metric = "temperature", string timeRange = "7d")
        {
            try
            {
                var device1 = await _apiClient.GetAsync<DeviceDto>($"/api/v1/devices/{device1Id}");
                var device2 = await _apiClient.GetAsync<DeviceDto>($"/api/v1/devices/{device2Id}");
                
                if (device1 == null || device2 == null)
                {
                    return NotFound();
                }
                
                string timeParam = timeRange switch
                {
                    "24h" => "limit=24",
                    "7d" => "limit=168", // 7 * 24
                    "30d" => "limit=720", // 30 * 24
                    _ => "limit=168"
                };
                
                var device1Measurements = await _apiClient.GetAsync<List<MeasurementDto>>($"/api/v1/devices/{device1Id}/measurements?{timeParam}");
                var device2Measurements = await _apiClient.GetAsync<List<MeasurementDto>>($"/api/v1/devices/{device2Id}/measurements?{timeParam}");
                
                var viewModel = new DeviceComparisonViewModel
                {
                    Device1 = device1,
                    Device2 = device2,
                    Device1Measurements = device1Measurements ?? new List<MeasurementDto>(),
                    Device2Measurements = device2Measurements ?? new List<MeasurementDto>(),
                    SelectedMetric = metric,
                    SelectedTimeRange = timeRange
                };
                
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading device comparison");
                return RedirectToAction(nameof(Index));
            }
        }
        
        public async Task<IActionResult> TrendAnalysis(Guid deviceId, string metric = "temperature", string timeRange = "30d")
        {
            try
            {
                var device = await _apiClient.GetAsync<DeviceDto>($"/api/v1/devices/{deviceId}");
                
                if (device == null)
                {
                    return NotFound();
                }
                
                string timeParam = timeRange switch
                {
                    "7d" => "limit=168", // 7 * 24
                    "30d" => "limit=720", // 30 * 24
                    "90d" => "limit=2160", // 90 * 24
                    _ => "limit=720"
                };
                
                var measurements = await _apiClient.GetAsync<List<MeasurementDto>>($"/api/v1/devices/{deviceId}/measurements?{timeParam}");
                
                var viewModel = new TrendAnalysisViewModel
                {
                    Device = device,
                    Measurements = measurements ?? new List<MeasurementDto>(),
                    SelectedMetric = metric,
                    SelectedTimeRange = timeRange
                };
                
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading trend analysis");
                return RedirectToAction(nameof(Index));
            }
        }
        
        public async Task<IActionResult> Export(Guid deviceId, string format = "csv", string timeRange = "30d")
        {
            try
            {
                var device = await _apiClient.GetAsync<DeviceDto>($"/api/v1/devices/{deviceId}");
                
                if (device == null)
                {
                    return NotFound();
                }
                
                // In a real application, this would call an API endpoint to get the data in the requested format
                // For demonstration, we'll just redirect back with a message
                
                TempData["SuccessMessage"] = $"Data export for {device.Name} has been initiated. You will receive a download link shortly.";
                
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting data");
                return RedirectToAction(nameof(Index));
            }
        }
    }
} 