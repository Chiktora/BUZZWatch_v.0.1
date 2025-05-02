using AutoMapper;
using BuzzWatch.Contracts.Measurements;
using BuzzWatch.Domain.Common;
using BuzzWatch.Infrastructure.Abstractions;
using BuzzWatch.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BuzzWatch.Infrastructure.Notifications
{
    public class MeasurementCreatedHandler : INotificationHandler<MeasurementCreatedEvent>
    {
        private readonly IMeasurementNotificationService _notificationService;
        private readonly ApplicationDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ILogger<MeasurementCreatedHandler> _logger;

        public MeasurementCreatedHandler(
            IMeasurementNotificationService notificationService,
            ApplicationDbContext dbContext,
            IMapper mapper,
            ILogger<MeasurementCreatedHandler> logger)
        {
            _notificationService = notificationService;
            _dbContext = dbContext;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task Handle(MeasurementCreatedEvent notification, CancellationToken cancellationToken)
        {
            try
            {
                // Get the measurement from the database
                var measurement = await _dbContext.Headers
                    .Include(h => h.TempIn)
                    .Include(h => h.HumIn)
                    .Include(h => h.TempOut)
                    .Include(h => h.HumOut)
                    .Include(h => h.Weight)
                    .FirstOrDefaultAsync(h => h.Id == notification.MeasurementId, cancellationToken);

                if (measurement == null)
                {
                    _logger.LogWarning("Measurement {Id} not found to send notification", notification.MeasurementId);
                    return;
                }

                // Map to DTO
                var dto = new MeasurementDto
                {
                    Id = measurement.Id,
                    DeviceId = measurement.DeviceId,
                    Timestamp = measurement.RecordedAt,
                    TempInsideC = measurement.TempIn?.ValueC,
                    HumInsidePct = measurement.HumIn?.ValuePct,
                    TempOutsideC = measurement.TempOut?.ValueC,
                    HumOutsidePct = measurement.HumOut?.ValuePct,
                    WeightKg = measurement.Weight?.ValueKg
                };

                // Send notification through abstraction layer
                await _notificationService.NotifyMeasurementAdded(dto, cancellationToken);

                _logger.LogInformation("Sent measurement {Id} notification for device {DeviceId}",
                    notification.MeasurementId, notification.DeviceId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending measurement notification");
            }
        }
    }
} 