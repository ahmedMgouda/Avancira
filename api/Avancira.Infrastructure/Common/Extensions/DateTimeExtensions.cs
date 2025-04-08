using TimeZoneConverter;

namespace Avancira.Infrastructure.Common.Extensions;
public static class DateTimeExtensions
{
    public static DateTime ToUserTime(this DateTime utcDateTime, string timeZoneId)
    {
        var tz = TZConvert.GetTimeZoneInfo(timeZoneId);
        return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, tz);
    }

    public static DateTime ToUtcFromUserTime(this DateTime localDateTime, string timeZoneId)
    {
        var tz = TZConvert.GetTimeZoneInfo(timeZoneId);
        return TimeZoneInfo.ConvertTimeToUtc(localDateTime, tz);
    }
}