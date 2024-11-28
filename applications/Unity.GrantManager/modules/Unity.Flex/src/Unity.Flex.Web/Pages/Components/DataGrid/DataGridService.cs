using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Unity.Flex.Web.Views.Shared.Components.DataGridWidget;
using Unity.Flex.Web.Views.Shared.Components.WorksheetInstanceWidget.ViewModels;
using Unity.Flex.WorksheetInstances;
using Unity.Flex.Worksheets;
using Unity.Flex.Worksheets.Definitions;
using Unity.Flex.Worksheets.Values;
using Volo.Abp.Application.Services;

namespace Unity.Flex.Web.Pages.Flex
{
    public class DataGridService(ICustomFieldValueAppService customFieldValueAppService, ICustomFieldAppService customFieldAppService) : ApplicationService
    {
        public async Task<List<WorksheetFieldViewModel>> GetEditableDataRowFieldsAsync(Guid valueId, uint rowNumber)
        {
            var customFieldValue = await customFieldValueAppService.GetAsync(valueId);
            if (customFieldValue.CurrentValue == null) return [];

            var datagridDefinition = await customFieldAppService.GetAsync(customFieldValue.CustomFieldId);
            if (datagridDefinition == null) return [];

            var dataGridValue = JsonSerializer.Deserialize<DataGridValue>(customFieldValue.CurrentValue ?? "{}");

            return ConvertDataGridValue(dataGridValue, datagridDefinition, rowNumber);
        }

        private static List<WorksheetFieldViewModel> ConvertDataGridValue(DataGridValue? value, CustomFieldDto datagridDefinition, uint rowNumber)
        {
            var fieldsToEdit = new List<WorksheetFieldViewModel>();
            var dataGridRowsValue = JsonSerializer.Deserialize<DataGridRowsValue>(value?.Value?.ToString() ?? "{}");

            var definition = JsonSerializer.Deserialize<DataGridDefinition>(datagridDefinition?.Definition ?? "{}");

            if (definition == null) return [];

            var dataRow = dataGridRowsValue?.Rows[(int)rowNumber];

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
                    Type = (CustomFieldType)Enum.Parse(typeof(CustomFieldType), column.Type),
                    UiAnchor = string.Empty,
                    Order = rowNumber
                });
            }

            return fieldsToEdit;
        }

        private static string GetDefaultDefinition(string type)
        {
            return DefinitionResolver.Resolve((CustomFieldType)Enum.Parse(typeof(CustomFieldType), type), null);
        }

        private static string? GetCurrentValueAndTransform(DataGridRow? dataRow, DataGridDefinitionColumn column)
        {
            if (dataRow == null) return null;
            var cell = dataRow.Cells.Find(s => s.Key == column.Name);
            return ValueConverter.Convert(cell?.Value ?? string.Empty, CustomFieldType.Text);
        }

        public async Task<List<Tuple<string, string, CustomFieldType>>> GenerateKeyValueTypesAsync(Guid customFieldId, Dictionary<string, string>? keyValuePairs)
        {
            var result = new List<Tuple<string, string, CustomFieldType>>();
            var datagridDefinition = await customFieldAppService.GetAsync(customFieldId);
            var definition = JsonSerializer.Deserialize<DataGridDefinition>(datagridDefinition?.Definition ?? "{}");

            foreach (var keyValuePair in keyValuePairs ?? new Dictionary<string, string>())
            {
                result.Add(new Tuple<string, string, CustomFieldType>(keyValuePair.Key,
                    keyValuePair.Value,
                    ResolveTypeColumnName(keyValuePair.Key, definition)));
            }

            return result;
        }

        private static CustomFieldType ResolveTypeColumnName(string key, DataGridDefinition? definition)
        {
            if (definition == null) return CustomFieldType.Text;
            var columnMatch = definition.Columns.Find(s => s.Name == key);
            if (columnMatch == null) return CustomFieldType.Text;
            return Enum.Parse<CustomFieldType>(columnMatch.Type);
        }

        public async Task<string?> CalculateDeltaAsync(Guid valueId, uint row, List<Tuple<string, string, CustomFieldType>> keyValueTypes)
        {
            var currentValue = await customFieldValueAppService.GetAsync(valueId);
            var dataGridValue = DeserializeDataGridValue(currentValue.CurrentValue?.ToString());
            if (dataGridValue == null) return null;

            var dataGridRowsValue = DeserializeDataGridRowsValue(dataGridValue.Value?.ToString());
            if (dataGridRowsValue == null) return null;

            var rowToUpdate = dataGridRowsValue.Rows[(int)row];
            UpdateRowCells(keyValueTypes, rowToUpdate);

            dataGridValue.Value = dataGridRowsValue;
            return JsonSerializer.Serialize(dataGridValue);
        }

        private static DataGridValue? DeserializeDataGridValue(string? json)
        {
            return JsonSerializer.Deserialize<DataGridValue>(json ?? "{}");
        }

        private static DataGridRowsValue? DeserializeDataGridRowsValue(string? json)
        {
            return JsonSerializer.Deserialize<DataGridRowsValue>(json ?? "{}");
        }

        private static void UpdateRowCells(List<Tuple<string, string, CustomFieldType>> keyValueTypes, DataGridRow row)
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

        public Dictionary<string, string> ApplyPresentationFormat(
            Dictionary<string, string> keyValuePairs,
            List<Tuple<string, string, CustomFieldType>> keyValueTypes)
        {
            var formattedKeyValuePairs = new Dictionary<string, string>();

            foreach (var keyValue in keyValuePairs)
            {
                var key = keyValue.Key;
                var value = keyValue.Value;

                var typeTuple = keyValueTypes.Find(kvt => kvt.Item1 == key);
                if (typeTuple != null)
                {
                    var formattedValue = value.ApplyFormatting(typeTuple.Item3.ToString(), null);
                    formattedKeyValuePairs.Add(key, formattedValue);
                }
                else
                {
                    formattedKeyValuePairs.Add(key, value);
                }
            }

            return formattedKeyValuePairs;
        }
    }
}
