namespace BuzzWatch.Domain.Measurements
{
    public sealed record MeasurementHumIn
    {
        public const decimal Min = 0m;
        public const decimal Max = 100m;

        public long Id { get; }
        public decimal ValuePct { get; }

        public MeasurementHumIn(long id, decimal valuePct)
        {
            if (valuePct < Min || valuePct > Max)
                throw new ArgumentOutOfRangeException(nameof(valuePct), $"Humidity must be between {Min}% and {Max}%");

            Id = id;
            ValuePct = valuePct;
        }
    }
} 