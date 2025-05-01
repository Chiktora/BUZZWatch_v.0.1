using BuzzWatch.Contracts.Measurements;
using BuzzWatch.Domain.Devices;
using BuzzWatch.Domain.Measurements;
using BuzzWatch.Infrastructure.Data;
using BuzzWatch.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace BuzzWatch.Tests.Integration.Api
{
    public class MeasurementEndpointTests
    {
        [Fact]
        public async Task Create_Measurement_Creates_Database_Entry()
        {
            // Arrange
            var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(connection)
                .Options;
                
            await using var context = new ApplicationDbContext(options);
            await context.Database.EnsureCreatedAsync();
            
            // Create a device
            var device = Device.Create("Test Device");
            context.Devices.Add(device);
            await context.SaveChangesAsync();
            
            // Create measurement
            var repository = new MeasurementRepository(context);
            var unitOfWork = new UnitOfWork(context);
            
            var header = MeasurementHeader.Create(device.Id, DateTimeOffset.UtcNow);
            header.AttachTempInside(36.5m);
            
            await repository.AddAsync(header, CancellationToken.None);
            await unitOfWork.SaveChangesAsync(CancellationToken.None);
            
            // Assert
            var savedHeader = await context.Headers.FindAsync(header.Id);
            savedHeader.Should().NotBeNull();
            savedHeader!.DeviceId.Should().Be(device.Id);
            savedHeader.TempIn.Should().NotBeNull();
            savedHeader.TempIn!.ValueC.Should().Be(36.5m);
        }
    }
} 