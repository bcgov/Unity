using System;
using System.Globalization;
using Unity.Flex.Worksheets;

namespace Unity.Flex
{
    public static class DataGridExtensions
    {
        public static string ApplyPresentationFormatting(this string value, string columnType, string? format)
        {
            if (value == null) return string.Empty;

            return columnType switch
            {
                var ct when IsDateColumn(ct) && TryParseDate(value, format, out string formattedDate) => formattedDate,
                var ct when IsDateTimeColumn(ct) && TryParseDateTime(value, format, out string formattedDateTime) => formattedDateTime,
                var ct when IsCurrencyColumn(ct) && TryParseCurrency(value, format, out string formattedCurrency) => formattedCurrency,
                var ct when IsYesNoColumn(ct) && TryFormatYesNo(value, out string formattedYesNo) => formattedYesNo,
                var ct when IsCheckBoxColumn(ct) && TryFormatCheckbox(value, out string formattedCheckbox) => formattedCheckbox,
                _ => value
            };
        }


        public static string ApplyStoreFormatting(this string value, string columnType)
        {
            return columnType switch
            {
                var ct when IsDateColumn(ct) => ValueConverterHelpers.ConvertDate(value),
                var ct when IsDateTimeColumn(ct) => ValueConverterHelpers.ConvertDateTime(value),
                var ct when IsCurrencyColumn(ct) => ValueConverterHelpers.ConvertDecimal(value),
                var ct when IsYesNoColumn(ct) => ValueConverterHelpers.ConvertYesNo(value),
                var ct when IsCheckBoxColumn(ct) => ValueConverterHelpers.ConvertCheckbox(value),
                _ => value
            };
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style",
            "IDE0060:Remove unused parameter",
            Justification = "We ignore the format provided from CHEFS for datetime as this does not display correctly")]
        private static bool TryParseDateTime(string value, string? format, out string formattedDateTime)
        {
            const string fixedFormat = "yyyy-MM-dd hh:mm:ss tt";
            if (DateTime.TryParse(value, new CultureInfo("en-CA"), DateTimeStyles.None, out DateTime dateTime))
            {
                // The format that CHEFS provides vs the provided value don't format correctly
                format = fixedFormat;
                var appliedFormat = !string.IsNullOrEmpty(format) ? format : fixedFormat;
                formattedDateTime = dateTime.ToString(appliedFormat, CultureInfo.InvariantCulture);
                return true;
            }
            formattedDateTime = string.Empty;
            return false;
        }

        private static bool IsDateTimeColumn(string columnType)
        {
            return columnType == CustomFieldType.DateTime.ToString();
        }

        private static bool TryFormatCheckbox(string value, out string formattedCheckbox)
        {
            if (value.IsTruthy()) formattedCheckbox = "true";
            else
                formattedCheckbox = "false";
            return true;
        }

        private static bool IsCheckBoxColumn(string columnType)
        {
            return columnType == CustomFieldType.Checkbox.ToString();
        }

        private static bool TryFormatYesNo(string value, out string formattedYesNo)
        {
            if (string.IsNullOrEmpty(value) || value == "Please choose...")
            {
                formattedYesNo = string.Empty;
                return true;
            }
            formattedYesNo = value;
            return false;
        }

        private static bool IsYesNoColumn(string columnType)
        {
            return columnType == CustomFieldType.YesNo.ToString();
        }

        private static bool IsDateColumn(string columnType)
        {
            return columnType == CustomFieldType.Date.ToString();
        }

        private static bool IsCurrencyColumn(string columnType)
        {
            return columnType == CustomFieldType.Currency.ToString();
        }

        private static bool TryParseDate(string value, string? format, out string formattedDate)
        {
            if (DateTime.TryParse(value, new CultureInfo("en-CA"), DateTimeStyles.None, out DateTime dateTime))
            {
                var appliedFormat = !string.IsNullOrEmpty(format) ? format : "yyyy-MM-dd";
                formattedDate = dateTime.ToString(appliedFormat, CultureInfo.InvariantCulture);
                return true;
            }
            formattedDate = string.Empty;
            return false;
        }

        private static bool TryParseCurrency(string value, string? format, out string formattedCurrency)
        {
            if (decimal.TryParse(value, out decimal number))
            {
                var currencyCode = !string.IsNullOrEmpty(format) ? format : "CAD";
                var culture = GetCultureInfoByCurrencyCode(currencyCode);
                formattedCurrency = number.ToString("C", culture);
                return true;
            }
            formattedCurrency = string.Empty;
            return false;
        }

        private static CultureInfo GetCultureInfoByCurrencyCode(string currencyCode)
        {
            foreach (var culture in CultureInfo.GetCultures(CultureTypes.SpecificCultures))
            {
                var region = new RegionInfo(culture.Name);
                if (region.ISOCurrencySymbol == currencyCode)
                {
                    return culture;
                }
            }

            throw new ArgumentException("Invalid or unsupported currency code.");
        }
    }
}
