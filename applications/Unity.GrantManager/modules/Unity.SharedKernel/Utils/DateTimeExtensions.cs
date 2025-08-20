using System;
using System.Globalization;
using System.Runtime.InteropServices;

namespace Unity.Modules.Shared.Utils;
public static class DateTimeExtensions
{
    private const string WindowsPacificId = "Pacific Standard Time";
    private const string IanaPacificId = "America/Los_Angeles";

    // Lazy-initialized cached timezone to avoid repeated OS lookups.
    private static readonly Lazy<TimeZoneInfo> PacificTimeZone = new(GetPacificTimeZone, isThreadSafe: true);

    /// <summary>
    /// Formats a nullable <see cref="DateTime"/> value as an ISO 8601-compliant UTC timestamp.
    /// </summary>
    /// <remarks>If the provided <paramref name="utcTime"/> is not already in UTC, it will be treated as a
    /// local time and converted to UTC before formatting.</remarks>
    /// <param name="utcTime">The nullable <see cref="DateTime"/> to format. If the value is not in UTC, it will be converted to UTC.</param>
    /// <returns>A string representation of the <paramref name="utcTime"/> in ISO 8601 format, or an empty string if <paramref
    /// name="utcTime"/> is <see langword="null"/>.</returns>
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
    /// Converts a given UTC time to Pacific Time and formats it as a string with the appropriate time zone
    /// abbreviation.
    /// </summary>
    /// <remarks>The method ensures that the input <paramref name="utcTime"/> is treated as UTC. If the input
    /// time is not explicitly marked as UTC, it is converted to UTC before performing the time zone
    /// conversion.</remarks>
    /// <param name="utcTime">The UTC time to convert. If <see langword="null"/>, an empty string is returned.</param>
    /// <returns>A string representing the Pacific Time equivalent of the provided UTC time, formatted as "yyyy-MM-dd h:mm tt"
    /// followed by the time zone abbreviation "(PDT)" for daylight saving time or "(PST)" for standard time. Returns an
    /// empty string if <paramref name="utcTime"/> is <see langword="null"/>.</returns>
    public static string FormatPacificTime(DateTime? utcTime)
    {
        if (!utcTime.HasValue)
            return string.Empty;

        var utcTimeValue = utcTime.Value.Kind == DateTimeKind.Utc
            ? utcTime.Value
            : DateTime.SpecifyKind(utcTime.Value, DateTimeKind.Utc);

        var pacificTimeZone = PacificTimeZone.Value;
        var pacificDateTime = TimeZoneInfo.ConvertTimeFromUtc(utcTimeValue, pacificTimeZone);

        string timeZoneAbbreviation = pacificTimeZone.IsDaylightSavingTime(pacificDateTime) ? "(PDT)" : "(PST)";
        return $"{pacificDateTime.ToString("yyyy-MM-dd h:mm tt", CultureInfo.InvariantCulture)} {timeZoneAbbreviation}";
    }

    private static TimeZoneInfo GetPacificTimeZone()
    {
        // If running on Windows, attempt Windows ID first; otherwise attempt IANA first.
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            if (TryFindTimeZone(WindowsPacificId, out var tz)) return tz;
            if (TryFindTimeZone(IanaPacificId, out tz)) return tz;
        }
        else
        {
            if (TryFindTimeZone(IanaPacificId, out var tz)) return tz;
            if (TryFindTimeZone(WindowsPacificId, out tz)) return tz;
        }

        throw new TimeZoneNotFoundException(
            $"Neither '{WindowsPacificId}' nor '{IanaPacificId}' time zone IDs were found on this system.");
    }
    private static bool TryFindTimeZone(string id, out TimeZoneInfo tz)
    {
        try
        {
            tz = TimeZoneInfo.FindSystemTimeZoneById(id);
            return true;
        }
        catch (TimeZoneNotFoundException) { }
        catch (InvalidTimeZoneException) { }
        tz = null!;
        return false;
    }
}
