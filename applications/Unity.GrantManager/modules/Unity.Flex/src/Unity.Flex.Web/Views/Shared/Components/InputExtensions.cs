using System.Linq;
using System.Text.Json;
using Unity.Flex.Scoresheets;
using Unity.Flex.Worksheets;
using Unity.Flex.Worksheets.Definitions;
using Unity.Flex.Worksheets.Values;

namespace Unity.Flex.Web.Views.Shared.Components
{
    public static class InputExtensions
    {
        public static string ConvertInputType(this CustomFieldType type)
        {
            return type switch
            {
                CustomFieldType.Text => "text",
                CustomFieldType.Numeric => "number",
                CustomFieldType.Currency => "text",
                CustomFieldType.DateTime => "datetime-local",
                CustomFieldType.Date => "date",
                CustomFieldType.Radio => "radio",
                CustomFieldType.Checkbox => "checkbox",
                CustomFieldType.CheckboxGroup => "checkbox",
                CustomFieldType.Email => "email",                
                _ => "text",
            };
        }

        public static object? ConvertInputValueOrNull(this string currentValue, CustomFieldType type)
        {
            return ValueResolver.Resolve(currentValue, type);
        }

        public static CustomFieldDefinition? ConvertDefinition(this string definition, CustomFieldType type)
        {
            return type switch
            {
                CustomFieldType.Text => JsonSerializer.Deserialize<TextDefinition>(definition),
                CustomFieldType.Numeric => JsonSerializer.Deserialize<NumericDefinition>(definition),
                CustomFieldType.Currency => JsonSerializer.Deserialize<CurrencyDefinition>(definition),
                CustomFieldType.DateTime => JsonSerializer.Deserialize<DateTimeDefinition>(definition),
                CustomFieldType.Date => JsonSerializer.Deserialize<DateDefinition>(definition),
                CustomFieldType.YesNo => JsonSerializer.Deserialize<YesNoDefinition>(definition),
                CustomFieldType.Phone => JsonSerializer.Deserialize<PhoneDefinition>(definition),
                CustomFieldType.Email => JsonSerializer.Deserialize<EmailDefinition>(definition),
                CustomFieldType.Radio => JsonSerializer.Deserialize<RadioDefinition>(definition),
                CustomFieldType.Checkbox => JsonSerializer.Deserialize<CheckboxDefinition>(definition),
                CustomFieldType.CheckboxGroup => JsonSerializer.Deserialize<CheckboxGroupDefinition>(definition),
                CustomFieldType.SelectList => JsonSerializer.Deserialize<SelectListDefinition>(definition),
                CustomFieldType.BCAddress => JsonSerializer.Deserialize<BCAddressDefinition>(definition),
                CustomFieldType.TextArea => JsonSerializer.Deserialize<TextAreaDefinition>(definition),
                _ => null,
            };
        }

        public static CustomFieldDefinition? ConvertDefinition(this string definition, QuestionType type)
        {
            return type switch
            {
                QuestionType.Text => JsonSerializer.Deserialize<TextDefinition>(definition),
                QuestionType.Number => JsonSerializer.Deserialize<NumericDefinition>(definition),
                QuestionType.YesNo => JsonSerializer.Deserialize<QuestionYesNoDefinition>(definition),
                QuestionType.SelectList => JsonSerializer.Deserialize<QuestionSelectListDefinition>(definition),
                _ => null,
            };
        }

        public static string[] GetCheckedOptions(this string value)
        {
            if (string.IsNullOrEmpty(value)) return [];
            var currentValue = JsonSerializer.Deserialize<CheckboxGroupValue>(value);
            if (currentValue == null) return [];
            if (currentValue.Value?.ToString() == string.Empty) return [];
            var values = JsonSerializer.Deserialize<CheckboxGroupValueOption[]>(currentValue.Value?.ToString() ?? "[]");
            return values?.Where(s => s.Value).Select(s => s.Key).ToArray() ?? [];
        }

        public static bool CompareYesNoSelectListValue(this string value, string compare)
        {
            var yesNo = JsonSerializer.Deserialize<YesNoValue>(value);
            return yesNo?.Value?.ToString() == compare;
        }

        public static bool CompareSelectListKey(this string key, string compare)
        {
            var value = JsonSerializer.Deserialize<SelectListOption>(key);
            return value?.Value.ToString() == compare;
        }

        public static string ApplyCssClass(this CustomFieldType type)
        {
            return type switch
            {
                CustomFieldType.Currency => "form-control custom-currency-input",
                CustomFieldType.YesNo => "form-select form-control",
                _ => "form-control",
            };
        }

        public static string? GetMinValueOrNull(this CustomFieldDefinition field)
        {
            return DefinitionResolver.ResolveMin(field);                                    
        }

        public static string? GetMaxValueOrNull(this CustomFieldDefinition field)
        {
            return DefinitionResolver.ResolveMax(field);
        }

        public static string? GetYesValueOrNull(this CustomFieldDefinition field)
        {
            return DefinitionResolver.ResolveYesValue(field);
        }

        public static string? GetNoValueOrNull(this CustomFieldDefinition field)
        {
            return DefinitionResolver.ResolveNoValue(field);
        }

        public static string? GetMinLengthValueOrNull(this CustomFieldDefinition field)
        {
            return DefinitionResolver.ResolveMinLength(field);
        }

        public static string? GetMaxLengthValueOrNull(this CustomFieldDefinition field)
        {
            return DefinitionResolver.ResolveMaxLength(field);
        }
    }
}
