using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Unity.Flex.Domain.Worksheets;
using Unity.Flex.Worksheets.Values;

namespace Unity.Flex.Reporting.DataGenerators.CustomFieldValueGenerators
{
    public class DataGridReportDataGenerator(CustomField customField, FieldInstanceValue value)
       : ReportingDataGenerator(customField, value), IReportingDataGenerator
    {
        /// <summary>
        /// Generate the keys and values for a datagrid for reporting
        /// </summary>
        /// <returns>ictionary of unique keys with any matching values for the keys</returns>
        public Dictionary<string, List<string>> Generate()
        {
            var values = new Dictionary<string, List<string>>();
            var rowsValue = JsonSerializer.Deserialize<DataGridRowsValue>(value.Value);

            if (rowsValue == null) return values;

            foreach (var cells in rowsValue.Rows.Select(s => s.Cells))
            {
                foreach (var cell in cells)
                {
                    var fieldName = $"{customField.Key}-{cell.Key}";
                    if (values.TryGetValue(fieldName, out List<string>? value))
                    {
                        value.Add(cell.Value);
                    }
                    else
                    {
                        values.Add(fieldName, [cell.Value]);
                    }
                }
            }

            return values;
        }
    }
}
