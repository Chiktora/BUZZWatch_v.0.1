using BuzzWatch.Application.Abstractions;
using BuzzWatch.Application.Measurements.Commands;
using BuzzWatch.Application.Measurements.Commands.Handlers;
using BuzzWatch.Domain.Common;
using BuzzWatch.Domain.Devices;
using BuzzWatch.Domain.Measurements;
using FluentAssertions;
using Xunit;

namespace BuzzWatch.Tests.Unit.Application.Measurements
{
    public class CreateMeasurementHandlerTests
    {
        // Mock implementation of interfaces
        public class FakeMeasurementRepository : IMeasurementRepository
        {
            // Make measurements accessible through a property
            public List<MeasurementHeader> Measurements => _measurements;
            private readonly List<MeasurementHeader> _measurements = new();

            public bool AssignIdOnAdd { get; set; } = false;

            public Task<MeasurementHeader?> GetAsync(long id, CancellationToken ct) =>
                Task.FromResult(_measurements.FirstOrDefault(m => m.Id == id));
            
            public Task<List<MeasurementHeader>> GetByDeviceAsync(
                Guid deviceId, 
                DateTimeOffset from, 
                DateTimeOffset to, 
                int limit = 1000,
                CancellationToken ct = default) =>
                Task.FromResult(_measurements
                    .Where(m => m.DeviceId == deviceId && m.RecordedAt >= from && m.RecordedAt <= to)
                    .OrderByDescending(m => m.RecordedAt)
                    .Take(limit)
                    .ToList());
                    
            public Task<List<MeasurementHeader>> GetByDeviceChunkedAsync(
                Guid deviceId,
                DateTimeOffset from,
                DateTimeOffset to,
                int chunkIntervalMinutes = 60,
                CancellationToken ct = default) =>
                Task.FromResult(_measurements
                    .Where(m => m.DeviceId == deviceId && m.RecordedAt >= from && m.RecordedAt <= to)
                    .OrderBy(m => m.RecordedAt)
                    .ToList());

            public Task<MeasurementHeader?> GetLatestByDeviceAsync(Guid deviceId, CancellationToken ct) =>
                Task.FromResult(_measurements
                    .Where(m => m.DeviceId == deviceId)
                    .OrderByDescending(m => m.RecordedAt)
                    .FirstOrDefault());

            public Task AddAsync(MeasurementHeader measurement, CancellationToken ct)
            {
                if (AssignIdOnAdd)
                {
                    // Simulate EF Core's behavior of assigning sequential IDs
                    measurement.GetType().GetProperty("Id")?.SetValue(measurement, _measurements.Count + 1);
                }
                _measurements.Add(measurement);
                return Task.CompletedTask;
            }
        }

        private class FakeDeviceRepository : IDeviceRepository
        {
            public List<Device> Devices => _devices;
            private readonly List<Device> _devices = new List<Device>();

            public FakeDeviceRepository()
            {
                // Default constructor with no initial devices
            }

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
            public bool SaveChangesCalled => SaveChangesCount > 0;

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
        public async Task Handle_DeviceNotFound_ReturnsFailure()
        {
            // Arrange
            var command = new CreateMeasurementCommand(
                DeviceId: Guid.NewGuid(),
                RecordedAt: DateTimeOffset.UtcNow,
                TempIn: 25.5m,
                HumIn: 45.0m,
                TempOut: 30.0m,
                HumOut: 55.0m,
                Weight: 10.0m
            );

            var measurementRepo = new FakeMeasurementRepository();
            var deviceRepo = new FakeDeviceRepository();
            var unitOfWork = new FakeUnitOfWork();
            var dateTimeProvider = new FakeDateTimeProvider();

            var handler = new CreateMeasurementHandler(
                measurementRepo, 
                deviceRepo, 
                unitOfWork,
                dateTimeProvider
            );

            // Act
            async Task action() => await handler.Handle(command, CancellationToken.None);

            // Assert
            await Assert.ThrowsAsync<InvalidOperationException>(action);
            measurementRepo.Measurements.Should().BeEmpty();
            unitOfWork.SaveChangesCount.Should().Be(0);
        }

        [Fact]
        public async Task Handle_ValidCommand_CreatesMeasurement()
        {
            // Arrange
            var repo = new FakeMeasurementRepository();
            var unitOfWork = new FakeUnitOfWork();
            var deviceRepo = new FakeDeviceRepository();
            var dateTimeProvider = new FakeDateTimeProvider();
            
            var device = Device.Create("Test Device");
            typeof(BaseEntity<Guid>).GetProperty("Id")?.SetValue(device, Guid.NewGuid());
            deviceRepo.Devices.Add(device);
            
            var command = new CreateMeasurementCommand(
                DeviceId: device.Id,
                RecordedAt: DateTimeOffset.UtcNow,
                TempIn: 25.5m,
                HumIn: 45.0m,
                TempOut: 30.0m,
                HumOut: 55.0m,
                Weight: 10.0m
            );
            
            var handler = new CreateMeasurementHandler(
                repo, 
                deviceRepo, 
                unitOfWork,
                dateTimeProvider
            );
            
            // Modify the FakeMeasurementRepository.AddAsync to assign an ID
            repo.AssignIdOnAdd = true;
            
            // Act
            var result = await handler.Handle(command, CancellationToken.None);
            
            // Assert
            result.Should().BeGreaterThan(0);
            repo.Measurements.Should().HaveCount(1);
            
            // Verify the repository was called correctly
            var measurement = repo.Measurements.First();
            measurement.DeviceId.Should().Be(device.Id);
            measurement.TempIn?.ValueC.Should().Be(25.5m);
            measurement.HumIn?.ValuePct.Should().Be(45.0m);
            measurement.TempOut?.ValueC.Should().Be(30.0m);
            measurement.HumOut?.ValuePct.Should().Be(55.0m);
            measurement.Weight?.ValueKg.Should().Be(10.0m);
            
            // Verify unit of work was called
            unitOfWork.SaveChangesCalled.Should().BeTrue();
        }
        
        [Fact]
        public async Task Handle_DefaultRecordedTime_UsesCurrentTime()
        {
            // Arrange
            var repo = new FakeMeasurementRepository();
            var unitOfWork = new FakeUnitOfWork();
            var deviceRepo = new FakeDeviceRepository();
            var dateTimeProvider = new FakeDateTimeProvider();
            
            var device = Device.Create("Test Device");
            typeof(BaseEntity<Guid>).GetProperty("Id")?.SetValue(device, Guid.NewGuid());
            deviceRepo.Devices.Add(device);
            
            var command = new CreateMeasurementCommand(
                DeviceId: device.Id,
                RecordedAt: default, // This should trigger using the current time
                TempIn: 25.5m,
                HumIn: 45.0m,
                TempOut: 30.0m,
                HumOut: 55.0m,
                Weight: 10.0m
            );
            
            var handler = new CreateMeasurementHandler(
                repo, 
                deviceRepo, 
                unitOfWork,
                dateTimeProvider
            );
            
            // Act
            var result = await handler.Handle(command, CancellationToken.None);
            
            // Assert
            var measurement = repo.Measurements.First();
            measurement.RecordedAt.Should().Be(dateTimeProvider.UtcNow);
        }
        
        [Fact]
        public async Task Handle_PartialMeasurements_OnlyStoresProvidedValues()
        {
            // Arrange
            var repo = new FakeMeasurementRepository();
            var unitOfWork = new FakeUnitOfWork();
            var deviceRepo = new FakeDeviceRepository();
            var dateTimeProvider = new FakeDateTimeProvider();
            
            var device = Device.Create("Test Device");
            typeof(BaseEntity<Guid>).GetProperty("Id")?.SetValue(device, Guid.NewGuid());
            deviceRepo.Devices.Add(device);
            
            // Only provide temperature data
            var command = new CreateMeasurementCommand(
                DeviceId: device.Id,
                RecordedAt: DateTimeOffset.UtcNow,
                TempIn: 25.5m,
                HumIn: null,
                TempOut: null,
                HumOut: null,
                Weight: null
            );
            
            var handler = new CreateMeasurementHandler(
                repo, 
                deviceRepo, 
                unitOfWork,
                dateTimeProvider
            );
            
            // Act
            var result = await handler.Handle(command, CancellationToken.None);
            
            // Assert
            var measurement = repo.Measurements.First();
            measurement.TempIn.Should().NotBeNull();
            measurement.TempIn?.ValueC.Should().Be(25.5m);
            
            // Verify other measurements are null
            measurement.HumIn.Should().BeNull();
            measurement.TempOut.Should().BeNull();
            measurement.HumOut.Should().BeNull();
            measurement.Weight.Should().BeNull();
        }
        
        [Fact]
        public async Task Handle_MultipleMeasurements_StoresSeparatelyForDevice()
        {
            // Arrange
            var repo = new FakeMeasurementRepository();
            var unitOfWork = new FakeUnitOfWork();
            var deviceRepo = new FakeDeviceRepository();
            var dateTimeProvider = new FakeDateTimeProvider();
            
            var device = Device.Create("Test Device");
            typeof(BaseEntity<Guid>).GetProperty("Id")?.SetValue(device, Guid.NewGuid());
            deviceRepo.Devices.Add(device);
            
            var handler = new CreateMeasurementHandler(
                repo, 
                deviceRepo, 
                unitOfWork,
                dateTimeProvider
            );
            
            // Create first measurement
            var command1 = new CreateMeasurementCommand(
                DeviceId: device.Id,
                RecordedAt: DateTimeOffset.UtcNow.AddHours(-1),
                TempIn: 25.5m,
                HumIn: 45.0m,
                TempOut: null,
                HumOut: null,
                Weight: null
            );
            
            // Create second measurement
            var command2 = new CreateMeasurementCommand(
                DeviceId: device.Id,
                RecordedAt: DateTimeOffset.UtcNow,
                TempIn: 26.0m,
                HumIn: 46.0m,
                TempOut: null,
                HumOut: null,
                Weight: null
            );
            
            // Act
            await handler.Handle(command1, CancellationToken.None);
            await handler.Handle(command2, CancellationToken.None);
            
            // Assert
            repo.Measurements.Should().HaveCount(2);
            unitOfWork.SaveChangesCount.Should().Be(2);
            
            var measurements = repo.Measurements.OrderBy(m => m.RecordedAt).ToList();
            
            measurements[0].TempIn?.ValueC.Should().Be(25.5m);
            measurements[0].HumIn?.ValuePct.Should().Be(45.0m);
            
            measurements[1].TempIn?.ValueC.Should().Be(26.0m);
            measurements[1].HumIn?.ValuePct.Should().Be(46.0m);
        }
        
        [Fact]
        public async Task Handle_InvalidInsideTemp_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var repo = new FakeMeasurementRepository();
            var unitOfWork = new FakeUnitOfWork();
            var deviceRepo = new FakeDeviceRepository();
            var dateTimeProvider = new FakeDateTimeProvider();
            
            var device = Device.Create("Test Device");
            typeof(BaseEntity<Guid>).GetProperty("Id")?.SetValue(device, Guid.NewGuid());
            deviceRepo.Devices.Add(device);
            
            var command = new CreateMeasurementCommand(
                DeviceId: device.Id,
                RecordedAt: DateTimeOffset.UtcNow,
                TempIn: -100m, // Invalid temperature
                HumIn: 45.0m,
                TempOut: 30.0m,
                HumOut: 55.0m,
                Weight: 10.0m
            );
            
            var handler = new CreateMeasurementHandler(
                repo, 
                deviceRepo, 
                unitOfWork,
                dateTimeProvider
            );
            
            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
                async () => await handler.Handle(command, CancellationToken.None)
            );
            
            exception.ParamName.Should().Be("valueC");
        }
        
        [Fact]
        public async Task Handle_InvalidInsideHumidity_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var repo = new FakeMeasurementRepository();
            var unitOfWork = new FakeUnitOfWork();
            var deviceRepo = new FakeDeviceRepository();
            var dateTimeProvider = new FakeDateTimeProvider();
            
            var device = Device.Create("Test Device");
            typeof(BaseEntity<Guid>).GetProperty("Id")?.SetValue(device, Guid.NewGuid());
            deviceRepo.Devices.Add(device);
            
            var command = new CreateMeasurementCommand(
                DeviceId: device.Id,
                RecordedAt: DateTimeOffset.UtcNow,
                TempIn: 25.5m,
                HumIn: 101.0m, // Invalid humidity
                TempOut: 30.0m,
                HumOut: 55.0m,
                Weight: 10.0m
            );
            
            var handler = new CreateMeasurementHandler(
                repo, 
                deviceRepo, 
                unitOfWork,
                dateTimeProvider
            );
            
            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
                async () => await handler.Handle(command, CancellationToken.None)
            );
            
            exception.ParamName.Should().Be("valuePct");
        }
        
        [Fact]
        public async Task Handle_InvalidOutsideTemp_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var repo = new FakeMeasurementRepository();
            var unitOfWork = new FakeUnitOfWork();
            var deviceRepo = new FakeDeviceRepository();
            var dateTimeProvider = new FakeDateTimeProvider();
            
            var device = Device.Create("Test Device");
            typeof(BaseEntity<Guid>).GetProperty("Id")?.SetValue(device, Guid.NewGuid());
            deviceRepo.Devices.Add(device);
            
            var command = new CreateMeasurementCommand(
                DeviceId: device.Id,
                RecordedAt: DateTimeOffset.UtcNow,
                TempIn: 25.5m,
                HumIn: 45.0m,
                TempOut: -100.0m, // Invalid temperature
                HumOut: 55.0m,
                Weight: 10.0m
            );
            
            var handler = new CreateMeasurementHandler(
                repo, 
                deviceRepo, 
                unitOfWork,
                dateTimeProvider
            );
            
            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
                async () => await handler.Handle(command, CancellationToken.None)
            );
            
            exception.ParamName.Should().Be("valueC");
        }
        
        [Fact]
        public async Task Handle_InvalidOutsideHumidity_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var repo = new FakeMeasurementRepository();
            var unitOfWork = new FakeUnitOfWork();
            var deviceRepo = new FakeDeviceRepository();
            var dateTimeProvider = new FakeDateTimeProvider();
            
            var device = Device.Create("Test Device");
            typeof(BaseEntity<Guid>).GetProperty("Id")?.SetValue(device, Guid.NewGuid());
            deviceRepo.Devices.Add(device);
            
            var command = new CreateMeasurementCommand(
                DeviceId: device.Id,
                RecordedAt: DateTimeOffset.UtcNow,
                TempIn: 25.5m,
                HumIn: 45.0m,
                TempOut: 30.0m,
                HumOut: 101.0m, // Invalid humidity
                Weight: 10.0m
            );
            
            var handler = new CreateMeasurementHandler(
                repo, 
                deviceRepo, 
                unitOfWork,
                dateTimeProvider
            );
            
            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
                async () => await handler.Handle(command, CancellationToken.None)
            );
            
            exception.ParamName.Should().Be("valuePct");
        }
        
        [Fact]
        public async Task Handle_InvalidWeight_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var repo = new FakeMeasurementRepository();
            var unitOfWork = new FakeUnitOfWork();
            var deviceRepo = new FakeDeviceRepository();
            var dateTimeProvider = new FakeDateTimeProvider();
            
            var device = Device.Create("Test Device");
            typeof(BaseEntity<Guid>).GetProperty("Id")?.SetValue(device, Guid.NewGuid());
            deviceRepo.Devices.Add(device);
            
            var command = new CreateMeasurementCommand(
                DeviceId: device.Id,
                RecordedAt: DateTimeOffset.UtcNow,
                TempIn: 25.5m,
                HumIn: 45.0m,
                TempOut: 30.0m,
                HumOut: 55.0m,
                Weight: -1.0m // Invalid weight
            );
            
            var handler = new CreateMeasurementHandler(
                repo, 
                deviceRepo, 
                unitOfWork,
                dateTimeProvider
            );
            
            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
                async () => await handler.Handle(command, CancellationToken.None)
            );
            
            exception.ParamName.Should().Be("valueKg");
        }
    }
} 