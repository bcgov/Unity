using System;
using System.Globalization;

namespace Unity.GrantManager.Intakes.Mapping
{
    public static class MappingUtil
    {
        public static string ResolveAndTruncateField(int maxLength, string defaultFieldName, string? valueString)
        {
            string fieldValue = defaultFieldName;

            if (!string.IsNullOrEmpty(valueString) && valueString.Length > maxLength)
            {
                fieldValue = valueString.Substring(0, maxLength);
            }
            else if (!string.IsNullOrEmpty(valueString))
            {
                fieldValue = valueString.Trim();
            }

            return fieldValue;
        }

        public static int? ConvertToIntFromString(string? intString)
        {
            if (int.TryParse(intString, out int intParse))
            {
                return intParse;
            }

            return null;
        }

        public static decimal ConvertToDecimalFromStringDefaultZero(string? decimalString)
        {
            decimal decimalValue;
            if (decimal.TryParse(decimalString, out decimal decimalParse))
            {
                decimalValue = decimalParse;
            }
            else
            {
                decimalValue = Convert.ToDecimal("0");
            }
            return decimalValue;
        }

        public static DateTime? ConvertDateTimeNullableFromString(string? dateTime)
        {
            var culture = CultureInfo.InvariantCulture;
            return ConvertDateTimeNullableFromStringWithCulture(dateTime, culture);
        }

        public static DateTime ConvertDateTimeFromStringDefaultNow(string? dateTime)
        {
            if (string.IsNullOrEmpty(dateTime))
            {
                return DateTime.UtcNow;
            } else {
                var culture = CultureInfo.InvariantCulture;
                DateTime.TryParse(dateTime, culture, DateTimeStyles.None, out DateTime parsedDateTime);
                return parsedDateTime;
            }
        }


        /// <summary>
        /// Converts a nullable string to a nullable DateTime using a specified culture.
        /// </summary>
        /// <param name="dateTime">The date and time string to convert.</param>
        /// <param name="culture">The culture information.</param>
        /// <returns>A nullable DateTime, or null if conversion fails.</returns>
        public static DateTime? ConvertDateTimeNullableFromStringWithCulture(string? dateTime, CultureInfo culture)
        {
            if (string.IsNullOrWhiteSpace(dateTime))
            {
                return null;
            }

            if (DateTime.TryParse(dateTime, culture, DateTimeStyles.None, out DateTime parsedDateTime))
            {
                return parsedDateTime;
            }
            return null;
        }

        /// <summary>
        /// Converts a date string from various CHEFS form component formats to a nullable DateTime.
        /// Handles formats from simpledatetime, simpleday, and simpledatetimeadvanced components.
        /// Examples:
        /// - "2025-06-06T00:00:00-07:00" (simpledatetime/simpledatetimeadvanced - ISO 8601 with timezone)
        /// - "06/06/2025" (simpleday - MM/DD/YYYY)
        /// - "2025-06-06" (ISO 8601 date only)
        /// </summary>
        /// <param name="dateString">The date string from a CHEFS form component.</param>
        /// <returns>A nullable DateTime with the date portion, or null if conversion fails.</returns>
        public static DateTime? ConvertDateFromChefsFormat(string? dateString)
        {
            if (string.IsNullOrWhiteSpace(dateString))
            {
                return null;
            }

            // Try ISO 8601 formats with timezone (simpledatetime, simpledatetimeadvanced)
            // Example: "2025-06-06T00:00:00-07:00"
            if (DateTime.TryParse(dateString, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime parsedWithTimezone))
            {
                return parsedWithTimezone.Date;
            }

            // Try MM/DD/YYYY format (simpleday)
            // Example: "06/06/2025"
            string[] formats = new[]
            {
                "MM/dd/yyyy",
                "M/d/yyyy",
                "MM-dd-yyyy",
                "M-d-yyyy"
            };

            if (DateTime.TryParseExact(dateString, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedExact))
            {
                return parsedExact.Date;
            }

            // Try standard ISO date format (yyyy-MM-dd)
            if (DateTime.TryParseExact(dateString, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedIso))
            {
                return parsedIso.Date;
            }

            // Fallback to general parsing with InvariantCulture
            if (DateTime.TryParse(dateString, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedGeneral))
            {
                return parsedGeneral.Date;
            }

            return null;
        }

        public static bool IsJObject(dynamic? applicantAgent)
        {
            if (applicantAgent == null)
                return false;
            try
            {
                if (applicantAgent is Newtonsoft.Json.Linq.JObject)
                {
                    return true;
                }
            }
            catch
            {
                return false;
            }

            return false;
        }
    }
}
