using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using BuzzWatch.Contracts.Measurements;

namespace BuzzWatch.Web.Hubs
{
    [Authorize]
    public class MeasurementNotificationHub : Hub
    {
        private readonly ILogger<MeasurementNotificationHub> _logger;

        public MeasurementNotificationHub(ILogger<MeasurementNotificationHub> logger)
        {
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.Identity?.Name;
            _logger.LogInformation("User {UserId} connected to measurement hub", userId);
            
            // Add the user to their own group for targeted notifications
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");
            }
            
            await base.OnConnectedAsync();
        }
        
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.Identity?.Name;
            _logger.LogInformation("User {UserId} disconnected from measurement hub", userId);
            
            await base.OnDisconnectedAsync(exception);
        }
        
        public async Task JoinDeviceGroup(string deviceId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"device-{deviceId}");
            _logger.LogInformation("Client {ConnectionId} joined device group {DeviceId}", Context.ConnectionId, deviceId);
        }
        
        public async Task LeaveDeviceGroup(string deviceId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"device-{deviceId}");
            _logger.LogInformation("Client {ConnectionId} left device group {DeviceId}", Context.ConnectionId, deviceId);
        }
        
        // These methods will be called by server-side code, not by clients
        
        public async Task BroadcastMeasurement(MeasurementDto measurement)
        {
            await Clients.All.SendAsync("ReceiveMeasurement", measurement);
        }
        
        public async Task SendMeasurementToDevice(string deviceId, MeasurementDto measurement)
        {
            await Clients.Group($"device-{deviceId}").SendAsync("ReceiveMeasurement", measurement);
        }
        
        public async Task SendAlert(string userId, string message, string severity)
        {
            await Clients.Group($"user-{userId}").SendAsync("ReceiveAlert", new { message, severity });
        }
    }
} 