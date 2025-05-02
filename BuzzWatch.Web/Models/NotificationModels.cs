using System;
using System.Collections.Generic;

namespace BuzzWatch.Web.Models
{
    public class NotificationDto
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public NotificationType Type { get; set; }
        public string DeviceId { get; set; } = string.Empty;
        public string DeviceName { get; set; } = string.Empty;
        public DateTimeOffset Timestamp { get; set; }
        public bool IsRead { get; set; }
        public string Url { get; set; } = string.Empty;
    }

    public enum NotificationType
    {
        Info,
        Warning,
        Alert,
        Success
    }

    public class NotificationListViewModel
    {
        public List<NotificationDto> Notifications { get; set; } = new List<NotificationDto>();
        public int UnreadCount { get; set; }
    }

    public class NotificationCenterViewModel
    {
        public int UnreadCount { get; set; }
        public List<NotificationDto> RecentNotifications { get; set; } = new List<NotificationDto>();
    }
} 