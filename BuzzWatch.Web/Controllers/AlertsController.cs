using BuzzWatch.Contracts.Alerts;
using BuzzWatch.Contracts.Devices;
using BuzzWatch.Web.Models;
using BuzzWatch.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BuzzWatch.Web.Controllers
{
    [Authorize]
    public class AlertsController : Controller
    {
        private readonly ApiClient _apiClient;
        private readonly ILogger<AlertsController> _logger;

        public AlertsController(ApiClient apiClient, ILogger<AlertsController> logger)
        {
            _apiClient = apiClient;
            _logger = logger;
        }

        public async Task<IActionResult> Index(string severity = null, string status = null)
        {
            try
            {
                string queryString = "/api/v1/alerts";
                
                // Add query parameters if specified
                var queryParams = new List<string>();
                if (!string.IsNullOrEmpty(severity))
                {
                    queryParams.Add($"severity={severity}");
                }
                
                if (!string.IsNullOrEmpty(status))
                {
                    queryParams.Add($"status={status}");
                }
                
                if (queryParams.Any())
                {
                    queryString += "?" + string.Join("&", queryParams);
                }
                
                var alerts = await _apiClient.GetAsync<List<AlertDto>>(queryString);
                var devices = await _apiClient.GetAsync<List<DeviceDto>>("/api/v1/devices");
                
                var viewModel = new AlertsViewModel
                {
                    Alerts = alerts ?? new List<AlertDto>(),
                    Devices = devices ?? new List<DeviceDto>(),
                    SelectedSeverity = severity,
                    SelectedStatus = status
                };
                
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading alerts");
                return View(new AlertsViewModel());
            }
        }
        
        [HttpPost]
        public async Task<IActionResult> Acknowledge(Guid id)
        {
            try
            {
                await _apiClient.PostAsync<object>($"/api/v1/alerts/{id}/acknowledge", null);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error acknowledging alert {AlertId}", id);
                return RedirectToAction(nameof(Index));
            }
        }
        
        [HttpPost]
        public async Task<IActionResult> Resolve(Guid id)
        {
            try
            {
                await _apiClient.PostAsync<object>($"/api/v1/alerts/{id}/resolve", null);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resolving alert {AlertId}", id);
                return RedirectToAction(nameof(Index));
            }
        }
        
        [HttpPost]
        public async Task<IActionResult> ResolveAll()
        {
            try
            {
                await _apiClient.PostAsync<object>("/api/v1/alerts/resolve-all", null);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resolving all alerts");
                return RedirectToAction(nameof(Index));
            }
        }
    }
} 