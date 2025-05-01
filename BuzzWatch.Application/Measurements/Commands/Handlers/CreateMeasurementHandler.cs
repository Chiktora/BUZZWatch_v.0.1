using BuzzWatch.Application.Abstractions;
using BuzzWatch.Domain.Measurements;
using MediatR;

namespace BuzzWatch.Application.Measurements.Commands.Handlers
{
    public class CreateMeasurementHandler : IRequestHandler<CreateMeasurementCommand, long>
    {
        private readonly IMeasurementRepository _measurementRepository;
        private readonly IDeviceRepository _deviceRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IDateTimeProvider _dateTimeProvider;

        public CreateMeasurementHandler(
            IMeasurementRepository measurementRepository,
            IDeviceRepository deviceRepository,
            IUnitOfWork unitOfWork,
            IDateTimeProvider dateTimeProvider)
        {
            _measurementRepository = measurementRepository;
            _deviceRepository = deviceRepository;
            _unitOfWork = unitOfWork;
            _dateTimeProvider = dateTimeProvider;
        }

        public async Task<long> Handle(CreateMeasurementCommand request, CancellationToken cancellationToken)
        {
            // Verify the device exists
            var device = await _deviceRepository.GetAsync(request.DeviceId, cancellationToken);
            if (device == null)
            {
                throw new InvalidOperationException($"Device with ID {request.DeviceId} not found");
            }

            // Create the measurement header
            var measurementTime = request.RecordedAt == default 
                ? _dateTimeProvider.UtcNow 
                : request.RecordedAt;
                
            var header = MeasurementHeader.Create(request.DeviceId, measurementTime);

            // Attach measurements if provided
            if (request.TempIn.HasValue)
            {
                header.AttachTempInside(request.TempIn.Value);
            }

            if (request.HumIn.HasValue)
            {
                header.AttachHumInside(request.HumIn.Value);
            }

            if (request.TempOut.HasValue)
            {
                header.AttachTempOutside(request.TempOut.Value);
            }

            if (request.HumOut.HasValue)
            {
                header.AttachHumOutside(request.HumOut.Value);
            }

            if (request.Weight.HasValue)
            {
                header.AttachWeight(request.Weight.Value);
            }

            // Save to repository
            await _measurementRepository.AddAsync(header, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return header.Id;
        }
    }
} 