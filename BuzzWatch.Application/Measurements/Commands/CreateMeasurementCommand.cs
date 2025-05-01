using MediatR;

namespace BuzzWatch.Application.Measurements.Commands
{
    public record CreateMeasurementCommand(
        Guid DeviceId,
        DateTimeOffset RecordedAt,
        decimal? TempIn,
        decimal? HumIn,
        decimal? TempOut,
        decimal? HumOut,
        decimal? Weight) : IRequest<long>;
} 