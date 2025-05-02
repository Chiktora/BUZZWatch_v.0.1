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
                // Eventually fetch summary data from the API
                // For now, return a basic view
                var viewModel = new AdminDashboardViewModel
                {
                    TotalUsers = 1, // We'll implement this later
                    TotalDevices = 0,
                    ActiveAlerts = 0
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching admin dashboard data");
                return View("Error");
            }
        }
    }

    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalDevices { get; set; }
        public int ActiveAlerts { get; set; }
        public List<AlertSummary> RecentAlerts { get; set; } = new();
        public List<DeviceSummary> Devices { get; set; } = new();
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
} 