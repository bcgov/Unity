using System;
using System.Text.Json;
using Unity.Flex.Scoresheets;

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
                CustomFieldType.Currency => JsonSerializer.Serialize(new CurrencyValue(ValueConverterHelpers.ConvertDecimal(currentValue))),
                CustomFieldType.Date => JsonSerializer.Serialize(new DateValue(ValueConverterHelpers.ConvertDate(currentValue))),
                CustomFieldType.DateTime => JsonSerializer.Serialize(new DateTimeValue(currentValue)),
                CustomFieldType.YesNo => JsonSerializer.Serialize(new YesNoValue(ValueConverterHelpers.ConvertYesNo(currentValue))),
                CustomFieldType.Phone => JsonSerializer.Serialize(new PhoneValue(currentValue)),
                CustomFieldType.Email => JsonSerializer.Serialize(new EmailValue(currentValue)),
                CustomFieldType.Radio => JsonSerializer.Serialize(new RadioValue(currentValue)),
                CustomFieldType.Checkbox => JsonSerializer.Serialize(new CheckboxValue(ValueConverterHelpers.ConvertCheckbox(currentValue))),
                CustomFieldType.CheckboxGroup => JsonSerializer.Serialize(new CheckboxGroupValue(currentValue)),
                CustomFieldType.SelectList => JsonSerializer.Serialize(new SelectListValue(currentValue)),
                CustomFieldType.BCAddress => JsonSerializer.Serialize(new BCAddressValue(currentValue)),
                CustomFieldType.TextArea => JsonSerializer.Serialize(new TextAreaValue(currentValue)),
                _ => throw new NotImplementedException()
            };
        }

        public static string Convert(string currentValue, QuestionType type)
        {
            return type switch
            {
                QuestionType.Text => JsonSerializer.Serialize(new TextValue(currentValue)),
                QuestionType.Number => JsonSerializer.Serialize(new NumericValue(currentValue)),
                QuestionType.YesNo => JsonSerializer.Serialize(new YesNoValue(ValueConverterHelpers.ConvertYesNo(currentValue))),
                QuestionType.SelectList => JsonSerializer.Serialize(new SelectListValue( currentValue)),
                _ => throw new NotImplementedException()
            };
        }
    }
}
