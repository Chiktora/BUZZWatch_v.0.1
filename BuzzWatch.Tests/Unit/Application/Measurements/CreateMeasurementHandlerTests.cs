using BuzzWatch.Application.Abstractions;
using BuzzWatch.Application.Measurements.Commands;
using BuzzWatch.Application.Measurements.Commands.Handlers;
using BuzzWatch.Domain.Devices;
using BuzzWatch.Domain.Measurements;
using FluentAssertions;
using Xunit;

namespace BuzzWatch.Tests.Unit.Application.Measurements
{
    public class CreateMeasurementHandlerTests
    {
        // Mock implementation of interfaces
        private class FakeMeasurementRepository : IMeasurementRepository
        {
            public List<MeasurementHeader> Measurements { get; } = new List<MeasurementHeader>();

            public Task<MeasurementHeader?> GetAsync(long id, CancellationToken ct)
            {
                return Task.FromResult(Measurements.FirstOrDefault(m => m.Id == id));
            }

            public Task<List<MeasurementHeader>> GetByDeviceAsync(Guid deviceId, DateTimeOffset from, DateTimeOffset to, CancellationToken ct)
            {
                return Task.FromResult(Measurements.Where(m => m.DeviceId == deviceId && m.RecordedAt >= from && m.RecordedAt <= to).ToList());
            }

            public Task<MeasurementHeader?> GetLatestByDeviceAsync(Guid deviceId, CancellationToken ct)
            {
                return Task.FromResult(Measurements.Where(m => m.DeviceId == deviceId).OrderByDescending(m => m.RecordedAt).FirstOrDefault());
            }

            public Task AddAsync(MeasurementHeader measurement, CancellationToken ct)
            {
                measurement.GetType().GetProperty("Id")?.SetValue(measurement, Measurements.Count + 1);
                Measurements.Add(measurement);
                return Task.CompletedTask;
            }
        }

        private class FakeDeviceRepository : IDeviceRepository
        {
            private readonly List<Device> _devices = new List<Device>();

            public FakeDeviceRepository(IEnumerable<Device> initialDevices)
            {
                _devices.AddRange(initialDevices);
            }

            public Task<Device?> GetAsync(Guid id, CancellationToken ct)
            {
                return Task.FromResult(_devices.FirstOrDefault(d => d.Id == id));
            }

            public Task<List<Device>> GetAllAsync(CancellationToken ct)
            {
                return Task.FromResult(_devices.ToList());
            }

            public Task AddAsync(Device device, CancellationToken ct)
            {
                _devices.Add(device);
                return Task.CompletedTask;
            }

            public void Update(Device device)
            {
                // In a real implementation, we would track changes
            }

            public void Remove(Device device)
            {
                _devices.Remove(device);
            }
        }

        private class FakeUnitOfWork : IUnitOfWork
        {
            public int SaveChangesCount { get; private set; } = 0;

            public Task<int> SaveChangesAsync(CancellationToken ct)
            {
                SaveChangesCount++;
                return Task.FromResult(1);
            }
        }

        private class FakeDateTimeProvider : IDateTimeProvider
        {
            public DateTimeOffset UtcNow => new DateTimeOffset(2025, 5, 1, 12, 0, 0, TimeSpan.Zero);
        }

        [Fact]
        public async Task Handle_CreatesNewMeasurementWithAllValues()
        {
            // Arrange
            var deviceId = Guid.NewGuid();
            var device = Device.Create("Test Device");
            device.GetType().GetProperty("Id")?.SetValue(device, deviceId);

            var deviceRepo = new FakeDeviceRepository(new[] { device });
            var measurementRepo = new FakeMeasurementRepository();
            var unitOfWork = new FakeUnitOfWork();
            var dateTimeProvider = new FakeDateTimeProvider();

            var handler = new CreateMeasurementHandler(
                measurementRepo,
                deviceRepo,
                unitOfWork,
                dateTimeProvider);

            var command = new CreateMeasurementCommand(
                deviceId,
                dateTimeProvider.UtcNow,
                25.5m,  // TempIn
                45.0m,  // HumIn
                18.2m,  // TempOut
                65.0m,  // HumOut
                42.2m   // Weight
            );

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().Be(1); // First measurement
            measurementRepo.Measurements.Should().HaveCount(1);
            unitOfWork.SaveChangesCount.Should().Be(1);

            var measurement = measurementRepo.Measurements.First();
            measurement.DeviceId.Should().Be(deviceId);
            measurement.RecordedAt.Should().Be(dateTimeProvider.UtcNow);
            measurement.TempIn.Should().NotBeNull();
            measurement.TempIn!.ValueC.Should().Be(25.5m);
            measurement.HumIn.Should().NotBeNull();
            measurement.HumIn!.ValuePct.Should().Be(45.0m);
            measurement.TempOut.Should().NotBeNull();
            measurement.TempOut!.ValueC.Should().Be(18.2m);
            measurement.HumOut.Should().NotBeNull();
            measurement.HumOut!.ValuePct.Should().Be(65.0m);
            measurement.Weight.Should().NotBeNull();
            measurement.Weight!.ValueKg.Should().Be(42.2m);
        }

        [Fact]
        public async Task Handle_CreatesNewMeasurementWithPartialValues()
        {
            // Arrange
            var deviceId = Guid.NewGuid();
            var device = Device.Create("Test Device");
            device.GetType().GetProperty("Id")?.SetValue(device, deviceId);

            var deviceRepo = new FakeDeviceRepository(new[] { device });
            var measurementRepo = new FakeMeasurementRepository();
            var unitOfWork = new FakeUnitOfWork();
            var dateTimeProvider = new FakeDateTimeProvider();

            var handler = new CreateMeasurementHandler(
                measurementRepo,
                deviceRepo,
                unitOfWork,
                dateTimeProvider);

            var command = new CreateMeasurementCommand(
                deviceId,
                dateTimeProvider.UtcNow,
                25.5m,  // TempIn
                null,   // HumIn
                null,   // TempOut
                null,   // HumOut
                42.2m   // Weight
            );

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().Be(1); // First measurement
            measurementRepo.Measurements.Should().HaveCount(1);
            unitOfWork.SaveChangesCount.Should().Be(1);

            var measurement = measurementRepo.Measurements.First();
            measurement.DeviceId.Should().Be(deviceId);
            measurement.RecordedAt.Should().Be(dateTimeProvider.UtcNow);
            measurement.TempIn.Should().NotBeNull();
            measurement.TempIn!.ValueC.Should().Be(25.5m);
            measurement.HumIn.Should().BeNull();
            measurement.TempOut.Should().BeNull();
            measurement.HumOut.Should().BeNull();
            measurement.Weight.Should().NotBeNull();
            measurement.Weight!.ValueKg.Should().Be(42.2m);
        }

        [Fact]
        public async Task Handle_ThrowsException_WhenDeviceNotFound()
        {
            // Arrange
            var deviceId = Guid.NewGuid();
            var nonExistentDeviceId = Guid.NewGuid();

            var device = Device.Create("Test Device");
            device.GetType().GetProperty("Id")?.SetValue(device, deviceId);

            var deviceRepo = new FakeDeviceRepository(new[] { device });
            var measurementRepo = new FakeMeasurementRepository();
            var unitOfWork = new FakeUnitOfWork();
            var dateTimeProvider = new FakeDateTimeProvider();

            var handler = new CreateMeasurementHandler(
                measurementRepo,
                deviceRepo,
                unitOfWork,
                dateTimeProvider);

            var command = new CreateMeasurementCommand(
                nonExistentDeviceId,
                dateTimeProvider.UtcNow,
                25.5m,
                45.0m,
                null,
                null,
                null
            );

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                handler.Handle(command, CancellationToken.None));

            measurementRepo.Measurements.Should().BeEmpty();
            unitOfWork.SaveChangesCount.Should().Be(0);
        }
    }
} 