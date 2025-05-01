using BuzzWatch.Domain.Common;

namespace BuzzWatch.Domain.Security
{
    public class ApiKey : BaseEntity<Guid>
    {
        public string Key { get; private set; } = default!;
        public Guid DeviceId { get; private set; }
        public DateTimeOffset ExpiresAt { get; private set; }

        private ApiKey() { } // Required for EF Core

        public static ApiKey Issue(Guid deviceId, TimeSpan ttl)
            => new()
            {
                Id = Guid.NewGuid(),
                DeviceId = deviceId,
                Key = Guid.NewGuid().ToString("N"),
                ExpiresAt = DateTimeOffset.UtcNow.Add(ttl)
            };
    }
} 