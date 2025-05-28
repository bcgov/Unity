using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Text.Json;
using Unity.Flex.Domain.WorksheetInstances;
using Unity.Flex.Domain.Worksheets;
using Unity.Flex.Worksheets.Values;

namespace Unity.Flex.Reporting.DataGenerators.CustomFieldValueGenerators
{
    public class CheckboxGroupReportDataGenerator(CustomField customField, CustomFieldValue value)
        : ReportingDataGenerator(customField, value), IReportingDataGenerator
    {
        /// <summary>
        /// Generate a list of keys and matched values for reporting data for a checkboxgroup component
        /// </summary>
        /// <returns>Dictionary of unique keys with any matching values for the keys</returns>
        public (Dictionary<string, List<string>> keyValuePairs, bool compressArray) Generate()
        {
            var values = new Dictionary<string, List<string>>();
            JObject dataValue = JObject.Parse(value.CurrentValue);

            var checkboxValue = JsonSerializer.Deserialize<CheckboxGroupValueOption[]>(dataValue["value"]?.ToString() ?? string.Empty);

            foreach (var option in checkboxValue ?? [])
            {
                values.Add($"{customField.Key}-{option.Key}", [option.Value.ToString()]);
            }

            return (values, true);
        }        
    }
}
