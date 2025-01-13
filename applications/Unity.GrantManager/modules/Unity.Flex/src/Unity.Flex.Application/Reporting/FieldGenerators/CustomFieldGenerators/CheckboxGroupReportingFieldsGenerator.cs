using System.Linq;
using System.Text;
using System.Text.Json;
using Unity.Flex.Domain.Worksheets;
using Unity.Flex.Worksheets.Definitions;

namespace Unity.Flex.Reporting.FieldGenerators.CustomFieldGenerators
{
    public class CheckboxGroupReportingFieldsGenerator(CustomField customField)
        : CustomFieldsReportingGenerator(customField), IReportingFieldsGenerator
    {
        public (string keys, string columns) Generate()
        {
            var value = JsonSerializer.Deserialize<CheckboxGroupDefinition>(customField.Definition.ToString());
            StringBuilder keysString = new();
            StringBuilder columnsString = new();

            if (value == null)
            {
                return (string.Empty, string.Empty);
            }

            var options = value.Options ?? [];

            foreach (var key in options.Select(s => s.Key))
            {
                keysString
                    .Append(key)
                    .Append(ReportingConsts.ReportFieldDelimiter);

                columnsString
                    .Append(key)
                    .Append(ReportingConsts.ReportFieldDelimiter);
            }

            keysString.TrimEndDelimeter();
            columnsString.TrimEndDelimeter();

            return (keysString.ToString(), columnsString.ToString());
        }
    }
}
