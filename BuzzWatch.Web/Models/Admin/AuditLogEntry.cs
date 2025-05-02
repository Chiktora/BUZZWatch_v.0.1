using System;
using System.Text.Json;

namespace BuzzWatch.Web.Models.Admin
{
    public class AuditLogEntry
    {
        public Guid Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Area { get; set; } = string.Empty;
        public string Controller { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public string OldValues { get; set; } = string.Empty;
        public string NewValues { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public DateTimeOffset Timestamp { get; set; }

        // Helper methods for setting values
        public void SetOldValues<T>(T oldValues)
        {
            OldValues = JsonSerializer.Serialize(oldValues);
        }
        
        public void SetNewValues<T>(T newValues)
        {
            NewValues = JsonSerializer.Serialize(newValues);
        }
        
        // Helper method to create common audit entry
        public static AuditLogEntry CreateEntry(string userId, string userName, string action,
            string area, string controller, string description = "")
        {
            return new AuditLogEntry
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                UserName = userName,
                Action = action,
                Area = area,
                Controller = controller,
                Description = description,
                Timestamp = DateTimeOffset.UtcNow
            };
        }
    }
} 