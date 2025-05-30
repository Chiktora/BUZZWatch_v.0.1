using BuzzWatch.Domain.Common;
using BuzzWatch.Domain.Measurements;

namespace BuzzWatch.Domain.Devices
{
    /// <summary>
    /// Represents a BuzzWatch device.
    /// 
    /// Note: Access control to devices is managed through the UserDeviceAccess entity,
    /// not through properties on the Device itself. This ensures proper separation of concerns.
    /// 
    /// Only administrators and explicitly authorized users should have access to devices.
    /// </summary>
    public class Device : BaseEntity<Guid>
    {
        private Device() { } // EF Core constructor

        public string Name { get; private set; } = default!;
        public HiveLocation? Location { get; private set; }
        public DateTimeOffset CreatedAt { get; private set; }

        private readonly List<MeasurementHeader> _measurements = new();
        public IReadOnlyCollection<MeasurementHeader> Measurements => _measurements;

        public static Device Create(string name, HiveLocation? location = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Device name required", nameof(name));

            return new Device
            {
                Id = Guid.NewGuid(),
                Name = name.Trim(),
                Location = location,
                CreatedAt = DateTimeOffset.UtcNow
            };
        }

        public void UpdateName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Device name required", nameof(name));

            Name = name.Trim();
        }

        public void UpdateLocation(HiveLocation? location)
        {
            Location = location;
        }
    }
} 