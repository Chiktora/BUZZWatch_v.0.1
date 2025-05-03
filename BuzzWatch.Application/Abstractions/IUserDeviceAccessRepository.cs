namespace BuzzWatch.Application.Abstractions
{
    public interface IUserDeviceAccessRepository
    {
        Task<bool> HasAccessAsync(string userId, Guid deviceId, CancellationToken ct = default);
        Task<List<Guid>> GetAccessibleDeviceIdsAsync(string userId, CancellationToken ct = default);
        Task GrantAccessAsync(string userId, Guid deviceId, bool canManage = false, CancellationToken ct = default);
        Task RevokeAccessAsync(string userId, Guid deviceId, CancellationToken ct = default);
    }
} 