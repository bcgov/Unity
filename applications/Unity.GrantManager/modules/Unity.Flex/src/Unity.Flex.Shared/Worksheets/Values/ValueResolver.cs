using System;
using System.Text.Json;

namespace Unity.Flex.Worksheets
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
                _ => throw new NotImplementedException()
            }; ;
        }
    }
}
