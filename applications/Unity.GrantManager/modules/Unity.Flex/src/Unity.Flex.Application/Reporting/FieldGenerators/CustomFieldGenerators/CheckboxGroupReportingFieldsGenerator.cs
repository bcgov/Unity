using System.Linq;
using System.Text.Json;
using Unity.Flex.Domain.Worksheets;
using Unity.Flex.Worksheets.Definitions;

namespace Unity.Flex.Reporting.FieldGenerators.CustomFieldGenerators
{
    public class CheckboxGroupReportingFieldsGenerator(CustomField customField)
        : CustomFieldsReportingGenerator(customField), IReportingFieldsGenerator
    {
        /// <summary>
        /// Generate the keys and columns string that represents the fields data for reporting purposes base on the custom field
        /// </summary>
        /// <returns>A tuple with the keys and column delimited by the reporting fields delimeter</returns>
        public (string keys, string columns) Generate()
        {
            var definition = JsonSerializer.Deserialize<CheckboxGroupDefinition>(customField.Definition.ToString());

            if (definition == null)
            {
                return (string.Empty, string.Empty);
            }

            var options = definition.Options ?? [];

            foreach (var key in options.Select(s => s.Key))
            {
                var fieldName = $"{customField.Key}-{key}";
                keysString.AddFieldAndDelimiter(fieldName);
                columnsString.AddFieldAndDelimiter(fieldName);
            }

            return TrimAndCreateKeysAndColumns(keysString, columnsString);
        }
    }
}
