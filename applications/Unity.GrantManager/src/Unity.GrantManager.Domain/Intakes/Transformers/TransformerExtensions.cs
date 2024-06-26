using Newtonsoft.Json.Linq;
using System.Text.Json;
using Unity.Flex.Worksheets;
using Unity.Flex.Worksheets.Values;
using Unity.GrantManager.Intakes.Transformers;

namespace Unity.GrantManager.Intakes
{
    public static class TransformerExtensions
    {
        public static CustomValueBase ApplyTransformer(this JToken token, CustomFieldType type)
        {
            // Transform the raw data object from CHEFS to UNITY
            return type switch
            {
                CustomFieldType.CheckboxGroup => new CheckboxGroupTransformer().Transform(token),
                CustomFieldType.Currency => new CurrencyValue(token),
                CustomFieldType.Numeric => new NumericValue(token),
                CustomFieldType.DateTime => new DateTimeValue(token),
                CustomFieldType.Date => new DateValue(token),
                CustomFieldType.Email => new EmailValue(token),
                CustomFieldType.YesNo => new YesNoValue(token),
                CustomFieldType.Text => new TextValue(token),
                CustomFieldType.Checkbox => new CheckboxValue(token),
                CustomFieldType.Phone => new PhoneValue(token),
                CustomFieldType.Radio => new RadioValue(token),
                CustomFieldType.SelectList => new SelectListValue(token),
                CustomFieldType.Undefined => new TextValue(token),
                CustomFieldType.BCAddress => new BCAddressTransformer().Transform(token),
                _ => new TextValue(token),
            };
        }

        public static string ApplySerializer(this CustomValueBase value)
        {
            return value switch
            {
                CheckboxGroupValue => JsonSerializer.Serialize(value.Value),       
                BCAddressValue => JsonSerializer.Serialize(value.Value),
                _ => value?.Value?.ToString() ?? string.Empty,
            };
        }
    }
}
