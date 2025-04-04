using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Unity.Flex.Domain.WorksheetInstances;
using Unity.Flex.Domain.Worksheets;
using Unity.Flex.Worksheets.Definitions;
using Unity.Flex.Worksheets.Values;

namespace Unity.Flex.Reporting.DataGenerators.CustomFieldValueGenerators
{
    public class DataGridReportDataGenerator(CustomField customField, CustomFieldValue value)
       : ReportingDataGenerator(customField, value), IReportingDataGenerator
    {
        /// <summary>
        /// Generate the keys and values for a datagrid for reporting
        /// </summary>
        /// <returns>Dictionary of unique keys with any matching values for the keys</returns>
        public (Dictionary<string, List<string>> keyValuePairs, bool compressArray) Generate()
        {
            var values = new Dictionary<string, List<string>>();
            JObject dataValue = JObject.Parse(value.CurrentValue);

            var rowsValue = JsonSerializer.Deserialize<DataGridRowsValue>(dataValue["value"]?.ToString() ?? string.Empty);

            if (rowsValue == null) return (values, false);

            var definition = JsonSerializer.Deserialize<DataGridDefinition>(customField.Definition);

            if (definition == null) return (values, false);

            var dynamicDataGrid = definition.Dynamic;
            var dynamicKeyColumn = "DynamicColumns";
            var dynamicColumn = $"{customField.Key}-{dynamicKeyColumn}";

            foreach (var cells in rowsValue.Rows.Select(s => s.Cells))
            {
                foreach (var cell in cells)
                {
                    var fieldName = $"{customField.Key}-{cell.Key}";

                    if (values.TryGetValue(fieldName, out List<string>? cellValue))
                    {
                        cellValue.Add(cell.Value);
                    }
                    else
                    {
                        values.Add(fieldName, [cell.Value]);
                    }
                }
            }

            // We have some special handling for dynamic columns
            if (dynamicDataGrid)
            {
                return (CaterForDynamicColumns(values, definition.Columns, customField.Key, dynamicColumn), false);
            }

            return (values, false);
        }

        private static Dictionary<string, List<string>> CaterForDynamicColumns(Dictionary<string, List<string>> values,
            List<DataGridDefinitionColumn> dataGridDefinitionColumns,
            string fieldKey,
            string dynamicColumn)
        {
            var dynamicColumns = new Dictionary<string, List<string>>();
            var keysToRemove = new List<string>();

            foreach (var value in from value in values
                                  where IsDynamicColumn(value, fieldKey, dataGridDefinitionColumns)
                                  select value)
            {
                // Add to dynamic field and mark for removal
                dynamicColumns.Add(StripKey(fieldKey, value.Key), value.Value);
                keysToRemove.Add(value.Key);
            }

            // Remove the dynamic columns from the original values dictionary
            foreach (var key in keysToRemove)
            {
                values.Remove(key);
            }

            // Add the dynamic columns to the values dictionary
            values.Add(dynamicColumn,
                [JsonSerializer.Serialize(dynamicColumns)]);

            return values;
        }

        // Strip the key for dynamic column
        private static string StripKey(string fieldKey, string key)
        {
            return key.StartsWith($"{fieldKey}-") ? key[(fieldKey.Length + 1)..] : key;
        }

        // Check if the column is a dynamic column - we know this by checking against the defintion columns and matching
        private static bool IsDynamicColumn(KeyValuePair<string, List<string>> value,
            string fieldKey,
            List<DataGridDefinitionColumn> dataGridDefinitionColumns)
        {
            var keyCheck = value.Key.StartsWith($"{fieldKey}-") ? value.Key[(fieldKey.Length + 1)..] : value.Key;
            var columnDefinition = dataGridDefinitionColumns.Exists(column => column.Name == keyCheck);
            return !columnDefinition;
        }
    }
}
