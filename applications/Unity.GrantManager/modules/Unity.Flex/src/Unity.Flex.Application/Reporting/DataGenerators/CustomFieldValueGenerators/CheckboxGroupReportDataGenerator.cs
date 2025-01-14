using System.Collections.Generic;
using System.Text.Json;
using Unity.Flex.Domain.Worksheets;
using Unity.Flex.Worksheets.Values;

namespace Unity.Flex.Reporting.DataGenerators.CustomFieldValueGenerators
{
    public class CheckboxGroupReportDataGenerator(CustomField customField, FieldInstanceValue value)
        : ReportingDataGeneratorBase(customField, value), IReportingDataGenerator
    {
        public Dictionary<string, List<string>> Generate()
        {
            var values = new Dictionary<string, List<string>>();

            var checkboxValue = JsonSerializer.Deserialize<CheckboxGroupValueOption[]>(value.Value);

            foreach (var option in checkboxValue ?? [])
            {
                values.Add(option.Key, [option.Value.ToString()]);
            }

            return values;
        }
    }
}
