using BuzzWatch.Web.Models;
using BuzzWatch.Web.Services;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace BuzzWatch.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApiClient _apiClient;

        public HomeController(ILogger<HomeController> logger, ApiClient apiClient)
        {
            _logger = logger;
            _apiClient = apiClient;
        }

        public async Task<IActionResult> Index()
        {
            // For unauthenticated users, just show the landing page
            if (!User.Identity?.IsAuthenticated ?? false)
            {
                return View();
            }

            // For authenticated users, get the overview data
            try
            {
                var overview = new HomeOverviewViewModel();
                
                // Get device counts
                var deviceStats = await _apiClient.GetAsync<DeviceStatisticsDto>("/api/v1/devices/statistics");
                if (deviceStats != null)
                {
                    overview.TotalDevices = deviceStats.Total;
                    overview.OnlineDevices = deviceStats.Online;
                    overview.OfflineDevices = deviceStats.Offline;
                }
                
                // Get alert counts
                var alertStats = await _apiClient.GetAsync<AlertStatisticsDto>("/api/v1/alerts/statistics");
                if (alertStats != null)
                {
                    overview.ActiveAlerts = alertStats.Active;
                    overview.ResolvedAlerts = alertStats.Resolved;
                    overview.TotalAlerts = alertStats.Total;
                }
                
                // Get the device list for the device status table
                var devices = await _apiClient.GetAsync<List<DeviceListItemDto>>("/api/v1/devices");
                if (devices != null)
                {
                    overview.Devices = devices;
                }
                
                // Get recent alerts
                var recentAlerts = await _apiClient.GetAsync<List<AlertListItemDto>>("/api/v1/alerts/recent?count=3");
                if (recentAlerts != null)
                {
                    overview.RecentAlerts = recentAlerts;
                }
                
                // Get weather data if we had a real weather API integration
                // This would be done here, but for now we'll use the static data in the view
                // overview.WeatherData = await _weatherService.GetCurrentWeatherAsync();
                
                return View(overview);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching home overview data");
                return View(new HomeOverviewViewModel());
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
