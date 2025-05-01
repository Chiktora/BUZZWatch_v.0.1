using BuzzWatch.Application.Measurements.Commands;
using BuzzWatch.Domain.Measurements;
using FluentValidation;

namespace BuzzWatch.Application.Measurements.Validators
{
    public class CreateMeasurementCommandValidator : AbstractValidator<CreateMeasurementCommand>
    {
        public CreateMeasurementCommandValidator()
        {
            RuleFor(x => x.DeviceId)
                .NotEmpty()
                .WithMessage("Device ID is required");

            RuleFor(x => x.RecordedAt)
                .NotEmpty()
                .WithMessage("Measurement timestamp is required");

            When(x => x.TempIn.HasValue, () =>
            {
                RuleFor(x => x.TempIn!.Value)
                    .InclusiveBetween(MeasurementTempIn.Min, MeasurementTempIn.Max)
                    .WithMessage($"Inside temperature must be between {MeasurementTempIn.Min}°C and {MeasurementTempIn.Max}°C");
            });

            When(x => x.HumIn.HasValue, () =>
            {
                RuleFor(x => x.HumIn!.Value)
                    .InclusiveBetween(MeasurementHumIn.Min, MeasurementHumIn.Max)
                    .WithMessage($"Inside humidity must be between {MeasurementHumIn.Min}% and {MeasurementHumIn.Max}%");
            });

            When(x => x.TempOut.HasValue, () =>
            {
                RuleFor(x => x.TempOut!.Value)
                    .InclusiveBetween(MeasurementTempOut.Min, MeasurementTempOut.Max)
                    .WithMessage($"Outside temperature must be between {MeasurementTempOut.Min}°C and {MeasurementTempOut.Max}°C");
            });

            When(x => x.HumOut.HasValue, () =>
            {
                RuleFor(x => x.HumOut!.Value)
                    .InclusiveBetween(MeasurementHumOut.Min, MeasurementHumOut.Max)
                    .WithMessage($"Outside humidity must be between {MeasurementHumOut.Min}% and {MeasurementHumOut.Max}%");
            });

            When(x => x.Weight.HasValue, () =>
            {
                RuleFor(x => x.Weight!.Value)
                    .InclusiveBetween(MeasurementWeight.Min, MeasurementWeight.Max)
                    .WithMessage($"Weight must be between {MeasurementWeight.Min}kg and {MeasurementWeight.Max}kg");
            });

            // At least one measurement should be provided
            RuleFor(x => new { x.TempIn, x.HumIn, x.TempOut, x.HumOut, x.Weight })
                .Must(x => x.TempIn.HasValue || x.HumIn.HasValue || x.TempOut.HasValue || x.HumOut.HasValue || x.Weight.HasValue)
                .WithMessage("At least one measurement value must be provided");
        }
    }
} 