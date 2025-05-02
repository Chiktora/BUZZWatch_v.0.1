using System.Text.Json.Serialization;

namespace BuzzWatch.Contracts.Devices
{
    public class DeviceDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Location { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
} 