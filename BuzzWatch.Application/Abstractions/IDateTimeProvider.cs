namespace BuzzWatch.Application.Abstractions
{
    public interface IDateTimeProvider
    {
        DateTimeOffset UtcNow { get; }
    }
} 