using System.Text.Json.Serialization;

namespace BuzzWatch.Contracts.Devices
{
    public class DeviceDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Location { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public string Status { get; set; } = "Offline";
        public DateTimeOffset LastSeen { get; set; }
        public string Type { get; set; } = "Standard";
        public DateTimeOffset? InstalledOn { get; set; }
        public string FirmwareVersion { get; set; } = "1.0.0";
    }
} 