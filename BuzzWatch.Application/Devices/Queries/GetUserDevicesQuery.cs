using BuzzWatch.Application.Abstractions;
using BuzzWatch.Contracts.Devices;
using MediatR;

namespace BuzzWatch.Application.Devices.Queries
{
    public record GetUserDevicesQuery(string UserId) : IRequest<List<DeviceDto>>;
    
    public class GetUserDevicesQueryHandler : IRequestHandler<GetUserDevicesQuery, List<DeviceDto>>
    {
        private readonly IDeviceRepository _deviceRepository;
        private readonly IUserDeviceAccessRepository _userDeviceAccessRepository;
        
        public GetUserDevicesQueryHandler(
            IDeviceRepository deviceRepository,
            IUserDeviceAccessRepository userDeviceAccessRepository)
        {
            _deviceRepository = deviceRepository;
            _userDeviceAccessRepository = userDeviceAccessRepository;
        }
        
        public async Task<List<DeviceDto>> Handle(GetUserDevicesQuery request, CancellationToken cancellationToken)
        {
            // For administrators, return all devices
            // var isAdmin = await _userService.IsInRoleAsync(request.UserId, "Admin");
            
            // For now, just return all devices - we'll implement proper access control later
            var devices = await _deviceRepository.GetAllAsync(cancellationToken);
            
            // Map to DTOs
            return devices.Select(d => new DeviceDto
            {
                Id = d.Id,
                Name = d.Name,
                Location = d.Location?.ToString() ?? "Unknown",
                Status = GetDeviceStatus(d),
                FirmwareVersion = "1.0", // Placeholder, would come from device properties
                LastSeen = GetLastSeenTime(d),
                InstalledOn = DateTimeOffset.UtcNow.AddDays(-30), // Placeholder
                Type = "Standard" // Placeholder
            }).ToList();
        }
        
        private string GetDeviceStatus(Domain.Devices.Device device)
        {
            // If we have measurements in the last hour, consider online
            var latestMeasurement = device.Measurements
                .OrderByDescending(m => m.RecordedAt)
                .FirstOrDefault();
                
            if (latestMeasurement == null)
                return "Offline";
                
            return latestMeasurement.RecordedAt > DateTimeOffset.UtcNow.AddHours(-1)
                ? "Online"
                : "Offline";
        }
        
        private DateTimeOffset GetLastSeenTime(Domain.Devices.Device device)
        {
            var latestMeasurement = device.Measurements
                .OrderByDescending(m => m.RecordedAt)
                .FirstOrDefault();
                
            return latestMeasurement?.RecordedAt ?? DateTimeOffset.UtcNow.AddDays(-1);
        }
    }
} 