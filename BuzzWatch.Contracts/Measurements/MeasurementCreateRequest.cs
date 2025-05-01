namespace BuzzWatch.Contracts.Measurements
{
    public record MeasurementCreateRequest(
        DateTimeOffset RecordedAt,
        decimal? TempInsideC,
        decimal? HumInsidePct,
        decimal? TempOutsideC,
        decimal? HumOutsidePct,
        decimal? WeightKg
    );
} 