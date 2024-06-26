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
                CustomFieldType.Currency => ValueResolverHelpers.ConvertCurrency(JsonSerializer.Deserialize<CurrencyValue>(currentValue)?.Value),
                CustomFieldType.Date => JsonSerializer.Deserialize<DateValue>(currentValue)?.Value,
                CustomFieldType.DateTime => JsonSerializer.Deserialize<DateTimeValue>(currentValue)?.Value,
                CustomFieldType.YesNo => JsonSerializer.Deserialize<YesNoValue>(currentValue)?.Value,
                CustomFieldType.Phone => JsonSerializer.Deserialize<PhoneValue>(currentValue)?.Value,
                CustomFieldType.Email => JsonSerializer.Deserialize<EmailValue>(currentValue)?.Value,
                CustomFieldType.Radio => JsonSerializer.Deserialize<RadioValue>(currentValue)?.Value,
                CustomFieldType.Checkbox => ValueResolverHelpers.ConvertCheckbox(JsonSerializer.Deserialize<CheckboxValue>(currentValue)?.Value),
                CustomFieldType.CheckboxGroup => JsonSerializer.Deserialize<CheckboxGroupValue>(currentValue)?.Value,
                CustomFieldType.SelectList => JsonSerializer.Deserialize<SelectListValue>(currentValue)?.Value,
                CustomFieldType.BCAddress => ValueResolverHelpers.ConvertBCAddress(JsonSerializer.Deserialize<BCAddressValue>(currentValue)?.Value),
                _ => throw new NotImplementedException()
            };
        }
    }
}
