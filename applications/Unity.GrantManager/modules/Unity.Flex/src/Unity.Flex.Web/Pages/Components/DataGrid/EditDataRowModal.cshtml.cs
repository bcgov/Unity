using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Unity.Flex.Web.Views.Shared.Components.WorksheetInstanceWidget.ViewModels;
using Unity.Flex.WorksheetInstances;
using Unity.Flex.Worksheets;
using Unity.Flex.Worksheets.Definitions;
using Unity.Flex.Worksheets.Values;
using System.Linq;

namespace Unity.Flex.Web.Pages.Flex;

public class EditDataRowModalModel(ICustomFieldValueAppService customFieldValueAppService,
    ICustomFieldAppService customFieldAppService) : FlexPageModel
{
    [BindProperty]
    public Guid ValueId { get; set; }

    [BindProperty]
    public uint Row { get; set; }

    [BindProperty]
    public List<WorksheetFieldViewModel>? Properties { get; set; }

    public async Task OnGetAsync(Guid valueId, uint row)
    {
        Row = row;
        ValueId = valueId;
        Properties = await GetEditableDataRowFieldsAsync(valueId, row);
    }

    private async Task<List<WorksheetFieldViewModel>> GetEditableDataRowFieldsAsync(Guid valueId, uint rowNumber)
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

        // We only edit additional columns / metadata
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

    public async Task<IActionResult> OnPostAsync()
    {
        // pattern matching as we are using the same worksheet components under the hood
        // X.X.00000000-0000-0000-0000-000000000000 - where X is the column name
        var keyValuePairs = GetKeyValuePairs(Request.Form);

        var currentValue = await customFieldValueAppService.GetAsync(ValueId);
        var calculatedDelta = CalculateDelta(keyValuePairs, currentValue, Row);

        if (calculatedDelta != null)
        {
            await customFieldValueAppService.ExplicitSetAsync(ValueId, calculatedDelta);
        }

        return new OkObjectResult(new ModalResponse()
        {
            ValueId = ValueId,
            Row = Row
        });
    }

    private static string? CalculateDelta(Dictionary<string, string> keyValuePairs, CustomFieldValueDto currentValue, uint row)
    {
        // Figure out what needs to be updated or added and then update accordingly
        var dataGridValue = JsonSerializer.Deserialize<DataGridValue>(currentValue.CurrentValue?.ToString() ?? "{}");
        if (dataGridValue == null) return null;

        var dataGridRowsValue = JsonSerializer.Deserialize<DataGridRowsValue>(dataGridValue.Value?.ToString() ?? "{}");

        if (dataGridRowsValue == null) return null;
        var rowToUpdate = dataGridRowsValue.Rows[(int)row];

        foreach (var key in keyValuePairs.Keys)
        {
            var cell = rowToUpdate.Cells.Find(s => s.Key == key);
            if (cell != null)
            {
                cell.Value = keyValuePairs[key];
            }
            else
            {
                rowToUpdate.Cells.Add(new DataGridRowCell { Key = key, Value = keyValuePairs[key] });
            }
        }

        dataGridValue.Value = dataGridRowsValue;
        return JsonSerializer.Serialize(dataGridValue);
    }

    private static Dictionary<string, string> GetKeyValuePairs(IFormCollection form)
    {
        var keyValuePairs = new Dictionary<string, string>();
        var pattern = @"^(?<prefix>.+?)\..*";
        foreach (var (prefix, value) in from string key in form.Keys
                                        where Regex.IsMatch(key, pattern)
                                        let match = Regex.Match(key, pattern)
                                        let prefix = match.Groups["prefix"].Value
                                        let value = form[key]
                                        select (prefix, value))
        {
            keyValuePairs[prefix] = value.ToString();
        }

        return keyValuePairs;
    }

    public class ModalResponse
    {
        public uint Row { get; set; }
        public Guid ValueId { get; set; }
    }
}
