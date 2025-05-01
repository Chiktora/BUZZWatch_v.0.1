using BuzzWatch.Domain.Measurements;

namespace BuzzWatch.Application.Abstractions
{
    public interface IMeasurementRepository
    {
        Task<MeasurementHeader?> GetAsync(long id, CancellationToken ct);
        Task<List<MeasurementHeader>> GetByDeviceAsync(Guid deviceId, DateTimeOffset from, DateTimeOffset to, CancellationToken ct);
        Task<MeasurementHeader?> GetLatestByDeviceAsync(Guid deviceId, CancellationToken ct);
        Task AddAsync(MeasurementHeader measurement, CancellationToken ct);
    }
} 