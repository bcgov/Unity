using System;
using System.Globalization;

namespace Unity.Modules.Shared.Utils;
public static class DateTimeExtensions
{
    public static string FormatTimestamp(DateTime? utcTime)
    {
        if (!utcTime.HasValue)
            return string.Empty;

        var utcTimeValue = utcTime.Value.Kind == DateTimeKind.Utc
            ? utcTime.Value
            : DateTime.SpecifyKind(utcTime.Value, DateTimeKind.Utc);

        return utcTimeValue.ToString("o", CultureInfo.InvariantCulture);
    }

    public static string FormatPacificTime(DateTime? utcTime)
    {
        if (!utcTime.HasValue)
            return string.Empty;

        var utcTimeValue = utcTime.Value.Kind == DateTimeKind.Utc
            ? utcTime.Value
            : DateTime.SpecifyKind(utcTime.Value, DateTimeKind.Utc);

        var pacificTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");
        var pacificDateTime = TimeZoneInfo.ConvertTimeFromUtc(utcTimeValue, pacificTimeZone);

        string timeZoneAbbreviation = pacificTimeZone.IsDaylightSavingTime(pacificDateTime) ? "(PDT)" : "(PST)";

        return $"{pacificDateTime.ToString("yyyy-MM-dd h:mm tt", CultureInfo.InvariantCulture)} {timeZoneAbbreviation}";
    }
}
