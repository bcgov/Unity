using System;
using System.Globalization;
using System.Runtime.InteropServices;

namespace Unity.Modules.Shared.Utils;
public static class DateTimeExtensions
{
    // BC Pacific timezone: PST does NOT observe DST in 2026 — fixed UTC-8 year-round.
    private static readonly TimeSpan BcPstOffset = TimeSpan.FromHours(-8);

    // BC Mountain timezone: Peace River / NE BC region — MST/MDT, DST still applies.
    private const string WindowsMountainId = "Mountain Standard Time";
    private const string IanaMountainId = "America/Edmonton";
    private static readonly Lazy<TimeZoneInfo> MountainTimeZone = new(GetMountainTimeZone, isThreadSafe: true);

    /// <summary>
    /// Formats a nullable <see cref="DateTime"/> value as an ISO 8601-compliant UTC timestamp.
    /// </summary>
    public static string FormatTimestamp(DateTime? utcTime)
    {
        if (!utcTime.HasValue)
            return string.Empty;

        var utcTimeValue = utcTime.Value.Kind == DateTimeKind.Utc
            ? utcTime.Value
            : DateTime.SpecifyKind(utcTime.Value, DateTimeKind.Utc);

        return utcTimeValue.ToString("o", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Converts a given UTC time to BC Pacific Standard Time and formats it as a string.
    /// BC's Pacific timezone does NOT observe Daylight Saving Time in 2026; PST (UTC-8)
    /// is applied year-round. For the Peace River / NE BC region (Mountain Time), use
    /// <see cref="FormatMountainTime"/> instead.
    /// </summary>
    /// <param name="utcTime">The UTC time to convert. If <see langword="null"/>, an empty string is returned.</param>
    /// <returns>A string formatted as "yyyy-MM-dd h:mm tt (PST)".</returns>
    public static string FormatPacificTime(DateTime? utcTime)
    {
        if (!utcTime.HasValue)
            return string.Empty;

        var utcTimeValue = utcTime.Value.Kind == DateTimeKind.Utc
            ? utcTime.Value
            : DateTime.SpecifyKind(utcTime.Value, DateTimeKind.Utc);

        // BC PST is a fixed UTC-8 offset — no DST adjustment in 2026.
        var bcPstDateTime = new DateTimeOffset(utcTimeValue, TimeSpan.Zero).ToOffset(BcPstOffset);

        return $"{bcPstDateTime.ToString("yyyy-MM-dd h:mm tt", CultureInfo.InvariantCulture)} (PST)";
    }

    /// <summary>
    /// Converts a given UTC time to BC Mountain Time (Peace River / NE BC region) and formats it.
    /// Unlike BC's Pacific zone, the Mountain timezone in BC DOES observe Daylight Saving Time in 2026:
    /// MST (UTC-7) in winter, MDT (UTC-6) in summer.
    /// </summary>
    /// <param name="utcTime">The UTC time to convert. If <see langword="null"/>, an empty string is returned.</param>
    /// <returns>A string formatted as "yyyy-MM-dd h:mm tt (MST)" or "yyyy-MM-dd h:mm tt (MDT)".</returns>
    public static string FormatMountainTime(DateTime? utcTime)
    {
        if (!utcTime.HasValue)
            return string.Empty;

        var utcTimeValue = utcTime.Value.Kind == DateTimeKind.Utc
            ? utcTime.Value
            : DateTime.SpecifyKind(utcTime.Value, DateTimeKind.Utc);

        var mountainTz = MountainTimeZone.Value;
        var mtDateTime = TimeZoneInfo.ConvertTimeFromUtc(utcTimeValue, mountainTz);
        string abbr = mountainTz.IsDaylightSavingTime(mtDateTime) ? "(MDT)" : "(MST)";

        return $"{mtDateTime.ToString("yyyy-MM-dd h:mm tt", CultureInfo.InvariantCulture)} {abbr}";
    }

    private static TimeZoneInfo GetMountainTimeZone()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            if (TryFindTimeZone(WindowsMountainId, out var tz)) return tz;
            if (TryFindTimeZone(IanaMountainId, out tz)) return tz;
        }
        else
        {
            if (TryFindTimeZone(IanaMountainId, out var tz)) return tz;
            if (TryFindTimeZone(WindowsMountainId, out tz)) return tz;
        }

        throw new TimeZoneNotFoundException(
            $"Neither '{WindowsMountainId}' nor '{IanaMountainId}' time zone IDs were found on this system.");
    }

    private static bool TryFindTimeZone(string id, out TimeZoneInfo tz)
    {
        try
        {
            tz = TimeZoneInfo.FindSystemTimeZoneById(id);
            return true;
        }
        catch (TimeZoneNotFoundException) {
            tz = null!;
            return false;
        }
        catch (InvalidTimeZoneException) {
            tz = null!;
            return false;
        }
    }
}
