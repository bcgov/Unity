using System;
using System.Globalization;

namespace Unity.Flex.Worksheets.Values
{
    internal static class ValueConverterHelpers
    {
        internal static string ConvertDate(object? value)
        {
            if (value == null) return string.Empty;
            var strVal = value.ToString();
            if (strVal == null) return string.Empty;

            var culture = new CultureInfo("en-CA");
            var parseDate = DateTime.TryParse(strVal, culture, DateTimeStyles.None, out DateTime date);
            if (parseDate) return date.ToString("yyyy-MM-dd");
            return string.Empty;
        }

        internal static string ConvertYesNo(object? value)
        {
            if (value == null) return string.Empty;
            var strVal = value.ToString();
            if (strVal == null) return string.Empty;

            var valCheck = strVal.Trim().ToLower();
            if (valCheck == "yes" || valCheck == "true" || valCheck == "1") return "Yes";
            if (valCheck == "no" || valCheck == "false" || valCheck == "0") return "No";
            return string.Empty;
        }
    }
}
