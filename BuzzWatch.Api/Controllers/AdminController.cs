using BuzzWatch.Contracts.Admin;
using BuzzWatch.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

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

        public AdminController(
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole<Guid>> roleManager,
            ILogger<AdminController> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
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
                // TODO: Replace this with actual database query when device repository is available
                // For now, return some mock devices
                var devices = new List<DeviceResponse>
                {
                    new DeviceResponse
                    {
                        Id = Guid.Parse("10000000-0000-0000-0000-000000000001"),
                        Name = "Hive Monitor 1",
                        Location = "Apiary 1",
                        Status = "Online",
                        BatteryLevel = 85,
                        LastSeen = DateTimeOffset.UtcNow.AddMinutes(-5)
                    },
                    new DeviceResponse
                    {
                        Id = Guid.Parse("20000000-0000-0000-0000-000000000002"),
                        Name = "Hive Monitor 2",
                        Location = "Apiary 1",
                        Status = "Online",
                        BatteryLevel = 72,
                        LastSeen = DateTimeOffset.UtcNow.AddMinutes(-12)
                    },
                    new DeviceResponse
                    {
                        Id = Guid.Parse("30000000-0000-0000-0000-000000000003"),
                        Name = "Hive Monitor 3",
                        Location = "Apiary 2",
                        Status = "Offline",
                        BatteryLevel = 14,
                        LastSeen = DateTimeOffset.UtcNow.AddHours(-6)
                    }
                };

                // Add an artificial delay to simulate async behavior
                await Task.Delay(1);
                
                return Ok(devices);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving devices");
                return StatusCode(500, "An error occurred while retrieving devices");
            }
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

                // TODO: Replace this with actual database query when device repository is available
                // For now, return some mock device permissions
                var devicePermissions = new List<UserDeviceResponse>
                {
                    new UserDeviceResponse
                    {
                        UserId = userId,
                        DeviceId = Guid.Parse("10000000-0000-0000-0000-000000000001"),
                        DeviceName = "Hive Monitor 1",
                        DeviceLocation = "Apiary 1",
                        HasAccess = true,
                        CanManage = true
                    },
                    new UserDeviceResponse
                    {
                        UserId = userId,
                        DeviceId = Guid.Parse("20000000-0000-0000-0000-000000000002"),
                        DeviceName = "Hive Monitor 2",
                        DeviceLocation = "Apiary 1",
                        HasAccess = userId == Guid.Parse("00000000-0000-0000-0000-000000000001"), // Admin has full access
                        CanManage = userId == Guid.Parse("00000000-0000-0000-0000-000000000001")
                    },
                    new UserDeviceResponse
                    {
                        UserId = userId,
                        DeviceId = Guid.Parse("30000000-0000-0000-0000-000000000003"),
                        DeviceName = "Hive Monitor 3",
                        DeviceLocation = "Apiary 2",
                        HasAccess = false,
                        CanManage = false
                    }
                };

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
    }
} 