namespace BuzzWatch.Domain.Measurements
{
    public sealed record MeasurementTempIn
    {
        public const decimal Min = -40m;
        public const decimal Max = 60m;

        public long Id { get; }
        public decimal ValueC { get; }

        public MeasurementTempIn(long id, decimal valueC)
        {
            if (valueC < Min || valueC > Max)
                throw new ArgumentOutOfRangeException(nameof(valueC), $"Temperature must be between {Min}°C and {Max}°C");

            Id = id;
            ValueC = valueC;
        }
    }
} 