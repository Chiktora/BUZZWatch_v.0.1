using BuzzWatch.Web.Models.Api;
using BuzzWatch.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;

namespace BuzzWatch.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        private readonly ApiClient _apiClient;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(ApiClient apiClient, ILogger<DashboardController> logger)
        {
            _apiClient = apiClient;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                // Try to fetch data from the API
                var dashboardData = await FetchDashboardDataAsync();
                
                // Map API response to view model
                var viewModel = new AdminDashboardViewModel
                {
                    TotalUsers = dashboardData?.TotalUsers ?? 1,
                    TotalDevices = dashboardData?.TotalDevices ?? 0,
                    ActiveAlerts = dashboardData?.ActiveAlerts ?? 0
                };

                // Map devices if available
                if (dashboardData?.RecentDevices != null && dashboardData.RecentDevices.Any())
                {
                    viewModel.Devices = dashboardData.RecentDevices.Select(d => new DeviceSummary
                    {
                        Id = d.Id,
                        Name = d.Name,
                        Location = d.Location,
                        LastReading = d.LastSeen,
                        IsOnline = d.IsOnline
                    }).ToList();
                }

                // Map alerts if available
                if (dashboardData?.RecentAlerts != null && dashboardData.RecentAlerts.Any())
                {
                    viewModel.RecentAlerts = dashboardData.RecentAlerts.Select(a => new AlertSummary
                    {
                        Id = a.Id,
                        DeviceName = a.DeviceName,
                        Message = a.Message,
                        StartTime = a.Timestamp,
                        IsResolved = a.IsResolved
                    }).ToList();
                }

                // Add chart data
                viewModel.DeviceStatusChart = GetDeviceStatusChartData(dashboardData);
                viewModel.AlertTrendsChart = GetAlertTrendsChartData(dashboardData);
                viewModel.UserActivityChart = GetUserActivityChartData();
                viewModel.SensorReadingsChart = GetSensorReadingsChartData();

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching admin dashboard data");
                
                // Provide a basic view model with some data to display even if the API fails
                var fallbackViewModel = new AdminDashboardViewModel
                {
                    TotalUsers = 1,
                    TotalDevices = 0,
                    ActiveAlerts = 0,
                    DeviceStatusChart = GetDeviceStatusChartData(null),
                    AlertTrendsChart = GetAlertTrendsChartData(null),
                    UserActivityChart = GetUserActivityChartData(),
                    SensorReadingsChart = GetSensorReadingsChartData()
                };
                
                return View(fallbackViewModel);
            }
        }

        private async Task<AdminDashboardResponse?> FetchDashboardDataAsync()
        {
            try
            {
                var response = await _apiClient.GetAsync("/api/v1/admin/dashboard");
                
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<AdminDashboardResponse>();
                }
                
                // If we get a 404 or other error, log it but don't throw an exception
                _logger.LogWarning("API returned status code {StatusCode} when fetching dashboard data", 
                    response.StatusCode);
                
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while fetching dashboard data from API");
                return null;
            }
        }

        private ChartData GetDeviceStatusChartData(AdminDashboardResponse? dashboardData)
        {
            // Default mock data if API doesn't return anything
            int online = 0;
            int offline = 0;
            int needsAttention = 0;

            // Use real data if available
            if (dashboardData?.RecentDevices != null)
            {
                online = dashboardData.RecentDevices.Count(d => d.IsOnline);
                offline = dashboardData.RecentDevices.Count(d => !d.IsOnline);
                // Check for devices with alerts using a safer approach since HasAlerts doesn't exist
                needsAttention = dashboardData.RecentDevices.Count(d => dashboardData.RecentAlerts?.Any(a => a.DeviceId == d.Id) ?? false);
            }
            else
            {
                // Mock data for demo purposes
                online = 7;
                offline = 2;
                needsAttention = 1;
            }

            return new ChartData
            {
                Labels = new[] { "Online", "Offline", "Needs Attention" },
                Datasets = new List<Dataset>
                {
                    new Dataset
                    {
                        Data = new object[] { online, offline, needsAttention },
                        BackgroundColor = "#28a745" // Using the first color only for now
                    }
                }
            };
        }

        private ChartData GetAlertTrendsChartData(AdminDashboardResponse? dashboardData)
        {
            // Generate labels for the last 7 days
            var labels = Enumerable.Range(0, 7)
                .Select(i => DateTime.Now.AddDays(-i).ToString("MMM dd"))
                .Reverse()
                .ToArray();

            // Mock data for demo purposes
            var temperatureAlerts = new[] { 2, 1, 0, 1, 0, 3, 2 };
            var humidityAlerts = new[] { 1, 0, 1, 2, 1, 1, 0 };
            var batteryAlerts = new[] { 0, 1, 0, 0, 1, 0, 1 };

            return new ChartData
            {
                Labels = labels,
                Datasets = new List<Dataset>
                {
                    new Dataset
                    {
                        Label = "Temperature",
                        Data = temperatureAlerts.Cast<object>().ToArray(),
                        BorderColor = "#dc3545",
                        BackgroundColor = "rgba(220, 53, 69, 0.1)",
                        Fill = true
                    },
                    new Dataset
                    {
                        Label = "Humidity",
                        Data = humidityAlerts.Cast<object>().ToArray(),
                        BorderColor = "#0dcaf0",
                        BackgroundColor = "rgba(13, 202, 240, 0.1)",
                        Fill = true
                    },
                    new Dataset
                    {
                        Label = "Battery",
                        Data = batteryAlerts.Cast<object>().ToArray(),
                        BorderColor = "#fd7e14",
                        BackgroundColor = "rgba(253, 126, 20, 0.1)",
                        Fill = true
                    }
                }
            };
        }

        private ChartData GetUserActivityChartData()
        {
            // Generate labels for the last 7 days
            var labels = Enumerable.Range(0, 7)
                .Select(i => DateTime.Now.AddDays(-i).ToString("MMM dd"))
                .Reverse()
                .ToArray();

            // Mock data for demo purposes
            var logins = new[] { 5, 7, 4, 6, 8, 9, 12 };
            var apiCalls = new[] { 15, 22, 18, 25, 30, 28, 32 };

            return new ChartData
            {
                Labels = labels,
                Datasets = new List<Dataset>
                {
                    new Dataset
                    {
                        Label = "Logins",
                        Data = logins.Cast<object>().ToArray(),
                        BorderColor = "#6f42c1",
                        BackgroundColor = "#6f42c1"
                    },
                    new Dataset
                    {
                        Label = "API Calls",
                        Data = apiCalls.Cast<object>().ToArray(),
                        BorderColor = "#20c997",
                        BackgroundColor = "#20c997"
                    }
                }
            };
        }

        private ChartData GetSensorReadingsChartData()
        {
            // Generate hourly labels for the current day
            var labels = Enumerable.Range(0, 12)
                .Select(i => DateTime.Now.AddHours(-i * 2).ToString("HH:00"))
                .Reverse()
                .ToArray();

            // Mock temperature and humidity data for demo
            var random = new Random();
            var temperatures = Enumerable.Range(0, 12)
                .Select(_ => random.Next(18, 28))
                .ToArray();

            var humidity = Enumerable.Range(0, 12)
                .Select(_ => random.Next(40, 70))
                .ToArray();

            return new ChartData
            {
                Labels = labels,
                Datasets = new List<Dataset>
                {
                    new Dataset
                    {
                        Label = "Temperature (°C)",
                        Data = temperatures.Cast<object>().ToArray(),
                        BorderColor = "#dc3545",
                        BackgroundColor = "rgba(220, 53, 69, 0.1)",
                        YAxisID = "y-temperature",
                        Fill = true
                    },
                    new Dataset
                    {
                        Label = "Humidity (%)",
                        Data = humidity.Cast<object>().ToArray(),
                        BorderColor = "#0dcaf0",
                        BackgroundColor = "rgba(13, 202, 240, 0.1)",
                        YAxisID = "y-humidity",
                        Fill = true
                    }
                }
            };
        }
    }

    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalDevices { get; set; }
        public int ActiveAlerts { get; set; }
        public List<AlertSummary> RecentAlerts { get; set; } = new();
        public List<DeviceSummary> Devices { get; set; } = new();
        public ChartData DeviceStatusChart { get; set; } = new();
        public ChartData AlertTrendsChart { get; set; } = new();
        public ChartData UserActivityChart { get; set; } = new();
        public ChartData SensorReadingsChart { get; set; } = new();
    }

    public class AlertSummary
    {
        public Guid Id { get; set; }
        public string DeviceName { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTimeOffset StartTime { get; set; }
        public bool IsResolved { get; set; }
    }

    public class DeviceSummary
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public DateTimeOffset LastReading { get; set; }
        public bool IsOnline { get; set; }
    }

    public class ChartData
    {
        public string[] Labels { get; set; } = Array.Empty<string>();
        public List<Dataset> Datasets { get; set; } = new();
    }

    public class Dataset
    {
        public string? Label { get; set; }
        public object[]? Data { get; set; }
        public string? BackgroundColor { get; set; }
        public string? BorderColor { get; set; }
        public bool Fill { get; set; }
        public string? YAxisID { get; set; }
    }
} 