using System.ComponentModel.DataAnnotations;
using BuzzWatch.Contracts.Admin;

namespace BuzzWatch.Web.Models.Api
{
    // API Request Models
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

    public class UpdateUserDeviceRequest
    {
        public Guid UserId { get; set; }
        public Guid DeviceId { get; set; }
        public bool HasAccess { get; set; }
        public bool CanManage { get; set; }
    }

    // API Response Models
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

    public class UserDeviceResponse
    {
        public Guid UserId { get; set; }
        public Guid DeviceId { get; set; }
        public string DeviceName { get; set; } = string.Empty;
        public string DeviceLocation { get; set; } = string.Empty;
        public bool HasAccess { get; set; }
        public bool CanManage { get; set; }
    }
} 