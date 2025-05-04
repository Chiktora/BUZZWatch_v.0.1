using BuzzWatch.Contracts.Admin;
using BuzzWatch.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using BuzzWatch.Application.Abstractions;
using BuzzWatch.Domain.Alerts;
using ComparisonOp = BuzzWatch.Domain.Enums.ComparisonOperator;
using AdminCreateAlertRuleRequest = BuzzWatch.Contracts.Admin.CreateAlertRuleRequest;
using AdminUpdateAlertRuleRequest = BuzzWatch.Contracts.Admin.UpdateAlertRuleRequest;
using BuzzWatch.Domain.Devices;
using BuzzWatch.Contracts.Devices;

namespace BuzzWatch.Api.Controllers
{
    [ApiController]
    [Route("api/v1/admin")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;
        private readonly ILogger<AdminController> _logger;
        private readonly IAlertRuleRepository _alertRuleRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IDeviceRepository _deviceRepository;

        public AdminController(
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole<Guid>> roleManager,
            ILogger<AdminController> logger,
            IAlertRuleRepository alertRuleRepository,
            IUnitOfWork unitOfWork,
            IDeviceRepository deviceRepository)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
            _alertRuleRepository = alertRuleRepository;
            _unitOfWork = unitOfWork;
            _deviceRepository = deviceRepository;
        }

        [HttpGet("dashboard")]
        public async Task<ActionResult<AdminDashboardResponse>> GetDashboard()
        {
            try
            {
                // Get counts from database
                var totalUsers = await _userManager.Users.CountAsync();
                
                // For now return some basic data - we'll expand this later
                var response = new AdminDashboardResponse
                {
                    TotalUsers = totalUsers,
                    TotalDevices = 0, // We'll implement these in the future
                    ActiveAlerts = 0,
                    SystemHealth = new SystemHealthResponse
                    {
                        DatabaseConnected = true,
                        ApiServicesRunning = true,
                        BackgroundTasksActive = true,
                        SystemLoad = 0.1,
                        MemoryUsage = 30.0,
                        DiskUsage = 40.0
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving admin dashboard data");
                return StatusCode(500, "An error occurred while retrieving dashboard data");
            }
        }

        [HttpGet("users")]
        public async Task<ActionResult<List<UserResponse>>> GetUsers(
            [FromQuery] string? search = null,
            [FromQuery] string? role = null,
            [FromQuery] bool? isActive = null)
        {
            try
            {
                var usersQuery = _userManager.Users.AsQueryable();

                // Apply status filter
                if (isActive.HasValue)
                {
                    usersQuery = usersQuery.Where(u => u.IsActive == isActive.Value);
                }

                // Apply search filter
                if (!string.IsNullOrWhiteSpace(search))
                {
                    search = search.ToLower();
                    usersQuery = usersQuery.Where(u => 
                        (u.Email != null && u.Email.ToLower().Contains(search)) || 
                        (u.UserName != null && u.UserName.ToLower().Contains(search)) || 
                        (u.Name != null && u.Name.ToLower().Contains(search)));
                }

                var users = await usersQuery.ToListAsync();
                var userResponses = new List<UserResponse>();

                foreach (var user in users)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    var userRole = roles.FirstOrDefault() ?? "User";

                    // Apply role filter
                    if (!string.IsNullOrWhiteSpace(role) && userRole != role)
                        continue;

                    userResponses.Add(new UserResponse
                    {
                        Id = user.Id,
                        Email = user.Email ?? string.Empty,
                        Name = user.UserName ?? string.Empty,
                        Role = userRole,
                        IsActive = user.IsActive,
                        CreatedAt = user.CreatedAt,
                        LastLogin = user.LastLoginAt
                    });
                }

                return Ok(userResponses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving users");
                return StatusCode(500, "An error occurred while retrieving users");
            }
        }

        [HttpGet("users/{id}")]
        public async Task<ActionResult<UserResponse>> GetUser(Guid id)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id.ToString());
                if (user == null)
                {
                    return NotFound();
                }

                var roles = await _userManager.GetRolesAsync(user);
                var role = roles.FirstOrDefault() ?? "User";

                var response = new UserResponse
                {
                    Id = user.Id,
                    Email = user.Email ?? string.Empty,
                    Name = user.UserName ?? string.Empty,
                    Role = role,
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt,
                    LastLogin = user.LastLoginAt
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user with ID {UserId}", id);
                return StatusCode(500, "An error occurred while retrieving the user");
            }
        }

        [HttpPost("users")]
        public async Task<ActionResult<UserResponse>> CreateUser([FromBody] CreateUserRequest request)
        {
            try
            {
                // Check if email is already in use
                var existingUser = await _userManager.FindByEmailAsync(request.Email);
                if (existingUser != null)
                {
                    return BadRequest("Email is already in use");
                }

                // Validate role
                if (!await _roleManager.RoleExistsAsync(request.Role))
                {
                    return BadRequest("Invalid role specified");
                }

                // Create user
                var user = new AppUser
                {
                    UserName = request.Email,
                    Email = request.Email,
                    Name = request.Name,
                    IsActive = true,
                    CreatedAt = DateTimeOffset.UtcNow
                };

                var result = await _userManager.CreateAsync(user, request.Password);
                if (!result.Succeeded)
                {
                    return BadRequest(result.Errors.Select(e => e.Description));
                }

                // Assign role
                await _userManager.AddToRoleAsync(user, request.Role);

                // Return the newly created user
                var response = new UserResponse
                {
                    Id = user.Id,
                    Email = user.Email ?? string.Empty,
                    Name = user.Name,
                    Role = request.Role,
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt,
                    LastLogin = null
                };

                return CreatedAtAction(nameof(GetUser), new { id = user.Id }, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user");
                return StatusCode(500, "An error occurred while creating the user");
            }
        }

        [HttpPut("users/{id}")]
        public async Task<ActionResult> UpdateUser(Guid id, [FromBody] UpdateUserRequest request)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id.ToString());
                if (user == null)
                {
                    return NotFound();
                }

                // Check if this is the current user trying to modify their own account
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                bool isCurrentUser = (currentUserId == id.ToString());
                
                // Don't allow admins to deactivate their own account
                if (isCurrentUser && !request.IsActive)
                {
                    return BadRequest("You cannot deactivate your own account");
                }

                // Update basic properties
                user.Name = request.Name;
                user.IsActive = request.IsActive;
                
                // Update email if it has changed
                if (user.Email != request.Email)
                {
                    // Check if the new email is already in use by another user
                    var existingUser = await _userManager.FindByEmailAsync(request.Email);
                    if (existingUser != null && existingUser.Id != id)
                    {
                        return BadRequest("Email is already in use by another user");
                    }

                    user.UserName = request.Email; // ASP.NET Identity often uses email as username
                    user.Email = request.Email;
                }

                // Save the changes
                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    return BadRequest(updateResult.Errors.Select(e => e.Description));
                }

                // Update role if necessary
                var currentRoles = await _userManager.GetRolesAsync(user);
                var currentRole = currentRoles.FirstOrDefault();

                if (currentRole != request.Role)
                {
                    // Don't allow admins to change their own role
                    if (isCurrentUser && currentRole == "Admin")
                    {
                        return BadRequest("You cannot change your own admin role");
                    }

                    // Remove from current roles
                    if (currentRole != null)
                    {
                        await _userManager.RemoveFromRoleAsync(user, currentRole);
                    }

                    // Add to new role
                    if (!string.IsNullOrEmpty(request.Role))
                    {
                        if (!await _roleManager.RoleExistsAsync(request.Role))
                        {
                            return BadRequest("Invalid role specified");
                        }

                        await _userManager.AddToRoleAsync(user, request.Role);
                    }
                }

                // Update password if provided
                if (!string.IsNullOrEmpty(request.Password))
                {
                    var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                    var passwordResult = await _userManager.ResetPasswordAsync(user, token, request.Password);
                    
                    if (!passwordResult.Succeeded)
                    {
                        return BadRequest(passwordResult.Errors.Select(e => e.Description));
                    }
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user with ID {UserId}", id);
                return StatusCode(500, "An error occurred while updating the user");
            }
        }

        [HttpDelete("users/{id}")]
        public async Task<ActionResult> DeleteUser(Guid id)
        {
            try
            {
                // Don't allow deleting the admin user
                if (id == Guid.Parse("00000000-0000-0000-0000-000000000001"))
                {
                    return BadRequest("Cannot delete the default admin user");
                }

                var user = await _userManager.FindByIdAsync(id.ToString());
                if (user == null)
                {
                    return NotFound();
                }

                // Check if this is the current user - don't allow self-deletion
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (currentUserId == id.ToString())
                {
                    return BadRequest("Cannot delete your own account");
                }

                // Delete the user
                var result = await _userManager.DeleteAsync(user);
                if (!result.Succeeded)
                {
                    return BadRequest(result.Errors.Select(e => e.Description));
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user with ID {UserId}", id);
                return StatusCode(500, "An error occurred while deleting the user");
            }
        }

        // User device management endpoints

        [HttpGet("devices")]
        public async Task<ActionResult<List<DeviceResponse>>> GetDevices()
        {
            try
            {
                // Get devices from repository instead of using mock data
                var devices = await _deviceRepository.GetAllAsync(CancellationToken.None);
                
                // Map to response model
                var response = devices.Select(d => new DeviceResponse
                {
                    Id = d.Id,
                    Name = d.Name,
                    Location = d.Location?.ToString() ?? "Not specified",
                    Status = GetDeviceStatus(d),
                    BatteryLevel = 85, // Placeholder
                    LastSeen = GetLastSeenTime(d)
                }).ToList();
                
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving devices");
                return StatusCode(500, "An error occurred while retrieving devices");
            }
        }
        
        // Helper method to get device status
        private string GetDeviceStatus(Domain.Devices.Device device)
        {
            var latestMeasurement = device.Measurements
                .OrderByDescending(m => m.RecordedAt)
                .FirstOrDefault();
                
            if (latestMeasurement == null)
                return "Offline";
                
            return latestMeasurement.RecordedAt > DateTimeOffset.UtcNow.AddHours(-1)
                ? "Online"
                : "Offline";
        }
        
        // Helper method to get last seen time
        private DateTimeOffset GetLastSeenTime(Domain.Devices.Device device)
        {
            var latestMeasurement = device.Measurements
                .OrderByDescending(m => m.RecordedAt)
                .FirstOrDefault();
                
            return latestMeasurement?.RecordedAt ?? device.CreatedAt;
        }

        [HttpGet("user-devices")]
        public async Task<ActionResult<List<UserDeviceResponse>>> GetUserDevices([FromQuery] Guid userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId.ToString());
                if (user == null)
                {
                    return NotFound("User not found");
                }

                // Get all devices to build the response
                var allDevices = await _deviceRepository.GetAllAsync(CancellationToken.None);
                
                // Get devices this user has access to (for now, showing all devices)
                // In a real implementation, we would use a proper access control repository
                var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
                
                // Build response
                var devicePermissions = new List<UserDeviceResponse>();
                
                foreach (var device in allDevices)
                {
                    // For now, give admin full access and others basic access to all devices
                    devicePermissions.Add(new UserDeviceResponse
                    {
                        UserId = userId,
                        DeviceId = device.Id,
                        DeviceName = device.Name,
                        DeviceLocation = device.Location?.ToString() ?? "Not specified",
                        HasAccess = true, // All users have access for demo
                        CanManage = isAdmin // Only admins can manage
                    });
                }

                return Ok(devicePermissions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving device permissions for user {UserId}", userId);
                return StatusCode(500, "An error occurred while retrieving device permissions");
            }
        }

        [HttpPost("user-devices")]
        public async Task<ActionResult> AssignDeviceToUser([FromBody] UpdateUserDeviceRequest request)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(request.UserId.ToString());
                if (user == null)
                {
                    return NotFound("User not found");
                }

                // TODO: Check if device exists and save the permission to database
                // This would be where you'd await something like:
                // await _deviceRepository.AssignDeviceToUserAsync(request.UserId, request.DeviceId, request.HasAccess, request.CanManage);
                // For now, just return success since we're using mock data

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning device {DeviceId} to user {UserId}", 
                    request.DeviceId, request.UserId);
                return StatusCode(500, "An error occurred while assigning the device to the user");
            }
        }

        [HttpDelete("user-devices")]
        public async Task<ActionResult> RemoveDeviceFromUser([FromQuery] Guid userId, [FromQuery] Guid deviceId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId.ToString());
                if (user == null)
                {
                    return NotFound("User not found");
                }

                // TODO: Check if device exists and remove the permission from database
                // This would be where you'd await something like:
                // await _deviceRepository.RemoveDeviceFromUserAsync(userId, deviceId);
                // For now, just return success since we're using mock data

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing device {DeviceId} from user {UserId}", 
                    deviceId, userId);
                return StatusCode(500, "An error occurred while removing the device from the user");
            }
        }

        [HttpGet("roles")]
        public async Task<ActionResult<List<string>>> GetRoles()
        {
            try
            {
                var roles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
                return Ok(roles.Where(r => r != null).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving roles");
                return StatusCode(500, "An error occurred while retrieving roles");
            }
        }

        [HttpGet("devices/{id}")]
        public async Task<ActionResult<DeviceResponse>> GetDevice(Guid id)
        {
            try
            {
                // Get device from repository
                var device = await _deviceRepository.GetAsync(id, CancellationToken.None);
                if (device == null)
                {
                    return NotFound($"Device with ID {id} not found");
                }
                
                // Map to response
                var response = new DeviceResponse
                {
                    Id = device.Id,
                    Name = device.Name,
                    Location = device.Location?.ToString() ?? "Not specified",
                    Status = GetDeviceStatus(device),
                    BatteryLevel = 85, // Placeholder
                    LastSeen = GetLastSeenTime(device)
                };
                
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving device with ID {DeviceId}", id);
                return StatusCode(500, "An error occurred while retrieving the device");
            }
        }

        [HttpGet("devices/{id}/api-key")]
        public async Task<ActionResult<ApiKeyResponse>> GetDeviceApiKey(Guid id)
        {
            try
            {
                // Check if device exists
                var device = await _deviceRepository.GetAsync(id, CancellationToken.None);
                if (device == null)
                {
                    return NotFound($"Device with ID {id} not found");
                }
                
                // Generate a simulated API key based on device ID to ensure it's consistent
                // In a real app, this would come from a secure API key store
                var apiKey = $"apk_{id.ToString().Substring(0, 8)}_{DateTime.UtcNow.Year}";
                
                return Ok(new ApiKeyResponse 
                { 
                    Key = apiKey,
                    ExpiresAt = DateTimeOffset.UtcNow.AddYears(1)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving API key for device with ID {DeviceId}", id);
                return StatusCode(500, "An error occurred while retrieving the device API key");
            }
        }
        
        // Helper method to retrieve a device from repository
        private async Task<DeviceResponse?> GetDeviceFromRepository(Guid id)
        {
            var device = await _deviceRepository.GetAsync(id, CancellationToken.None);
            if (device == null)
                return null;
                
            return new DeviceResponse
            {
                Id = device.Id,
                Name = device.Name,
                Location = device.Location?.ToString() ?? "Not specified",
                Status = GetDeviceStatus(device),
                BatteryLevel = 85, // Placeholder
                LastSeen = GetLastSeenTime(device)
            };
        }

        #region Alert Rules

        [HttpGet("alert-rules")]
        public async Task<ActionResult<List<AlertRuleResponse>>> GetAlertRules()
        {
            try
            {
                var rules = await _alertRuleRepository.GetActiveRulesAsync();
                
                // Get device information to attach the device names
                var devices = await GetDevicesFromRepository();
                var deviceMap = devices.ToDictionary(d => d.Id, d => d.Name);
                
                var response = rules.Select(rule => new AlertRuleResponse
                {
                    Id = rule.Id,
                    DeviceId = rule.DeviceId,
                    DeviceName = deviceMap.ContainsKey(rule.DeviceId) ? deviceMap[rule.DeviceId] : "Unknown Device",
                    Metric = rule.Metric,
                    Operator = ConvertOperatorToString(MapToComparisonOp(rule.Operator)),
                    Threshold = rule.Threshold,
                    DurationSeconds = rule.DurationSeconds,
                    Active = rule.Active,
                    CreatedAt = rule.CreatedAt
                }).ToList();
                
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving alert rules");
                return StatusCode(500, "An error occurred while retrieving alert rules");
            }
        }

        [HttpGet("alert-rules/{id}")]
        public async Task<ActionResult<AlertRuleResponse>> GetAlertRule(Guid id)
        {
            try
            {
                var rule = await _alertRuleRepository.GetAsync(id);
                if (rule == null)
                {
                    return NotFound($"Alert rule with ID {id} not found");
                }
                
                // Get device information
                var device = await GetDeviceFromRepository(rule.DeviceId);
                
                var response = new AlertRuleResponse
                {
                    Id = rule.Id,
                    DeviceId = rule.DeviceId,
                    DeviceName = device?.Name ?? "Unknown Device",
                    Metric = rule.Metric,
                    Operator = ConvertOperatorToString(MapToComparisonOp(rule.Operator)),
                    Threshold = rule.Threshold,
                    DurationSeconds = rule.DurationSeconds,
                    Active = rule.Active,
                    CreatedAt = rule.CreatedAt
                };
                
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving alert rule with ID {AlertRuleId}", id);
                return StatusCode(500, "An error occurred while retrieving the alert rule");
            }
        }

        [HttpPost("alert-rules")]
        public async Task<ActionResult<AlertRuleResponse>> CreateAlertRule([FromBody] AdminCreateAlertRuleRequest request)
        {
            try
            {
                // Validate device exists
                var device = await GetDeviceFromRepository(request.DeviceId);
                if (device == null)
                {
                    return BadRequest($"Device with ID {request.DeviceId} not found");
                }
                
                // Validate operator
                if (!TryParseOperator(request.Operator, out ComparisonOp enumOp))
                {
                    return BadRequest($"Invalid operator: {request.Operator}");
                }

                // Convert to the Domain.Alerts ComparisonOperator
                var domainOp = MapToDomainOperator(enumOp);
                
                // Create alert rule
                var rule = AlertRule.Create(
                    request.DeviceId,
                    request.Metric,
                    domainOp,
                    request.Threshold,
                    request.DurationSeconds
                );
                
                // Set active state if specified
                if (!request.Active)
                {
                    rule.Disable();
                }
                
                // Save to database
                await _alertRuleRepository.AddAsync(rule);
                await _unitOfWork.SaveChangesAsync(CancellationToken.None);
                
                // Return the created rule
                var response = new AlertRuleResponse
                {
                    Id = rule.Id,
                    DeviceId = rule.DeviceId,
                    DeviceName = device.Name,
                    Metric = rule.Metric,
                    Operator = request.Operator,
                    Threshold = rule.Threshold,
                    DurationSeconds = rule.DurationSeconds,
                    Active = rule.Active,
                    CreatedAt = rule.CreatedAt
                };
                
                return CreatedAtAction(nameof(GetAlertRule), new { id = rule.Id }, response);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating alert rule");
                return StatusCode(500, "An error occurred while creating the alert rule");
            }
        }

        [HttpPut("alert-rules/{id}")]
        public async Task<ActionResult> UpdateAlertRule(Guid id, [FromBody] AdminUpdateAlertRuleRequest request)
        {
            try
            {
                // Get existing rule
                var rule = await _alertRuleRepository.GetAsync(id);
                if (rule == null)
                {
                    return NotFound($"Alert rule with ID {id} not found");
                }
                
                // Validate device exists
                var device = await GetDeviceFromRepository(request.DeviceId);
                if (device == null)
                {
                    return BadRequest($"Device with ID {request.DeviceId} not found");
                }
                
                // Validate operator
                if (!TryParseOperator(request.Operator, out ComparisonOp enumOp))
                {
                    return BadRequest($"Invalid operator: {request.Operator}");
                }

                // Convert to the Domain.Alerts ComparisonOperator
                var domainOp = MapToDomainOperator(enumOp);
                
                // Since our domain entity has private setters, we'll need to create a new one
                // In a real application, you might add methods to the entity to update its properties
                var updatedRule = AlertRule.Create(
                    request.DeviceId,
                    request.Metric,
                    domainOp,
                    request.Threshold,
                    request.DurationSeconds
                );
                
                // Set the same ID and created date
                typeof(AlertRule).GetProperty("Id")?.SetValue(updatedRule, rule.Id);
                typeof(AlertRule).GetProperty("CreatedAt")?.SetValue(updatedRule, rule.CreatedAt);
                
                // Set active state
                if (request.Active)
                {
                    updatedRule.Enable();
                }
                else
                {
                    updatedRule.Disable();
                }
                
                // Update in repository
                _alertRuleRepository.Update(updatedRule);
                await _unitOfWork.SaveChangesAsync(CancellationToken.None);
                
                return NoContent();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating alert rule with ID {AlertRuleId}", id);
                return StatusCode(500, "An error occurred while updating the alert rule");
            }
        }

        [HttpDelete("alert-rules/{id}")]
        public async Task<ActionResult> DeleteAlertRule(Guid id)
        {
            try
            {
                var rule = await _alertRuleRepository.GetAsync(id);
                if (rule == null)
                {
                    return NotFound($"Alert rule with ID {id} not found");
                }
                
                _alertRuleRepository.Remove(rule);
                await _unitOfWork.SaveChangesAsync(CancellationToken.None);
                
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting alert rule with ID {AlertRuleId}", id);
                return StatusCode(500, "An error occurred while deleting the alert rule");
            }
        }

        [HttpGet("alert-events")]
        public async Task<ActionResult<List<AdminAlertEventResponse>>> GetAlertEvents()
        {
            try
            {
                // TODO: In a real application, fetch this from a repository
                // For now, return mock data until we implement the alerts history repository
                var mockEvents = await Task.FromResult(new List<AdminAlertEventResponse>
                {
                    new AdminAlertEventResponse
                    {
                        Id = Guid.NewGuid(),
                        DeviceName = "Hive Monitor 1",
                        AlertType = "Temperature",
                        Message = "Temperature exceeded 35°C for more than 5 minutes",
                        Timestamp = DateTimeOffset.UtcNow.AddHours(-2),
                        IsResolved = true
                    },
                    new AdminAlertEventResponse
                    {
                        Id = Guid.NewGuid(),
                        DeviceName = "Hive Monitor 2",
                        AlertType = "Humidity",
                        Message = "Humidity dropped below 40% for more than 10 minutes",
                        Timestamp = DateTimeOffset.UtcNow.AddHours(-6),
                        IsResolved = false
                    },
                    new AdminAlertEventResponse
                    {
                        Id = Guid.NewGuid(),
                        DeviceName = "Hive Monitor 3",
                        AlertType = "Weight",
                        Message = "Sudden weight decrease detected",
                        Timestamp = DateTimeOffset.UtcNow.AddDays(-1),
                        IsResolved = true
                    }
                });
                
                return Ok(mockEvents);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving alert events");
                return StatusCode(500, "An error occurred while retrieving alert events");
            }
        }

        #endregion

        #region Helper Methods

        private string ConvertOperatorToString(ComparisonOp op)
        {
            return op switch
            {
                ComparisonOp.LessThan => "<",
                ComparisonOp.LessThanOrEqual => "<=",
                ComparisonOp.Equal => "==",
                ComparisonOp.GreaterThanOrEqual => ">=",
                ComparisonOp.GreaterThan => ">",
                _ => "=="
            };
        }

        private bool TryParseOperator(string opString, out ComparisonOp op)
        {
            op = ComparisonOp.Equal;
            
            switch (opString)
            {
                case "<":
                    op = ComparisonOp.LessThan;
                    return true;
                case "<=":
                    op = ComparisonOp.LessThanOrEqual;
                    return true;
                case "==":
                    op = ComparisonOp.Equal;
                    return true;
                case ">=":
                    op = ComparisonOp.GreaterThanOrEqual;
                    return true;
                case ">":
                    op = ComparisonOp.GreaterThan;
                    return true;
                default:
                    return false;
            }
        }

        // Convert between the different ComparisonOperator types
        private ComparisonOp MapToComparisonOp(BuzzWatch.Domain.Alerts.ComparisonOperator op)
        {
            return op switch
            {
                BuzzWatch.Domain.Alerts.ComparisonOperator.LessThan => ComparisonOp.LessThan,
                BuzzWatch.Domain.Alerts.ComparisonOperator.LessThanOrEqual => ComparisonOp.LessThanOrEqual,
                BuzzWatch.Domain.Alerts.ComparisonOperator.Equals => ComparisonOp.Equal,
                BuzzWatch.Domain.Alerts.ComparisonOperator.GreaterThanOrEqual => ComparisonOp.GreaterThanOrEqual,
                BuzzWatch.Domain.Alerts.ComparisonOperator.GreaterThan => ComparisonOp.GreaterThan,
                _ => ComparisonOp.Equal
            };
        }

        private BuzzWatch.Domain.Alerts.ComparisonOperator MapToDomainOperator(ComparisonOp op)
        {
            return op switch
            {
                ComparisonOp.LessThan => BuzzWatch.Domain.Alerts.ComparisonOperator.LessThan,
                ComparisonOp.LessThanOrEqual => BuzzWatch.Domain.Alerts.ComparisonOperator.LessThanOrEqual,
                ComparisonOp.Equal => BuzzWatch.Domain.Alerts.ComparisonOperator.Equals,
                ComparisonOp.GreaterThanOrEqual => BuzzWatch.Domain.Alerts.ComparisonOperator.GreaterThanOrEqual,
                ComparisonOp.GreaterThan => BuzzWatch.Domain.Alerts.ComparisonOperator.GreaterThan,
                _ => BuzzWatch.Domain.Alerts.ComparisonOperator.Equals
            };
        }

        private async Task<List<DeviceResponse>> GetDevicesFromRepository()
        {
            var devices = await _deviceRepository.GetAllAsync(CancellationToken.None);
            
            return devices.Select(d => new DeviceResponse
            {
                Id = d.Id,
                Name = d.Name,
                Location = d.Location?.ToString() ?? "Not specified",
                Status = GetDeviceStatus(d),
                BatteryLevel = 85, // Placeholder for now
                LastSeen = GetLastSeenTime(d)
            }).ToList();
        }

        #endregion

        [HttpPost("devices")]
        public async Task<ActionResult<DeviceResponse>> CreateAdminDevice([FromBody] CreateDeviceRequest request)
        {
            try
            {
                // Create domain entity
                HiveLocation? location = null;
                if (!string.IsNullOrWhiteSpace(request.Location))
                {
                    location = new HiveLocation(
                        request.Location,
                        request.Latitude,
                        request.Longitude);
                }
                
                var device = Device.Create(request.Name, location);
                
                // Save to database
                await _deviceRepository.AddAsync(device, CancellationToken.None);
                await _unitOfWork.SaveChangesAsync(CancellationToken.None);
                
                // Return response
                var response = await GetDeviceFromRepository(device.Id);
                
                if (response == null)
                {
                    return StatusCode(500, "Error retrieving the created device");
                }
                
                return CreatedAtAction(nameof(GetDevice), new { id = device.Id }, response);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Invalid data for device creation: {Message}", ex.Message);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating device");
                return StatusCode(500, "Error creating device");
            }
        }

        [HttpPut("devices/{id}")]
        public async Task<ActionResult> UpdateAdminDevice(Guid id, [FromBody] UpdateDeviceRequest request)
        {
            try
            {
                // Get existing device
                var device = await _deviceRepository.GetAsync(id, CancellationToken.None);
                if (device == null)
                {
                    return NotFound($"Device with ID {id} not found");
                }
                
                // Update properties
                device.UpdateName(request.Name);
                
                HiveLocation? location = null;
                if (!string.IsNullOrWhiteSpace(request.Location))
                {
                    location = new HiveLocation(
                        request.Location,
                        request.Latitude,
                        request.Longitude);
                }
                
                device.UpdateLocation(location);
                
                // Save to database
                _deviceRepository.Update(device);
                await _unitOfWork.SaveChangesAsync(CancellationToken.None);
                
                return NoContent();
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Invalid data for device update: {Message}", ex.Message);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating device with ID {DeviceId}", id);
                return StatusCode(500, "Error updating device");
            }
        }

        [HttpDelete("devices/{id}")]
        public async Task<ActionResult> DeleteAdminDevice(Guid id)
        {
            try
            {
                // Get existing device
                var device = await _deviceRepository.GetAsync(id, CancellationToken.None);
                if (device == null)
                {
                    return NotFound($"Device with ID {id} not found");
                }
                
                // Remove device
                _deviceRepository.Remove(device);
                await _unitOfWork.SaveChangesAsync(CancellationToken.None);
                
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting device with ID {DeviceId}", id);
                return StatusCode(500, "Error deleting device");
            }
        }

        [HttpPost("devices/{id}/api-key")]
        public async Task<ActionResult<ApiKeyResponse>> RegenerateDeviceApiKey(Guid id)
        {
            try
            {
                // Check if device exists
                var device = await _deviceRepository.GetAsync(id, CancellationToken.None);
                if (device == null)
                {
                    return NotFound($"Device with ID {id} not found");
                }
                
                // Generate new API key (simple implementation for now)
                var apiKey = GenerateApiKey();
                
                // In a real implementation, we would store this securely
                var expiryDate = DateTimeOffset.UtcNow.AddDays(90);
                
                return Ok(new ApiKeyResponse
                {
                    Key = apiKey,
                    ExpiresAt = expiryDate
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error regenerating API key for device {DeviceId}", id);
                return StatusCode(500, "Error regenerating API key");
            }
        }
        
        private string GenerateApiKey()
        {
            // Generate a random API key (in real app, use more secure method)
            return Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
        }
    }
} 