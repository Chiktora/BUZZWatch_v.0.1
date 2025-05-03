using BuzzWatch.Application.Abstractions;
using BuzzWatch.Contracts.Devices;
using BuzzWatch.Contracts.Measurements;
using MediatR;

namespace BuzzWatch.Application.Devices.Queries
{
    public record ExportDeviceDataQuery(Guid DeviceId, int Days = 30) : IRequest<DeviceExportDataDto?>;
    
    public class ExportDeviceDataQueryHandler : IRequestHandler<ExportDeviceDataQuery, DeviceExportDataDto?>
    {
        private readonly IDeviceRepository _deviceRepository;
        private readonly IMeasurementRepository _measurementRepository;
        private readonly IDateTimeProvider _dateTimeProvider;
        
        public ExportDeviceDataQueryHandler(
            IDeviceRepository deviceRepository,
            IMeasurementRepository measurementRepository,
            IDateTimeProvider dateTimeProvider)
        {
            _deviceRepository = deviceRepository;
            _measurementRepository = measurementRepository;
            _dateTimeProvider = dateTimeProvider;
        }
        
        public async Task<DeviceExportDataDto?> Handle(ExportDeviceDataQuery request, CancellationToken cancellationToken)
        {
            var device = await _deviceRepository.GetAsync(request.DeviceId, cancellationToken);
            
            if (device == null)
                return null;
                
            var from = _dateTimeProvider.UtcNow.AddDays(-request.Days);
            var to = _dateTimeProvider.UtcNow;
            
            var measurements = await _measurementRepository.GetByDeviceAsync(request.DeviceId, from, to, 10000, cancellationToken);
            
            // Map device to DTO
            var deviceDto = new DeviceDto
            {
                Id = device.Id,
                Name = device.Name,
                Location = device.Location?.ToString() ?? "Unknown",
                Status = GetDeviceStatus(device),
                FirmwareVersion = "1.0", // Placeholder
                LastSeen = GetLastSeenTime(device),
                InstalledOn = DateTimeOffset.UtcNow.AddDays(-30), // Placeholder
                Type = "Standard" // Placeholder
            };
            
            // Map measurements to DTOs
            var measurementDtos = measurements.Select(m => new MeasurementDto
            {
                Id = m.Id,
                DeviceId = m.DeviceId,
                Timestamp = m.RecordedAt,
                TempInsideC = m.TempIn?.ValueC,
                HumInsidePct = m.HumIn?.ValuePct,
                TempOutsideC = m.TempOut?.ValueC,
                HumOutsidePct = m.HumOut?.ValuePct,
                WeightKg = m.Weight?.ValueKg,
                BatteryPct = 100 // Placeholder
            }).ToList();
            
            return new DeviceExportDataDto
            {
                Device = deviceDto,
                Measurements = measurementDtos,
                ExportedAt = _dateTimeProvider.UtcNow,
                TimeSpanDays = request.Days
            };
        }
        
        private string GetDeviceStatus(Domain.Devices.Device device)
        {
            // If we have measurements in the last hour, consider online
            var latestMeasurement = device.Measurements
                .OrderByDescending(m => m.RecordedAt)
                .FirstOrDefault();
                
            if (latestMeasurement == null)
                return "Offline";
                
            return latestMeasurement.RecordedAt > _dateTimeProvider.UtcNow.AddHours(-1)
                ? "Online"
                : "Offline";
        }
        
        private DateTimeOffset GetLastSeenTime(Domain.Devices.Device device)
        {
            var latestMeasurement = device.Measurements
                .OrderByDescending(m => m.RecordedAt)
                .FirstOrDefault();
                
            return latestMeasurement?.RecordedAt ?? _dateTimeProvider.UtcNow.AddDays(-1);
        }
    }
} 