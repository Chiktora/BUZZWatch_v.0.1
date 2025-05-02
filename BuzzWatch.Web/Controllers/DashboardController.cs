using BuzzWatch.Contracts.Devices;
using BuzzWatch.Contracts.Measurements;
using BuzzWatch.Web.Models;
using BuzzWatch.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BuzzWatch.Web.Controllers
{
    [Authorize]
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
                var devices = await _apiClient.GetAsync<List<DeviceDto>>("/api/v1/devices");
                return View(new DashboardViewModel { Devices = devices ?? new List<DeviceDto>() });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard");
                return View(new DashboardViewModel { Devices = new List<DeviceDto>() });
            }
        }
    }
} 