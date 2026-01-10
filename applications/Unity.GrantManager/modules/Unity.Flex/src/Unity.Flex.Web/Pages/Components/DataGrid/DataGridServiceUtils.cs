using System;
using System.Collections.Generic;
using System.Text.Json;
using Unity.Flex.Web.Views.Shared.Components.WorksheetInstanceWidget.ViewModels;
using Unity.Flex.Worksheets;
using Unity.Flex.Worksheets.Definitions;
using Unity.Flex.Worksheets.Values;

namespace Unity.Flex.Web.Pages.Flex
{
    public static class DataGridServiceUtils
    {
        internal static List<WorksheetFieldViewModel> ExtractCustomColumnsValues(DataGridValue? value,
            CustomFieldDto datagridDefinition,
            uint rowNumber,
            bool isNew)
        {
            var fieldsToEdit = new List<WorksheetFieldViewModel>();
            var dataGridRowsValue = JsonSerializer.Deserialize<DataGridRowsValue>(value?.Value?.ToString() ?? "{}");

            var definition = JsonSerializer.Deserialize<DataGridDefinition>(datagridDefinition?.Definition ?? "{}");

            if (definition == null) return [];

            DataGridRow? dataRow = null;

            if (!isNew)
            {
                dataRow = (dataGridRowsValue != null && dataGridRowsValue.Rows.Count > rowNumber)
                   ? dataGridRowsValue.Rows[(int)rowNumber] : null;
            }

            foreach (var column in definition.Columns)
            {
                fieldsToEdit.Add(new WorksheetFieldViewModel()
                {
                    Name = column.Name,
                    Label = column.Name,
                    Id = Guid.Empty,
                    CurrentValue = GetCurrentValueAndTransform(dataRow, column),
                    CurrentValueId = Guid.Empty,
                    Definition = GetDefaultDefinition(column.Type),
                    Enabled = true,
                    Type = Enum.Parse<CustomFieldType>(column.Type),
                    UiAnchor = string.Empty,
                    Order = rowNumber
                });
            }

            return fieldsToEdit;
        }

        internal static string GetDefaultDefinition(string type)
        {
            return DefinitionResolver.Resolve(Enum.Parse<CustomFieldType>(type), null);
        }

        internal static string? GetCurrentValueAndTransform(DataGridRow? dataRow, DataGridDefinitionColumn column)
        {
            if (dataRow == null) return null;
            var cell = dataRow.Cells.Find(s => s.Key == column.Name);
            return ValueConverter.Convert(cell?.Value ?? string.Empty, CustomFieldType.Text);
        }

        internal static CustomFieldType ResolveTypeColumnName(string key, DataGridDefinition? definition)
        {
            if (definition == null) return CustomFieldType.Text;
            var columnMatch = definition.Columns.Find(s => s.Name == key);
            if (columnMatch == null) return CustomFieldType.Text;
            return Enum.Parse<CustomFieldType>(columnMatch.Type);
        }

        internal static DataGridValue? DeserializeDataGridValue(string? json)
        {
            return JsonSerializer.Deserialize<DataGridValue>(json ?? "{}");
        }

        internal static DataGridRowsValue? DeserializeDataGridRowsValue(string? json)
        {
            return JsonSerializer.Deserialize<DataGridRowsValue>(json ?? "{}");
        }

        internal static void UpdateRowCells(List<Tuple<string, string, CustomFieldType>> keyValueTypes, DataGridRow row)
        {
            foreach (var (key, value, type) in keyValueTypes)
            {
                var cell = row.Cells.Find(s => s.Key == key);
                if (cell != null)
                {
                    cell.Value = FormatValue(value, type);
                }
                else
                {
                    row.Cells.Add(new DataGridRowCell
                    {
                        Key = key,
                        Value = FormatValue(value, type)
                    });
                }
            }
        }

        private static string FormatValue(string value, CustomFieldType type)
        {
            return type switch
            {
                CustomFieldType.Currency => ValueConverterHelpers.ConvertDecimal(value),
                CustomFieldType.YesNo => ValueConverterHelpers.ConvertYesNo(value),
                CustomFieldType.Checkbox => ValueConverterHelpers.ConvertCheckbox(value),
                _ => value
            };
        }

        internal static List<DataGridRowCell> SetRowCells(List<Tuple<string, string, CustomFieldType>> keyValueTypes)
        {
            var cells = new List<DataGridRowCell>();
            foreach (var (key, value, type) in keyValueTypes)
            {
                cells.Add(new DataGridRowCell
                {
                    Key = key,
                    Value = FormatValue(value, type)
                });
            }
            return cells;
        }

        internal static KeyValuePair<string, string>[] ExtractDynamicColumnsPairs(DataGridValue? dataGridValue, 
            uint rowNumber, 
            PresentationSettings presentationSettings)
        {
            var keyValues = new List<KeyValuePair<string, string>>();
            var gridValue = DeserializeDataGridValue(dataGridValue?.Value?.ToString());
            if (gridValue == null) return [];
            var gridRowsValue = DeserializeDataGridRowsValue(dataGridValue?.Value?.ToString());
            if (gridRowsValue == null) return [];
            var row = gridRowsValue.Rows[(int)rowNumber];

            foreach (var column in dataGridValue?.Columns ?? [])
            {
                var cell = row.Cells.Find(s => s.Key == column.Key);

                if (cell != null)
                {
                    keyValues.Add(new(column.Name, cell.Value.ApplyPresentationFormatting(column.Type, null, presentationSettings)));
                }
            }

            return [.. keyValues];
        }
    }

    public class RowInputData
    {
        public Guid FieldId { get; set; }
        public Guid? ValueId { get; set; }
        public uint Row { get; set; }
        public bool IsNew { get; set; }
        public Guid WorksheetId { get; set; }
        public Guid WorksheetInstanceId { get; set; }
        public Dictionary<string, string>? KeyValuePairs { get; set; }
        public Guid FormVersionId { get; internal set; }
        public Guid ApplicationId { get; internal set; }
        public string UiAnchor { get; internal set; } = string.Empty;
    }

    public class WriteDataRowResponse
    {
        public bool IsNew { get; set; }
        public List<Tuple<string, string, CustomFieldType>> MappedValues { get; set; } = [];
        public Guid ValueId { get; set; }
        public Guid WorksheetInstanceId { get; set; }
        public Guid WorksheetId { get; set; }
        public uint Row { get; set; }
    }
}
