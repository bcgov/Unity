using System;

namespace Unity.Flex.Web.Views.Shared.Components
{
    internal static class ValueResolverHelpers
    {
        internal static object? ConvertCheckbox(object? value)
        {
            if (value == null) return false;
            var strVal = value.ToString();
            if (string.IsNullOrEmpty(strVal)) return false;
            var boolValParse = bool.TryParse(strVal, out bool result);
            if (boolValParse) { return result; } else return false;
        }

        internal static object? ConvertCurrency(object? value)
        {
            // We present the currency as thousand separated string
            if (value == null) return null;
            var strVal = value.ToString();
            if (string.IsNullOrEmpty(strVal)) return null;
            var currencyParse = decimal.TryParse(strVal, out decimal currency);
            if (!currencyParse) { return null; }
            IFormatProvider caFormatProvider = new System.Globalization.CultureInfo("en-CA");
            return currency.ToString("#,##0.00", caFormatProvider);
        }
    }
}