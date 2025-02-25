using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using Unity.Flex.Worksheets;
using Unity.Flex.Worksheets.Values;

namespace Unity.Flex
{
    internal static class DynamicDataBuilder
    {
        internal static DataGridValue BuildDataGrid(string currentValue, string? chefsFieldName, string? versionData)
        {
            if (versionData == null) return new DataGridValue();
            if (chefsFieldName == null) return new DataGridValue();

            var jObject = JObject.Parse(versionData);
            if (jObject["schema"] is not JObject schemaObject) return new DataGridValue();

            var result = FindComponents(schemaObject, chefsFieldName, "datagrid");

            var dataGridColumns = new List<DataGridColumn>();
            var dataGridRows = new List<DataGridRow>();
            var dateTimeCheck = CustomFieldType.DateTime.ToString();

            foreach (var component in result)
            {
                var key = component["key"]?.ToString() ?? string.Empty;
                var name = component["label"]?.ToString() ?? string.Empty;

                var type = ChefsToUnityTypes
                    .Convert(component["type"]?.ToString() ?? string.Empty,
                        CustomFieldType.Text.ToString());

                // Explicit handling of datetime property
                if (type == dateTimeCheck)
                {
                    var enableTime = component["enableTime"];
                    if (enableTime != null
                        && !enableTime.Value<bool>())
                    {
                        type = CustomFieldType.Date.ToString();
                    }
                }

                var format = ResolveFormatter(component);

                dataGridColumns.Add(new DataGridColumn(key, name, type, format));
            }

            var jsonArray = JArray.Parse(currentValue);

            foreach (var item in jsonArray)
            {
                var dataGridRow = new DataGridRow();

                var props = ((JObject)item).Properties();

                foreach (var prop in props)
                {
                    var key = prop.Name;
                    var column = dataGridColumns.Find(s => s.Key == key);

                    if (column != null)
                    {
                        string value;

                        if (column.Type.RequiresDateTimeConversion())
                        {
                            value = prop.Value.ApplyDateTimeConversion();
                        }
                        else
                        {
                            // Otherwise, use the value as-is
                            value = prop.Value.ToString();
                        }

                        dataGridRow.Cells.Add(new DataGridRowCell(key, value));
                    }
                }

                dataGridRows.Add(dataGridRow);
            }

            return new DataGridValue(new DataGridRowsValue(dataGridRows)) { Columns = dataGridColumns };
        }

        private static string? ResolveFormatter(JToken component)
        {
            // look for format, then currency as a possible formatter
            var format = component["format"]?.ToString() ?? string.Empty;
            if (format == string.Empty)
            {
                format = component["currency"]?.ToString() ?? string.Empty;
            }
            return format;
        }

        private static JArray FindComponents(JObject jObject, string key, string type)
        {
            JArray foundComponents = [];
            TraverseComponents(jObject, key, type, foundComponents);
            return foundComponents;
        }

        private static void TraverseComponents(JToken jToken, string key, string type, JArray foundComponents)
        {
            if (jToken is JObject obj)
            {
                foreach (var property in obj.Properties())
                {
                    if (property.Name == "components" && property.Value != null)
                    {
                        TraverseComponents(property.Value, key, type, foundComponents);
                    }
                    else if (property.Name == "key" && (property.Value?.ToString() ?? string.Empty) == key)
                    {
                        AddFoundComponents(type, foundComponents, obj);
                    }
                }
            }
            else if (jToken is JArray array)
            {
                foreach (var token in array)
                {
                    TraverseComponents(token, key, type, foundComponents);
                }
            }
        }

        private static void AddFoundComponents(string type, JArray foundComponents, JObject obj)
        {
            if (obj != null
                && obj["type"] != null
                && (obj["type"]?.ToString() ?? string.Empty) == type
                && obj["components"] is JArray childComponents)
            {
                foreach (var child in childComponents)
                {
                    foundComponents.Add(child);
                }
            }
        }
    }
}
