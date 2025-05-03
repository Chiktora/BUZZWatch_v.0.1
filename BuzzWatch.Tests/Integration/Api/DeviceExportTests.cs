using BuzzWatch.Api;
using BuzzWatch.Contracts.Devices;
using BuzzWatch.Contracts.Measurements;
using BuzzWatch.Domain.Devices;
using BuzzWatch.Domain.Measurements;
using BuzzWatch.Infrastructure.Data;
using BuzzWatch.Infrastructure.Identity;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Xunit;

namespace BuzzWatch.Tests.Integration.Api
{
    public class DeviceExportTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        
        public DeviceExportTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    // Remove the existing DbContext registration
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                    
                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }
                    
                    // Create a new in-memory database for testing
                    var connection = new SqliteConnection("DataSource=:memory:");
                    connection.Open();
                    
                    services.AddDbContext<ApplicationDbContext>(options =>
                    {
                        options.UseSqlite(connection);
                    });
                    
                    // Build the service provider
                    var sp = services.BuildServiceProvider();
                    
                    // Create a scope to obtain a reference to the database
                    using var scope = sp.CreateScope();
                    var scopedServices = scope.ServiceProvider;
                    var db = scopedServices.GetRequiredService<ApplicationDbContext>();
                    
                    // Ensure database is created
                    db.Database.EnsureCreated();
                    
                    // Seed test data
                    SeedDatabase(db).Wait();
                });
            });
        }
        
        private async Task SeedDatabase(ApplicationDbContext db)
        {
            // Create a test device
            var device = Device.Create("Test Beehive");
            db.Devices.Add(device);
            await db.SaveChangesAsync();
            
            // Add some measurement data
            var now = DateTimeOffset.UtcNow;
            
            for (int i = 0; i < 10; i++)
            {
                var timestamp = now.AddHours(-i);
                var header = MeasurementHeader.Create(device.Id, timestamp);
                
                header.AttachTempInside(20 + (i % 5));
                header.AttachHumInside(50 + (i % 10));
                header.AttachWeight(80 - (i % 3));
                
                db.Headers.Add(header);
            }
            
            await db.SaveChangesAsync();
            
            // Create a test user
            var user = new AppUser
            {
                UserName = "test@example.com",
                Email = "test@example.com",
                EmailConfirmed = true,
                Name = "Test User"
            };
            
            db.Users.Add(user);
            await db.SaveChangesAsync();
        }
        
        private async Task<string> GetTestTokenAsync()
        {
            // In a real implementation, we would use a test login endpoint 
            // For simplicity, we'll return a mock token
            return "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IlRlc3QgVXNlciIsImlhdCI6MTUxNjIzOTAyMn0.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";
        }
        
        [Fact]
        public async Task ExportDeviceData_Returns_ValidData()
        {
            // Arrange
            var client = _factory.CreateClient();
            var token = await GetTestTokenAsync();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, token);
            
            // Get a device ID from the database
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var deviceId = await db.Devices.Select(d => d.Id).FirstOrDefaultAsync();
            
            // Act
            var response = await client.GetAsync($"/api/v1/devices/{deviceId}/export?days=7");
            
            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            
            var content = await response.Content.ReadAsStringAsync();
            var exportData = JsonSerializer.Deserialize<DeviceExportDataDto>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            
            exportData.Should().NotBeNull();
            exportData!.Device.Should().NotBeNull();
            exportData.Device.Id.Should().Be(deviceId);
            exportData.Measurements.Should().NotBeEmpty();
            
            // Check statistics are calculated correctly
            exportData.MinTemperature.Should().NotBeNull();
            exportData.MaxTemperature.Should().NotBeNull();
            exportData.AvgTemperature.Should().NotBeNull();
            exportData.MinWeight.Should().NotBeNull();
            exportData.MaxWeight.Should().NotBeNull();
            exportData.AvgWeight.Should().NotBeNull();
            
            // Check time span
            exportData.TimeSpanDays.Should().Be(7);
            exportData.RecordCount.Should().BeGreaterThan(0);
        }
        
        [Fact]
        public async Task ExportDeviceData_InvalidDevice_Returns_NotFound()
        {
            // Arrange
            var client = _factory.CreateClient();
            var token = await GetTestTokenAsync();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, token);
            
            var invalidDeviceId = Guid.NewGuid();
            
            // Act
            var response = await client.GetAsync($"/api/v1/devices/{invalidDeviceId}/export?days=7");
            
            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
        
        [Fact]
        public async Task ExportDeviceData_Unauthorized_Returns_Unauthorized()
        {
            // Arrange
            var client = _factory.CreateClient();
            // No authentication token
            
            // Get a device ID from the database
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var deviceId = await db.Devices.Select(d => d.Id).FirstOrDefaultAsync();
            
            // Act
            var response = await client.GetAsync($"/api/v1/devices/{deviceId}/export?days=7");
            
            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }
} 