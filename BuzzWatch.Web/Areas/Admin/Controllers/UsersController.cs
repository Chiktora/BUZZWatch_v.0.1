using BuzzWatch.Web.Models.Api;
using BuzzWatch.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using System.Text.Json;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

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
        public async Task<IActionResult> Index(string? search = null, string? role = null, bool? isActive = null)
        {
            try
            {
                // Store filter values in ViewBag for form retention
                ViewBag.CurrentSearch = search;
                ViewBag.CurrentRole = role;
                ViewBag.CurrentIsActive = isActive;
                
                // Fetch available roles for the filter dropdown
                ViewBag.Roles = await FetchRolesAsync();
                
                // Fetch users from API with filters
                var usersResponse = await FetchUsersAsync(search, role, isActive);
                
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
                if (users.Count == 0 && string.IsNullOrEmpty(search) && string.IsNullOrEmpty(role) && !isActive.HasValue)
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
                
                // Add a note about role changes and authentication
                ViewBag.AdminNote = "Note: When changing a user's role, they must log out and log back in for the changes to take effect.";

                return View(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching users");
                
                // Return a minimal list with just the admin user if there are no filters
                if (string.IsNullOrEmpty(search) && string.IsNullOrEmpty(role) && !isActive.HasValue)
                {
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
                
                // Return empty list for filtered searches that fail
                return View(new List<UserViewModel>());
            }
        }

        // GET: /Admin/Users/Create
        public async Task<IActionResult> Create()
        {
            // Fetch available roles from API
            var roles = await FetchRolesAsync();
            ViewBag.Roles = roles;
            
            return View();
        }

        // POST: /Admin/Users/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Additional validations
                if (string.IsNullOrEmpty(model.Role))
                {
                    ModelState.AddModelError("Role", "Role is required");
                    
                    // Fetch roles for the view again
                    var roles = await FetchRolesAsync();
                    ViewBag.Roles = roles;
                    
                    return View(model);
                }

                if (!model.Password.Equals(model.ConfirmPassword))
                {
                    ModelState.AddModelError("ConfirmPassword", "Passwords do not match");
                    
                    // Fetch roles for the view again
                    var roles = await FetchRolesAsync();
                    ViewBag.Roles = roles;
                    
                    return View(model);
                }

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
                    
                    // Log the request content
                    _logger.LogInformation("Sending create user request: {Request}", 
                        System.Text.Json.JsonSerializer.Serialize(createRequest));
                    
                    // Send request to API
                    var response = await _apiClient.PostAsJsonAsync("api/v1/admin/users", createRequest);
                    
                    // Log the response details
                    _logger.LogInformation("Create user response: {StatusCode} {ReasonPhrase}", 
                        response.StatusCode, response.ReasonPhrase);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        TempData["SuccessMessage"] = "User created successfully.";
                        return RedirectToAction(nameof(Index));
                    }
                    
                    // Handle error responses
                    string errorContent = await response.Content.ReadAsStringAsync();
                    
                    _logger.LogWarning("API returned error: {StatusCode} - {Content}", 
                        response.StatusCode, errorContent);
                    
                    // Try to extract a more readable error message
                    try
                    {
                        // If it's a JSON array of error messages (ASP.NET Identity format)
                        if (errorContent.StartsWith("[") && errorContent.EndsWith("]"))
                        {
                            var errors = System.Text.Json.JsonSerializer.Deserialize<List<string>>(errorContent);
                            foreach (var error in errors ?? new List<string>())
                            {
                                ModelState.AddModelError("", error);
                            }
                        }
                        else 
                        {
                            // Handle simple string error message
                            ModelState.AddModelError("", $"Error from API: {errorContent.Trim('"')}");
                        }
                    }
                    catch
                    {
                        // Fall back to raw error content if JSON parsing fails
                        ModelState.AddModelError("", $"Error from API: {errorContent}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating user");
                    ModelState.AddModelError("", "Error creating user. Please try again.");
                }
            }
            else
            {
                // Log model validation errors
                foreach (var state in ModelState)
                {
                    foreach (var error in state.Value.Errors)
                    {
                        _logger.LogWarning("Model validation error for {Property}: {Error}", 
                            state.Key, error.ErrorMessage);
                    }
                }
            }

            // Fetch roles for the view again after an error
            var rolesForView = await FetchRolesAsync();
            ViewBag.Roles = rolesForView;
            
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
                
                // Fetch available roles from API
                var roles = await FetchRolesAsync();
                ViewBag.Roles = roles;
                
                // Check if this is the current user (email matches from current principal)
                bool isCurrentUser = User.Identity?.Name == userResponse.Email;
                if (isCurrentUser)
                {
                    ViewBag.SelfEditWarning = "Warning: You are editing your own account. You cannot change your admin role or deactivate your own account.";
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
                    // Get the current user data to check if role is being changed
                    var currentUser = await FetchUserAsync(id);
                    bool isRoleChanged = currentUser != null && currentUser.Role != model.Role;
                    
                    // Validate password fields if provided
                    if (!string.IsNullOrEmpty(model.NewPassword))
                    {
                        if (string.IsNullOrEmpty(model.ConfirmPassword))
                        {
                            ModelState.AddModelError("ConfirmPassword", "Confirm Password is required when setting a new password");
                            
                            // Fetch roles for the view
                            var roles = await FetchRolesAsync();
                            ViewBag.Roles = roles;
                            
                            return View(model);
                        }
                        
                        if (model.NewPassword != model.ConfirmPassword)
                        {
                            ModelState.AddModelError("ConfirmPassword", "The new password and confirmation do not match");
                            
                            // Fetch roles for the view
                            var roles = await FetchRolesAsync();
                            ViewBag.Roles = roles;
                            
                            return View(model);
                        }
                    }
                    
                    // Map view model to API request
                    var updateRequest = new UpdateUserRequest
                    {
                        Email = model.Email,
                        Name = model.Name,
                        Role = model.Role,
                        IsActive = model.IsActive,
                        Password = !string.IsNullOrEmpty(model.NewPassword) ? model.NewPassword : null
                    };
                    
                    // Log the request data (sanitizing password)
                    var requestToLog = new 
                    { 
                        updateRequest.Email, 
                        updateRequest.Name, 
                        updateRequest.Role, 
                        updateRequest.IsActive, 
                        HasPassword = !string.IsNullOrEmpty(updateRequest.Password) 
                    };
                    _logger.LogInformation("Sending update request for user {UserId}: {@UpdateRequest}", 
                        id, requestToLog);
                    
                    // Send request to API using PutAsJsonAsync like we do for Create
                    var response = await _apiClient.PutAsJsonAsync($"api/v1/admin/users/{id}", updateRequest);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        TempData["SuccessMessage"] = "User updated successfully.";
                        
                        // Add a note about role changes requiring re-login
                        if (isRoleChanged)
                        {
                            TempData["InfoMessage"] = "Role was changed. The user will need to log out and log back in for the new permissions to take effect.";
                        }
                        
                        return RedirectToAction(nameof(Index));
                    }
                    
                    // Handle error response
                    string errorContent = await response.Content.ReadAsStringAsync();
                    
                    _logger.LogWarning("API returned error: {StatusCode} - {Content} when updating user {UserId}", 
                        response.StatusCode, errorContent, id);
                    
                    // Log complete request details for troubleshooting
                    _logger.LogError("Failed update request details - URL: {Url}, Headers: {@Headers}", 
                        response.RequestMessage?.RequestUri,
                        response.RequestMessage?.Headers);
                    
                    // Show special warning for self-editing errors
                    if (errorContent.Contains("own admin role") || errorContent.Contains("own account"))
                    {
                        TempData["ErrorMessage"] = errorContent.Trim('"');
                        return RedirectToAction(nameof(Edit), new { id });
                    }
                    
                    // Try to extract a more readable error message
                    try 
                    {
                        // If it's a JSON response, try to deserialize it
                        if (errorContent.StartsWith("{") || errorContent.StartsWith("["))
                        {
                            var jsonDocument = JsonDocument.Parse(errorContent);
                            if (jsonDocument.RootElement.ValueKind == JsonValueKind.Array)
                            {
                                var errors = JsonSerializer.Deserialize<List<string>>(errorContent);
                                foreach (var error in errors ?? new List<string>())
                                {
                                    ModelState.AddModelError("", error);
                                }
                            }
                            else if (jsonDocument.RootElement.TryGetProperty("message", out var messageElement))
                            {
                                ModelState.AddModelError("", messageElement.GetString() ?? errorContent);
                            }
                            else
                            {
                                ModelState.AddModelError("", $"Error from API: {errorContent}");
                            }
                        }
                        else
                        {
                            ModelState.AddModelError("", $"Error from API: {errorContent}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error parsing API error response");
                        ModelState.AddModelError("", $"Error from API: {errorContent}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating user {UserId}", id);
                    ModelState.AddModelError("", "Error updating user. Please try again.");
                }
            }

            // Fetch roles for the view again after an error
            var rolesForView = await FetchRolesAsync();
            ViewBag.Roles = rolesForView;
            
            return View(model);
        }
        
        // POST: /Admin/Users/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                // Check if the user is trying to delete themselves
                var currentUserEmail = User.Identity?.Name;
                var userToDelete = await FetchUserAsync(id);
                
                if (userToDelete != null && currentUserEmail != null && 
                    userToDelete.Email.Equals(currentUserEmail, StringComparison.OrdinalIgnoreCase))
                {
                    TempData["ErrorMessage"] = "You cannot delete your own account.";
                    return RedirectToAction(nameof(Index));
                }
                
                // Send request to API
                var response = await _apiClient.DeleteAsync($"api/v1/admin/users/{id}");
                
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
                var response = await _apiClient.DeleteAsync($"api/v1/admin/user-devices?userId={userId}&deviceId={deviceId}");
                
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
        
        // GET: /Admin/Users/TestCreateUser
        public async Task<IActionResult> TestCreateUser()
        {
            try
            {
                // Create a test user request
                var createRequest = new CreateUserRequest
                {
                    Email = "test.user@example.com",
                    Name = "Test User",
                    Password = "TestPass123!",
                    Role = "User"
                };
                
                // Send request to API and get raw response
                var response = await _apiClient.PostAsJsonAsync("api/v1/admin/users", createRequest);
                
                // Read the full content regardless of status code
                var content = await response.Content.ReadAsStringAsync();
                
                // Display request details and response
                return Content(
                    $"Status: {response.StatusCode} {response.ReasonPhrase}\n\n" +
                    $"Request URL: {response.RequestMessage?.RequestUri}\n\n" +
                    $"Request Content: {System.Text.Json.JsonSerializer.Serialize(createRequest)}\n\n" +
                    $"Response Body: {content}",
                    "text/plain"
                );
            }
            catch (Exception ex)
            {
                return Content($"Error: {ex.Message}\n\n{ex.StackTrace}", "text/plain");
            }
        }
        
        // GET: /Admin/Users/TestUpdateUser/{id}
        public async Task<IActionResult> TestUpdateUser(Guid id)
        {
            try
            {
                // First get the current user details
                var user = await FetchUserAsync(id);
                if (user == null)
                {
                    return Content("User not found", "text/plain");
                }
                
                // Create a test update request with minimal changes
                var updateRequest = new UpdateUserRequest
                {
                    Email = user.Email,
                    Name = user.Name,
                    Role = user.Role,
                    IsActive = user.IsActive,
                    Password = null  // Don't change password
                };
                
                // Send request using the ApiClient's PutAsJsonAsync method
                var response = await _apiClient.PutAsJsonAsync($"api/v1/admin/users/{id}", updateRequest);
                
                // Read the full content regardless of status code
                var responseContent = await response.Content.ReadAsStringAsync();
                
                // Display request details and response
                return Content(
                    $"Status: {response.StatusCode} {response.ReasonPhrase}\n\n" +
                    $"Request URL: {response.RequestMessage?.RequestUri}\n\n" +
                    $"Request Content: {JsonSerializer.Serialize(updateRequest)}\n\n" +
                    $"Response Body: {responseContent}",
                    "text/plain"
                );
            }
            catch (Exception ex)
            {
                return Content($"Error: {ex.Message}\n\n{ex.StackTrace}", "text/plain");
            }
        }
        
        // GET: /Admin/Users/TestUpdateUserDirect/{id}
        public async Task<IActionResult> TestUpdateUserDirect(Guid id)
        {
            try
            {
                // First get the current user details
                var user = await FetchUserAsync(id);
                if (user == null)
                {
                    return Content("User not found", "text/plain");
                }
                
                // Create a test update request with minimal changes
                var updateRequest = new UpdateUserRequest
                {
                    Email = user.Email,
                    Name = user.Name + " (Updated)",  // Make a small visible change
                    Role = user.Role,
                    IsActive = user.IsActive,
                    Password = null  // Don't change password
                };
                
                // Directly use HttpClient to bypass any potential ApiClient issues
                using var httpClient = new HttpClient();
                
                // Get the token from the current client
                var token = HttpContext.Session.GetString("JwtToken");
                if (!string.IsNullOrEmpty(token))
                {
                    httpClient.DefaultRequestHeaders.Authorization = 
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                }
                
                // Get the base address from the ApiClient service URL
                var baseAddress = new Uri("https://localhost:7116/");
                var requestUri = new Uri(baseAddress, $"api/v1/admin/users/{id}");
                
                // Serialize with camelCase
                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                var json = JsonSerializer.Serialize(updateRequest, jsonOptions);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                
                // Make the request
                var response = await httpClient.PutAsync(requestUri, content);
                
                // Read the response
                var responseContent = await response.Content.ReadAsStringAsync();
                
                // Display all details
                return Content(
                    $"Status: {response.StatusCode} {response.ReasonPhrase}\n\n" +
                    $"Request URL: {requestUri}\n\n" +
                    $"Authorization: {httpClient.DefaultRequestHeaders.Authorization}\n\n" +
                    $"Request Content: {json}\n\n" +
                    $"Response Body: {responseContent}",
                    "text/plain"
                );
            }
            catch (Exception ex)
            {
                return Content($"Error: {ex.Message}\n\n{ex.StackTrace}", "text/plain");
            }
        }
        
        // GET: /Admin/Users/TestAdminAccess
        public IActionResult TestAdminAccess()
        {
            var output = new StringBuilder();
            
            // Get current user identity
            var username = User.Identity?.Name;
            var isAuthenticated = User.Identity?.IsAuthenticated ?? false;
            
            output.AppendLine($"Username: {username ?? "not set"}");
            output.AppendLine($"IsAuthenticated: {isAuthenticated}");
            output.AppendLine();
            
            // Check roles
            var isAdmin = User.IsInRole("Admin");
            var isUser = User.IsInRole("User");
            var isReadOnly = User.IsInRole("ReadOnly");
            
            output.AppendLine($"IsInRole(\"Admin\"): {isAdmin}");
            output.AppendLine($"IsInRole(\"User\"): {isUser}");
            output.AppendLine($"IsInRole(\"ReadOnly\"): {isReadOnly}");
            output.AppendLine();
            
            // Get all claims
            output.AppendLine("Claims:");
            foreach (var claim in User.Claims)
            {
                output.AppendLine($"- {claim.Type}: {claim.Value}");
            }
            
            return Content(output.ToString(), "text/plain");
        }
        
        // GET: /Admin/Users/ForceTokenRefresh/{id}
        public async Task<IActionResult> ForceTokenRefresh(Guid id)
        {
            try
            {
                // Get the user
                var user = await FetchUserAsync(id);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "User not found.";
                    return RedirectToAction(nameof(Index));
                }
                
                // Create a minimal token refresh request with just the user ID
                var refreshRequest = new 
                {
                    UserId = id,
                    ForceRefresh = true
                };
                
                // Send request to API
                var response = await _apiClient.PostAsJsonAsync("api/v1/admin/users/refresh-token", refreshRequest);
                
                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = $"Token refresh forced for {user.Name}. User will need to log in again.";
                }
                else
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    TempData["ErrorMessage"] = $"Failed to refresh token: {errorContent}";
                    _logger.LogWarning("Token refresh failed: {StatusCode} - {Content}", 
                        response.StatusCode, errorContent);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing token for user {UserId}", id);
                TempData["ErrorMessage"] = "Error refreshing token.";
            }
            
            return RedirectToAction(nameof(Index));
        }
        
        #region API Helper Methods
        
        private async Task<List<UserResponse>?> FetchUsersAsync(string? search = null, string? role = null, bool? isActive = null)
        {
            try
            {
                // Build query string with filters
                var queryParams = new List<string>();
                if (!string.IsNullOrEmpty(search))
                    queryParams.Add($"search={Uri.EscapeDataString(search)}");
                if (!string.IsNullOrEmpty(role))
                    queryParams.Add($"role={Uri.EscapeDataString(role)}");
                if (isActive.HasValue)
                    queryParams.Add($"isActive={isActive.Value}");
                
                string queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
                
                // Make the API request with filters
                var response = await _apiClient.GetAsync($"api/v1/admin/users{queryString}");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<List<UserResponse>>();
                }
                
                _logger.LogWarning("Failed to fetch users. Status: {Status}", response.StatusCode);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in FetchUsersAsync");
                return null;
            }
        }
        
        private async Task<UserResponse?> FetchUserAsync(Guid id)
        {
            try
            {
                var response = await _apiClient.GetAsync($"api/v1/admin/users/{id}");
                
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
                var response = await _apiClient.GetAsync($"api/v1/admin/user-devices?userId={userId}");
                
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
                var response = await _apiClient.GetAsync("api/v1/admin/devices");
                
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
        
        // Helper method to fetch roles from API
        private async Task<List<string>> FetchRolesAsync()
        {
            try
            {
                var response = await _apiClient.GetAsync("api/v1/admin/roles");
                
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<List<string>>() ?? new List<string>();
                }
                
                _logger.LogWarning("API returned status code {StatusCode} when fetching roles", 
                    response.StatusCode);
                
                // Fallback to default roles if the API call fails
                return new List<string> { "Admin", "Moderator", "User" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while fetching roles from API");
                // Fallback to default roles
                return new List<string> { "Admin", "Moderator", "User" };
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
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        public string Email { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Name is required")]
        public string Name { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Password is required")]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
        public string Password { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Confirm Password is required")]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Role is required")]
        public string Role { get; set; } = string.Empty;
    }

    public class EditUserViewModel
    {
        public Guid Id { get; set; }
        
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        public string Email { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Name is required")]
        public string Name { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Role is required")]
        public string Role { get; set; } = string.Empty;
        
        public bool IsActive { get; set; }
        
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
        public string? NewPassword { get; set; }
        
        [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
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