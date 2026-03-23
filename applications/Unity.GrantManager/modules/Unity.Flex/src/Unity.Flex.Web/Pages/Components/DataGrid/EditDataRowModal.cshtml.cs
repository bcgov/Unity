using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Unity.Flex.Web.Views.Shared.Components.WorksheetInstanceWidget.ViewModels;
using Unity.Flex.Worksheets;
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
    public DynamicFieldMap[]? DynamicFields { get; set; }

    [BindProperty]
    public string? CheckboxKeys { get; set; }

    [BindProperty]
    public string? DynamicKeyMap { get; set; }

    public List<EditRowField> AllFields { get; private set; } = [];

    private const string DynamicFieldPrefix = "dynamicXdF-";

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
        DynamicFields = PrefixDynamicFields(dynamicFields ?? []);

        var customCheckboxKeys = Properties?.Where(s => s.Type == CustomFieldType.Checkbox).Select(s => s.Name) ?? [];
        var dynamicCheckboxKeys = DynamicFields?.Where(df => df.Type == CustomFieldType.Checkbox.ToString()).Select(df => df.Key[DynamicFieldPrefix.Length..]) ?? [];
        CheckboxKeys = string.Join(',', customCheckboxKeys.Concat(dynamicCheckboxKeys));

        DynamicKeyMap = JsonSerializer.Serialize(
            DynamicFields?.ToDictionary(
                df => df.Key[DynamicFieldPrefix.Length..],
                df => new DynamicKeyMapEntry(df.Name, df.Type)
            ) ?? new Dictionary<string, DynamicKeyMapEntry>());

        AllFields = MergeAndSortFields(DynamicFields ?? [], Properties ?? []);
    }

    private static DynamicFieldMap[] PrefixDynamicFields(DynamicFieldMap[] dynamicFieldMaps)
    {
        foreach (var map in dynamicFieldMaps)
        {
            map.Key = $"{DynamicFieldPrefix}{map.Key}";
        }

        return dynamicFieldMaps;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var keyValuePairs = StripDynamicFieldPrefix(GetKeyValuePairs(Request.Form));
        var presentationSettings = new PresentationSettings() { BrowserOffsetMinutes = browserUtils.GetBrowserOffset() };

        if (CheckboxKeys != null)
        {
            foreach (var key in CheckboxKeys.Split(','))
            {
                keyValuePairs[key] = keyValuePairs.TryGetValue(key, out var existing) && existing.IsTruthy()
                    ? "true"
                    : "false";
            }
        }

        var dynamicTypeMap = JsonSerializer.Deserialize<Dictionary<string, DynamicKeyMapEntry>>(DynamicKeyMap ?? "{}") ?? [];
        ConvertDateTimeValuesForStorage(keyValuePairs, dynamicTypeMap, presentationSettings.BrowserOffsetMinutes);

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
        var updates = DataGridReadService.ApplyPresentationFormat(keyValuePairs, result.MappedValues, presentationSettings);
        ApplyDynamicFieldPresentationFormat(updates, dynamicTypeMap, presentationSettings);
        var translatedUpdates = TranslateKeysToColumnNames(updates, dynamicTypeMap);

        return new OkObjectResult(new ModalResponse()
        {
            ValueId = result.ValueId,
            FieldId = FieldId,
            WorksheetInstanceId = result.WorksheetInstanceId,
            WorksheetId = result.WorksheetId,
            Row = result.Row,
            IsNew = result.IsNew,
            Updates = translatedUpdates,
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

        foreach (string key in form.Keys.Where(key =>
            key.StartsWith(DynamicFieldPrefix, StringComparison.Ordinal) && !keyValuePairs.ContainsKey(key)))
        {
            keyValuePairs[key] = form[key].ToString();
        }

        return keyValuePairs;
    }

    private static Dictionary<string, string> StripDynamicFieldPrefix(Dictionary<string, string> keyValuePairs)
        => keyValuePairs.ToDictionary(
            kvp => kvp.Key.StartsWith(DynamicFieldPrefix, StringComparison.Ordinal)
                ? kvp.Key[DynamicFieldPrefix.Length..]
                : kvp.Key,
            kvp => kvp.Value);

    private static Dictionary<string, string> TranslateKeysToColumnNames(
        Dictionary<string, string> updates,
        Dictionary<string, DynamicKeyMapEntry> keyToColumnMap)
    {
        return updates.ToDictionary(
            kvp => keyToColumnMap.TryGetValue(kvp.Key, out var entry) ? entry.Name : kvp.Key,
            kvp => kvp.Value);
    }

    private static void ConvertDateTimeValuesForStorage(
        Dictionary<string, string> keyValuePairs,
        Dictionary<string, DynamicKeyMapEntry> dynamicTypeMap,
        int browserOffsetMinutes)
    {
        var browserOffset = TimeSpan.FromMinutes(-browserOffsetMinutes);
        var dateTimeType = CustomFieldType.DateTime.ToString();

        foreach (var (key, _) in dynamicTypeMap.Where(e => e.Value.Type == dateTimeType))
        {
            if (keyValuePairs.TryGetValue(key, out var rawValue)
                && !string.IsNullOrEmpty(rawValue)
                && DateTime.TryParse(rawValue, CultureInfo.InvariantCulture, DateTimeStyles.None, out var localDateTime))
            {
                var dto = new DateTimeOffset(localDateTime, browserOffset);
                keyValuePairs[key] = dto.ToString("yyyy-MM-ddTHH:mm:sszzz", CultureInfo.InvariantCulture);
            }
        }
    }

    private static void ApplyDynamicFieldPresentationFormat(
        Dictionary<string, string> updates,
        Dictionary<string, DynamicKeyMapEntry> dynamicTypeMap,
        PresentationSettings presentationSettings)
    {
        foreach (var (key, entry) in dynamicTypeMap.Where(e =>
            updates.ContainsKey(e.Key) && !string.IsNullOrEmpty(updates[e.Key])))
        {
            updates[key] = updates[key].ApplyPresentationFormatting(entry.Type, null, presentationSettings);
        }
    }

    private sealed record DynamicKeyMapEntry(string Name, string Type);

    private static List<EditRowField> MergeAndSortFields(DynamicFieldMap[] dynamicFields, List<WorksheetFieldViewModel> customFields)
    {
        var fields = new List<EditRowField>();

        foreach (var df in dynamicFields)
        {
            fields.Add(new EditRowField
            {
                SortKey = df.Name,
                DynamicField = df
            });
        }

        foreach (var cf in customFields)
        {
            fields.Add(new EditRowField
            {
                SortKey = cf.Label,
                CustomField = cf
            });
        }

        return [.. fields.OrderBy(f => f.SortKey, StringComparer.OrdinalIgnoreCase)];
    }

    public class EditRowField
    {
        public string SortKey { get; set; } = string.Empty;
        public WorksheetFieldViewModel? CustomField { get; set; }
        public DynamicFieldMap? DynamicField { get; set; }
        public bool IsDynamic => DynamicField != null;
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

