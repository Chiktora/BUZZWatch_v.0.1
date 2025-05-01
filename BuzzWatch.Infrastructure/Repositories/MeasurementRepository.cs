using BuzzWatch.Application.Abstractions;
using BuzzWatch.Domain.Measurements;
using BuzzWatch.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BuzzWatch.Infrastructure.Repositories
{
    public class MeasurementRepository : IMeasurementRepository
    {
        private readonly ApplicationDbContext _db;
        
        public MeasurementRepository(ApplicationDbContext db) => _db = db;

        public async Task<MeasurementHeader?> GetAsync(long id, CancellationToken ct) =>
            await _db.Headers.FindAsync(new object[] { id }, ct);

        public async Task<List<MeasurementHeader>> GetByDeviceAsync(Guid deviceId, DateTimeOffset from, DateTimeOffset to, CancellationToken ct) =>
            await _db.Headers
                .Where(m => m.DeviceId == deviceId && m.RecordedAt >= from && m.RecordedAt <= to)
                .OrderBy(m => m.RecordedAt)
                .ToListAsync(ct);

        public async Task<MeasurementHeader?> GetLatestByDeviceAsync(Guid deviceId, CancellationToken ct) =>
            await _db.Headers
                .Where(m => m.DeviceId == deviceId)
                .OrderByDescending(m => m.RecordedAt)
                .FirstOrDefaultAsync(ct);

        public async Task AddAsync(MeasurementHeader measurement, CancellationToken ct)
        {
            await _db.Headers.AddAsync(measurement, ct);
        }
    }
} 