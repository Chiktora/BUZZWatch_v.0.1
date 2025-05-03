using BuzzWatch.Application.Abstractions;
using BuzzWatch.Contracts.Devices;
using MediatR;

namespace BuzzWatch.Application.Devices.Queries
{
    public record GetDeviceQuery(Guid Id) : IRequest<DeviceDto?>;
    
    public class GetDeviceQueryHandler : IRequestHandler<GetDeviceQuery, DeviceDto?>
    {
        private readonly IDeviceRepository _deviceRepository;
        
        public GetDeviceQueryHandler(IDeviceRepository deviceRepository)
        {
            _deviceRepository = deviceRepository;
        }
        
        public async Task<DeviceDto?> Handle(GetDeviceQuery request, CancellationToken cancellationToken)
        {
            var device = await _deviceRepository.GetAsync(request.Id, cancellationToken);
            
            if (device == null)
                return null;
                
            // Map to DTO
            return new DeviceDto
            {
                Id = device.Id,
                Name = device.Name,
                Location = device.Location?.ToString() ?? "Unknown",
                Status = GetDeviceStatus(device),
                FirmwareVersion = "1.0", // Placeholder, would come from device properties
                LastSeen = GetLastSeenTime(device),
                InstalledOn = DateTimeOffset.UtcNow.AddDays(-30), // Placeholder
                Type = "Standard" // Placeholder
            };
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