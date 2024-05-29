using System;
using System.Text.Json;

namespace Unity.Flex.Worksheets.Values
{
    public static class ValueConverter
    {
        public static string Convert(string currentValue, CustomFieldType type)
        {
            return type switch
            {
                CustomFieldType.Text => JsonSerializer.Serialize(new TextValue(currentValue)),
                CustomFieldType.Numeric => JsonSerializer.Serialize(new NumericValue(currentValue)),
                CustomFieldType.Currency => JsonSerializer.Serialize(new CurrencyValue(decimal.Parse(currentValue))),
                CustomFieldType.Date => JsonSerializer.Serialize(new DateValue(currentValue)),
                CustomFieldType.DateTime => JsonSerializer.Serialize(new DateTimeValue(currentValue)),
                CustomFieldType.YesNo => JsonSerializer.Serialize(new YesNoValue(currentValue)),
                _ => throw new NotImplementedException()
            }; ;
        }
    }
}
