using BuzzWatch.Domain.Devices;

namespace BuzzWatch.Application.Abstractions
{
    public interface IDeviceRepository
    {
        Task<Device?> GetAsync(Guid id, CancellationToken ct);
        Task<List<Device>> GetAllAsync(CancellationToken ct);
        Task AddAsync(Device device, CancellationToken ct);
        void Update(Device device);
        void Remove(Device device);
    }
} 