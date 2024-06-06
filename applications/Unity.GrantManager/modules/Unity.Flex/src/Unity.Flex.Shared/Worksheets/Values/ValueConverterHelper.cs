using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using Unity.Flex.Worksheets.Definitions;

namespace Unity.Flex.Worksheets.Values
{
    internal static class ValueConverterHelpers
    {
        internal static string ConvertDate(object? value)
        {
            if (value == null) return string.Empty;
            var strVal = value.ToString();
            if (strVal == null) return string.Empty;

            var culture = new CultureInfo("en-CA");
            var parseDate = DateTime.TryParse(strVal, culture, DateTimeStyles.None, out DateTime date);
            if (parseDate) return date.ToString("yyyy-MM-dd");
            return string.Empty;
        }

        internal static string ConvertDecimal(object? value)
        {
            var valid = decimal.TryParse(value?.ToString(), out decimal decimalValue);
            if (valid) return decimalValue.ToString();
            return string.Empty;
        }

        internal static string ConvertYesNo(object? value)
        {
            if (value == null) return string.Empty;
            var strVal = value.ToString();
            if (strVal == null) return string.Empty;

            var valCheck = strVal.Trim().ToLower();
            if (valCheck == "yes" || valCheck == "true" || valCheck == "1") return "Yes";
            if (valCheck == "no" || valCheck == "false" || valCheck == "0") return "No";
            return string.Empty;
        }

        internal static string ConvertCheckbox(object? value)
        {
            if (value == null) return "false";
            var strVal = value.ToString();
            if (strVal == null) return "false";

            var valCheck = strVal.Trim().ToLower();
            if (valCheck == "on" || valCheck == "true" || valCheck == "1") return "true";
            if (valCheck == "" || valCheck == "false" || valCheck == "0") return "false";
            return string.Empty;
        }

        internal static CheckboxGroupValueOption[] ConvertCheckboxGroupCurrentValue(object? value, string? definition)
        {
            if (value == null) return [];
            var token = JToken.Parse(value.ToString());
            if (token is JArray)
            {
                return JsonSerializer.Deserialize<CheckboxGroupValueOption[]>(value.ToString()) ?? [];
            }
            else
            {
                // raw post from CHEFS object                                
                var fieldDefinition = JsonSerializer.Deserialize<CheckboxGroupDefinition>(definition ?? "{[]}") ?? new CheckboxGroupDefinition();
                var checkBoxGroupValueOptions = new List<CheckboxGroupValueOption>();
                foreach (var check in fieldDefinition.Options)
                {
                    JToken? jToken = ((JObject)token).SelectToken(check.Key);
                    var fieldOption = fieldDefinition.Options.Find(s => s.Key == check.Key);
                    if (fieldOption != null)
                    {
                        checkBoxGroupValueOptions.Add(new CheckboxGroupValueOption() { Key = fieldOption.Key, Value = bool.Parse(jToken?.SelectToken(check.Key)?.ToString() ?? "false") });
                    }
                }

                return checkBoxGroupValueOptions.ToArray();
            }
        }
    }
}
