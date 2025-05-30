using BuzzWatch.Domain.Measurements;

namespace BuzzWatch.Application.Abstractions
{
    public interface IMeasurementRepository
    {
        Task<MeasurementHeader?> GetAsync(long id, CancellationToken ct);
        Task<List<MeasurementHeader>> GetByDeviceAsync(
            Guid deviceId, 
            DateTimeOffset from, 
            DateTimeOffset to, 
            int limit = 1000,
            CancellationToken ct = default);
        Task<List<MeasurementHeader>> GetByDeviceChunkedAsync(
            Guid deviceId,
            DateTimeOffset from,
            DateTimeOffset to,
            int chunkIntervalMinutes = 60,
            CancellationToken ct = default);
        Task<MeasurementHeader?> GetLatestByDeviceAsync(Guid deviceId, CancellationToken ct);
        Task AddAsync(MeasurementHeader measurement, CancellationToken ct);
    }
} 