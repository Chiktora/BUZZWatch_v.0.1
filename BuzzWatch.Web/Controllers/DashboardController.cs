using BuzzWatch.Contracts.Devices;
using BuzzWatch.Contracts.Measurements;
using BuzzWatch.Contracts.Alerts;
using BuzzWatch.Web.Models;
using BuzzWatch.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Text;
using CsvHelper;
using System.Globalization;
using System.IO;
using ClosedXML.Excel;

namespace BuzzWatch.Web.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApiClient _apiClient;
        private readonly ILogger<DashboardController> _logger;
        private readonly IPredictiveAnalyticsService? _predictiveAnalytics;

        public DashboardController(
            ApiClient apiClient, 
            ILogger<DashboardController> logger,
            IPredictiveAnalyticsService? predictiveAnalytics = null) // Optional to avoid breaking existing tests
        {
            _apiClient = apiClient;
            _logger = logger;
            _predictiveAnalytics = predictiveAnalytics;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var viewModel = new DashboardViewModel();
                
                // Get user preferences (could be from DB/API in the future)
                viewModel.UserPreferences = GetUserPreferences();
                
                // Get all user devices
                viewModel.Devices = await _apiClient.GetAsync<List<DeviceDto>>("/api/v1/devices") ?? new List<DeviceDto>();
                
                // Get recent measurements for each device
                if (viewModel.Devices.Any())
                {
                    foreach (var device in viewModel.Devices)
                    {
                        var measurements = await _apiClient.GetAsync<List<MeasurementDto>>($"/api/v1/devices/{device.Id}/measurements?limit=24");
                        if (measurements != null)
                        {
                            viewModel.RecentMeasurements[device.Id] = measurements;
                        }
                    }
                    
                    // Get recent alerts
                    viewModel.RecentAlerts = await _apiClient.GetAsync<List<AlertDto>>("/api/v1/alerts?limit=10") ?? new List<AlertDto>();
                }
                
                // Load default widgets
                viewModel.Widgets = GetDefaultWidgets();
                
                // Generate predictive insights if the service is available
                if (_predictiveAnalytics != null && viewModel.Devices.Any())
                {
                    viewModel.PredictiveInsights = await _predictiveAnalytics.GenerateInsightsAsync(
                        viewModel.Devices, 
                        viewModel.RecentMeasurements
                    );
                }
                
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard");
                return View(new DashboardViewModel());
            }
        }
        
        public async Task<IActionResult> DeviceDetails(Guid id)
        {
            try
            {
                var device = await _apiClient.GetAsync<DeviceDto>($"/api/v1/devices/{id}");
                if (device == null)
                {
                    return NotFound();
                }
                
                var measurements = await _apiClient.GetAsync<List<MeasurementDto>>($"/api/v1/devices/{id}/measurements?limit=100");
                var alerts = await _apiClient.GetAsync<List<AlertDto>>($"/api/v1/devices/{id}/alerts");
                
                var viewModel = new DeviceDetailViewModel
                {
                    Device = device,
                    Measurements = measurements ?? new List<MeasurementDto>(),
                    Alerts = alerts ?? new List<AlertDto>()
                };
                
                // Generate device-specific predictions if the service is available
                if (_predictiveAnalytics != null && measurements?.Any() == true)
                {
                    var recentMeasurements = new Dictionary<Guid, List<MeasurementDto>> 
                    { 
                        { id, measurements } 
                    };
                    
                    var insights = await _predictiveAnalytics.GenerateInsightsAsync(
                        new List<DeviceDto> { device }, 
                        recentMeasurements
                    );
                    
                    viewModel.PredictiveInsights = insights;
                }
                
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading device details for {DeviceId}", id);
                return RedirectToAction(nameof(Index));
            }
        }
        
        [HttpPost]
        public IActionResult SaveWidgetLayout([FromBody] List<DashboardWidgetDto> widgets)
        {
            try
            {
                // In a real implementation, you would save this to the user's profile
                // For now, we'll just return success
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving widget layout");
                return Json(new { success = false, message = "Failed to save layout" });
            }
        }
        
        [HttpPost]
        public IActionResult SaveUserPreferences([FromBody] UserPreferencesDto preferences)
        {
            try
            {
                // In a real implementation, you would save this to the user's profile
                // For now, we'll just return success
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving user preferences");
                return Json(new { success = false, message = "Failed to save preferences" });
            }
        }
        
        [HttpGet]
        public async Task<IActionResult> ExportDeviceData(Guid id, string format = "csv", int days = 30)
        {
            try
            {
                var device = await _apiClient.GetAsync<DeviceDto>($"/api/v1/devices/{id}");
                if (device == null)
                {
                    return NotFound();
                }
                
                var measurements = await _apiClient.GetAsync<List<MeasurementDto>>($"/api/v1/devices/{id}/measurements?limit=1000&days={days}");
                if (measurements == null || !measurements.Any())
                {
                    return NotFound("No data available for export");
                }
                
                switch (format.ToLower())
                {
                    case "csv":
                        return ExportToCsv(device, measurements);
                    
                    case "json":
                        return ExportToJson(device, measurements);
                    
                    case "excel":
                        return ExportToExcel(device, measurements);
                    
                    default:
                        return BadRequest("Unsupported export format");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting data for device {DeviceId}", id);
                return BadRequest("Failed to export data");
            }
        }
        
        #region Helper Methods
        
        private FileResult ExportToCsv(DeviceDto device, List<MeasurementDto> measurements)
        {
            using var memoryStream = new MemoryStream();
            using var writer = new StreamWriter(memoryStream);
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
            
            // Write header
            csv.WriteField("Timestamp");
            csv.WriteField("Temperature (°C)");
            csv.WriteField("Humidity (%)");
            csv.WriteField("Weight (kg)");
            csv.WriteField("Battery (%)");
            csv.NextRecord();
            
            // Write data
            foreach (var measurement in measurements.OrderBy(m => m.Timestamp))
            {
                csv.WriteField(measurement.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"));
                csv.WriteField(measurement.Temperature);
                csv.WriteField(measurement.Humidity);
                csv.WriteField(measurement.Weight);
                csv.WriteField(measurement.BatteryLevel);
                csv.NextRecord();
            }
            
            writer.Flush();
            memoryStream.Position = 0;
            
            return File(
                memoryStream.ToArray(),
                "text/csv",
                $"BuzzWatch_{device.Name}_{DateTime.UtcNow:yyyyMMdd}.csv"
            );
        }
        
        private FileResult ExportToJson(DeviceDto device, List<MeasurementDto> measurements)
        {
            var deviceData = new
            {
                Device = device,
                Measurements = measurements.OrderBy(m => m.Timestamp),
                ExportedAt = DateTime.UtcNow
            };
            
            var jsonData = System.Text.Json.JsonSerializer.Serialize(deviceData, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });
            
            var bytes = Encoding.UTF8.GetBytes(jsonData);
            
            return File(
                bytes,
                "application/json",
                $"BuzzWatch_{device.Name}_{DateTime.UtcNow:yyyyMMdd}.json"
            );
        }
        
        private FileResult ExportToExcel(DeviceDto device, List<MeasurementDto> measurements)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Measurements");

            // Add header row
            worksheet.Cell(1, 1).Value = "Timestamp";
            worksheet.Cell(1, 2).Value = "Temperature (°C)";
            worksheet.Cell(1, 3).Value = "Humidity (%)";
            worksheet.Cell(1, 4).Value = "Weight (kg)";
            worksheet.Cell(1, 5).Value = "Battery (%)";

            // Style header row
            var headerRow = worksheet.Row(1);
            headerRow.Style.Font.Bold = true;
            headerRow.Style.Fill.BackgroundColor = XLColor.LightGray;

            // Add data rows
            int row = 2;
            foreach (var measurement in measurements.OrderBy(m => m.Timestamp))
            {
                worksheet.Cell(row, 1).Value = measurement.Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
                worksheet.Cell(row, 2).Value = measurement.Temperature;
                worksheet.Cell(row, 3).Value = measurement.Humidity;
                worksheet.Cell(row, 4).Value = measurement.Weight;
                worksheet.Cell(row, 5).Value = measurement.BatteryLevel;
                row++;
            }

            // Auto-size columns
            worksheet.Columns().AdjustToContents();

            // Add device info and metadata
            var infoSheet = workbook.Worksheets.Add("Device Info");
            infoSheet.Cell(1, 1).Value = "Device Information";
            infoSheet.Cell(1, 1).Style.Font.Bold = true;
            
            infoSheet.Cell(2, 1).Value = "Name:";
            infoSheet.Cell(2, 2).Value = device.Name;
            infoSheet.Cell(3, 1).Value = "Location:";
            infoSheet.Cell(3, 2).Value = device.Location;
            infoSheet.Cell(4, 1).Value = "Device ID:";
            infoSheet.Cell(4, 2).Value = device.Id.ToString();
            infoSheet.Cell(5, 1).Value = "Export Date:";
            infoSheet.Cell(5, 2).Value = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
            infoSheet.Cell(6, 1).Value = "Total Measurements:";
            infoSheet.Cell(6, 2).Value = measurements.Count;
            
            infoSheet.Columns().AdjustToContents();

            // Save to memory stream
            using var memoryStream = new MemoryStream();
            workbook.SaveAs(memoryStream);
            memoryStream.Position = 0;

            return File(
                memoryStream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"BuzzWatch_{device.Name}_{DateTime.UtcNow:yyyyMMdd}.xlsx"
            );
        }
        
        private UserPreferencesDto GetUserPreferences()
        {
            // In a real implementation, you would get this from the user's profile
            // For now, return defaults
            return new UserPreferencesDto
            {
                EnabledWidgets = new List<string> { "device-summary", "alerts", "weather", "analytics" },
                DashboardLayout = "grid",
                MetricPreference = "celsius",
                DefaultTimeRange = 24,
                Theme = "light"
            };
        }
        
        private List<DashboardWidgetDto> GetDefaultWidgets()
        {
            return new List<DashboardWidgetDto>
            {
                new DashboardWidgetDto
                {
                    Id = "device-summary",
                    Type = "device-summary",
                    Title = "Device Overview",
                    Order = 1,
                    IsEnabled = true,
                    Size = "large"
                },
                new DashboardWidgetDto
                {
                    Id = "alerts",
                    Type = "alert-feed",
                    Title = "Recent Alerts",
                    Order = 2,
                    IsEnabled = true,
                    Size = "medium"
                },
                new DashboardWidgetDto
                {
                    Id = "weather",
                    Type = "weather",
                    Title = "Weather Conditions",
                    Order = 3,
                    IsEnabled = true,
                    Size = "small",
                    Settings = new Dictionary<string, object>
                    {
                        { "showForecast", true }
                    }
                },
                new DashboardWidgetDto
                {
                    Id = "analytics",
                    Type = "predictive-insights",
                    Title = "Hive Health Predictions",
                    Order = 4,
                    IsEnabled = true,
                    Size = "medium"
                }
            };
        }
        
        #endregion
    }
} 