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
            // Admin users have access to all devices
            var isAdmin = await _db.UserRoles
                .Join(_db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name })
                .AnyAsync(x => x.UserId.ToString() == userId && x.Name == "Admin", ct);
                
            if (isAdmin)
                return true;
                
            // Check user-specific device permissions
            return await _db.UserDeviceAccess
                .AnyAsync(uda => uda.UserId == userId && uda.DeviceId == deviceId, ct);
        }
        
        public async Task<List<Guid>> GetAccessibleDeviceIdsAsync(string userId, CancellationToken ct = default)
        {
            // Admin users have access to all devices
            var isAdmin = await _db.UserRoles
                .Join(_db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name })
                .AnyAsync(x => x.UserId.ToString() == userId && x.Name == "Admin", ct);
                
            if (isAdmin)
                return await _db.Devices.Select(d => d.Id).ToListAsync(ct);
                
            // Get user-specific device permissions
            return await _db.UserDeviceAccess
                .Where(uda => uda.UserId == userId)
                .Select(uda => uda.DeviceId)
                .ToListAsync(ct);
        }
        
        public async Task<List<UserDeviceAccess>> GetUserDeviceAccessesAsync(string userId, CancellationToken ct = default)
        {
            return await _db.UserDeviceAccess
                .Where(uda => uda.UserId == userId)
                .ToListAsync(ct);
        }
        
        public async Task<bool> CanManageAsync(string userId, Guid deviceId, CancellationToken ct = default)
        {
            // Admin users can manage all devices
            var isAdmin = await _db.UserRoles
                .Join(_db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name })
                .AnyAsync(x => x.UserId.ToString() == userId && x.Name == "Admin", ct);
                
            if (isAdmin)
                return true;
                
            // Check user's management permissions for this specific device
            var access = await _db.UserDeviceAccess
                .FirstOrDefaultAsync(uda => uda.UserId == userId && uda.DeviceId == deviceId, ct);
                
            return access?.CanManage ?? false;
        }
        
        public async Task GrantAccessAsync(string userId, Guid deviceId, bool canManage = false, CancellationToken ct = default)
        {
            // Check if access already exists
            var existingAccess = await _db.UserDeviceAccess
                .FirstOrDefaultAsync(uda => uda.UserId == userId && uda.DeviceId == deviceId, ct);
                
            if (existingAccess != null)
            {
                // Update existing access
                existingAccess.UpdatePermissions(canManage);
            }
            else
            {
                // Create new access
                var newAccess = UserDeviceAccess.Create(userId, deviceId, canManage);
                await _db.UserDeviceAccess.AddAsync(newAccess, ct);
            }
        }
        
        public async Task RevokeAccessAsync(string userId, Guid deviceId, CancellationToken ct = default)
        {
            var access = await _db.UserDeviceAccess
                .FirstOrDefaultAsync(uda => uda.UserId == userId && uda.DeviceId == deviceId, ct);
                
            if (access != null)
            {
                _db.UserDeviceAccess.Remove(access);
            }
        }
    }
} 