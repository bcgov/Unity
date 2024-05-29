using System;

namespace Unity.Flex.Worksheets.Values
{
    internal static class ValueResolverHelpers
    {
        internal static object? ConvertCurrency(object? value)
        {
            // We present the currency as thousand separated string
            if (value == null) return null;
            var strVal = value.ToString();
            if (strVal == null) return null;
            var currency = decimal.Parse(strVal);
            IFormatProvider caFormatProvider = new System.Globalization.CultureInfo("en-CA");
            return currency.ToString("#,##0.00", caFormatProvider);
        }
    }
}