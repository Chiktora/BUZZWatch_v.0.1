namespace BuzzWatch.Web.Models.Api
{
    // API Request Models
    public class CreateDeviceRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string DeviceKey { get; set; } = string.Empty;
    }

    public class UpdateDeviceRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    // API Response Models
    public class DeviceResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string DeviceKey { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTimeOffset LastSeen { get; set; }
        public bool IsOnline { get; set; }
    }

    public class DeviceWithReadingsResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTimeOffset LastSeen { get; set; }
        public bool IsOnline { get; set; }
        public List<ReadingResponse> LatestReadings { get; set; } = new();
    }

    public class ReadingResponse
    {
        public Guid Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public double Value { get; set; }
        public string Unit { get; set; } = string.Empty;
        public DateTimeOffset Timestamp { get; set; }
    }
} 