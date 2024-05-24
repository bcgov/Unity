using System;
using System.Text.Json;

namespace Unity.Flex.Worksheets.Values
{
    public static class ValueResolver
    {
        public static object? Resolve(string currentValue, CustomFieldType type)
        {
            return type switch
            {
                CustomFieldType.Text => JsonSerializer.Deserialize<TextValue>(currentValue)?.Value,
                CustomFieldType.Numeric => JsonSerializer.Deserialize<NumericValue>(currentValue)?.Value,
                CustomFieldType.Currency => JsonSerializer.Deserialize<CurrencyValue>(currentValue)?.Value,
                CustomFieldType.Date => JsonSerializer.Deserialize<DateValue>(currentValue)?.Value,
                CustomFieldType.DateTime => JsonSerializer.Deserialize<DateTimeValue>(currentValue)?.Value,
                CustomFieldType.YesNo => JsonSerializer.Deserialize<YesNoValue>(currentValue)?.Value,
                _ => throw new NotImplementedException()
            }; ;
        }
    }
}
