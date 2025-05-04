using BuzzWatch.Application.Abstractions;
using BuzzWatch.Domain.Security;
using BuzzWatch.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BuzzWatch.Infrastructure.Repositories
{
    public class ApiKeyRepository : IApiKeyRepository
    {
        private readonly ApplicationDbContext _dbContext;

        public ApiKeyRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<ApiKey?> GetByKeyAsync(string key, CancellationToken ct = default)
        {
            return await _dbContext.ApiKeys
                .FirstOrDefaultAsync(k => k.Key == key && k.ExpiresAt > DateTimeOffset.UtcNow, ct);
        }

        public async Task<ApiKey?> GetByDeviceIdAsync(Guid deviceId, CancellationToken ct = default)
        {
            return await _dbContext.ApiKeys
                .FirstOrDefaultAsync(k => k.DeviceId == deviceId && k.ExpiresAt > DateTimeOffset.UtcNow, ct);
        }

        public async Task<bool> DeleteForDeviceAsync(Guid deviceId, CancellationToken ct = default)
        {
            var existingKeys = await _dbContext.ApiKeys
                .Where(k => k.DeviceId == deviceId)
                .ToListAsync(ct);

            if (!existingKeys.Any())
                return false;

            _dbContext.ApiKeys.RemoveRange(existingKeys);
            return true;
        }

        public async Task AddAsync(ApiKey apiKey, CancellationToken ct = default)
        {
            await _dbContext.ApiKeys.AddAsync(apiKey, ct);
        }
    }
} 