using BuzzWatch.Application.Alerts.Interfaces;
using BuzzWatch.Domain.Alerts;
using BuzzWatch.Domain.Devices;
using BuzzWatch.Domain.Measurements;
using BuzzWatch.Infrastructure.Data;
using BuzzWatch.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace BuzzWatch.Tests.Integration.Worker
{
    public class AlertEngineTests : IClassFixture<SqliteEfFixture>
    {
        private readonly SqliteEfFixture _fixture;
        private readonly IAlertEvaluator _alertEvaluator;
        private readonly Device _testDevice;
        private readonly AlertRule _testRule;

        public AlertEngineTests(SqliteEfFixture fixture)
        {
            _fixture = fixture;
            _alertEvaluator = new AlertEvaluator(
                _fixture.Db,
                NullLogger<AlertEvaluator>.Instance);

            // Create test device
            _testDevice = Device.Create("TestDevice");
            _fixture.Db.Devices.Add(_testDevice);
            _fixture.Db.SaveChanges();

            // Create test alert rule (temperature > 30°C)
            _testRule = AlertRule.Create(
                _testDevice.Id,
                "TempInside",
                ComparisonOperator.GreaterThan,
                30.0m,
                0); // immediate alert, no duration check

            _fixture.Db.Set<AlertRule>().Add(_testRule);
            _fixture.Db.SaveChanges();
        }

        [Fact]
        public async Task AlertEngine_CreatesAlertEvent_WhenConditionMet()
        {
            // Arrange - Create a measurement that will trigger the alert
            var measurement = MeasurementHeader.Create(_testDevice.Id, DateTimeOffset.UtcNow);
            measurement.AttachTempInside(35.0m); // This should trigger the alert (> 30°C)
            
            _fixture.Db.Headers.Add(measurement);
            await _fixture.Db.SaveChangesAsync();

            // Act - Run the alert engine
            await _alertEvaluator.ProcessAsync(CancellationToken.None);

            // Assert - Check if an alert was created
            var alertEvent = await _fixture.Db.Set<AlertEvent>()
                .FirstOrDefaultAsync(a => a.RuleId == _testRule.Id);

            Assert.NotNull(alertEvent);
            Assert.Equal(_testDevice.Id, alertEvent.DeviceId);
            Assert.Null(alertEvent.EndTime); // Alert should be open
            
            // Also verify an outbox message was created
            var outboxMessage = await _fixture.Db.OutboxMessages
                .FirstOrDefaultAsync(m => m.Type == "AlertTriggered");
                
            Assert.NotNull(outboxMessage);
        }

        [Fact]
        public async Task AlertEngine_ClosesAlertEvent_WhenConditionNoLongerMet()
        {
            // Arrange - Create an initial measurement that will trigger the alert
            var measurement1 = MeasurementHeader.Create(_testDevice.Id, DateTimeOffset.UtcNow.AddMinutes(-5));
            measurement1.AttachTempInside(35.0m); // This should trigger the alert (> 30°C)
            
            _fixture.Db.Headers.Add(measurement1);
            await _fixture.Db.SaveChangesAsync();

            // Run the alert engine to create the alert
            await _alertEvaluator.ProcessAsync(CancellationToken.None);

            // Create a new measurement that will clear the alert
            var measurement2 = MeasurementHeader.Create(_testDevice.Id, DateTimeOffset.UtcNow);
            measurement2.AttachTempInside(25.0m); // This should clear the alert (<= 30°C)
            
            _fixture.Db.Headers.Add(measurement2);
            await _fixture.Db.SaveChangesAsync();

            // Act - Run the alert engine again
            await _alertEvaluator.ProcessAsync(CancellationToken.None);

            // Assert - Check if the alert was closed
            var alertEvent = await _fixture.Db.Set<AlertEvent>()
                .FirstOrDefaultAsync(a => a.RuleId == _testRule.Id);

            Assert.NotNull(alertEvent);
            Assert.NotNull(alertEvent.EndTime); // Alert should be closed
            
            // Also verify an outbox message was created for the close event
            var outboxMessage = await _fixture.Db.OutboxMessages
                .FirstOrDefaultAsync(m => m.Type == "AlertClosed");
                
            Assert.NotNull(outboxMessage);
        }
    }
} 