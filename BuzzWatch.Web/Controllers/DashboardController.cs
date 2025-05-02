using BuzzWatch.Contracts.Devices;
using BuzzWatch.Contracts.Measurements;
using BuzzWatch.Contracts.Alerts;
using BuzzWatch.Web.Models;
using BuzzWatch.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

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
                var viewModel = new DashboardViewModel();
                
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
                
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading device details for {DeviceId}", id);
                return RedirectToAction(nameof(Index));
            }
        }
    }
} 