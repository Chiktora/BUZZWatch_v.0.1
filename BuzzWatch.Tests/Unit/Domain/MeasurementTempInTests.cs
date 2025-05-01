using BuzzWatch.Domain.Measurements;
using FluentAssertions;
using Xunit;

namespace BuzzWatch.Tests.Unit.Domain
{
    public class MeasurementTempInTests
    {
        [Theory]
        [InlineData(-50)]  // Below minimum
        [InlineData(70)]   // Above maximum
        public void Constructor_Should_Throw_When_TemperatureOutOfRange(decimal invalidTemp)
        {
            // Act
            Action act = () => new MeasurementTempIn(1, invalidTemp);

            // Assert
            act.Should().Throw<ArgumentOutOfRangeException>()
                .WithMessage("*Temperature must be between*");
        }

        [Theory]
        [InlineData(-40)]  // Minimum
        [InlineData(0)]    // Zero
        [InlineData(25)]   // Room temperature
        [InlineData(60)]   // Maximum
        public void Constructor_Should_Accept_ValidTemperatures(decimal validTemp)
        {
            // Act
            var measurement = new MeasurementTempIn(1, validTemp);

            // Assert
            measurement.ValueC.Should().Be(validTemp);
            measurement.Id.Should().Be(1);
        }
    }
} 