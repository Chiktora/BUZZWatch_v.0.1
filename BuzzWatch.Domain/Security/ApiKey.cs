using BuzzWatch.Domain.Common;

namespace BuzzWatch.Domain.Security
{
    public class ApiKey : BaseEntity<Guid>
    {
        private ApiKey() { } // EF Core constructor
        
        public string Name { get; private set; } = default!;
        public Guid DeviceId { get; private set; }
        public DateTimeOffset CreatedAt { get; private set; }
        public DateTimeOffset? ExpiresAt { get; private set; }
        public bool IsActive => !ExpiresAt.HasValue || ExpiresAt.Value > DateTimeOffset.UtcNow;
        
        public static ApiKey Create(string name, Guid deviceId, DateTimeOffset? expiresAt = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("API key name is required", nameof(name));
                
            return new ApiKey
            {
                Id = Guid.NewGuid(),
                Name = name.Trim(),
                DeviceId = deviceId,
                CreatedAt = DateTimeOffset.UtcNow,
                ExpiresAt = expiresAt
            };
        }
        
        public void Extend(DateTimeOffset newExpiry)
        {
            if (newExpiry <= DateTimeOffset.UtcNow)
                throw new ArgumentException("Expiry date must be in the future", nameof(newExpiry));
                
            ExpiresAt = newExpiry;
        }
        
        public void Revoke()
        {
            ExpiresAt = DateTimeOffset.UtcNow;
        }
    }
} 