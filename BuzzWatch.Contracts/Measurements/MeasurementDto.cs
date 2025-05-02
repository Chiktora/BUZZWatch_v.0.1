using System.Text.Json.Serialization;

namespace BuzzWatch.Contracts.Measurements
{
    public class MeasurementDto
    {
        public long Id { get; set; }
        public Guid DeviceId { get; set; }
        public DateTimeOffset Timestamp { get; set; }
        
        [JsonPropertyName("tempInsideC")]
        public decimal? TempInsideC { get; set; }
        
        [JsonPropertyName("humInsidePct")]
        public decimal? HumInsidePct { get; set; }
        
        [JsonPropertyName("tempOutsideC")]
        public decimal? TempOutsideC { get; set; }
        
        [JsonPropertyName("humOutsidePct")]
        public decimal? HumOutsidePct { get; set; }
        
        [JsonPropertyName("weightKg")]
        public decimal? WeightKg { get; set; }
    }
} 