using BuzzWatch.Application.Abstractions;
using BuzzWatch.Domain.Devices;
using BuzzWatch.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BuzzWatch.Infrastructure.Repositories
{
    public class DeviceRepository : IDeviceRepository
    {
        private readonly ApplicationDbContext _db;
        
        public DeviceRepository(ApplicationDbContext db) => _db = db;

        public async Task<Device?> GetAsync(Guid id, CancellationToken ct) =>
            await _db.Devices
                .Include(d => d.Measurements.OrderByDescending(m => m.RecordedAt).Take(1))
                .FirstOrDefaultAsync(d => d.Id == id, ct);

        public async Task<List<Device>> GetAllAsync(CancellationToken ct) =>
            await _db.Devices.ToListAsync(ct);

        public async Task AddAsync(Device device, CancellationToken ct)
        {
            await _db.Devices.AddAsync(device, ct);
        }

        public void Update(Device device)
        {
            _db.Devices.Update(device);
        }

        public void Remove(Device device)
        {
            _db.Devices.Remove(device);
        }
    }
} 