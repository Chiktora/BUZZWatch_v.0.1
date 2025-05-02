namespace BuzzWatch.Contracts.Admin
{
    public class UserResponse
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? LastLogin { get; set; }
    }

    public class CreateUserRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }

    public class UpdateUserRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string? Password { get; set; }
    }

    public class DeviceResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int BatteryLevel { get; set; }
        public DateTimeOffset LastSeen { get; set; }
        public bool IsOnline => Status.ToLower() == "online";
    }

    public class UserDeviceResponse
    {
        public Guid UserId { get; set; }
        public Guid DeviceId { get; set; }
        public string DeviceName { get; set; } = string.Empty;
        public string DeviceLocation { get; set; } = string.Empty;
        public bool HasAccess { get; set; }
        public bool CanManage { get; set; }
    }

    public class UpdateUserDeviceRequest
    {
        public Guid UserId { get; set; }
        public Guid DeviceId { get; set; }
        public bool HasAccess { get; set; }
        public bool CanManage { get; set; }
    }
} 