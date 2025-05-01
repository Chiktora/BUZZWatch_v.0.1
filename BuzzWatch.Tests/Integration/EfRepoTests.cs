using BuzzWatch.Domain.Devices;
using BuzzWatch.Domain.Measurements;
using BuzzWatch.Infrastructure.Repositories;
using FluentAssertions;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace BuzzWatch.Tests.Integration
{
    public class EfRepoTests : IClassFixture<SqliteEfFixture>
    {
        private readonly SqliteEfFixture _fixture;
        private readonly MeasurementRepository _measurementRepo;
        private readonly DeviceRepository _deviceRepo;
        private readonly UnitOfWork _unitOfWork;

        public EfRepoTests(SqliteEfFixture fixture)
        {
            _fixture = fixture;
            _measurementRepo = new MeasurementRepository(fixture.Db);
            _deviceRepo = new DeviceRepository(fixture.Db);
            _unitOfWork = new UnitOfWork(fixture.Db);
        }

        [Fact]
        public async Task Add_Header_Persists()
        {
            // Arrange
            var deviceId = Guid.NewGuid();
            var device = Device.Create("Test Device");
            await _deviceRepo.AddAsync(device, CancellationToken.None);
            await _unitOfWork.SaveChangesAsync(CancellationToken.None);

            var header = MeasurementHeader.Create(device.Id, DateTimeOffset.UtcNow);
            header.AttachTempInside(23.5m);

            // Act
            await _measurementRepo.AddAsync(header, CancellationToken.None);
            await _unitOfWork.SaveChangesAsync(CancellationToken.None);

            // Assert
            var savedHeader = await _measurementRepo.GetAsync(header.Id, CancellationToken.None);
            savedHeader.Should().NotBeNull();
            savedHeader!.TempIn.Should().NotBeNull();
            savedHeader.TempIn!.ValueC.Should().Be(23.5m);
        }
    }
} 