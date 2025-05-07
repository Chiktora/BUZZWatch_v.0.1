using System;

namespace BuzzWatch.Web.Models
{
    public class UserDevicePermissionDto
    {
        public Guid UserId { get; set; }
        public Guid DeviceId { get; set; }
        public string DeviceName { get; set; } = string.Empty;
        public string DeviceLocation { get; set; } = string.Empty;
        public bool HasAccess { get; set; }
        public bool CanManage { get; set; }
    }
} 