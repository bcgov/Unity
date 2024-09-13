using System;
using System.Text.Json;
using Unity.Flex.Scoresheets;

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
                CustomFieldType.TextArea => JsonSerializer.Deserialize<TextAreaValue>(currentValue)?.Value,
                _ => throw new NotImplementedException()
            };
        }

        public static object? Resolve(string currentValue, QuestionType type)
        {
            return type switch
            {
                QuestionType.Text => JsonSerializer.Deserialize<TextValue>(currentValue)?.Value,
                QuestionType.Number => ResolveNumber(currentValue),
                QuestionType.YesNo => JsonSerializer.Deserialize<YesNoValue>(currentValue)?.Value,
                QuestionType.SelectList => JsonSerializer.Deserialize<SelectListValue>(currentValue)?.Value,
                _ => throw new NotImplementedException()
            };

            static object ResolveNumber(string currentValue)
            {
                var numericValue = JsonSerializer.Deserialize<NumericValue>(currentValue)?.Value;
                if (numericValue == null || string.IsNullOrEmpty(numericValue.ToString()))
                {
                    return 0;
                }
                return numericValue;
            }
        }
    }
}
