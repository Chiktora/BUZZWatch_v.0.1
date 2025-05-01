namespace BuzzWatch.Domain.Measurements
{
    public sealed record MeasurementWeight
    {
        public const decimal Min = 0m;
        public const decimal Max = 500m;

        public long Id { get; }
        public decimal ValueKg { get; }

        public MeasurementWeight(long id, decimal valueKg)
        {
            if (valueKg < Min || valueKg > Max)
                throw new ArgumentOutOfRangeException(nameof(valueKg), $"Weight must be between {Min}kg and {Max}kg");

            Id = id;
            ValueKg = valueKg;
        }
    }
} 