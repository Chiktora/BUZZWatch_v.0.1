using BuzzWatch.Domain.Devices;
using FluentAssertions;
using Xunit;

namespace BuzzWatch.Tests.Unit.Domain
{
    public class DeviceTests
    {
        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void Create_Should_Throw_When_NameEmpty(string invalidName)
        {
            // Act
            Action act = () => Device.Create(invalidName);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("*name*");
        }

        [Fact]
        public void Create_Should_GenerateId_And_SetName()
        {
            // Arrange
            var name = "Test Hive 1";
            
            // Act
            var device = Device.Create(name);
            
            // Assert
            device.Id.Should().NotBe(Guid.Empty);
            device.Name.Should().Be(name);
            device.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void Create_Should_AcceptLocation()
        {
            // Arrange
            var name = "Test Hive 2";
            var location = new HiveLocation("Test Farm", 42.123, -71.456);
            
            // Act
            var device = Device.Create(name, location);
            
            // Assert
            device.Location.Should().Be(location);
        }
        
        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void UpdateName_Should_Throw_When_NameEmpty(string invalidName)
        {
            // Arrange
            var device = Device.Create("Valid Name");
            
            // Act
            Action act = () => device.UpdateName(invalidName);
            
            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("*name*");
        }
        
        [Fact]
        public void UpdateName_Should_ChangeName()
        {
            // Arrange
            var device = Device.Create("Old Name");
            var newName = "New Name";
            
            // Act
            device.UpdateName(newName);
            
            // Assert
            device.Name.Should().Be(newName);
        }
        
        [Fact]
        public void UpdateLocation_Should_ChangeLocation()
        {
            // Arrange
            var device = Device.Create("Test Device");
            var newLocation = new HiveLocation("New Farm", 43.0, -72.0);
            
            // Act
            device.UpdateLocation(newLocation);
            
            // Assert
            device.Location.Should().Be(newLocation);
        }
    }
} 