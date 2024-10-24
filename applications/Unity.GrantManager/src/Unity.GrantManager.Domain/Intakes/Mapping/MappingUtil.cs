using System;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace Unity.GrantManager.Intakes.Mapping
{
    public static class MappingUtil
    {
        public static string ResolveAndTruncateField(int maxLength, string defaultFieldName, string? valueString, ILogger logger)
        {
            string fieldValue = defaultFieldName;

            if (!string.IsNullOrEmpty(valueString) && valueString.Length > maxLength)
            {
                logger.LogError("Truncation: {FieldName} has been truncated! - Max length: {Length}", defaultFieldName, maxLength);
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


    }

}
