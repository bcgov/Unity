using System;
using System.Globalization;

namespace Unity.Flex
{
    public static class ValueConverterHelpers
    {
        public static string ConvertDate(object? value)
        {
            if (value == null) return string.Empty;
            var strVal = value.ToString();
            if (strVal == null) return string.Empty;

            var culture = new CultureInfo("en-CA");
            var parseDate = DateTime.TryParse(strVal, culture, DateTimeStyles.None, out DateTime date);
            if (parseDate) return date.ToString("yyyy-MM-dd");
            return string.Empty;
        }

        public static string ConvertDateTime(object? value)
        {
            if (value == null) return string.Empty;
            var strVal = value.ToString();
            if (strVal == null) return string.Empty;

            var culture = new CultureInfo("en-CA");
            var parseDate = DateTime.TryParse(strVal, culture, DateTimeStyles.None, out DateTime date);
            if (parseDate) return date.ToString("yyyy-MM-dd HH:mm:ss");
            return string.Empty;
        }

        public static string ConvertDecimal(object? value)
        {
            var valid = decimal.TryParse(value?.ToString(), out decimal decimalValue);
            if (valid) return decimalValue.ToString("0.00");
            return string.Empty;
        }

        public static string ConvertYesNo(object? value)
        {
            if (value == null) return string.Empty;
            var strVal = value.ToString();
            if (strVal == null) return string.Empty;

            var valCheck = strVal.Trim().ToLower();
            if (valCheck == "yes" || valCheck == "true" || valCheck == "1") return "Yes";
            if (valCheck == "no" || valCheck == "false" || valCheck == "0") return "No";
            return string.Empty;
        }

        public static string ConvertCheckbox(object? value)
        {
            const string falseStr = "false";
            const string trueStr = "true";
            if (value == null) return falseStr;
            var strVal = value.ToString();
            if (strVal == null) return falseStr;

            var valCheck = strVal.Trim().ToLower();
            if (valCheck == "on" || valCheck == trueStr || valCheck == "1") return trueStr;
            if (valCheck == "" || valCheck == falseStr || valCheck == "0") return falseStr;
            return string.Empty;
        }
    }
}
