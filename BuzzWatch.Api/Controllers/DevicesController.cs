using BuzzWatch.Application.Abstractions;
using BuzzWatch.Domain.Devices;
using BuzzWatch.Domain.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using BuzzWatch.Contracts.Devices;
using BuzzWatch.Contracts.Admin;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using BuzzWatch.Contracts.Auth;

namespace BuzzWatch.Api.Controllers
{
    [ApiController]
    [Route("api/v1/devices")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class DevicesController : ControllerBase
    {
        private readonly IDeviceRepository _deviceRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserDeviceAccessRepository _userDeviceAccessRepository;
        private readonly IApiKeyRepository _apiKeyRepository;
        private readonly ILogger<DevicesController> _logger;
        
        public DevicesController(
            IDeviceRepository deviceRepository,
            IUnitOfWork unitOfWork,
            IUserDeviceAccessRepository userDeviceAccessRepository,
            IApiKeyRepository apiKeyRepository,
            ILogger<DevicesController> logger)
        {
            _deviceRepository = deviceRepository;
            _unitOfWork = unitOfWork;
            _userDeviceAccessRepository = userDeviceAccessRepository;
            _apiKeyRepository = apiKeyRepository;
            _logger = logger;
        }
        
        // GET: api/v1/devices
        [HttpGet]
        public async Task<ActionResult<List<DeviceDto>>> GetDevices(CancellationToken ct)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("User not authenticated");
                }
                
                var isAdmin = User.IsInRole("Admin");
                
                List<Device> devices;
                if (isAdmin)
                {
                    // Admins can see all devices
                    devices = await _deviceRepository.GetAllAsync(ct);
                }
                else
                {
                    // Regular users only see devices they have access to
                    var accessibleDeviceIds = await _userDeviceAccessRepository.GetAccessibleDeviceIdsAsync(
                        Guid.Parse(userId).ToString(), ct);
                    
                    devices = new List<Device>();
                    foreach (var deviceId in accessibleDeviceIds)
                    {
                        var device = await _deviceRepository.GetAsync(deviceId, ct);
                        if (device != null)
                        {
                            devices.Add(device);
                        }
                    }
                }
                
                // Map to response
                var response = devices.Select(d => MapToDeviceDto(d)).ToList();
                
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving devices");
                return StatusCode(500, "Error retrieving devices");
            }
        }
        
        // GET: api/v1/devices/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<DeviceDto>> GetDevice(Guid id, CancellationToken ct)
        {
            try
            {
                var device = await _deviceRepository.GetAsync(id, ct);
                if (device == null)
                {
                    return NotFound($"Device with ID {id} not found");
                }
                
                // Check access
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("User not authenticated");
                }
                
                var isAdmin = User.IsInRole("Admin");
                if (!isAdmin)
                {
                    var hasAccess = await _userDeviceAccessRepository.HasAccessAsync(userId, id, ct);
                    
                    if (!hasAccess)
                    {
                        _logger.LogWarning("Access denied: User {UserId} attempted to access Device {DeviceId} without permission", userId, id);
                        return Forbid("You don't have access to this device");
                    }
                }
                
                // Map to response
                var response = MapToDeviceDto(device);
                
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving device with ID {DeviceId}", id);
                return StatusCode(500, "Error retrieving device");
            }
        }
        
        // POST: api/v1/devices
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<DeviceDto>> CreateDevice(
            [FromBody] CreateDeviceRequest request, CancellationToken ct)
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
                await _deviceRepository.AddAsync(device, ct);
                await _unitOfWork.SaveChangesAsync(ct);
                
                // Return response
                var response = MapToDeviceDto(device);
                
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
        
        // PUT: api/v1/devices/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult> UpdateDevice(
            Guid id, [FromBody] UpdateDeviceRequest request, CancellationToken ct)
        {
            try
            {
                // Get existing device
                var device = await _deviceRepository.GetAsync(id, ct);
                if (device == null)
                {
                    return NotFound($"Device with ID {id} not found");
                }
                
                // Check if user has permission to manage this device
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("User not authenticated");
                }
                
                var isAdmin = User.IsInRole("Admin");
                var canManage = isAdmin || await _userDeviceAccessRepository.CanManageAsync(userId, id, ct);
                
                if (!canManage)
                {
                    _logger.LogWarning("Access denied: User {UserId} attempted to update Device {DeviceId} without permission", userId, id);
                    return Forbid("You don't have permission to manage this device");
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
                await _unitOfWork.SaveChangesAsync(ct);
                
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
        
        // DELETE: api/v1/devices/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> DeleteDevice(Guid id, CancellationToken ct)
        {
            try
            {
                // Get existing device
                var device = await _deviceRepository.GetAsync(id, ct);
                if (device == null)
                {
                    return NotFound($"Device with ID {id} not found");
                }
                
                // Remove device
                _deviceRepository.Remove(device);
                await _unitOfWork.SaveChangesAsync(ct);
                
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting device with ID {DeviceId}", id);
                return StatusCode(500, "Error deleting device");
            }
        }
        
        // GET: api/v1/devices/{id}/api-key
        [HttpGet("{id}/api-key")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiKeyResponse>> GetDeviceApiKey(Guid id, CancellationToken ct)
        {
            try
            {
                // Check if device exists
                var device = await _deviceRepository.GetAsync(id, ct);
                if (device == null)
                {
                    return NotFound($"Device with ID {id} not found");
                }
                
                // Get the API key from repository
                var apiKey = await _apiKeyRepository.GetByDeviceIdAsync(id, ct);
                
                if (apiKey == null)
                {
                    // No API key exists yet, generate a new one
                    var ttl = TimeSpan.FromDays(365); // 1 year expiration
                    apiKey = ApiKey.Issue(id, ttl);
                    
                    // Save to database
                    await _apiKeyRepository.AddAsync(apiKey, ct);
                    await _unitOfWork.SaveChangesAsync(ct);
                }
                
                var response = new ApiKeyResponse
                {
                    Key = apiKey.Key,
                    ExpiresAt = apiKey.ExpiresAt
                };
                
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving API key for device with ID {DeviceId}", id);
                return StatusCode(500, "Error retrieving API key");
            }
        }
        
        // POST: api/v1/devices/{id}/regenerate-key
        [HttpPost("{id}/regenerate-key")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiKeyResponse>> RegenerateDeviceApiKey(Guid id, CancellationToken ct)
        {
            try
            {
                // Check if device exists
                var device = await _deviceRepository.GetAsync(id, ct);
                if (device == null)
                {
                    return NotFound($"Device with ID {id} not found");
                }
                
                // Delete any existing API keys for this device
                await _apiKeyRepository.DeleteForDeviceAsync(id, ct);
                
                // Create a new API key with 1 year expiration
                var ttl = TimeSpan.FromDays(365);
                var apiKey = ApiKey.Issue(id, ttl);
                
                // Save to database
                await _apiKeyRepository.AddAsync(apiKey, ct);
                await _unitOfWork.SaveChangesAsync(ct);
                
                var response = new ApiKeyResponse
                {
                    Key = apiKey.Key,
                    ExpiresAt = apiKey.ExpiresAt
                };
                
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error regenerating API key for device with ID {DeviceId}", id);
                return StatusCode(500, "Error regenerating API key");
            }
        }
        
        // POST: api/v1/devices/{id}/api-key
        [HttpPost("{id}/api-key")]
        public async Task<ActionResult<ApiKeyResponse>> RegenerateApiKey(Guid id, CancellationToken ct)
        {
            try
            {
                // Get existing device
                var device = await _deviceRepository.GetAsync(id, ct);
                if (device == null)
                {
                    return NotFound($"Device with ID {id} not found");
                }
                
                // Check if user has permission to manage this device
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("User not authenticated");
                }
                
                var isAdmin = User.IsInRole("Admin");
                var canManage = isAdmin || await _userDeviceAccessRepository.CanManageAsync(userId, id, ct);
                
                if (!canManage)
                {
                    _logger.LogWarning("Access denied: User {UserId} attempted to regenerate API key for Device {DeviceId} without permission", userId, id);
                    return Forbid("You don't have permission to manage this device");
                }
                
                // Generate a new API key
                var ttl = TimeSpan.FromDays(365); // 1 year expiration
                var apiKey = ApiKey.Issue(id, ttl);
                
                // Save API key to the device
                await _apiKeyRepository.AddAsync(apiKey, ct);
                await _unitOfWork.SaveChangesAsync(ct);
                
                // Return the new API key
                return Ok(new ApiKeyResponse
                {
                    Key = apiKey.Key,
                    ExpiresAt = apiKey.ExpiresAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error regenerating API key for device with ID {DeviceId}", id);
                return StatusCode(500, "Error regenerating API key");
            }
        }
        
        #region Helper Methods
        
        private DeviceDto MapToDeviceDto(Device device)
        {
            // Get the latest measurement for device status
            var latestMeasurement = device.Measurements
                .OrderByDescending(m => m.RecordedAt)
                .FirstOrDefault();
            
            // Determine status and last seen time
            var status = "Offline";
            var lastSeen = device.CreatedAt;
            
            if (latestMeasurement != null)
            {
                lastSeen = latestMeasurement.RecordedAt;
                status = latestMeasurement.RecordedAt > DateTimeOffset.UtcNow.AddHours(-1)
                    ? "Online"
                    : "Offline";
            }
            
            return new DeviceDto
            {
                Id = device.Id,
                Name = device.Name,
                Location = device.Location?.ToString(),
                CreatedAt = device.CreatedAt,
                Status = status,
                LastSeen = lastSeen,
                Type = "Standard",
                InstalledOn = device.CreatedAt,
                FirmwareVersion = "1.0.0" // Placeholder
            };
        }
        
        #endregion
    }
} 