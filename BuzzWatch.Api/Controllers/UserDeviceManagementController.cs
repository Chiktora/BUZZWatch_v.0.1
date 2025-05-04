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
                // Verify user exists
                var user = await _userManager.FindByIdAsync(userId.ToString());
                if (user == null)
                {
                    return NotFound("User not found");
                }

                // Get devices this user has access to
                var accessibleDeviceIds = await _userDeviceAccessRepository.GetAccessibleDeviceIdsAsync(
                    userId.ToString(), ct);
                
                // Get all devices to include those the user doesn't have access to
                var allDevices = await _deviceRepository.GetAllAsync(ct);
                
                // Build response
                var response = new List<UserDeviceResponse>();
                
                foreach (var device in allDevices)
                {
                    // Check if user has access to this device
                    var hasAccess = accessibleDeviceIds.Contains(device.Id);
                    
                    response.Add(new UserDeviceResponse
                    {
                        UserId = userId,
                        DeviceId = device.Id,
                        DeviceName = device.Name,
                        DeviceLocation = device.Location?.ToString() ?? "Not specified",
                        HasAccess = hasAccess,
                        CanManage = false // We don't have this information currently
                    });
                }
                
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving device permissions for user {UserId}", userId);
                return StatusCode(500, "An error occurred while retrieving device permissions");
            }
        }

        // POST: api/v1/user-devices
        [HttpPost]
        public async Task<ActionResult> AssignDeviceToUser([FromBody] UpdateUserDeviceRequest request, CancellationToken ct)
        {
            try
            {
                // Verify user exists
                var user = await _userManager.FindByIdAsync(request.UserId.ToString());
                if (user == null)
                {
                    return NotFound("User not found");
                }
                
                // Verify device exists
                var device = await _deviceRepository.GetAsync(request.DeviceId, ct);
                if (device == null)
                {
                    return NotFound("Device not found");
                }
                
                if (request.HasAccess)
                {
                    // Grant access to device
                    await _userDeviceAccessRepository.GrantAccessAsync(
                        request.UserId.ToString(), request.DeviceId, request.CanManage, ct);
                }
                else
                {
                    // Remove access from device
                    await _userDeviceAccessRepository.RevokeAccessAsync(
                        request.UserId.ToString(), request.DeviceId, ct);
                }
                
                await _unitOfWork.SaveChangesAsync(ct);
                
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating device access for user {UserId} to device {DeviceId}",
                    request.UserId, request.DeviceId);
                return StatusCode(500, "An error occurred while updating device access");
            }
        }

        // DELETE: api/v1/user-devices?userId={userId}&deviceId={deviceId}
        [HttpDelete]
        public async Task<ActionResult> RemoveDeviceFromUser([FromQuery] Guid userId, [FromQuery] Guid deviceId, CancellationToken ct)
        {
            try
            {
                // Verify user exists
                var user = await _userManager.FindByIdAsync(userId.ToString());
                if (user == null)
                {
                    return NotFound("User not found");
                }
                
                // Verify device exists
                var device = await _deviceRepository.GetAsync(deviceId, ct);
                if (device == null)
                {
                    return NotFound("Device not found");
                }
                
                // Remove access from device
                await _userDeviceAccessRepository.RevokeAccessAsync(userId.ToString(), deviceId, ct);
                await _unitOfWork.SaveChangesAsync(ct);
                
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing device access for user {UserId} to device {DeviceId}",
                    userId, deviceId);
                return StatusCode(500, "An error occurred while removing device access");
            }
        }
    }
} 