using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Unity.Flex.Web.Views.Shared.Components.WorksheetInstanceWidget.ViewModels;
using System.Linq;
using Unity.Modules.Shared.Utils;

namespace Unity.Flex.Web.Pages.Flex;

public class EditDataRowModalModel(DataGridWriteService dataGridWriteService,
    DataGridReadService dataGridReadService,
    BrowserUtils browserUtils) : FlexPageModel
{
    [BindProperty]
    public Guid? ValueId { get; set; }

    [BindProperty]
    public uint Row { get; set; }

    [BindProperty]
    public Guid FieldId { get; set; }

    [BindProperty]
    public Guid WorksheetId { get; set; }

    [BindProperty]
    public Guid WorksheetInstanceId { get; set; }

    [BindProperty]
    public string? UiAnchor { get; set; }

    [BindProperty]
    public Guid FormVersionId { get; set; }

    [BindProperty]
    public Guid ApplicationId { get; set; }

    [BindProperty]
    public bool IsNew { get; set; }

    [BindProperty]
    public List<WorksheetFieldViewModel>? Properties { get; set; }

    [BindProperty]
    public KeyValuePair<string, string>[]? DynamicFields { get; set; }

    [BindProperty]
    public string? CheckboxKeys { get; set; }

    public async Task OnGetAsync(Guid valueId,
        Guid fieldId,
        uint row,
        bool isNew,
        Guid worksheetId,
        Guid worksheetInstanceId,
        Guid formVersionId,
        Guid applicationId,
        string uiAnchor)
    {
        Row = row;
        ValueId = valueId;
        FieldId = fieldId;
        WorksheetId = worksheetId;
        WorksheetInstanceId = worksheetInstanceId;
        FormVersionId = formVersionId;
        ApplicationId = applicationId;
        UiAnchor = uiAnchor;
        IsNew = isNew;

        var dataProps = new RowInputData()
        {
            FieldId = FieldId,
            ValueId = ValueId,
            Row = Row,
            IsNew = isNew,
            WorksheetId = worksheetId,
            WorksheetInstanceId = worksheetInstanceId,
            FormVersionId = formVersionId,
            ApplicationId = applicationId,
            UiAnchor = uiAnchor
        };

        PresentationSettings presentationSettings = new() { BrowserOffsetMinutes = browserUtils.GetBrowserOffset() };
        var (dynamicFields, customFields) = await dataGridReadService.GetPropertiesAsync(dataProps, presentationSettings);
        Properties = customFields;
        DynamicFields = dynamicFields ?? [];
        CheckboxKeys = string.Join(',', Properties?.Where(s => s.Type == Worksheets.CustomFieldType.Checkbox).Select(s => s.Name) ?? []);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var keyValuePairs = GetKeyValuePairs(Request.Form);
        var presentationSettings = new PresentationSettings() { BrowserOffsetMinutes = browserUtils.GetBrowserOffset() };

        if (CheckboxKeys != null)
        {
            var keysToCheck = CheckboxKeys.Split(',');
            foreach (var key in keysToCheck)
            {
                keyValuePairs.TryAdd(key, "false");
            }
        }

        var dataProps = new RowInputData()
        {
            FieldId = FieldId,
            ValueId = ValueId,
            Row = Row,
            WorksheetId = WorksheetId,
            WorksheetInstanceId = WorksheetInstanceId,
            KeyValuePairs = keyValuePairs,
            UiAnchor = UiAnchor ?? string.Empty,
            FormVersionId = FormVersionId,
            ApplicationId = ApplicationId,
            IsNew = IsNew
        };

        var result = await dataGridWriteService.WriteRowAsync(dataProps);

        return new OkObjectResult(new ModalResponse()
        {
            ValueId = result.ValueId,
            FieldId = FieldId,
            WorksheetInstanceId = result.WorksheetInstanceId,
            WorksheetId = result.WorksheetId,
            Row = result.Row,
            IsNew = result.IsNew,
            Updates = DataGridReadService.ApplyPresentationFormat(keyValuePairs, result.MappedValues, presentationSettings),
            UiAnchor = UiAnchor
        });
    }

    private static Dictionary<string, string> GetKeyValuePairs(IFormCollection form)
    {
        var keyValuePairs = new Dictionary<string, string>();
        var pattern = @"^(?<prefix>.+?)\..*";
        foreach (var (prefix, value) in from string key in form.Keys
                                        where Regex.IsMatch(key, pattern, RegexOptions.None, TimeSpan.FromSeconds(30))
                                        let match = Regex.Match(key, pattern, RegexOptions.None, TimeSpan.FromSeconds(30))
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
        public bool IsNew { get; set; }
        public Guid WorksheetInstanceId { get; set; }
        public Guid WorksheetId { get; set; }
        public Dictionary<string, string> Updates { get; set; } = [];
        public string? UiAnchor { get; set; }
    }
}

