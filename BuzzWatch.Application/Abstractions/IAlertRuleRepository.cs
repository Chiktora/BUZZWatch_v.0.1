using BuzzWatch.Domain.Alerts;

namespace BuzzWatch.Application.Abstractions
{
    public interface IAlertRuleRepository
    {
        Task<AlertRule?> GetAsync(Guid id, CancellationToken ct = default);
        Task<List<AlertRule>> GetByDeviceAsync(Guid deviceId, CancellationToken ct = default);
        Task<List<AlertRule>> GetActiveRulesAsync(CancellationToken ct = default);
        Task AddAsync(AlertRule rule, CancellationToken ct = default);
        void Update(AlertRule rule);
        void Remove(AlertRule rule);
    }
} 