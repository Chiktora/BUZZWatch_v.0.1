using BuzzWatch.Domain.Common;

namespace BuzzWatch.Domain.Security
{
    /// <summary>
    /// Represents user access permissions for a specific device
    /// </summary>
    public class UserDeviceAccess : BaseEntity<long>
    {
        private UserDeviceAccess() { } // EF Core constructor
        
        public string UserId { get; private set; } = default!;
        public Guid DeviceId { get; private set; }
        public bool CanManage { get; private set; }
        public DateTimeOffset GrantedAt { get; private set; }
        public DateTimeOffset? LastUpdatedAt { get; private set; }
        
        /// <summary>
        /// Creates a new device access permission for a user
        /// </summary>
        public static UserDeviceAccess Create(string userId, Guid deviceId, bool canManage = false)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID is required", nameof(userId));
                
            return new UserDeviceAccess
            {
                UserId = userId,
                DeviceId = deviceId,
                CanManage = canManage,
                GrantedAt = DateTimeOffset.UtcNow
            };
        }
        
        /// <summary>
        /// Updates management permissions for this device access
        /// </summary>
        public void UpdatePermissions(bool canManage)
        {
            CanManage = canManage;
            LastUpdatedAt = DateTimeOffset.UtcNow;
        }
    }
} 