using System;
using System.Globalization;
using Unity.Flex.Worksheets;

namespace Unity.Flex
{
    public static class DataGridExtensions
    {
        public static string ApplyPresentationFormatting(this string value, string columnType, string? format, PresentationSettings presentationSettings)
        {
            if (value == null) return string.Empty;

            return columnType switch
            {
                var ct when IsDateColumn(ct) && TryParseDate(value, out string formattedDate) => formattedDate,
                var ct when IsDateTimeColumn(ct) && TryParseDateTime(value, presentationSettings.BrowserOffsetMinutes, out string formattedDateTime) => formattedDateTime,
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

        private static bool TryParseDateTime(string value, int browserOffsetMinutes, out string formattedDateTime)
        {
            // Apply the browser offset before presenting the data
            const string fixedFormat = "yyyy-MM-dd hh:mm:ss tt";

            if (DateTimeOffset.TryParse(value, new CultureInfo("en-CA"), DateTimeStyles.None, out DateTimeOffset dateTimeOffset))
            {
                // Adjust the DateTimeOffset by the browser offset (in minutes)
                dateTimeOffset = dateTimeOffset.ToOffset(TimeSpan.FromMinutes(-browserOffsetMinutes));

                // Convert the DateTimeOffset to a DateTime
                DateTime dateTime = dateTimeOffset.DateTime;

                // The format that CHEFS provides vs the provided value don't format correctly
                formattedDateTime = dateTime.ToString(fixedFormat, CultureInfo.InvariantCulture);
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

        private static bool TryParseDate(string value, out string formattedDate)
        {
            const string fixedFormat = "yyyy-MM-dd";

            if (DateTime.TryParse(value, new CultureInfo("en-CA"), DateTimeStyles.None, out DateTime dateTime))
            {
                formattedDate = dateTime.ToString(fixedFormat, CultureInfo.InvariantCulture);
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

        static CultureInfo GetCultureInfoByCurrencyCode(string currencyCode)
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
