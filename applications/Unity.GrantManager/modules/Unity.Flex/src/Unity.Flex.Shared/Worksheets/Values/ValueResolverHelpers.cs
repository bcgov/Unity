using System.Text.Json;

namespace Unity.Flex.Worksheets.Values
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
#pragma warning disable S125 // Sections of code should not be commented out
        {
            // We present the currency as thousand separated string
            if (value == null) return null;
            var strVal = value.ToString();
            if (string.IsNullOrEmpty(strVal)) return null;
            var currencyParse = decimal.TryParse(strVal, out decimal currency);
            if (!currencyParse) { return null; }
            return currency.ToString();
            /*
             * 
              IFormatProvider caFormatProvider = new System.Globalization.CultureInfo("en-CA");
              return currency.ToString("#,##0.00", caFormatProvider);
            *
            */
        }
#pragma warning restore S125 // Sections of code should not be commented out

        internal static object? ConvertBCAddress(object? value)
        {
            if (value == null) return null;
            // flip from dynamic /JNode to an object
            return JsonSerializer.Deserialize<BCAddressLocationValue>(JsonSerializer.Serialize(value));
        }
    }
}