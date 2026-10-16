using Microsoft.Extensions.Options;
using TaskManager.Application.Abstractions;

namespace TaskManager.Infrastructure.Time;

public class BusinessClockOptions
{
    public const string SectionName = "BusinessClock";

    public string TimeZone { get; set; } = "America/Sao_Paulo";
}

public class BusinessClock(TimeProvider timeProvider, IOptions<BusinessClockOptions> options) : IBusinessClock
{
    private readonly TimeZoneInfo _timeZone = ResolveTimeZone(options.Value.TimeZone);

    public DateTime UtcNow => timeProvider.GetUtcNow().UtcDateTime;

    public DateOnly Today => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(UtcNow, _timeZone));

    private static TimeZoneInfo ResolveTimeZone(string id)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(id);
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.Utc;
        }
    }
}
