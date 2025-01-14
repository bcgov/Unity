using System.Linq;
using System.Text.Json;
using Unity.Flex.Domain.Worksheets;
using Unity.Flex.Worksheets.Definitions;

namespace Unity.Flex.Reporting.FieldGenerators.CustomFieldGenerators
{
    public class DataGridReportingFieldsGenerator(CustomField customField)
        : CustomFieldsReportingGenerator(customField), IReportingFieldsGenerator
    {
        public (string keys, string columns) Generate()
        {
            var dynamicKeyColumn = "DynamicColumns";
            var definition = JsonSerializer.Deserialize<DataGridDefinition>(customField.Definition.ToString());

            if (definition == null)
            {
                return (string.Empty, string.Empty);
            }

            if (definition.Dynamic)
            {
                var fieldName = $"{customField.Key}-{dynamicKeyColumn}";
                // We need to add a placeholder column for the dynamic columns, these are hydrated during an intake
                keysString.AddFieldAndDelimiter(fieldName);
                columnsString.AddFieldAndDelimiter(fieldName);
            }

            foreach (var columnName in definition.Columns.Select(s => s.Name))
            {
                var fieldName = $"{customField.Key}-{columnName}";
                keysString.AddFieldAndDelimiter(fieldName);
                columnsString.AddFieldAndDelimiter(fieldName);
            }

            return TrimAndCreateKeysAndColumns(keysString, columnsString);
        }
    }
}
