using BuzzWatch.Domain.Security;

namespace BuzzWatch.Application.Abstractions
{
    public interface IApiKeyRepository
    {
        Task<ApiKey?> GetByKeyAsync(string key, CancellationToken ct = default);
        Task<ApiKey?> GetByDeviceIdAsync(Guid deviceId, CancellationToken ct = default);
        Task<bool> DeleteForDeviceAsync(Guid deviceId, CancellationToken ct = default);
        Task AddAsync(ApiKey apiKey, CancellationToken ct = default);
    }
} 