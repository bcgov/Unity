using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Unity.Flex.Web.Views.Shared.Components.WorksheetInstanceWidget.ViewModels;
using Unity.Flex.WorksheetInstances;
using System.Linq;

namespace Unity.Flex.Web.Pages.Flex;

public class EditDataRowModalModel(ICustomFieldValueAppService customFieldValueAppService,
    DataGridService dataGridService) : FlexPageModel
{
    [BindProperty]
    public Guid ValueId { get; set; }

    [BindProperty]
    public uint Row { get; set; }

    [BindProperty]
    public Guid FieldId { get; set; }

    [BindProperty]
    public List<WorksheetFieldViewModel>? Properties { get; set; }

    public async Task OnGetAsync(Guid valueId, Guid fieldId, uint row)
    {
        Row = row;
        ValueId = valueId;
        FieldId = fieldId;

        Properties = await dataGridService.GetEditableDataRowFieldsAsync(valueId, row);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var keyValuePairs = GetKeyValuePairs(Request.Form);
        var keyValueTypes = await dataGridService.GenerateKeyValueTypesAsync(FieldId, keyValuePairs);
        var calculatedDelta = await dataGridService.CalculateDeltaAsync(ValueId, Row, keyValueTypes);

        if (calculatedDelta != null)
        {
            await customFieldValueAppService.ExplicitSetAsync(ValueId, calculatedDelta);
        }

        return new OkObjectResult(new ModalResponse()
        {
            ValueId = ValueId,
            FieldId = FieldId,
            Row = Row,
            Updates = dataGridService.ApplyPresentationFormat(keyValuePairs, keyValueTypes)
        });
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
        public Guid FieldId { get; set; }
        public Dictionary<string, string> Updates { get; set; } = [];
    }
}

