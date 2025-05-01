using BuzzWatch.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace BuzzWatch.Api.Authorization
{
    public class OwnsDeviceRequirement : IAuthorizationRequirement
    {
    }

    public class OwnsDeviceHandler : AuthorizationHandler<OwnsDeviceRequirement>
    {
        private readonly ApplicationDbContext _db;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public OwnsDeviceHandler(ApplicationDbContext db, IHttpContextAccessor httpContextAccessor)
        {
            _db = db;
            _httpContextAccessor = httpContextAccessor;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            OwnsDeviceRequirement requirement)
        {
            // Get the device ID from the route
            var routeData = _httpContextAccessor.HttpContext?.GetRouteData();
            if (routeData == null || !routeData.Values.TryGetValue("deviceId", out var deviceIdObj))
                return;

            if (!Guid.TryParse(deviceIdObj?.ToString(), out var deviceId))
                return;

            // Get the user ID from the claims
            var userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId == null || !Guid.TryParse(userId, out var userGuid))
                return;

            // Check if the user is an admin - admins can access all devices
            if (context.User.IsInRole("Admin"))
            {
                context.Succeed(requirement);
                return;
            }

            // For now, we'll just assume there's a UserDevice table
            // In the future, you would need to create this table and repository
            // For now, we'll just check if the user has any devices assigned
            
            var userOwnsDevice = false; // Replace with actual query to UserDevice table
            // var userOwnsDevice = await _db.UserDevices.AnyAsync(ud => ud.UserId == userGuid && ud.DeviceId == deviceId);
            
            if (userOwnsDevice)
                context.Succeed(requirement);
        }
    }
} 