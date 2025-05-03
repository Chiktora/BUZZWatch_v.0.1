using BuzzWatch.Contracts.Measurements;

namespace BuzzWatch.Contracts.Devices
{
    public class DeviceExportDataDto
    {
        public DeviceDto Device { get; set; } = new DeviceDto();
        public List<MeasurementDto> Measurements { get; set; } = new List<MeasurementDto>();
        public DateTimeOffset ExportedAt { get; set; }
        public int TimeSpanDays { get; set; }
        public string ExportFormat { get; set; } = "JSON";
        
        // Statistics
        public decimal? MinTemperature => Measurements.Any(m => m.TempInsideC.HasValue) 
            ? Measurements.Where(m => m.TempInsideC.HasValue).Min(m => m.TempInsideC!.Value) 
            : null;
            
        public decimal? MaxTemperature => Measurements.Any(m => m.TempInsideC.HasValue) 
            ? Measurements.Where(m => m.TempInsideC.HasValue).Max(m => m.TempInsideC!.Value) 
            : null;
            
        public decimal? AvgTemperature => Measurements.Any(m => m.TempInsideC.HasValue) 
            ? Measurements.Where(m => m.TempInsideC.HasValue).Average(m => (double)m.TempInsideC!.Value) is double avg ? (decimal)avg : null 
            : null;
            
        public decimal? MinHumidity => Measurements.Any(m => m.HumInsidePct.HasValue) 
            ? Measurements.Where(m => m.HumInsidePct.HasValue).Min(m => m.HumInsidePct!.Value) 
            : null;
            
        public decimal? MaxHumidity => Measurements.Any(m => m.HumInsidePct.HasValue) 
            ? Measurements.Where(m => m.HumInsidePct.HasValue).Max(m => m.HumInsidePct!.Value) 
            : null;
            
        public decimal? AvgHumidity => Measurements.Any(m => m.HumInsidePct.HasValue) 
            ? Measurements.Where(m => m.HumInsidePct.HasValue).Average(m => (double)m.HumInsidePct!.Value) is double avg ? (decimal)avg : null 
            : null;
            
        public decimal? MinWeight => Measurements.Any(m => m.WeightKg.HasValue) 
            ? Measurements.Where(m => m.WeightKg.HasValue).Min(m => m.WeightKg!.Value) 
            : null;
            
        public decimal? MaxWeight => Measurements.Any(m => m.WeightKg.HasValue) 
            ? Measurements.Where(m => m.WeightKg.HasValue).Max(m => m.WeightKg!.Value) 
            : null;
            
        public decimal? AvgWeight => Measurements.Any(m => m.WeightKg.HasValue) 
            ? Measurements.Where(m => m.WeightKg.HasValue).Average(m => (double)m.WeightKg!.Value) is double avg ? (decimal)avg : null 
            : null;
            
        public int RecordCount => Measurements.Count;
        
        public DateTimeOffset? FirstRecordTime => Measurements.Any() 
            ? Measurements.Min(m => m.Timestamp) 
            : null;
            
        public DateTimeOffset? LastRecordTime => Measurements.Any() 
            ? Measurements.Max(m => m.Timestamp) 
            : null;
    }
} 