using BuzzWatch.Web.Models.Admin;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace BuzzWatch.Web.Services
{
    public interface IAuditLogService
    {
        Task LogActionAsync(string action, string area, string controller, string description = "", string entityId = "", string entityType = "");
        Task LogActionAsync(AuditLogEntry entry);
        Task<List<AuditLogEntry>> GetRecentLogsAsync(int count = 20);
        Task<List<AuditLogEntry>> GetLogsByUserAsync(string userId, int count = 50);
        Task<List<AuditLogEntry>> GetLogsByDateRangeAsync(DateTimeOffset startDate, DateTimeOffset endDate, int page = 1, int pageSize = 20);
        Task<List<AuditLogEntry>> SearchLogsAsync(string searchTerm, int page = 1, int pageSize = 20);
    }

    public class AuditLogService : IAuditLogService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly List<AuditLogEntry> _mockAuditLogs; // Temporary in-memory storage for demo

        public AuditLogService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
            // Seed some mock data for demonstration
            _mockAuditLogs = GenerateMockAuditLogs();
        }

        public async Task LogActionAsync(string action, string area, string controller, string description = "", string entityId = "", string entityType = "")
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
                return;

            var userId = httpContext.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
            var userName = httpContext.User?.FindFirstValue(ClaimTypes.Name) ?? "System";
            var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            var entry = new AuditLogEntry
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                UserName = userName,
                Action = action,
                Area = area,
                Controller = controller,
                Description = description,
                EntityId = entityId,
                EntityType = entityType,
                IpAddress = ipAddress,
                Timestamp = DateTimeOffset.UtcNow
            };

            await LogActionAsync(entry);
        }

        public Task LogActionAsync(AuditLogEntry entry)
        {
            // In a real implementation, this would save to a database
            // For this demo, we'll add to our in-memory collection
            _mockAuditLogs.Add(entry);
            
            return Task.CompletedTask;
        }

        public Task<List<AuditLogEntry>> GetRecentLogsAsync(int count = 20)
        {
            var logs = _mockAuditLogs
                .OrderByDescending(l => l.Timestamp)
                .Take(count)
                .ToList();
                
            return Task.FromResult(logs);
        }

        public Task<List<AuditLogEntry>> GetLogsByUserAsync(string userId, int count = 50)
        {
            var logs = _mockAuditLogs
                .Where(l => l.UserId == userId)
                .OrderByDescending(l => l.Timestamp)
                .Take(count)
                .ToList();
                
            return Task.FromResult(logs);
        }

        public Task<List<AuditLogEntry>> GetLogsByDateRangeAsync(DateTimeOffset startDate, DateTimeOffset endDate, int page = 1, int pageSize = 20)
        {
            var logs = _mockAuditLogs
                .Where(l => l.Timestamp >= startDate && l.Timestamp <= endDate)
                .OrderByDescending(l => l.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
                
            return Task.FromResult(logs);
        }

        public Task<List<AuditLogEntry>> SearchLogsAsync(string searchTerm, int page = 1, int pageSize = 20)
        {
            searchTerm = searchTerm.ToLower();
            var logs = _mockAuditLogs
                .Where(l => 
                    l.Action.ToLower().Contains(searchTerm) ||
                    l.UserName.ToLower().Contains(searchTerm) ||
                    l.Description.ToLower().Contains(searchTerm) ||
                    l.EntityType.ToLower().Contains(searchTerm) ||
                    l.Controller.ToLower().Contains(searchTerm))
                .OrderByDescending(l => l.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
                
            return Task.FromResult(logs);
        }

        // Generate sample data for demonstration
        private List<AuditLogEntry> GenerateMockAuditLogs()
        {
            return new List<AuditLogEntry>
            {
                new AuditLogEntry { Id = Guid.NewGuid(), UserId = "admin-1", UserName = "Admin User", Action = "Create", Area = "Admin", Controller = "Users", Description = "Created new user", EntityId = Guid.NewGuid().ToString(), EntityType = "User", IpAddress = "192.168.1.1", Timestamp = DateTimeOffset.UtcNow.AddDays(-1) },
                new AuditLogEntry { Id = Guid.NewGuid(), UserId = "admin-1", UserName = "Admin User", Action = "Update", Area = "Admin", Controller = "Devices", Description = "Updated device settings", EntityId = Guid.NewGuid().ToString(), EntityType = "Device", IpAddress = "192.168.1.1", Timestamp = DateTimeOffset.UtcNow.AddHours(-3) },
                new AuditLogEntry { Id = Guid.NewGuid(), UserId = "admin-1", UserName = "Admin User", Action = "Delete", Area = "Admin", Controller = "AlertRules", Description = "Deleted alert rule", EntityId = Guid.NewGuid().ToString(), EntityType = "AlertRule", IpAddress = "192.168.1.1", Timestamp = DateTimeOffset.UtcNow.AddHours(-5) },
                new AuditLogEntry { Id = Guid.NewGuid(), UserId = "admin-2", UserName = "System Admin", Action = "Update", Area = "Admin", Controller = "Settings", Description = "Updated system settings", EntityId = "system", EntityType = "Settings", IpAddress = "192.168.1.2", Timestamp = DateTimeOffset.UtcNow.AddDays(-2) },
                new AuditLogEntry { Id = Guid.NewGuid(), UserId = "admin-2", UserName = "System Admin", Action = "Update", Area = "Admin", Controller = "Users", Description = "Modified user permissions", EntityId = Guid.NewGuid().ToString(), EntityType = "User", IpAddress = "192.168.1.2", Timestamp = DateTimeOffset.UtcNow.AddHours(-8) }
            };
        }
    }
} 