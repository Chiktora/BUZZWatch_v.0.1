using BuzzWatch.Domain.Common;
using BuzzWatch.Domain.Measurements;
using FluentAssertions;
using Xunit;

namespace BuzzWatch.Tests.Unit.Domain
{
    public class MeasurementHeaderTests
    {
        [Fact]
        public void Create_Should_SetDeviceIdAndRecordedAt()
        {
            // Arrange
            var deviceId = Guid.NewGuid();
            var recordedAt = DateTimeOffset.UtcNow;
            
            // Act
            var header = MeasurementHeader.Create(deviceId, recordedAt);
            
            // Assert
            header.DeviceId.Should().Be(deviceId);
            header.RecordedAt.Should().Be(recordedAt);
        }
        
        [Fact]
        public void AttachTempInside_Should_CreateMeasurementAndRaiseDomainEvent()
        {
            // Arrange
            var deviceId = Guid.NewGuid();
            var header = MeasurementHeader.Create(deviceId, DateTimeOffset.UtcNow);
            var tempValue = 25.5m;
            
            // Need to set the Id property before attaching measurements
            typeof(BaseEntity<long>)
                .GetProperty(nameof(BaseEntity<long>.Id))
                ?.SetValue(header, 123L);
            
            // Act
            header.AttachTempInside(tempValue);
            
            // Assert
            header.TempIn.Should().NotBeNull();
            header.TempIn!.ValueC.Should().Be(tempValue);
            header.DomainEvents.Should().ContainSingle(e => e is MeasurementCreatedEvent);
            
            var domainEvent = header.DomainEvents.First() as MeasurementCreatedEvent;
            domainEvent!.MeasurementId.Should().Be(123L);
            domainEvent.DeviceId.Should().Be(deviceId);
        }
        
        [Fact]
        public void AttachAllMeasurements_Should_CreateAllTypes()
        {
            // Arrange
            var deviceId = Guid.NewGuid();
            var header = MeasurementHeader.Create(deviceId, DateTimeOffset.UtcNow);
            
            // Need to set the Id property before attaching measurements
            typeof(BaseEntity<long>)
                .GetProperty(nameof(BaseEntity<long>.Id))
                ?.SetValue(header, 123L);
            
            // Act
            header.AttachTempInside(25.5m);
            header.AttachHumInside(45.0m);
            header.AttachTempOutside(18.2m);
            header.AttachHumOutside(65.0m);
            header.AttachWeight(42.2m);
            
            // Assert
            header.TempIn.Should().NotBeNull();
            header.HumIn.Should().NotBeNull();
            header.TempOut.Should().NotBeNull();
            header.HumOut.Should().NotBeNull();
            header.Weight.Should().NotBeNull();
            
            header.TempIn!.ValueC.Should().Be(25.5m);
            header.HumIn!.ValuePct.Should().Be(45.0m);
            header.TempOut!.ValueC.Should().Be(18.2m);
            header.HumOut!.ValuePct.Should().Be(65.0m);
            header.Weight!.ValueKg.Should().Be(42.2m);
        }
    }
} 