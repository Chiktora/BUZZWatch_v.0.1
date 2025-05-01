using BuzzWatch.Domain.Devices;

namespace BuzzWatch.Infrastructure.Services;

public class HourlyAggregate
{
    public Guid Id { get; set; }
    public Guid DeviceId { get; set; }
    public Device Device { get; set; } = null!;
    public string MetricType { get; set; } = string.Empty;
    public DateTimeOffset Period { get; set; }
    public decimal AvgValue { get; set; }
    public decimal MinValue { get; set; }
    public decimal MaxValue { get; set; }
    public int SampleCount { get; set; }
} 