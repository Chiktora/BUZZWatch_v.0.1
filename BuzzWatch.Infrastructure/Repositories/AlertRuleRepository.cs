using BuzzWatch.Application.Abstractions;
using BuzzWatch.Domain.Alerts;
using BuzzWatch.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BuzzWatch.Infrastructure.Repositories
{
    public class AlertRuleRepository : IAlertRuleRepository
    {
        private readonly ApplicationDbContext _db;
        
        public AlertRuleRepository(ApplicationDbContext db) => _db = db;
        
        public async Task<AlertRule?> GetAsync(Guid id, CancellationToken ct = default)
        {
            return await _db.Set<AlertRule>().FindAsync(new object[] { id }, ct);
        }
        
        public async Task<List<AlertRule>> GetByDeviceAsync(Guid deviceId, CancellationToken ct = default)
        {
            return await _db.Set<AlertRule>()
                .Where(r => r.DeviceId == deviceId)
                .ToListAsync(ct);
        }
        
        public async Task<List<AlertRule>> GetActiveRulesAsync(CancellationToken ct = default)
        {
            return await _db.Set<AlertRule>()
                .Where(r => r.Active)
                .ToListAsync(ct);
        }
        
        public async Task AddAsync(AlertRule rule, CancellationToken ct = default)
        {
            await _db.Set<AlertRule>().AddAsync(rule, ct);
        }
        
        public void Update(AlertRule rule)
        {
            _db.Set<AlertRule>().Update(rule);
        }
        
        public void Remove(AlertRule rule)
        {
            _db.Set<AlertRule>().Remove(rule);
        }
    }
} 