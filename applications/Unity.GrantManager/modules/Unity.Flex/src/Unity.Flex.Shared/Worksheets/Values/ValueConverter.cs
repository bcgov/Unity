using System;
using System.Text.Json;

namespace Unity.Flex.Worksheets
{
    public static class ValueConverter
    {
        public static string Convert(string currentValue, CustomFieldType type)
        {
            return type switch
            {
                CustomFieldType.Text => JsonSerializer.Serialize(new TextValue(currentValue)),
                CustomFieldType.Numeric => JsonSerializer.Serialize(new NumericValue(currentValue)),
                CustomFieldType.Currency => JsonSerializer.Serialize(new CurrencyValue(currentValue)),
                _ => throw new NotImplementedException()
            }; ;
        }
    }
}
