using BuzzWatch.Application.Abstractions;
using BuzzWatch.Domain.Security;
using BuzzWatch.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BuzzWatch.Infrastructure.Repositories
{
    public class UserDeviceAccessRepository : IUserDeviceAccessRepository
    {
        private readonly ApplicationDbContext _db;
        
        public UserDeviceAccessRepository(ApplicationDbContext db) => _db = db;
        
        public async Task<bool> HasAccessAsync(string userId, Guid deviceId, CancellationToken ct = default)
        {
            // For now, a simple implementation that grants access to everyone
            // In a real implementation, this would check a user_device_access table
            
            // Admin users have access to all devices
            var isAdmin = await _db.UserRoles
                .Join(_db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name })
                .AnyAsync(x => x.UserId.ToString() == userId && x.Name == "Admin", ct);
                
            if (isAdmin)
                return true;
                
            // TODO: Check user-specific device permissions
            // For now, return true for everyone
            return true;
        }
        
        public async Task<List<Guid>> GetAccessibleDeviceIdsAsync(string userId, CancellationToken ct = default)
        {
            // For now, return all device IDs
            // In a real implementation, this would filter based on user permissions
            
            // Admin users have access to all devices
            var isAdmin = await _db.UserRoles
                .Join(_db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name })
                .AnyAsync(x => x.UserId.ToString() == userId && x.Name == "Admin", ct);
                
            if (isAdmin)
                return await _db.Devices.Select(d => d.Id).ToListAsync(ct);
                
            // TODO: Get user-specific device permissions
            // For now, return all devices
            return await _db.Devices.Select(d => d.Id).ToListAsync(ct);
        }
        
        public async Task GrantAccessAsync(string userId, Guid deviceId, bool canManage = false, CancellationToken ct = default)
        {
            // TODO: Implement storing user device access permissions
            // This would create or update a record in a user_device_access table
            
            await Task.CompletedTask; // For now, just return completed task
        }
        
        public async Task RevokeAccessAsync(string userId, Guid deviceId, CancellationToken ct = default)
        {
            // TODO: Implement removing user device access permissions
            // This would delete a record from a user_device_access table
            
            await Task.CompletedTask; // For now, just return completed task
        }
    }
} 