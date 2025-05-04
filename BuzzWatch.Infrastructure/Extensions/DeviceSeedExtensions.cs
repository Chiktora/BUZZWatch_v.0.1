using BuzzWatch.Application.Abstractions;
using BuzzWatch.Domain.Devices;
using BuzzWatch.Domain.Measurements;
using Microsoft.Extensions.DependencyInjection;

namespace BuzzWatch.Infrastructure.Extensions
{
    public static class DeviceSeedExtensions
    {
        public static async Task SeedDevicesAsync(this IServiceProvider sp)
        {
            using var scope = sp.CreateScope();
            var deviceRepository = scope.ServiceProvider.GetRequiredService<IDeviceRepository>();
            var measurementRepository = scope.ServiceProvider.GetRequiredService<IMeasurementRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            
            // Check if we already have devices
            var existingDevices = await deviceRepository.GetAllAsync(CancellationToken.None);
            if (existingDevices.Count > 0)
            {
                // Devices already exist, no need to seed
                return;
            }
            
            // Create sample devices
            var devices = new List<Device>
            {
                CreateDevice("Hive Monitor 1", "Apiary 1", 39.7392, -104.9903),
                CreateDevice("Hive Monitor 2", "Apiary 1", 39.7391, -104.9905),
                CreateDevice("Hive Monitor 3", "Apiary 2", 40.0150, -105.2705)
            };
            
            // Add sample devices to repository
            foreach (var device in devices)
            {
                await deviceRepository.AddAsync(device, CancellationToken.None);
                
                // Add some sample measurements
                await AddSampleMeasurementsAsync(device, measurementRepository);
            }
            
            // Save changes
            await unitOfWork.SaveChangesAsync(CancellationToken.None);
            
            Console.WriteLine($"Seeded {devices.Count} devices with sample data");
        }
        
        private static Device CreateDevice(string name, string location, double? latitude = null, double? longitude = null)
        {
            HiveLocation? hiveLocation = null;
            if (!string.IsNullOrEmpty(location))
            {
                hiveLocation = new HiveLocation(location, latitude, longitude);
            }
            
            return Device.Create(name, hiveLocation);
        }
        
        private static async Task AddSampleMeasurementsAsync(Device device, IMeasurementRepository measurementRepository)
        {
            // Add a few measurements over the past few days
            var now = DateTimeOffset.UtcNow;
            
            for (int i = 0; i < 10; i++)
            {
                var time = now.AddHours(-i * 6); // Every 6 hours
                
                // Create measurement header
                var measurement = MeasurementHeader.Create(device.Id, time);
                
                // Save header to get an ID
                await measurementRepository.AddAsync(measurement, CancellationToken.None);
                
                // Now we can attach the measurements with the header ID
                decimal tempInValue = GetRandomTemperature(25, 35);
                decimal humInValue = GetRandomHumidity(40, 60);
                decimal tempOutValue = GetRandomTemperature(15, 30);
                decimal humOutValue = GetRandomHumidity(30, 80);
                decimal weightValue = GetRandomWeight(50, 80);
                
                measurement.AttachTempInside(tempInValue);
                measurement.AttachHumInside(humInValue);
                measurement.AttachTempOutside(tempOutValue);
                measurement.AttachHumOutside(humOutValue);
                measurement.AttachWeight(weightValue);
            }
        }
        
        private static decimal GetRandomTemperature(double min, double max)
        {
            var random = new Random();
            return (decimal)(min + random.NextDouble() * (max - min));
        }
        
        private static decimal GetRandomHumidity(double min, double max)
        {
            var random = new Random();
            return (decimal)(min + random.NextDouble() * (max - min));
        }
        
        private static decimal GetRandomWeight(double min, double max)
        {
            var random = new Random();
            return (decimal)(min + random.NextDouble() * (max - min));
        }
    }
} 