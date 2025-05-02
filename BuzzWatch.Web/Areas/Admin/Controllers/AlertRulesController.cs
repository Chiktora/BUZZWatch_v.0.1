using BuzzWatch.Web.Models.Api;
using BuzzWatch.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;

namespace BuzzWatch.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class AlertRulesController : Controller
    {
        private readonly ApiClient _apiClient;
        private readonly ILogger<AlertRulesController> _logger;

        public AlertRulesController(ApiClient apiClient, ILogger<AlertRulesController> logger)
        {
            _apiClient = apiClient;
            _logger = logger;
        }

        // GET: /Admin/AlertRules
        public async Task<IActionResult> Index()
        {
            try
            {
                // Fetch alert rules from API
                var alertRulesResponse = await FetchAlertRulesAsync();
                
                // Map API response to view model
                var alertRules = alertRulesResponse?.Select(r => new AlertRuleViewModel
                {
                    Id = r.Id,
                    DeviceName = r.DeviceName,
                    Metric = r.Metric,
                    Operator = r.Operator,
                    Threshold = r.Threshold,
                    DurationSeconds = r.DurationSeconds,
                    IsActive = r.Active
                }).ToList() ?? new List<AlertRuleViewModel>();
                
                return View(alertRules);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching alert rules");
                
                // Return an empty list as fallback
                return View(new List<AlertRuleViewModel>());
            }
        }
        
        // GET: /Admin/AlertRules/Create
        public async Task<IActionResult> Create()
        {
            try
            {
                // Fetch devices for the dropdown
                var devices = await FetchDevicesForDropdownAsync();
                ViewBag.Devices = devices;
                
                // Create new rule
                var model = new CreateAlertRuleViewModel
                {
                    IsActive = true,
                    DurationSeconds = 300 // Default to 5 minutes
                };
                
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error preparing alert rule creation page");
                TempData["ErrorMessage"] = "Error loading device list. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }
        
        // POST: /Admin/AlertRules/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateAlertRuleViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Map view model to API request
                    var createRequest = new CreateAlertRuleRequest
                    {
                        DeviceId = model.DeviceId,
                        Metric = model.Metric,
                        Operator = model.Operator,
                        Threshold = model.Threshold,
                        DurationSeconds = model.DurationSeconds,
                        Active = model.IsActive
                    };
                    
                    // Send request to API
                    var response = await _apiClient.PostAsJsonAsync("/api/v1/admin/alert-rules", createRequest);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        TempData["SuccessMessage"] = "Alert rule created successfully.";
                        return RedirectToAction(nameof(Index));
                    }
                    
                    // Handle error response
                    string errorContent = await response.Content.ReadAsStringAsync();
                    
                    _logger.LogWarning("API returned error: {StatusCode} - {Content}", 
                        response.StatusCode, errorContent);
                    
                    ModelState.AddModelError("", $"Error from API: {errorContent}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating alert rule");
                    ModelState.AddModelError("", "Error creating alert rule. Please try again.");
                }
            }

            // If we get here, there was an error, so reload the device dropdown
            ViewBag.Devices = await FetchDevicesForDropdownAsync();
            return View(model);
        }
        
        // GET: /Admin/AlertRules/Edit/5
        public async Task<IActionResult> Edit(Guid id)
        {
            try
            {
                // Fetch alert rule from API
                var rule = await FetchAlertRuleAsync(id);
                
                if (rule == null)
                {
                    return NotFound();
                }
                
                // Fetch devices for the dropdown
                var devices = await FetchDevicesForDropdownAsync();
                ViewBag.Devices = devices;
                
                // Map API response to view model
                var viewModel = new EditAlertRuleViewModel
                {
                    Id = rule.Id,
                    DeviceId = rule.DeviceId,
                    Metric = rule.Metric,
                    Operator = rule.Operator,
                    Threshold = rule.Threshold,
                    DurationSeconds = rule.DurationSeconds,
                    IsActive = rule.Active
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching alert rule {AlertRuleId}", id);
                return NotFound();
            }
        }
        
        // POST: /Admin/AlertRules/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, EditAlertRuleViewModel model)
        {
            if (id != model.Id)
            {
                return BadRequest();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Map view model to API request
                    var updateRequest = new UpdateAlertRuleRequest
                    {
                        DeviceId = model.DeviceId,
                        Metric = model.Metric,
                        Operator = model.Operator,
                        Threshold = model.Threshold,
                        DurationSeconds = model.DurationSeconds,
                        Active = model.IsActive
                    };
                    
                    // Send request to API
                    var response = await _apiClient.PutAsJsonAsync($"/api/v1/admin/alert-rules/{id}", updateRequest);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        TempData["SuccessMessage"] = "Alert rule updated successfully.";
                        return RedirectToAction(nameof(Index));
                    }
                    
                    // Handle error response
                    string errorContent = await response.Content.ReadAsStringAsync();
                    
                    _logger.LogWarning("API returned error: {StatusCode} - {Content}", 
                        response.StatusCode, errorContent);
                    
                    ModelState.AddModelError("", $"Error from API: {errorContent}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating alert rule {AlertRuleId}", id);
                    ModelState.AddModelError("", "Error updating alert rule. Please try again.");
                }
            }

            // If we get here, there was an error, so reload the device dropdown
            ViewBag.Devices = await FetchDevicesForDropdownAsync();
            return View(model);
        }
        
        // POST: /Admin/AlertRules/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                // Send request to API
                var response = await _apiClient.DeleteAsync($"/api/v1/admin/alert-rules/{id}");
                
                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Alert rule deleted successfully.";
                    return RedirectToAction(nameof(Index));
                }
                
                // Handle error response
                string errorContent = await response.Content.ReadAsStringAsync();
                
                _logger.LogWarning("API returned error: {StatusCode} - {Content} when deleting alert rule {AlertRuleId}", 
                    response.StatusCode, errorContent, id);
                
                TempData["ErrorMessage"] = $"Error deleting alert rule: {errorContent}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting alert rule {AlertRuleId}", id);
                TempData["ErrorMessage"] = "Error deleting alert rule. Please try again.";
            }
            
            return RedirectToAction(nameof(Index));
        }
        
        // GET: /Admin/AlertRules/History
        public async Task<IActionResult> History()
        {
            try
            {
                // Fetch alert history from API
                var alertEventsResponse = await FetchAlertEventsAsync();
                
                // Map API response to view model
                var alertEvents = alertEventsResponse?.Select(e => new AlertEventViewModel
                {
                    Id = e.Id,
                    DeviceName = e.DeviceName,
                    AlertType = e.AlertType,
                    Message = e.Message,
                    Timestamp = e.Timestamp,
                    IsResolved = e.IsResolved
                }).ToList() ?? new List<AlertEventViewModel>();
                
                return View(alertEvents);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching alert history");
                
                // Return an empty list as fallback
                return View(new List<AlertEventViewModel>());
            }
        }
        
        #region API Helper Methods
        
        private async Task<List<AlertRuleResponse>?> FetchAlertRulesAsync()
        {
            try
            {
                var response = await _apiClient.GetAsync("/api/v1/admin/alert-rules");
                
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<List<AlertRuleResponse>>();
                }
                
                _logger.LogWarning("API returned status code {StatusCode} when fetching alert rules", 
                    response.StatusCode);
                
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while fetching alert rules from API");
                return null;
            }
        }
        
        private async Task<AlertRuleResponse?> FetchAlertRuleAsync(Guid id)
        {
            try
            {
                var response = await _apiClient.GetAsync($"/api/v1/admin/alert-rules/{id}");
                
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<AlertRuleResponse>();
                }
                
                _logger.LogWarning("API returned status code {StatusCode} when fetching alert rule {AlertRuleId}", 
                    response.StatusCode, id);
                
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while fetching alert rule {AlertRuleId} from API", id);
                return null;
            }
        }
        
        private async Task<List<AlertEventResponse>?> FetchAlertEventsAsync()
        {
            try
            {
                var response = await _apiClient.GetAsync("/api/v1/admin/alert-events");
                
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<List<AlertEventResponse>>();
                }
                
                _logger.LogWarning("API returned status code {StatusCode} when fetching alert events", 
                    response.StatusCode);
                
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while fetching alert events from API");
                return null;
            }
        }
        
        private async Task<List<DeviceDropdownItem>> FetchDevicesForDropdownAsync()
        {
            try
            {
                var response = await _apiClient.GetAsync("/api/v1/admin/devices");
                
                if (response.IsSuccessStatusCode)
                {
                    var devices = await response.Content.ReadFromJsonAsync<List<DeviceResponse>>();
                    
                    if (devices != null)
                    {
                        return devices.Select(d => new DeviceDropdownItem
                        {
                            Id = d.Id,
                            Name = $"{d.Name} ({d.Location})"
                        }).ToList();
                    }
                }
                
                _logger.LogWarning("API returned status code {StatusCode} when fetching devices for dropdown", 
                    response.StatusCode);
                
                return new List<DeviceDropdownItem>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while fetching devices for dropdown from API");
                return new List<DeviceDropdownItem>();
            }
        }
        
        #endregion
    }
    
    #region View Models
    
    public class AlertRuleViewModel
    {
        public Guid Id { get; set; }
        public string DeviceName { get; set; } = string.Empty;
        public string Metric { get; set; } = string.Empty;
        public string Operator { get; set; } = string.Empty;
        public decimal Threshold { get; set; }
        public int DurationSeconds { get; set; }
        public bool IsActive { get; set; }
    }
    
    public class CreateAlertRuleViewModel
    {
        public Guid DeviceId { get; set; }
        public string Metric { get; set; } = string.Empty;
        public string Operator { get; set; } = string.Empty;
        public decimal Threshold { get; set; }
        public int DurationSeconds { get; set; }
        public bool IsActive { get; set; }
    }
    
    public class EditAlertRuleViewModel
    {
        public Guid Id { get; set; }
        public Guid DeviceId { get; set; }
        public string Metric { get; set; } = string.Empty;
        public string Operator { get; set; } = string.Empty;
        public decimal Threshold { get; set; }
        public int DurationSeconds { get; set; }
        public bool IsActive { get; set; }
    }
    
    public class AlertEventViewModel
    {
        public Guid Id { get; set; }
        public string DeviceName { get; set; } = string.Empty;
        public string AlertType { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTimeOffset Timestamp { get; set; }
        public bool IsResolved { get; set; }
    }
    
    public class DeviceDropdownItem
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
    
    #endregion
    
    #region API Request/Response Models
    
    public class AlertRuleResponse
    {
        public Guid Id { get; set; }
        public Guid DeviceId { get; set; }
        public string DeviceName { get; set; } = string.Empty;
        public string Metric { get; set; } = string.Empty;
        public string Operator { get; set; } = string.Empty;
        public decimal Threshold { get; set; }
        public int DurationSeconds { get; set; }
        public bool Active { get; set; }
    }
    
    public class AlertEventResponse
    {
        public Guid Id { get; set; }
        public string DeviceName { get; set; } = string.Empty;
        public string AlertType { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTimeOffset Timestamp { get; set; }
        public bool IsResolved { get; set; }
    }
    
    public class CreateAlertRuleRequest
    {
        public Guid DeviceId { get; set; }
        public string Metric { get; set; } = string.Empty;
        public string Operator { get; set; } = string.Empty;
        public decimal Threshold { get; set; }
        public int DurationSeconds { get; set; }
        public bool Active { get; set; }
    }
    
    public class UpdateAlertRuleRequest
    {
        public Guid DeviceId { get; set; }
        public string Metric { get; set; } = string.Empty;
        public string Operator { get; set; } = string.Empty;
        public decimal Threshold { get; set; }
        public int DurationSeconds { get; set; }
        public bool Active { get; set; }
    }
    
    #endregion
} 