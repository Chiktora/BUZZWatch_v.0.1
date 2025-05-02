using BuzzWatch.Web.Models.Api;
using BuzzWatch.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using BuzzWatch.Contracts.Admin;
using ContractsDeviceResponse = BuzzWatch.Contracts.Admin.DeviceResponse;
using ApiCreateDeviceRequest = BuzzWatch.Web.Models.Api.CreateDeviceRequest;
using ApiUpdateDeviceRequest = BuzzWatch.Web.Models.Api.UpdateDeviceRequest;

namespace BuzzWatch.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class DevicesController : Controller
    {
        private readonly ApiClient _apiClient;
        private readonly ILogger<DevicesController> _logger;

        public DevicesController(ApiClient apiClient, ILogger<DevicesController> logger)
        {
            _apiClient = apiClient;
            _logger = logger;
        }

        // GET: /Admin/Devices
        public async Task<IActionResult> Index()
        {
            try
            {
                // Fetch devices from API
                var devicesResponse = await FetchDevicesAsync();
                
                // Map API response to view model
                var devices = devicesResponse?.Select(d => new DeviceViewModel
                {
                    Id = d.Id,
                    Name = d.Name,
                    Location = d.Location,
                    Status = d.Status,
                    BatteryLevel = d.BatteryLevel,
                    LastSeen = d.LastSeen
                }).ToList() ?? new List<DeviceViewModel>();
                
                return View(devices);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching devices");
                
                // Return an empty list as fallback
                return View(new List<DeviceViewModel>());
            }
        }

        // GET: /Admin/Devices/Details/5
        public async Task<IActionResult> Details(Guid id)
        {
            try
            {
                // Fetch device details from API
                var device = await FetchDeviceAsync(id);
                
                if (device == null)
                {
                    return NotFound();
                }
                
                // Map API response to view model
                var viewModel = new DeviceDetailsViewModel
                {
                    Id = device.Id,
                    Name = device.Name,
                    Location = device.Location,
                    Status = device.Status,
                    BatteryLevel = device.BatteryLevel,
                    LastSeen = device.LastSeen,
                    ApiKey = "••••••••" // We wouldn't actually expose the API key in the view model
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching device {DeviceId}", id);
                return NotFound();
            }
        }
        
        // GET: /Admin/Devices/Create
        public IActionResult Create()
        {
            return View();
        }
        
        // POST: /Admin/Devices/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateDeviceViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Map view model to API request
                    var createRequest = new ApiCreateDeviceRequest
                    {
                        Name = model.Name,
                        Location = model.Location
                    };
                    
                    // Send request to API
                    var response = await _apiClient.PostAsJsonAsync("/api/v1/admin/devices", createRequest);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        TempData["SuccessMessage"] = "Device created successfully.";
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
                    _logger.LogError(ex, "Error creating device");
                    ModelState.AddModelError("", "Error creating device. Please try again.");
                }
            }

            return View(model);
        }
        
        // GET: /Admin/Devices/Edit/5
        public async Task<IActionResult> Edit(Guid id)
        {
            try
            {
                // Fetch device from API
                var device = await FetchDeviceAsync(id);
                
                if (device == null)
                {
                    return NotFound();
                }
                
                // Map API response to view model
                var viewModel = new EditDeviceViewModel
                {
                    Id = device.Id,
                    Name = device.Name,
                    Location = device.Location
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching device {DeviceId}", id);
                return NotFound();
            }
        }
        
        // POST: /Admin/Devices/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, EditDeviceViewModel model)
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
                    var updateRequest = new ApiUpdateDeviceRequest
                    {
                        Name = model.Name,
                        Location = model.Location
                    };
                    
                    // Send request to API
                    var response = await _apiClient.PutAsJsonAsync($"/api/v1/admin/devices/{id}", updateRequest);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        TempData["SuccessMessage"] = "Device updated successfully.";
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
                    _logger.LogError(ex, "Error updating device {DeviceId}", id);
                    ModelState.AddModelError("", "Error updating device. Please try again.");
                }
            }

            return View(model);
        }
        
        // POST: /Admin/Devices/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                // Send request to API
                var response = await _apiClient.DeleteAsync($"/api/v1/admin/devices/{id}");
                
                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Device deleted successfully.";
                    return RedirectToAction(nameof(Index));
                }
                
                // Handle error response
                string errorContent = await response.Content.ReadAsStringAsync();
                
                _logger.LogWarning("API returned error: {StatusCode} - {Content} when deleting device {DeviceId}", 
                    response.StatusCode, errorContent, id);
                
                TempData["ErrorMessage"] = $"Error deleting device: {errorContent}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting device {DeviceId}", id);
                TempData["ErrorMessage"] = "Error deleting device. Please try again.";
            }
            
            return RedirectToAction(nameof(Index));
        }
        
        // POST: /Admin/Devices/GenerateApiKey/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateApiKey(Guid id)
        {
            try
            {
                // Send request to API
                var response = await _apiClient.PostAsJsonAsync($"/api/v1/admin/devices/{id}/api-key", new {});
                
                if (response.IsSuccessStatusCode)
                {
                    var apiKeyResponse = await response.Content.ReadFromJsonAsync<ApiKeyResponse>();
                    
                    if (apiKeyResponse != null)
                    {
                        // Store the API key in TempData to show it just once
                        TempData["ApiKey"] = apiKeyResponse.Key;
                        TempData["SuccessMessage"] = "API key generated successfully. Please save it now, you won't be able to see it again.";
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "API key was generated but could not be retrieved.";
                    }
                }
                else
                {
                    // Handle error response
                    string errorContent = await response.Content.ReadAsStringAsync();
                    
                    _logger.LogWarning("API returned error: {StatusCode} - {Content} when generating API key for device {DeviceId}", 
                        response.StatusCode, errorContent, id);
                    
                    TempData["ErrorMessage"] = $"Error generating API key: {errorContent}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating API key for device {DeviceId}", id);
                TempData["ErrorMessage"] = "Error generating API key. Please try again.";
            }
            
            return RedirectToAction(nameof(Details), new { id });
        }
        
        #region API Helper Methods
        
        private async Task<List<ContractsDeviceResponse>?> FetchDevicesAsync()
        {
            try
            {
                var response = await _apiClient.GetAsync("/api/v1/admin/devices");
                
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<List<ContractsDeviceResponse>>();
                }
                
                _logger.LogWarning("API returned status code {StatusCode} when fetching devices", 
                    response.StatusCode);
                
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while fetching devices from API");
                return null;
            }
        }
        
        private async Task<ContractsDeviceResponse?> FetchDeviceAsync(Guid id)
        {
            try
            {
                var response = await _apiClient.GetAsync($"/api/v1/admin/devices/{id}");
                
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<ContractsDeviceResponse>();
                }
                
                _logger.LogWarning("API returned status code {StatusCode} when fetching device {DeviceId}", 
                    response.StatusCode, id);
                
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while fetching device {DeviceId} from API", id);
                return null;
            }
        }
        
        #endregion
    }
    
    #region View Models
    
    public class DeviceViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int BatteryLevel { get; set; }
        public DateTimeOffset LastSeen { get; set; }
    }
    
    public class DeviceDetailsViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int BatteryLevel { get; set; }
        public DateTimeOffset LastSeen { get; set; }
        public string ApiKey { get; set; } = string.Empty;
    }
    
    public class CreateDeviceViewModel
    {
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
    }
    
    public class EditDeviceViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
    }
    
    public class ApiKeyResponse
    {
        public string Key { get; set; } = string.Empty;
        public DateTimeOffset ExpiresAt { get; set; }
    }
    
    #endregion
} 