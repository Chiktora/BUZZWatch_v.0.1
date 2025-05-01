using BuzzWatch.Application.Abstractions;
using BuzzWatch.Infrastructure.Data;

namespace BuzzWatch.Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _db;
        
        public UnitOfWork(ApplicationDbContext db) => _db = db;

        public async Task<int> SaveChangesAsync(CancellationToken ct)
        {
            return await _db.SaveChangesAsync(ct);
        }
    }
} 