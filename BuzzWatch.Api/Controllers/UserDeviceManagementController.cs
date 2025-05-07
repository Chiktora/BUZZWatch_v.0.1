using BuzzWatch.Application.Abstractions;
using BuzzWatch.Contracts.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using BuzzWatch.Infrastructure.Identity;

namespace BuzzWatch.Api.Controllers
{
    [ApiController]
    [Route("api/v1/user-devices")]
    [Authorize(Roles = "Admin")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class UserDeviceManagementController : ControllerBase
    {
        private readonly IUserDeviceAccessRepository _userDeviceAccessRepository;
        private readonly IDeviceRepository _deviceRepository;
        private readonly UserManager<AppUser> _userManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UserDeviceManagementController> _logger;

        public UserDeviceManagementController(
            IUserDeviceAccessRepository userDeviceAccessRepository,
            IDeviceRepository deviceRepository,
            UserManager<AppUser> userManager,
            IUnitOfWork unitOfWork,
            ILogger<UserDeviceManagementController> logger)
        {
            _userDeviceAccessRepository = userDeviceAccessRepository;
            _deviceRepository = deviceRepository;
            _userManager = userManager;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        // GET: api/v1/user-devices?userId={userId}
        [HttpGet]
        public async Task<ActionResult<List<UserDeviceResponse>>> GetUserDevices([FromQuery] Guid userId, CancellationToken ct)
        {
            try
            {
                _logger.LogInformation("Getting device permissions for user: {UserId}", userId);
                
                // Verify user exists
                var user = await _userManager.FindByIdAsync(userId.ToString());
                if (user == null)
                {
                    _logger.LogWarning("User with ID {UserId} not found", userId);
                    return NotFound("User not found");
                }
                
                // Use the ID from the user object to ensure correct format
                string userIdString = user.Id.ToString();
                _logger.LogInformation("Using user ID: {UserId} (from user object)", userIdString);

                // Get all devices for building the response
                var allDevices = await _deviceRepository.GetAllAsync(ct);
                _logger.LogInformation("Retrieved {Count} devices", allDevices.Count);
                
                // Determine if the user is an admin
                var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
                
                // Get devices this user has access to
                var userDeviceAccesses = isAdmin 
                    ? null // Admins have access to all devices, no need to query
                    : await _userDeviceAccessRepository.GetUserDeviceAccessesAsync(userIdString, ct);
                
                if (!isAdmin && userDeviceAccesses != null)
                {
                    _logger.LogInformation("User has access to {Count} devices", userDeviceAccesses.Count);
                }
                
                // Build response
                var response = new List<UserDeviceResponse>();
                
                foreach (var device in allDevices)
                {
                    // For admin users, they have access to all devices
                    bool hasAccess = isAdmin;
                    bool canManage = isAdmin;
                    
                    // For non-admin users, check their specific permissions
                    if (!isAdmin && userDeviceAccesses != null)
                    {
                        var deviceAccess = userDeviceAccesses.FirstOrDefault(a => a.DeviceId == device.Id);
                        hasAccess = deviceAccess != null;
                        canManage = deviceAccess?.CanManage ?? false;
                    }
                    
                    response.Add(new UserDeviceResponse
                    {
                        UserId = userId,
                        DeviceId = device.Id,
                        DeviceName = device.Name,
                        DeviceLocation = device.Location?.ToString() ?? "Not specified",
                        HasAccess = hasAccess,
                        CanManage = canManage
                    });
                }
                
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving device permissions for user {UserId}", userId);
                
                // Return a more specific error message to help diagnose the issue
                string errorMessage = ex.InnerException != null 
                    ? $"Error: {ex.Message}. Inner error: {ex.InnerException.Message}"
                    : $"Error: {ex.Message}";
                
                return StatusCode(500, errorMessage);
            }
        }

        // POST: api/v1/user-devices
        [HttpPost]
        public async Task<ActionResult> AssignDeviceToUser([FromBody] UpdateUserDeviceRequest request, CancellationToken ct)
        {
            try
            {
                _logger.LogInformation("Assigning device to user. Request: {@Request}", request);
                
                // Verify user exists
                var user = await _userManager.FindByIdAsync(request.UserId.ToString());
                if (user == null)
                {
                    _logger.LogWarning("User with ID {UserId} not found", request.UserId);
                    return NotFound("User not found");
                }
                
                // Verify device exists
                var device = await _deviceRepository.GetAsync(request.DeviceId, ct);
                if (device == null)
                {
                    _logger.LogWarning("Device with ID {DeviceId} not found", request.DeviceId);
                    return NotFound("Device not found");
                }
                
                _logger.LogInformation("Found user {UserName} and device {DeviceName}", 
                    user.UserName, device.Name);
                
                try
                {
                    // Ensure we have the correct format for UserId - use the ID from the actual user object
                    string userId = user.Id.ToString();
                    _logger.LogInformation("Using user ID: {UserId} (from user object)", userId);
                    
                    if (request.HasAccess)
                    {
                        _logger.LogInformation("Granting access to device {DeviceId} for user {UserId}. CanManage={CanManage}", 
                            request.DeviceId, userId, request.CanManage);
                            
                        // Grant access to device
                        await _userDeviceAccessRepository.GrantAccessAsync(
                            userId, request.DeviceId, request.CanManage, ct);
                    }
                    else
                    {
                        _logger.LogInformation("Revoking access to device {DeviceId} for user {UserId}", 
                            request.DeviceId, userId);
                            
                        // Remove access from device
                        await _userDeviceAccessRepository.RevokeAccessAsync(
                            userId, request.DeviceId, ct);
                    }
                    
                    _logger.LogInformation("About to save changes to database");
                    await _unitOfWork.SaveChangesAsync(ct);
                    _logger.LogInformation("Changes saved successfully");
                    
                    return Ok();
                }
                catch (Exception innerEx)
                {
                    _logger.LogError(innerEx, "Exception in device assignment operations");
                    throw; // Re-throw to be caught by outer catch
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating device access for user {UserId} to device {DeviceId}",
                    request?.UserId, request?.DeviceId);
                
                // Return a more specific error message to help diagnose the issue
                string errorMessage = ex.InnerException != null 
                    ? $"Error: {ex.Message}. Inner error: {ex.InnerException.Message}"
                    : $"Error: {ex.Message}";
                
                return StatusCode(500, errorMessage);
            }
        }

        // DELETE: api/v1/user-devices?userId={userId}&deviceId={deviceId}
        [HttpDelete]
        public async Task<ActionResult> RemoveDeviceFromUser([FromQuery] Guid userId, [FromQuery] Guid deviceId, CancellationToken ct)
        {
            try
            {
                _logger.LogInformation("Removing device access. UserId: {UserId}, DeviceId: {DeviceId}", userId, deviceId);
                
                // Verify user exists
                var user = await _userManager.FindByIdAsync(userId.ToString());
                if (user == null)
                {
                    _logger.LogWarning("User with ID {UserId} not found", userId);
                    return NotFound("User not found");
                }
                
                // Verify device exists
                var device = await _deviceRepository.GetAsync(deviceId, ct);
                if (device == null)
                {
                    _logger.LogWarning("Device with ID {DeviceId} not found", deviceId);
                    return NotFound("Device not found");
                }
                
                _logger.LogInformation("Found user {UserName} and device {DeviceName}", 
                    user.UserName, device.Name);
                
                try
                {
                    // Use the ID from the user object to ensure correct format
                    string userIdString = user.Id.ToString();
                    _logger.LogInformation("Using user ID: {UserId} (from user object)", userIdString);
                    
                    // Remove access from device
                    await _userDeviceAccessRepository.RevokeAccessAsync(userIdString, deviceId, ct);
                    
                    _logger.LogInformation("About to save changes to database");
                    await _unitOfWork.SaveChangesAsync(ct);
                    _logger.LogInformation("Changes saved successfully");
                    
                    return Ok();
                }
                catch (Exception innerEx)
                {
                    _logger.LogError(innerEx, "Exception in device access removal operations");
                    throw; // Re-throw to be caught by outer catch
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing device access for user {UserId} to device {DeviceId}",
                    userId, deviceId);
                
                // Return a more specific error message to help diagnose the issue
                string errorMessage = ex.InnerException != null 
                    ? $"Error: {ex.Message}. Inner error: {ex.InnerException.Message}"
                    : $"Error: {ex.Message}";
                
                return StatusCode(500, errorMessage);
            }
        }
        
        // POST: api/v1/user-devices/test
        [HttpPost("test")]
        public async Task<ActionResult> TestAssignDeviceToUser([FromBody] UpdateUserDeviceRequest request)
        {
            _logger.LogInformation("Received test request: {@Request}", request);
            
            try
            {
                // Get the user to make sure we have the right ID format
                var user = await _userManager.FindByIdAsync(request.UserId.ToString());
                var device = await _deviceRepository.GetAsync(request.DeviceId, CancellationToken.None);
                
                // Generate debug user ID info
                string userIdFromRequest = request.UserId.ToString();
                string? userIdFromDb = user?.Id.ToString();
                
                // Return detailed information about what we received
                var response = new
                {
                    ReceivedData = request,
                    UserInfo = new
                    {
                        RequestUserId = userIdFromRequest,
                        DbUserId = userIdFromDb,
                        UserFound = user != null,
                        UserName = user?.UserName,
                    },
                    DeviceInfo = new
                    {
                        DeviceId = request.DeviceId,
                        DeviceFound = device != null,
                        DeviceName = device?.Name
                    },
                    ValidationChecks = new
                    {
                        UserIdMatch = userIdFromDb != null && userIdFromRequest == userIdFromDb,
                        IsValid = request != null && request.UserId != Guid.Empty && request.DeviceId != Guid.Empty
                    }
                };
                
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in test endpoint");
                return StatusCode(500, new { Error = ex.Message, InnerError = ex.InnerException?.Message });
            }
        }
    }
} 