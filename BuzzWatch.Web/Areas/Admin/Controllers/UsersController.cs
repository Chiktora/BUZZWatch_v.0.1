using BuzzWatch.Web.Models.Api;
using BuzzWatch.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using System.Text.Json;

namespace BuzzWatch.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly ApiClient _apiClient;
        private readonly ILogger<UsersController> _logger;

        public UsersController(ApiClient apiClient, ILogger<UsersController> logger)
        {
            _apiClient = apiClient;
            _logger = logger;
        }

        // GET: /Admin/Users
        public async Task<IActionResult> Index()
        {
            try
            {
                // Fetch users from API
                var usersResponse = await FetchUsersAsync();
                
                // Map API response to view model
                var users = usersResponse?.Select(u => new UserViewModel
                {
                    Id = u.Id,
                    Email = u.Email,
                    Name = u.Name,
                    Role = u.Role,
                    IsActive = u.IsActive
                }).ToList() ?? new List<UserViewModel>();
                
                // If the API request failed, add at least the admin user
                if (users.Count == 0)
                {
                    users.Add(new UserViewModel
                    {
                        Id = Guid.NewGuid(),
                        Email = "admin@local",
                        Name = "Admin User",
                        Role = "Admin",
                        IsActive = true
                    });
                }

                return View(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching users");
                
                // Return a minimal list with just the admin user
                var fallbackUsers = new List<UserViewModel>
                {
                    new UserViewModel
                    {
                        Id = Guid.NewGuid(),
                        Email = "admin@local",
                        Name = "Admin User",
                        Role = "Admin",
                        IsActive = true
                    }
                };
                
                return View(fallbackUsers);
            }
        }

        // GET: /Admin/Users/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Admin/Users/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Map view model to API request
                    var createRequest = new CreateUserRequest
                    {
                        Email = model.Email,
                        Name = model.Name,
                        Password = model.Password,
                        Role = model.Role
                    };
                    
                    // Send request to API
                    var response = await _apiClient.PostAsJsonAsync("/api/v1/admin/users", createRequest);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        TempData["SuccessMessage"] = "User created successfully.";
                        return RedirectToAction(nameof(Index));
                    }
                    
                    // Handle error responses
                    string errorContent = await response.Content.ReadAsStringAsync();
                    
                    _logger.LogWarning("API returned error: {StatusCode} - {Content}", 
                        response.StatusCode, errorContent);
                    
                    ModelState.AddModelError("", $"Error from API: {errorContent}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating user");
                    ModelState.AddModelError("", "Error creating user. Please try again.");
                }
            }

            return View(model);
        }
        
        // GET: /Admin/Users/Edit/5
        public async Task<IActionResult> Edit(Guid id)
        {
            try
            {
                // Fetch user from API
                var userResponse = await FetchUserAsync(id);
                
                if (userResponse == null)
                {
                    return NotFound();
                }
                
                // Map API response to view model
                var user = new EditUserViewModel
                {
                    Id = userResponse.Id,
                    Email = userResponse.Email,
                    Name = userResponse.Name,
                    Role = userResponse.Role,
                    IsActive = userResponse.IsActive
                };

                return View(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching user {UserId}", id);
                return NotFound();
            }
        }

        // POST: /Admin/Users/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, EditUserViewModel model)
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
                    var updateRequest = new UpdateUserRequest
                    {
                        Email = model.Email,
                        Name = model.Name,
                        Role = model.Role,
                        IsActive = model.IsActive,
                        Password = !string.IsNullOrEmpty(model.NewPassword) ? model.NewPassword : null
                    };
                    
                    // Send request to API
                    var response = await _apiClient.PutAsJsonAsync($"/api/v1/admin/users/{id}", updateRequest);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        TempData["SuccessMessage"] = "User updated successfully.";
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
                    _logger.LogError(ex, "Error updating user {UserId}", id);
                    ModelState.AddModelError("", "Error updating user. Please try again.");
                }
            }

            return View(model);
        }
        
        // POST: /Admin/Users/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                // Send request to API
                var response = await _apiClient.DeleteAsync($"/api/v1/admin/users/{id}");
                
                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "User deleted successfully.";
                    return RedirectToAction(nameof(Index));
                }
                
                // Handle error response
                string errorContent = await response.Content.ReadAsStringAsync();
                
                _logger.LogWarning("API returned error: {StatusCode} - {Content} when deleting user {UserId}", 
                    response.StatusCode, errorContent, id);
                
                TempData["ErrorMessage"] = $"Error deleting user: {errorContent}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {UserId}", id);
                TempData["ErrorMessage"] = "Error deleting user. Please try again.";
            }
            
            return RedirectToAction(nameof(Index));
        }

        // GET: /Admin/Users/Devices/5
        public async Task<IActionResult> Devices(Guid id)
        {
            try
            {
                // Fetch user details
                var user = await FetchUserAsync(id);
                if (user == null)
                {
                    return NotFound();
                }
                
                // Fetch user's device permissions
                var userDevices = await FetchUserDevicePermissionsAsync(id);
                
                // Fetch all available devices 
                var allDevices = await FetchDevicesAsync();
                
                // Create a list that includes all devices, marking which ones the user has access to
                var devices = new List<UserDevicePermission>();
                
                // If we have both device lists
                if (allDevices != null && userDevices != null)
                {
                    // First add the devices the user already has access to
                    foreach (var device in userDevices)
                    {
                        devices.Add(new UserDevicePermission
                        {
                            DeviceId = device.DeviceId,
                            DeviceName = device.DeviceName,
                            DeviceLocation = device.DeviceLocation,
                            HasAccess = device.HasAccess,
                            CanManage = device.CanManage
                        });
                    }
                    
                    // Then add devices the user doesn't have any permissions for
                    foreach (var device in allDevices)
                    {
                        if (!devices.Any(d => d.DeviceId == device.Id))
                        {
                            devices.Add(new UserDevicePermission
                            {
                                DeviceId = device.Id,
                                DeviceName = device.Name,
                                DeviceLocation = device.Location,
                                HasAccess = false,
                                CanManage = false
                            });
                        }
                    }
                }
                
                // Create the view model
                var model = new UserDevicesViewModel
                {
                    UserId = id,
                    UserName = user.Name,
                    UserEmail = user.Email,
                    Devices = devices
                };
                
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving devices for user {UserId}", id);
                TempData["ErrorMessage"] = "Error retrieving device permissions.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: /Admin/Users/AssignDevice
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignDevice(AssignDeviceViewModel model)
        {
            try
            {
                // Map view model to API request
                var updateRequest = new UpdateUserDeviceRequest
                {
                    UserId = model.UserId,
                    DeviceId = model.DeviceId,
                    HasAccess = model.HasAccess,
                    CanManage = model.CanManage
                };
                
                // Send request to API
                var response = await _apiClient.PostAsJsonAsync("/api/v1/admin/user-devices", updateRequest);
                
                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Device permissions updated successfully.";
                }
                else
                {
                    // Handle error response
                    string errorContent = await response.Content.ReadAsStringAsync();
                    
                    _logger.LogWarning("API returned error: {StatusCode} - {Content} when updating device permissions", 
                        response.StatusCode, errorContent);
                    
                    TempData["ErrorMessage"] = $"Error updating device permissions: {errorContent}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning device {DeviceId} to user {UserId}", 
                    model.DeviceId, model.UserId);
                TempData["ErrorMessage"] = "Error updating device permissions.";
            }
            
            return RedirectToAction(nameof(Devices), new { id = model.UserId });
        }
        
        // POST: /Admin/Users/RemoveDevice
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveDevice(Guid userId, Guid deviceId)
        {
            try
            {
                // Send request to API
                var response = await _apiClient.DeleteAsync($"/api/v1/admin/user-devices?userId={userId}&deviceId={deviceId}");
                
                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = "Device access removed successfully.";
                }
                else
                {
                    // Handle error response
                    string errorContent = await response.Content.ReadAsStringAsync();
                    
                    _logger.LogWarning("API returned error: {StatusCode} - {Content} when removing device access", 
                        response.StatusCode, errorContent);
                    
                    TempData["ErrorMessage"] = $"Error removing device access: {errorContent}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing device {DeviceId} from user {UserId}", 
                    deviceId, userId);
                TempData["ErrorMessage"] = "Error removing device access.";
            }
            
            return RedirectToAction(nameof(Devices), new { id = userId });
        }
        
        #region API Helper Methods
        
        private async Task<List<UserResponse>?> FetchUsersAsync()
        {
            try
            {
                var response = await _apiClient.GetAsync("/api/v1/admin/users");
                
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<List<UserResponse>>();
                }
                
                _logger.LogWarning("API returned status code {StatusCode} when fetching users", 
                    response.StatusCode);
                
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while fetching users from API");
                return null;
            }
        }
        
        private async Task<UserResponse?> FetchUserAsync(Guid id)
        {
            try
            {
                var response = await _apiClient.GetAsync($"/api/v1/admin/users/{id}");
                
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<UserResponse>();
                }
                
                _logger.LogWarning("API returned status code {StatusCode} when fetching user {UserId}", 
                    response.StatusCode, id);
                
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while fetching user {UserId} from API", id);
                return null;
            }
        }
        
        private async Task<List<UserDeviceResponse>?> FetchUserDevicePermissionsAsync(Guid userId)
        {
            try
            {
                var response = await _apiClient.GetAsync($"/api/v1/admin/user-devices?userId={userId}");
                
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<List<UserDeviceResponse>>();
                }
                
                _logger.LogWarning("API returned status code {StatusCode} when fetching device permissions for user {UserId}", 
                    response.StatusCode, userId);
                
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while fetching device permissions for user {UserId} from API", userId);
                return null;
            }
        }
        
        private async Task<List<DeviceResponse>?> FetchDevicesAsync()
        {
            try
            {
                var response = await _apiClient.GetAsync("/api/v1/admin/devices");
                
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<List<DeviceResponse>>();
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
        
        #endregion
    }

    public class UserViewModel
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class CreateUserViewModel
    {
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }

    public class EditUserViewModel
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string? NewPassword { get; set; }
        public string? ConfirmPassword { get; set; }
    }

    public class UserDevicesViewModel
    {
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public List<UserDevicePermission> Devices { get; set; } = new();
    }

    public class UserDevicePermission
    {
        public Guid DeviceId { get; set; }
        public string DeviceName { get; set; } = string.Empty;
        public string DeviceLocation { get; set; } = string.Empty;
        public bool HasAccess { get; set; }
        public bool CanManage { get; set; }
    }

    public class AssignDeviceViewModel
    {
        public Guid UserId { get; set; }
        public Guid DeviceId { get; set; }
        public bool HasAccess { get; set; }
        public bool CanManage { get; set; }
    }
} 