using BuzzWatch.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace BuzzWatch.Tests.Integration
{
    public class SqliteEfFixture : IDisposable
    {
        private readonly SqliteConnection _connection;
        public ApplicationDbContext Db { get; }

        public SqliteEfFixture()
        {
            // Create and open an in-memory SQLite connection
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            // Create DbContext options
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(_connection)
                .Options;

            // Create the DbContext and ensure database is created
            Db = new ApplicationDbContext(options);
            Db.Database.EnsureCreated();
        }

        public void Dispose()
        {
            Db.Dispose();
            _connection.Dispose();
        }
    }
} 