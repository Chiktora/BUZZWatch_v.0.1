using BuzzWatch.Contracts.Devices;
using BuzzWatch.Contracts.Measurements;
using BuzzWatch.Web.Models;
using BuzzWatch.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.IO;
using System.Text;
using CsvHelper;
using ClosedXML.Excel;

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
                
                string timeParam = timeRange switch
                {
                    "7d" => "limit=168", // 7 * 24
                    "30d" => "limit=720", // 30 * 24
                    "90d" => "limit=2160", // 90 * 24
                    _ => "limit=720"
                };
                
                var measurements = await _apiClient.GetAsync<List<MeasurementDto>>($"/api/v1/devices/{deviceId}/measurements?{timeParam}");
                
                if (measurements == null || !measurements.Any())
                {
                    TempData["ErrorMessage"] = "No data available for export.";
                    return RedirectToAction(nameof(Index));
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
                        TempData["ErrorMessage"] = "Unsupported export format.";
                        return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting data");
                TempData["ErrorMessage"] = "An error occurred while exporting data.";
                return RedirectToAction(nameof(Index));
            }
        }

        #region Export Helpers
        
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
            
            // Add analytics sheet with summary statistics
            var statsSheet = workbook.Worksheets.Add("Analytics");
            statsSheet.Cell(1, 1).Value = "Analytics";
            statsSheet.Cell(1, 1).Style.Font.Bold = true;
            
            // Calculate basic statistics
            statsSheet.Cell(2, 1).Value = "Metric";
            statsSheet.Cell(2, 2).Value = "Minimum";
            statsSheet.Cell(2, 3).Value = "Maximum";
            statsSheet.Cell(2, 4).Value = "Average";
            
            var validTemps = measurements.Where(m => m.Temperature.HasValue).Select(m => (double)m.Temperature.Value).ToList();
            var validHumidity = measurements.Where(m => m.Humidity.HasValue).Select(m => (double)m.Humidity.Value).ToList();
            var validWeight = measurements.Where(m => m.Weight.HasValue).Select(m => (double)m.Weight.Value).ToList();
            
            // Temperature stats
            statsSheet.Cell(3, 1).Value = "Temperature (°C)";
            if (validTemps.Any())
            {
                statsSheet.Cell(3, 2).Value = validTemps.Min();
                statsSheet.Cell(3, 3).Value = validTemps.Max();
                statsSheet.Cell(3, 4).Value = validTemps.Average();
            }
            
            // Humidity stats
            statsSheet.Cell(4, 1).Value = "Humidity (%)";
            if (validHumidity.Any())
            {
                statsSheet.Cell(4, 2).Value = validHumidity.Min();
                statsSheet.Cell(4, 3).Value = validHumidity.Max();
                statsSheet.Cell(4, 4).Value = validHumidity.Average();
            }
            
            // Weight stats
            statsSheet.Cell(5, 1).Value = "Weight (kg)";
            if (validWeight.Any())
            {
                statsSheet.Cell(5, 2).Value = validWeight.Min();
                statsSheet.Cell(5, 3).Value = validWeight.Max();
                statsSheet.Cell(5, 4).Value = validWeight.Average();
            }
            
            // Auto-size columns for all sheets
            foreach (var ws in workbook.Worksheets)
            {
                ws.Columns().AdjustToContents();
            }

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
        
        #endregion
    }
} 