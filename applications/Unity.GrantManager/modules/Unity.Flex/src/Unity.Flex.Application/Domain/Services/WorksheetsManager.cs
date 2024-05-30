using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Unity.Flex.Domain.WorksheetInstances;
using Unity.Flex.Domain.Worksheets;
using Unity.Flex.WorksheetInstances;
using Unity.Flex.Worksheets.Values;
using Unity.Modules.Shared.Correlation;
using Volo.Abp.Domain.Services;

namespace Unity.Flex.Domain.Services
{
    public class WorksheetsManager(IWorksheetInstanceRepository worksheetInstanceRepository, IWorksheetRepository worksheetRepository) : DomainService
    {
        public async Task PersistWorksheetData(PersistWorksheetIntanceValuesEto eventData)
        {
            if ((object?)eventData.CustomFields == null) { return; }

            string json = JsonSerializer.Serialize(eventData.CustomFields);
            var dictionary = JsonSerializer.Deserialize<Dictionary<string, string>>(json);

            if (dictionary == null || dictionary.Count == 0) return;
            var fields = BuildFields(dictionary);

            var worksheetInstance = await worksheetInstanceRepository.GetByCorrelationByAnchorAsync(eventData.CorrelationId, eventData.CorrelationProvider, eventData.UiAnchor, true);
            var worksheet = await worksheetRepository.GetByCorrelationByAnchorAsync(CurrentTenant.Id ?? Guid.Empty, CorrelationConsts.Tenant, eventData.UiAnchor, true);

            if (worksheetInstance == null)
            {
                if (worksheet == null) return;

                var newInstance = new WorksheetInstance(Guid.NewGuid(),
                      worksheet.Id,
                      eventData.CorrelationId,
                      eventData.CorrelationProvider,
                      eventData.UiAnchor);

                foreach (var field in fields)
                {
                    var customField = FindCustomFieldByName(worksheet, field.FieldName);
                    if (customField != null && field.Value != null)
                    {
                        newInstance.AddValue(customField.Id, customField.Definition ?? "{}", ValueConverter.Convert(field.Value, customField.Type));
                    }
                }

                await worksheetInstanceRepository.InsertAsync(newInstance);
            }
            else
            {
                foreach (var field in fields)
                {
                    var customField = FindCustomFieldByName(worksheet, field.FieldName);
                    var valueField = worksheetInstance.Values.FirstOrDefault(s => s.CustomFieldId == field.FieldId);
                    if (customField != null && field.Value != null && valueField != null)
                    {
                        valueField.SetValue(ValueConverter.Convert(field.Value, customField.Type));
                    }
                    else
                    {
                        // add the value to worksheet instance
                        if (worksheet != null)
                        {
                            var wsField = worksheet.Sections.SelectMany(s => s.Fields).FirstOrDefault(s => s.Name == field.FieldName);
                            if (wsField != null)
                            {
                                worksheetInstance.AddValue(wsField.Id, wsField.Definition ?? "{}", ValueConverter.Convert(field.Value ?? string.Empty, wsField.Type));
                            }
                        }
                    }
                }
            }
        }

        public async Task CreateWorksheetDataByFields(CreateWorksheetInstanceByFieldValuesEto eventData)
        {
            if (eventData.CustomFields.Count == 0) { return; }
            var worksheetNames = new List<string>();

            // naming convention custom_worksheetname_fieldname
            foreach (var field in eventData.CustomFields)
            {
                var split = field.Key.Split('_', StringSplitOptions.RemoveEmptyEntries);
                if (!worksheetNames.Contains(split[1]))
                {
                    worksheetNames.Add(split[1]);
                }
            }

            foreach (var worksheetName in worksheetNames)
            {
                var worksheet = await worksheetRepository.GetByCorrelationByNameAsync(CurrentTenant.Id ?? Guid.Empty, CorrelationConsts.Tenant, worksheetName, true);

                if (worksheet != null)
                {
                    var newInstance = new WorksheetInstance(Guid.NewGuid(),
                     worksheet.Id,
                     eventData.CorrelationId,
                     eventData.CorrelationProvider,
                     worksheet.UIAnchor);

                    var allFields = worksheet.Sections.SelectMany(s => s.Fields);

                    foreach (var field in allFields)
                    {
                        var match = eventData.CustomFields.Find(s => s.Key == field.Name);
                        newInstance.AddValue(field.Id, field.Definition ?? "{}", ValueConverter.Convert(match.Value?.ToString() ?? string.Empty, field.Type));
                    }

                    await worksheetInstanceRepository.InsertAsync(newInstance);
                }
            }
        }

        private static List<ValueFieldContainer> BuildFields(Dictionary<string, string> dictionary)
        {
            var fields = new List<ValueFieldContainer>();

            foreach (var field in dictionary)
            {
                // Field is broken down into {FieldName}.{UiAnchor}.{FieldId} and then value

                var split = field.Key.Split('.', StringSplitOptions.RemoveEmptyEntries);

                fields.Add(new ValueFieldContainer()
                {
                    FieldId = Guid.Parse(split[2]),
                    UiAnchor = split[1],
                    FieldName = split[0],
                    Value = field.Value
                });
            }

            return fields;
        }

        private static CustomField? FindCustomFieldByName(Worksheet? worksheet, string fieldName)
        {
            if (worksheet == null) return null;
            if (worksheet.Sections == null) return null;

            return worksheet.Sections.SelectMany(s => s.Fields).FirstOrDefault(s => s.Name == fieldName);
        }
    }

    public class ValueFieldContainer
    {
        public Guid FieldId { get; set; }
        public string UiAnchor { get; set; } = string.Empty;
        public string FieldName { get; set; } = string.Empty;
        public string? Value { get; set; }
    }
}
