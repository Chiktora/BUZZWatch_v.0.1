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

        public async Task<MeasurementHeader?> GetAsync(long id, CancellationToken ct)
        {
            return await _db.Headers
                .Include(h => h.TempIn)
                .Include(h => h.HumIn)
                .Include(h => h.TempOut)
                .Include(h => h.HumOut)
                .Include(h => h.Weight)
                .FirstOrDefaultAsync(m => m.Id == id, ct);
        }

        public async Task<List<MeasurementHeader>> GetByDeviceAsync(
            Guid deviceId, 
            DateTimeOffset from, 
            DateTimeOffset to, 
            int limit = 1000,
            CancellationToken ct = default)
        {
            return await _db.Headers
                .Where(m => m.DeviceId == deviceId && m.RecordedAt >= from && m.RecordedAt <= to)
                .OrderByDescending(m => m.RecordedAt)
                .Take(limit)
                .Include(h => h.TempIn)
                .Include(h => h.HumIn)
                .Include(h => h.TempOut)
                .Include(h => h.HumOut)
                .Include(h => h.Weight)
                .ToListAsync(ct);
        }

        public async Task<List<MeasurementHeader>> GetByDeviceChunkedAsync(
            Guid deviceId,
            DateTimeOffset from,
            DateTimeOffset to,
            int chunkIntervalMinutes = 60,
            CancellationToken ct = default)
        {
            // This method returns a downsampled dataset for chart display
            // Instead of all points, it returns the first point in each time interval
            
            var allHeaders = await _db.Headers
                .Where(h => h.DeviceId == deviceId && h.RecordedAt >= from && h.RecordedAt <= to)
                .OrderBy(h => h.RecordedAt)
                .ToListAsync(ct);
                
            // Group manually to avoid LINQ issues with complex expressions
            var result = allHeaders
                .GroupBy(h => new 
                { 
                    Year = h.RecordedAt.Year,
                    Month = h.RecordedAt.Month,
                    Day = h.RecordedAt.Day,
                    Hour = h.RecordedAt.Hour,
                    ChunkIndex = h.RecordedAt.Minute / chunkIntervalMinutes
                })
                .Select(g => g.OrderBy(h => h.RecordedAt).First())
                .ToList();
                
            // Load related data
            foreach (var header in result)
            {
                await _db.Entry(header).Reference(h => h.TempIn).LoadAsync(ct);
                await _db.Entry(header).Reference(h => h.HumIn).LoadAsync(ct);
                await _db.Entry(header).Reference(h => h.TempOut).LoadAsync(ct);
                await _db.Entry(header).Reference(h => h.HumOut).LoadAsync(ct);
                await _db.Entry(header).Reference(h => h.Weight).LoadAsync(ct);
            }
            
            return result.OrderBy(h => h.RecordedAt).ToList();
        }

        public async Task<MeasurementHeader?> GetLatestByDeviceAsync(Guid deviceId, CancellationToken ct)
        {
            return await _db.Headers
                .Where(m => m.DeviceId == deviceId)
                .OrderByDescending(m => m.RecordedAt)
                .Include(h => h.TempIn)
                .Include(h => h.HumIn)
                .Include(h => h.TempOut)
                .Include(h => h.HumOut)
                .Include(h => h.Weight)
                .FirstOrDefaultAsync(ct);
        }

        public async Task AddAsync(MeasurementHeader measurement, CancellationToken ct)
        {
            await _db.Headers.AddAsync(measurement, ct);
        }
    }
} 