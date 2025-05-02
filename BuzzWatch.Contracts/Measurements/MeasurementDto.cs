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
        
        [JsonPropertyName("batteryPct")]
        public decimal? BatteryPct { get; set; }
        
        // Accessor properties to match what's used in the views and viewmodels
        public decimal? Temperature => TempInsideC;
        public decimal? Humidity => HumInsidePct;
        public decimal? Weight => WeightKg;
        public decimal? BatteryLevel => BatteryPct;
    }
} 