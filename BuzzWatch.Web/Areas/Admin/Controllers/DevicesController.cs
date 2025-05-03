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
        public async Task<IActionResult> Index(string? search = null, string? status = null, string? location = null)
        {
            try
            {
                // Save filter values in ViewBag for form retention
                ViewBag.CurrentSearch = search;
                ViewBag.CurrentStatus = status;
                ViewBag.CurrentLocation = location;
                
                // Fetch devices from API
                var devicesResponse = await FetchDevicesAsync(search, status, location);
                
                // Get unique locations for filter dropdown
                var locations = devicesResponse?
                    .Select(d => d.Location)
                    .Where(l => !string.IsNullOrEmpty(l))
                    .Distinct()
                    .OrderBy(l => l)
                    .ToList() ?? new List<string>();
                
                ViewBag.Locations = locations;
                
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
        
        // GET: /Admin/Devices/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                // Fetch devices from API
                var devicesResponse = await FetchDevicesAsync();
                
                // Create dashboard statistics
                var dashboard = new DeviceDashboardViewModel
                {
                    TotalDevices = devicesResponse?.Count ?? 0,
                    OnlineDevices = devicesResponse?.Count(d => d.Status.ToLower() == "online") ?? 0,
                    LowBatteryDevices = devicesResponse?.Count(d => d.BatteryLevel < 20) ?? 0,
                    OfflineDevices = devicesResponse?.Count(d => d.Status.ToLower() == "offline") ?? 0,
                    RecentlyActiveDevices = devicesResponse?
                        .Where(d => d.LastSeen > DateTimeOffset.UtcNow.AddHours(-24))
                        .Count() ?? 0,
                    // Group devices by location
                    DevicesByLocation = devicesResponse?
                        .GroupBy(d => d.Location)
                        .Select(g => new DeviceLocationGroup 
                        { 
                            Location = g.Key, 
                            DeviceCount = g.Count(),
                            OnlineCount = g.Count(d => d.Status.ToLower() == "online"),
                            OfflineCount = g.Count(d => d.Status.ToLower() == "offline")
                        })
                        .OrderByDescending(g => g.DeviceCount)
                        .ToList() ?? new List<DeviceLocationGroup>(),
                    // Most recent devices
                    RecentDevices = devicesResponse?
                        .OrderByDescending(d => d.LastSeen)
                        .Take(5)
                        .Select(d => new DeviceViewModel
                        {
                            Id = d.Id,
                            Name = d.Name,
                            Location = d.Location,
                            Status = d.Status,
                            BatteryLevel = d.BatteryLevel,
                            LastSeen = d.LastSeen
                        })
                        .ToList() ?? new List<DeviceViewModel>(),
                    // Low battery devices
                    LowBatteryDevicesList = devicesResponse?
                        .Where(d => d.BatteryLevel < 20)
                        .OrderBy(d => d.BatteryLevel)
                        .Take(5)
                        .Select(d => new DeviceViewModel
                        {
                            Id = d.Id,
                            Name = d.Name,
                            Location = d.Location,
                            Status = d.Status,
                            BatteryLevel = d.BatteryLevel,
                            LastSeen = d.LastSeen
                        })
                        .ToList() ?? new List<DeviceViewModel>()
                };
                
                return View(dashboard);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching device dashboard data");
                
                // Return empty dashboard as fallback
                return View(new DeviceDashboardViewModel());
            }
        }
        
        // GET: /Admin/Devices/BulkOperations
        public async Task<IActionResult> BulkOperations()
        {
            try
            {
                // Fetch devices from API
                var devicesResponse = await FetchDevicesAsync();
                
                // Get unique locations for grouping
                var locations = devicesResponse?
                    .Select(d => d.Location)
                    .Where(l => !string.IsNullOrEmpty(l))
                    .Distinct()
                    .OrderBy(l => l)
                    .ToList() ?? new List<string>();
                
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
                
                // Create bulk operations model
                var model = new BulkOperationsViewModel
                {
                    Devices = devices,
                    Locations = locations,
                    SelectedOperation = "none"
                };
                
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading bulk operations page");
                TempData["ErrorMessage"] = "Error loading devices for bulk operations.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: /Admin/Devices/BulkOperations
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkOperations(BulkOperationsViewModel model)
        {
            if (model.SelectedDeviceIds == null || !model.SelectedDeviceIds.Any())
            {
                TempData["ErrorMessage"] = "Please select at least one device.";
                return RedirectToAction(nameof(BulkOperations));
            }
            
            try
            {
                string resultMessage;
                bool success = true;
                
                switch (model.SelectedOperation)
                {
                    case "updateLocation":
                        if (string.IsNullOrEmpty(model.NewLocation))
                        {
                            TempData["ErrorMessage"] = "Please enter a new location.";
                            return RedirectToAction(nameof(BulkOperations));
                        }
                        
                        (success, resultMessage) = await UpdateDeviceLocationsAsync(model.SelectedDeviceIds, model.NewLocation);
                        break;
                        
                    case "delete":
                        (success, resultMessage) = await DeleteDevicesAsync(model.SelectedDeviceIds);
                        break;
                        
                    default:
                        resultMessage = "No operation selected.";
                        success = false;
                        break;
                }
                
                if (success)
                {
                    TempData["SuccessMessage"] = resultMessage;
                }
                else
                {
                    TempData["ErrorMessage"] = resultMessage;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing bulk operation");
                TempData["ErrorMessage"] = "An error occurred while performing the bulk operation.";
            }
            
            return RedirectToAction(nameof(BulkOperations));
        }

        private async Task<(bool success, string message)> UpdateDeviceLocationsAsync(List<Guid> deviceIds, string newLocation)
        {
            int successCount = 0;
            int failCount = 0;
            
            foreach (var deviceId in deviceIds)
            {
                try
                {
                    // Get the device from API
                    var device = await FetchDeviceAsync(deviceId);
                    
                    if (device == null)
                    {
                        failCount++;
                        continue;
                    }
                    
                    // Update device
                    var updateRequest = new ApiUpdateDeviceRequest
                    {
                        Name = device.Name,
                        Location = newLocation
                    };
                    
                    var response = await _apiClient.PutAsJsonAsync($"/api/v1/admin/devices/{deviceId}", updateRequest);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        successCount++;
                    }
                    else
                    {
                        failCount++;
                        _logger.LogWarning("Failed to update device {DeviceId}. Status: {Status}", 
                            deviceId, response.StatusCode);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating device {DeviceId}", deviceId);
                    failCount++;
                }
            }
            
            if (failCount == 0)
            {
                return (true, $"Successfully updated {successCount} device(s) to location '{newLocation}'.");
            }
            else if (successCount > 0)
            {
                return (true, $"Partially successful: Updated {successCount} device(s), but failed to update {failCount} device(s).");
            }
            else
            {
                return (false, "Failed to update any devices.");
            }
        }

        private async Task<(bool success, string message)> DeleteDevicesAsync(List<Guid> deviceIds)
        {
            int successCount = 0;
            int failCount = 0;
            
            foreach (var deviceId in deviceIds)
            {
                try
                {
                    var response = await _apiClient.DeleteAsync($"/api/v1/admin/devices/{deviceId}");
                    
                    if (response.IsSuccessStatusCode)
                    {
                        successCount++;
                    }
                    else
                    {
                        failCount++;
                        _logger.LogWarning("Failed to delete device {DeviceId}. Status: {Status}", 
                            deviceId, response.StatusCode);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deleting device {DeviceId}", deviceId);
                    failCount++;
                }
            }
            
            if (failCount == 0)
            {
                return (true, $"Successfully deleted {successCount} device(s).");
            }
            else if (successCount > 0)
            {
                return (true, $"Partially successful: Deleted {successCount} device(s), but failed to delete {failCount} device(s).");
            }
            else
            {
                return (false, "Failed to delete any devices.");
            }
        }
        
        #region API Helper Methods
        
        private async Task<List<ContractsDeviceResponse>?> FetchDevicesAsync(string? search = null, string? status = null, string? location = null)
        {
            try
            {
                // Build query string with filters
                var queryParams = new List<string>();
                if (!string.IsNullOrEmpty(search))
                    queryParams.Add($"search={Uri.EscapeDataString(search)}");
                if (!string.IsNullOrEmpty(status))
                    queryParams.Add($"status={Uri.EscapeDataString(status)}");
                if (!string.IsNullOrEmpty(location))
                    queryParams.Add($"location={Uri.EscapeDataString(location)}");
                
                string queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
                
                // Make the API request with filters
                var response = await _apiClient.GetAsync($"/api/v1/admin/devices{queryString}");
                
                if (response.IsSuccessStatusCode)
                {
                    var devices = await response.Content.ReadFromJsonAsync<List<ContractsDeviceResponse>>();
                    return devices;
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
    
    public class DeviceDashboardViewModel
    {
        public int TotalDevices { get; set; }
        public int OnlineDevices { get; set; }
        public int OfflineDevices { get; set; }
        public int LowBatteryDevices { get; set; }
        public int RecentlyActiveDevices { get; set; }
        public List<DeviceLocationGroup> DevicesByLocation { get; set; } = new();
        public List<DeviceViewModel> RecentDevices { get; set; } = new();
        public List<DeviceViewModel> LowBatteryDevicesList { get; set; } = new();
    }
    
    public class DeviceLocationGroup
    {
        public string Location { get; set; } = string.Empty;
        public int DeviceCount { get; set; }
        public int OnlineCount { get; set; }
        public int OfflineCount { get; set; }
    }
    
    public class BulkOperationsViewModel
    {
        public List<DeviceViewModel> Devices { get; set; } = new();
        public List<string> Locations { get; set; } = new();
        public List<Guid> SelectedDeviceIds { get; set; } = new();
        public string SelectedOperation { get; set; } = string.Empty;
        public string NewLocation { get; set; } = string.Empty;
    }
    
    #endregion
}