using BuzzWatch.Application.Abstractions;

namespace BuzzWatch.Infrastructure.Services
{
    public class DateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }
} 